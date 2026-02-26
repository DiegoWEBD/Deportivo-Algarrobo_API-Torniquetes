using API_Torniquetes.Models;

namespace API_Torniquetes.Services
{
    public interface IZKTecoService
    {
        string Conectar(string ip, int puerto);
        void Desconectar();
        List<UsuarioZKTeco> ObtenerUsuarios();
        string CambiarEstadoUsuario(string userId, bool habilitar);
        UsuarioZKTeco? ObtenerUsuarioPorId(string userId);
        string ActualizarNombreUsuario(string userId, string nombre);
        string CopiarUsuarioConHuellas(string ipOrigen, string ipDestino, string userId);
        string ObtenerFirmware();
        string ObtenerAlgoritmoBiometrico();
    }
}
