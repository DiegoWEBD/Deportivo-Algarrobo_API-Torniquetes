using API_Torniquetes.Models;
using API_Torniquetes.Repositories.Reservas;
using API_Torniquetes.Services;
using API_Torniquetes.Services.Reservas;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace API_Torniquetes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TorniquetesController : ControllerBase
    {
        private readonly IZKTecoService zKTecoService;
        private readonly IReservasService reservasService;
        private const int PUERTO = 4370;
        private readonly Dictionary<string, string> IP_TORNIQUETES = new Dictionary<string, string>
        {
            { "ENROLADOR", "192.168.1.7" },
            { "GIMNASIO", "192.168.1.8" },
        };

        public TorniquetesController()
        {
            IReservaRepository reservaRepository = new ReservaRepository();
            this.reservasService = new ReservasService(reservaRepository);
            this.zKTecoService = new ZKTecoService();
        }

        [HttpPost(Name = "Conectar")]
        public string Conectar(string ip, int puerto = 4370)
        {
            return zKTecoService.Conectar(ip, puerto);
        }

        [HttpGet("usuarios")]
        public ActionResult<List<UsuarioZKTeco>> ObtenerUsuarios(string ip, int puerto = 4370)
        {
            var conexion = zKTecoService.Conectar(ip, puerto);

            if (!conexion.Contains("Conectado"))
                return BadRequest(conexion);

            var usuarios = zKTecoService.ObtenerUsuarios();

            zKTecoService.Desconectar();

            return Ok(usuarios);
        }

        [HttpPost("usuarios/estado")]
        public ActionResult CambiarEstadoUsuario(string ip, string userId, bool habilitar, int puerto = 4370)
        {
            var conexion = zKTecoService.Conectar(ip, puerto);

            if (!conexion.Contains("Conectado"))
                return BadRequest(conexion);

            var resultado = zKTecoService.CambiarEstadoUsuario(userId, habilitar);

            zKTecoService.Desconectar();

            return Ok(resultado);
        }

        [HttpGet("usuarios/{userId}")]
        public ActionResult<UsuarioZKTeco> ObtenerUsuarioPorId(string userId)
        {
            var conexion = zKTecoService.Conectar(IP_TORNIQUETES["ENROLADOR"], PUERTO);

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
            var conexion = zKTecoService.Conectar(ip, puerto);

            if (!conexion.Contains("Conectado"))
                return BadRequest(conexion);

            var resultado = zKTecoService.ActualizarNombreUsuario(userId, nombre);

            zKTecoService.Desconectar();

            if (resultado.Contains("Error"))
                return BadRequest(resultado);

            return Ok(resultado);
        }

        [HttpPost("reservas")]
        public ActionResult RegistrarReservaDeClase(string rut_usuario, string ip_torniquete, DateTime inicio_reserva, DateTime fin_reserva)
        {
            try
            {
                this.reservasService.RegistrarReservaDeClase(rut_usuario, ip_torniquete, inicio_reserva, fin_reserva);
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
    }
}
