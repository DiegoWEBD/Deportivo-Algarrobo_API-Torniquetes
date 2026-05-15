using API_Torniquetes.Services.Reservas;

namespace API_Torniquetes.Services.Background
{
    public class MonitorBloqueoBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory scopeFactory;
        private readonly int delaySegundos = 20;
        private readonly int thresholdBloqueoMinutos = 1;

        public MonitorBloqueoBackgroundService(IServiceScopeFactory scopeFactory)
        {
            this.scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();

                    var reservasService = scope.ServiceProvider.GetRequiredService<IReservasService>();
                    var zktecoService = scope.ServiceProvider.GetRequiredService<IZKTecoService>();

                    reservasService.RegistrarLog("Obteniendo fecha último log", "", "INFO");
                    DateTime? ultimaFechaLog = reservasService.ObtenerFechaUltimoLog();

                    if (ultimaFechaLog.HasValue)
                    {
                        var diferencia = DateTime.Now - ultimaFechaLog.Value;

                        if (diferencia.TotalMinutes >= thresholdBloqueoMinutos)
                        {
                            reservasService.RegistrarLog($"Bloqueo detectado ({diferencia.TotalMinutes} minutos)", "", "BLOQUEO");
                            zktecoService.ReiniciarServicio();
                            reservasService.RegistrarLog("Servicio reiniciado", "", "REINICIO");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error monitor bloqueo: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromSeconds(delaySegundos), stoppingToken);
            }
        }
    }
}
