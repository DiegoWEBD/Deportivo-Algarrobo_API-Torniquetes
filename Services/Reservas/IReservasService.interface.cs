using API_Torniquetes.Models.Reserva;

namespace API_Torniquetes.Services.Reservas
{
    public interface IReservasService
    {
        Reserva[] ObtenerReservasActivas(DateTime? fecha);
        //bool RegistrarReservaDeClase(string rutUsuario, string ipTorniquete, DateTime inicioReserva, DateTime finReserva);
    }
}
