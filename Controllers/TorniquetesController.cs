using API_Torniquetes.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API_Torniquetes.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TorniquetesController : ControllerBase
    {
        IZKTecoService zKTecoService = new ZKTecoService();

        [HttpPost(Name = "Conectar")]
        public string Conectar(string ip, int puerto)
        {
            return zKTecoService.Conectar(ip, puerto);
        }
    }
}
