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
        List<ResultadoCambioEstado> CambiarEstadoUsuarios(List<UsuarioEstadoVencido> usuarios);
        UsuarioZKTeco? ObtenerUsuarioPorId(string userId);
        string ActualizarNombreUsuario(string userId, string nombre);
        string CopiarUsuarioConHuellas(string rut, string[] ipsDestino);
        string CopiarUsuariosConHuellas(string ipOrigen, string ipDestino);
        string ObtenerFirmware();
        string ObtenerAlgoritmoBiometrico();
    }
}
