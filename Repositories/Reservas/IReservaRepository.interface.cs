using API_Torniquetes.Models.Reserva;

namespace API_Torniquetes.Repositories.Reservas
{
    public interface IReservaRepository
    {
        Reserva Add(Reserva reserva);
    }
}
