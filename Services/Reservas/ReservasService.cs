using API_Torniquetes.Models.Reserva;
using API_Torniquetes.Repositories.Reservas;

namespace API_Torniquetes.Services.Reservas
{
    public class ReservasService(IReservaRepository reservaRepository) : IReservasService
    {
        public Reserva[] ObtenerReservasActivas(DateTime? fecha)
        {
            if (fecha == null)
            {
                fecha = DateTime.Now;
            }

            return reservaRepository.ObtenerReservasActivas((DateTime)fecha);
        }

        /*public bool RegistrarReservaDeClase(string rutUsuario, string ipTorniquete, DateTime inicioReserva, DateTime finReserva)
        {
            string[] arrayRutUsuario = rutUsuario.Split('-');

            if(arrayRutUsuario.Length != 2)
            {
                throw new Exception("Rut de usuario inválido");
            }

            string idUsuario = arrayRutUsuario[0];

            Reserva reserva = new()
            { 
                idUsuario = idUsuario,
                ipTorniquete = ipTorniquete,
                inicioReserva = inicioReserva,
                finReserva = finReserva
            };

            reservaRepository.Add(reserva);
            return true;
        }*/
    }
}
