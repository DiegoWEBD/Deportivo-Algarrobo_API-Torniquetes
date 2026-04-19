using API_Torniquetes.Models.Reserva;
using API_Torniquetes.Models.Usuarios;

namespace API_Torniquetes.Repositories.Reservas
{
    public interface IReservaRepository
    {
        List<Reserva> ObtenerReservasActivas(DateTime fecha);
        int RegistrarUsuarioEnBD(string rutUsuario, string ipTorniquete, bool habilitado);
        Dictionary<string, List<UsuarioEstadoVencido>> ObtenerUsuariosConNuevoEstado();
        void CambiarEstadoUsuario(string idUsuario, string ipTorniquete, bool habilitado);
    }
}
