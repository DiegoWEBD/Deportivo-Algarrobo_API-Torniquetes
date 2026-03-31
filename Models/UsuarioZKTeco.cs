namespace API_Torniquetes.Models
{
    public class UsuarioZKTeco
    {
        public string UserID { get; set; }
        public string Nombre { get; set; }
        public string Password { get; set; }
        public int Privilegio { get; set; }
        public bool Habilitado { get; set; }
    }

}
