using API_Torniquetes.Models.Reserva;
using API_Torniquetes.Models.Usuarios;

namespace API_Torniquetes.Repositories.Reservas
{
    public interface IReservaRepository
    {
        Reserva Add(Reserva reserva);
        List<Reserva> ObtenerReservasActivas(DateTime fecha);
        int RegistrarUsuarioEnBD(string idUsuario, string ipTorniquete, bool habilitado);
        Dictionary<string, List<UsuarioEstadoVencido>> ObtenerUsuariosConNuevoEstado();
        void CambiarEstadoUsuario(string idUsuario, string ipTorniquete, bool habilitado);
    }
}
