using API_Torniquetes.Models;

namespace API_Torniquetes.Services
{
    public interface IZKTecoService
    {
        string Conectar(string ip, int puerto);
        void Desconectar();
        List<UsuarioZKTeco> ObtenerUsuarios();
        string CambiarEstadoUsuario(string userId, bool habilitar);
        string CrearTimeZone(
            int tzIndex,
            int sh1, int sm1, int eh1, int em1,
            int sh2, int sm2, int eh2, int em2,
            int sh3, int sm3, int eh3, int em3
        );
        List<object> ObtenerMarcajes();
    }
}
