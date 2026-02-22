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
        IZKTecoService zKTecoService = new ZKTecoService();
        private readonly IReservasService reservasService;

        public TorniquetesController()
        {
            IReservaRepository reservaRepository = new ReservaRepository();
            this.reservasService = new ReservasService(reservaRepository);
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

        [HttpPost("timezone")]
        public ActionResult CrearTimeZone(
            string ip,
            int tzIndex,
            int sh1, int sm1, int eh1, int em1,
            int sh2, int sm2, int eh2, int em2,
            int sh3, int sm3, int eh3, int em3,
            int puerto = 4370
        )
        {
            var conexion = zKTecoService.Conectar(ip, puerto);

            if (!conexion.Contains("Conectado"))
                return BadRequest(conexion);

            var resultado = zKTecoService.CrearTimeZone(
                tzIndex,
                sh1, sm1, eh1, em1,
                sh2, sm2, eh2, em2,
                sh3, sm3, eh3, em3);

            zKTecoService.Desconectar();

            return Ok(resultado);
        }

        [HttpGet("marcajes")]
        public ActionResult ObtenerMarcajes(string ip, int puerto = 4370)
        {
            var conexion = zKTecoService.Conectar(ip, puerto);

            if (!conexion.Contains("Conectado"))
                return BadRequest(conexion);

            var marcajes = zKTecoService.ObtenerMarcajes();

            zKTecoService.Desconectar();

            return Ok(marcajes);
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
