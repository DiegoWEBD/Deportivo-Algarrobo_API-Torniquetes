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
                Console.WriteLine($"{DateTime.Now}. Obteniendo estados vencidos.");
                var estadosVencidos = reservasService.ObtenerUsuariosConNuevoEstado();

                Console.WriteLine($"{DateTime.Now}. Actualizando estados vencidos.");
                foreach (var entry in estadosVencidos)
                {
                    string ipTorniquete = entry.Key;
                    zktecoService.Conectar(ipTorniquete);
                    zktecoService.CambiarEstadoUsuarios(entry.Value);

                    foreach(var usuario in entry.Value)
                    {
                        reservasService.CambiarEstadoUsuario(usuario.idUsuario, usuario.ipTorniquete, usuario.nuevoEstadoHabilitado);
                        Console.WriteLine($"{DateTime.Now}. Usuario {usuario.idUsuario} {(usuario.nuevoEstadoHabilitado ? "habilitado" : "deshabilitado")} en base de datos.");
                        ++actualizados;
                    }

                    zktecoService.Desconectar();
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
