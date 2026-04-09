using API_Torniquetes.Models;
using API_Torniquetes.Models.Usuarios;

namespace API_Torniquetes.Services
{
    public interface IZKTecoService
    {
        string Conectar(string ip, int puerto = 4370);
        void Desconectar();
        List<UsuarioZKTeco> ObtenerUsuarios();
        string CambiarEstadoUsuario(string userId, bool habilitar);
        string CambiarEstadoUsuarios(List<UsuarioEstadoVencido> usuarios);
        UsuarioZKTeco? ObtenerUsuarioPorId(string userId);
        string ActualizarNombreUsuario(string userId, string nombre);
        string CopiarUsuarioConHuellas(string ipOrigen, string ipDestino, string userId);
        string ObtenerFirmware();
        List<string> ObtenerHuellasUsuario(string userId);
        string ClonarDispositivo(string ipOrigen, string ipDestino);
    }
}
