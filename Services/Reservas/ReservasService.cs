using API_Torniquetes.Models.Reserva;
using API_Torniquetes.Models.Usuarios;
using API_Torniquetes.Repositories.Reservas;

namespace API_Torniquetes.Services.Reservas
{
    public class ReservasService(IReservaRepository reservaRepository) : IReservasService
    {
        public List<Reserva> ObtenerReservasActivas(DateTime? fecha)
        {
            if (fecha == null)
            {
                fecha = DateTime.Now;
            }

            return reservaRepository.ObtenerReservasActivas((DateTime)fecha);
        }

        public int RegistrarUsuarioEnBD(string idUsuario, string ipTorniquete, bool habilitado)
        {
            return reservaRepository.RegistrarUsuarioEnBD(idUsuario, ipTorniquete, habilitado);
        }

        public Dictionary<string, List<UsuarioEstadoVencido>> ObtenerUsuariosConNuevoEstado()
        {
            return reservaRepository.ObtenerUsuariosConNuevoEstado();
        }

        public void CambiarEstadoUsuario(string idUsuario, string ipTorniquete, bool habilitado)
        {
            reservaRepository.CambiarEstadoUsuario(idUsuario, ipTorniquete, habilitado);
        }

        public HashSet<string> ObtenerIdUsuariosFaltantes(string ipOrigen, string ipDestino)
        {
            return reservaRepository.ObtenerIdUsuariosFaltantes(ipOrigen, ipDestino);
        }

        public void RegistrarLog(string log, string ipTorniquete, string codigo)
        {
            reservaRepository.RegistrarLog(log, ipTorniquete, codigo);
        }

        public DateTime? ObtenerFechaUltimoLog()
        {
            return reservaRepository.ObtenerFechaUltimoLog();
        }
    }
}
