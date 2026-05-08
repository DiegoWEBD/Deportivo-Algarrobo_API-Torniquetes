namespace API_Torniquetes.Models.Usuarios
{
    public class UsuarioConHuellas
    {
        public string UserId { get; set; }
        public string Nombre { get; set; }
        public string Password { get; set; }
        public int Privilegio { get; set; }
        public bool Habilitado { get; set; }
        public List<Huella> Huellas { get; set; }
    }
}
