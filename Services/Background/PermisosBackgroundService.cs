using API_Torniquetes.Services.Reservas;
using System.Diagnostics;

namespace API_Torniquetes.Services.Background
{
    public class PermisosBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory scopeFactory;

        public PermisosBackgroundService(IServiceScopeFactory scopeFactory)
        {
            this.scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var intervalo = TimeSpan.FromSeconds(10);

            while (!stoppingToken.IsCancellationRequested)
            {
                var inicioEjecucion = DateTime.Now;

                await EjecutarProceso();

                // delay de 10 segundos
                var tiempoEjecutado = DateTime.Now - inicioEjecucion;
                var delay = intervalo - tiempoEjecutado;

                if (delay < TimeSpan.Zero)
                    delay = TimeSpan.Zero;

                await Task.Delay(delay, stoppingToken);
            }
        }

        private async Task EjecutarProceso()
        {
            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine($"{DateTime.Now}. Proceso iniciado.");

            using var scope = scopeFactory.CreateScope();

            var reservasService = scope.ServiceProvider.GetRequiredService<IReservasService>();
            var zktecoService = scope.ServiceProvider.GetRequiredService<IZKTecoService>();
            int actualizados = 0;

            try
            {
                reservasService.RegistrarLog("Obteniendo usuarios con estados vencidos", "", "INFO");
                var estadosVencidos = reservasService.ObtenerUsuariosConNuevoEstado();

                foreach (var entry in estadosVencidos)
                {
                    string ipTorniquete = entry.Key;
                    int total = entry.Value.Count;

                    reservasService.RegistrarLog($"Comenzando actualización de estado de {total} usuarios", ipTorniquete, "INFO");
                    zktecoService.CambiarEstadoUsuarios(entry.Value, ipTorniquete, reservasService);
                    reservasService.RegistrarLog($"Proceso finalizado. {total} usuario habilitados/deshabilitados", ipTorniquete, "INFO");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            stopwatch.Stop();

            Console.WriteLine(
                $"{DateTime.Now}. Proceso finalizado ({actualizados} estados actualizados). " +
                $"Tiempo total: {stopwatch.Elapsed.TotalSeconds:F2} segundos");

            await Task.CompletedTask;
        }
    }
}
