using API_Torniquetes.Models.Http;
using API_Torniquetes.Repositories.DB;
using API_Torniquetes.Services;
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

            zKTecoService.Desconectar();

            return Ok(new
            {
                conectado = true
            });
        }

        [HttpPost("usuarios/estado")]
        public ActionResult CambiarEstadoUsuario(string ip, string userId, bool habilitar, int puerto = 4370)
        {
            using var scope = scopeFactory.CreateScope();
            var zktecoService = scope.ServiceProvider.GetRequiredService<IZKTecoService>();

            var resultado = zktecoService.CambiarEstadoUsuario(userId, habilitar, ip);

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

        [HttpGet("usuarios/sincronizar-torniquetes")]
        public ActionResult CopiarUsuarioConHuellas(string ipOrigen, string ipDestino)
        {
            using var scope = scopeFactory.CreateScope();
            var zKTecoService = scope.ServiceProvider.GetRequiredService<IZKTecoService>();

            var resultado = zKTecoService.CopiarUsuariosConHuellas(ipOrigen, ipDestino);

            if (resultado.Contains("Error"))
                return BadRequest(resultado);

            return Ok(resultado);
        }

        [HttpGet("sincronizar")]
        public ActionResult SincronizarUsuariosTorniqueteBD(string ip)
        {
            using var scope = scopeFactory.CreateScope();
            var zKTecoService = scope.ServiceProvider.GetRequiredService<IZKTecoService>();
            var dbRepository = scope.ServiceProvider.GetRequiredService<IDBRepository>();

            var conexion = zKTecoService.Conectar(ip, PUERTO);

            if (!conexion.Contains("Conectado"))
                return BadRequest(conexion);

            var usuarios = zKTecoService.ObtenerUsuarios();
            int sincronizados = 0;

            zKTecoService.Desconectar();

            foreach (var usuario in usuarios)
            {
                sincronizados += dbRepository.RegistrarUsuarioEnBD(usuario.UserID, ip, true);
            }

            return Ok(new { 
                message = $"Usuarios sincronizados correctamente ({sincronizados}/{usuarios.Count})"
            });
        }
    }
}
