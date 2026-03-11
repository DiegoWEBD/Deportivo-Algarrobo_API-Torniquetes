namespace API_Torniquetes.Models.Reserva
{
    public class RegistrarReservaRequest
    {
        public int id { get; set; }
        public string rut_usuario { get; set; }
        public string ip_torniquete { get; set; }
        public DateTime inicio_reserva { get; set; }
        public DateTime fin_reserva { get; set; }
    }
}
