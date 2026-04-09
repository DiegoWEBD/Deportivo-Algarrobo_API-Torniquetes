namespace API_Torniquetes.Models.Biometrics
{
    public class UsuarioBiometrico
    {
        public string UserID { get; set; }
        public string Nombre { get; set; }
        public string Password { get; set; }
        public int Privilegio { get; set; }
        public bool Habilitado { get; set; }

        public List<HuellaBiometrica> Huellas { get; set; } = new();
    }
}
