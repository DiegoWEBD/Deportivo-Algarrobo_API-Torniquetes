using API_Torniquetes.Models;
using API_Torniquetes.Models.Http;
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

        [HttpPost("usuarios/estado")]
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

        [HttpPost("usuarios/{rut}/sincronizar-torniquetes")]
        public ActionResult CopiarUsuarioConHuellas(string rut, [FromBody] SincronizarUsuarioRequest request)
        {
            using var scope = scopeFactory.CreateScope();
            var zKTecoService = scope.ServiceProvider.GetRequiredService<IZKTecoService>();

            var resultado = zKTecoService.CopiarUsuarioConHuellas(rut, request.ips_destino);

            if (resultado.Contains("Error"))
                return BadRequest(resultado);

            return Ok(resultado);
        }

        [HttpPost("usuarios/sincronizar-torniquetes")]
        public ActionResult CopiarUsuarioConHuellas(string ipOrigen, string ipDestino)
        {
            using var scope = scopeFactory.CreateScope();
            var zKTecoService = scope.ServiceProvider.GetRequiredService<IZKTecoService>();

            var resultado = zKTecoService.CopiarUsuariosConHuellas(ipOrigen, ipDestino);

            if (resultado.Contains("Error"))
                return BadRequest(resultado);

            return Ok(resultado);
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

        [HttpGet("algoritmo")]
        public ActionResult ObtenerAlgoritmo(string ip, int puerto = 4370)
        {
            using var scope = scopeFactory.CreateScope();
            var zKTecoService = scope.ServiceProvider.GetRequiredService<IZKTecoService>();

            var conexion = zKTecoService.Conectar(ip, puerto);

            if (!conexion.Contains("Conectado"))
                return BadRequest(conexion);

            var algoritmo = zKTecoService.ObtenerAlgoritmoBiometrico();

            zKTecoService.Desconectar();

            if (algoritmo.Contains("Error"))
                return BadRequest(algoritmo);

            return Ok(new
            {
                ip,
                algoritmo
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
    }
}
