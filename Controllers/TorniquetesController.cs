using API_Torniquetes.Models;
using API_Torniquetes.Models.Reserva;
using API_Torniquetes.Services;
using API_Torniquetes.Services.Reservas;
using Microsoft.AspNetCore.Mvc;

namespace API_Torniquetes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TorniquetesController : ControllerBase
    {
        private readonly IServiceScopeFactory scopeFactory;
        private const int PUERTO = 4370;

        public TorniquetesController(IServiceScopeFactory scopeFactory)
        {
            this.scopeFactory = scopeFactory;
        }

        [HttpGet("verificar")]
        public ActionResult VerificarConexionAPI()
        {
            return Ok(new
            {
                message = "API conectada correctamente"
            });
        }

        [HttpGet(Name = "Conectar")]
        public ActionResult Conectar(string ip, int puerto = 4370)
        {
            using var scope = scopeFactory.CreateScope();
            var zKTecoService = scope.ServiceProvider.GetRequiredService<IZKTecoService>();

            string respuesta = zKTecoService.Conectar(ip, puerto);

            if (respuesta.Contains("Error"))
            {
                return NotFound(new
                {
                    conectado = false
                });
            }

            return Ok(new
            {
                conectado = true
            });
        }

        [HttpGet("usuarios")]
        public ActionResult<List<UsuarioZKTeco>> ObtenerUsuarios(string ip, int puerto = 4370)
        {
            using var scope = scopeFactory.CreateScope();
            var zKTecoService = scope.ServiceProvider.GetRequiredService<IZKTecoService>();

            var conexion = zKTecoService.Conectar(ip, puerto);

            if (!conexion.Contains("Conectado"))
                return BadRequest(conexion);

            var usuarios = zKTecoService.ObtenerUsuarios();

            zKTecoService.Desconectar();

            return Ok(usuarios);
        }

        [HttpGet("usuarios/nuevo-estado")]
        public ActionResult<List<UsuarioZKTeco>> ObtenerUsuariosConNuevoEstado()
        {
            using var scope = scopeFactory.CreateScope();
            var reservasService = scope.ServiceProvider.GetRequiredService<IReservasService>();

            var usuarios = reservasService.ObtenerUsuariosConNuevoEstado();

            return Ok(usuarios);
        }

        [HttpPost("usuarios/{userId}/estado")]
        public ActionResult CambiarEstadoUsuario(string ip, string userId, bool habilitar, int puerto = 4370)
        {
            using var scope = scopeFactory.CreateScope();
            var zktecoService = scope.ServiceProvider.GetRequiredService<IZKTecoService>();

            var conexion = zktecoService.Conectar(ip, puerto);

            if (!conexion.Contains("Conectado"))
                return BadRequest(conexion);

            var resultado = zktecoService.CambiarEstadoUsuario(userId, habilitar);

            zktecoService.Desconectar();

            return Ok(resultado);
        }

        [HttpGet("usuarios/{userId}")]
        public ActionResult<UsuarioZKTeco> ObtenerUsuarioPorId(string userId, string ip)
        {
            using var scope = scopeFactory.CreateScope();
            var zKTecoService = scope.ServiceProvider.GetRequiredService<IZKTecoService>();

            var conexion = zKTecoService.Conectar(ip, PUERTO);

            if (!conexion.Contains("Conectado"))
                return BadRequest(conexion);

            var usuario = zKTecoService.ObtenerUsuarioPorId(userId);

            zKTecoService.Desconectar();

            if (usuario == null)
                return NotFound("Usuario no encontrado");

            return Ok(usuario);
        }

        [HttpPut("usuarios/{userId}")]
        public ActionResult ActualizarNombreUsuario(string userId, string ip, string nombre, int puerto = 4370)
        {
            using var scope = scopeFactory.CreateScope();
            var zKTecoService = scope.ServiceProvider.GetRequiredService<IZKTecoService>();

            var conexion = zKTecoService.Conectar(ip, puerto);

            if (!conexion.Contains("Conectado"))
                return BadRequest(conexion);

            var resultado = zKTecoService.ActualizarNombreUsuario(userId, nombre);

            zKTecoService.Desconectar();

            if (resultado.Contains("Error"))
                return BadRequest(resultado);

            return Ok(resultado);
        }

        [HttpPost("usuarios/{userId}/sincronizar")]
        public ActionResult SincronizarUsuarioConHuellas(string ipOrigen, string ipDestino, string userId)
        {
            using var scope = scopeFactory.CreateScope();
            var zKTecoService = scope.ServiceProvider.GetRequiredService<IZKTecoService>();
            var reservasService = scope.ServiceProvider.GetRequiredService<IReservasService>();

            var resultado = zKTecoService.CopiarUsuarioConHuellas(ipOrigen, ipDestino, userId);

            if (resultado.Contains("Error"))
                return BadRequest(resultado);

            bool habilitadoPorDefecto = false;
            reservasService.RegistrarUsuarioEnBD(userId, ipDestino, habilitadoPorDefecto);

            return Ok(resultado);
        }

        [HttpGet("usuario/{userId}/huellas")]
        public ActionResult ObtenerHuellasUsuario(string ip, string userId)
        {
            using var scope = scopeFactory.CreateScope();
            var zKTecoService = scope.ServiceProvider.GetRequiredService<IZKTecoService>();

            var conexion = zKTecoService.Conectar(ip, PUERTO);

            if (!conexion.Contains("Conectado"))
                return BadRequest(conexion);

            var huellas = zKTecoService.ObtenerHuellasUsuario(userId);

            zKTecoService.Desconectar();

            if (huellas.Contains("Error"))
                return BadRequest(huellas);

            return Ok(new
            {
                ip,
                huellas
            });
        }

        [HttpGet("firmware")]
        public ActionResult ObtenerFirmware(string ip, int puerto = 4370)
        {
            using var scope = scopeFactory.CreateScope();
            var zKTecoService = scope.ServiceProvider.GetRequiredService<IZKTecoService>();

            var conexion = zKTecoService.Conectar(ip, puerto);

            if (!conexion.Contains("Conectado"))
                return BadRequest(conexion);

            var firmware = zKTecoService.ObtenerFirmware();

            zKTecoService.Desconectar();

            if (firmware.Contains("Error"))
                return BadRequest(firmware);

            return Ok(new
            {
                ip,
                firmware
            });
        }

        [HttpGet("sincronizar")]
        public ActionResult SincronizarUsuariosTorniqueteBD(string ip)
        {
            using var scope = scopeFactory.CreateScope();
            var zKTecoService = scope.ServiceProvider.GetRequiredService<IZKTecoService>();
            var reservasService = scope.ServiceProvider.GetRequiredService<IReservasService>();

            var conexion = zKTecoService.Conectar(ip, PUERTO);

            if (!conexion.Contains("Conectado"))
                return BadRequest(conexion);

            var usuarios = zKTecoService.ObtenerUsuarios();
            int sincronizados = 0;

            zKTecoService.Desconectar();

            foreach (var usuario in usuarios)
            {
                sincronizados += reservasService.RegistrarUsuarioEnBD(usuario.UserID, ip, true);
            }

            return Ok(new { 
                message = $"Usuarios sincronizados correctamente ({sincronizados}/{usuarios.Count})"
            });
        }

        [HttpGet("reservas")]
        public ActionResult<Reserva[]> ObtenerReservasActivas(DateTime? fecha)
        {
            using var scope = scopeFactory.CreateScope();
            var reservasService = scope.ServiceProvider.GetRequiredService<IReservasService>();

            List<Reserva> reservas = reservasService.ObtenerReservasActivas(fecha);

            return Ok(reservas);
        }

        [HttpPost("reservas")]
        public ActionResult RegistrarReservaDeClase([FromBody] RegistrarReservaRequest request)
        {
            using var scope = scopeFactory.CreateScope();
            var reservasService = scope.ServiceProvider.GetRequiredService<IReservasService>();

            try
            {
                reservasService.RegistrarReservaDeClase(request.id, request.rut_usuario, request.ip_torniquete, request.inicio_reserva, request.fin_reserva);
                return Created();
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    correcto = false,
                    mensaje = ex.Message
                });
            }
        }

        [HttpPost("clonar")]
        public ActionResult ClonarDispositivo(string ipOrigen, string ipDestino)
        {
            using var scope = scopeFactory.CreateScope();
            var zKTecoService = scope.ServiceProvider.GetRequiredService<IZKTecoService>();

            if (string.IsNullOrWhiteSpace(ipOrigen) ||
                string.IsNullOrWhiteSpace(ipDestino))
            {
                return BadRequest(new
                {
                    correcto = false,
                    mensaje = "Debe indicar ipOrigen e ipDestino"
                });
            }

            var resultado = zKTecoService.ClonarDispositivo(ipOrigen, ipDestino);

            if (resultado.Contains("Error") || resultado.Contains("No conecta"))
            {
                return BadRequest(new
                {
                    correcto = false,
                    mensaje = resultado
                });
            }

            return Ok(new
            {
                correcto = true,
                mensaje = resultado,
                origen = ipOrigen,
                destino = ipDestino
            });
        }

        [HttpPost("reiniciar")]
        public ActionResult ReiniciarDispositivo(string ip, int puerto = 4370)
        {
            using var scope = scopeFactory.CreateScope();
            var zktecoService = scope.ServiceProvider.GetRequiredService<IZKTecoService>();

            if (string.IsNullOrWhiteSpace(ip))
            {
                return BadRequest(new
                {
                    correcto = false,
                    mensaje = "Debe indicar la IP del dispositivo"
                });
            }

            var resultado = zktecoService.ReiniciarDispositivo(ip, puerto);

            if (resultado.Contains("Error") || resultado.Contains("No conecta"))
            {
                return BadRequest(new
                {
                    correcto = false,
                    mensaje = resultado,
                    ip
                });
            }

            return Ok(new
            {
                correcto = true,
                mensaje = resultado,
                ip
            });
        }
    }
}
