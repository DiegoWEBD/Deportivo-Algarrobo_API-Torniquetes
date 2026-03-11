namespace API_Torniquetes.Models.Usuarios
{
    public class UsuarioEstadoVencido
    {
        public string idUsuario { get; set; }
        public string ipTorniquete { get; set; }
        public bool estadoHabilitadoActual { get; set; }
        public bool nuevoEstadoHabilitado { get; set; }
    }
}
