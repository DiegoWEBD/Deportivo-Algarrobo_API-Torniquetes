using API_Torniquetes.Models.Reserva;
using API_Torniquetes.Models.Usuarios;

namespace API_Torniquetes.Services.Reservas
{
    public interface IReservasService
    {
        List<Reserva> ObtenerReservasActivas(DateTime? fecha);
        bool RegistrarReservaDeClase(int id, string rutUsuario, string ipTorniquete, DateTime inicioReserva, DateTime finReserva);
        int RegistrarUsuarioEnBD(string idUsuario, string ipTorniquete, bool habilitado);
        Dictionary<string, List<UsuarioEstadoVencido>> ObtenerUsuariosConNuevoEstado();
        void CambiarEstadoUsuario(string idUsuario, string ipTorniquete, bool habilitado);
    }
}
