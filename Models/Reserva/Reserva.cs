namespace API_Torniquetes.Models.Reserva
{
    public class Reserva
    {
        public int id { get; set; }
        public string idUsuario { get; set; }
        public string ipTorniquete { get; set; }
        public DateTime inicioReserva { get; set; }
        public DateTime finReserva { get; set; }
    }
}
