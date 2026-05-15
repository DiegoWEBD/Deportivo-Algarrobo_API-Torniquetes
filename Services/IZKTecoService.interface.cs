using API_Torniquetes.Models;
using API_Torniquetes.Models.Usuarios;
using API_Torniquetes.Services.Reservas;

namespace API_Torniquetes.Services
{
    public interface IZKTecoService
    {
        string Conectar(string ip, int puerto = 4370);
        void Desconectar();
        List<UsuarioZKTeco> ObtenerUsuarios();
        string CambiarEstadoUsuario(string userId, bool habilitar, string ip);
        string CambiarEstadoUsuarios(List<UsuarioEstadoVencido> usuarios, string ip, IReservasService reservasService);
        UsuarioZKTeco? ObtenerUsuarioPorId(string userId);
        string ActualizarNombreUsuario(string userId, string nombre);
        string CopiarUsuarioConHuellas(string rut, string[] ipsDestino);
        string CopiarUsuariosConHuellas(string ipOrigen, string ipDestino);
        string ObtenerFirmware();
        string ObtenerAlgoritmoBiometrico();
        void ReiniciarServicio();
    }
}
