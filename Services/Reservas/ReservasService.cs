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

        public bool RegistrarReservaDeClase(int id, string rutUsuario, string ipTorniquete, DateTime inicioReserva, DateTime finReserva)
        {
            string[] arrayRutUsuario = rutUsuario.Split('-');

            if(arrayRutUsuario.Length != 2)
            {
                throw new Exception("Rut de usuario inválido");
            }

            string idUsuario = arrayRutUsuario[0];

            Reserva reserva = new()
            { 
                id = id,
                idUsuario = idUsuario,
                ipTorniquete = ipTorniquete,
                inicioReserva = inicioReserva,
                finReserva = finReserva
            };

            reservaRepository.Add(reserva);
            return true;
        }

        public Dictionary<string, List<UsuarioEstadoVencido>> ObtenerUsuariosConNuevoEstado()
        {
            return reservaRepository.ObtenerUsuariosConNuevoEstado();
        }

        public void CambiarEstadoUsuario(string idUsuario, string ipTorniquete, bool habilitado)
        {
            reservaRepository.CambiarEstadoUsuario(idUsuario, ipTorniquete, habilitado);
        }
    }
}
