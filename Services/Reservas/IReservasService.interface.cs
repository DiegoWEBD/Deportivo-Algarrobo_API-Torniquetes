namespace API_Torniquetes.Services.Reservas
{
    public interface IReservasService
    {
        bool RegistrarReservaDeClase(string rutUsuario, string ipTorniquete, DateTime inicioReserva, DateTime finReserva);
    }
}
