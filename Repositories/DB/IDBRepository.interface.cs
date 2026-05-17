namespace API_Torniquetes.Repositories.DB
{
    public interface IDBRepository
    {
        int RegistrarUsuarioEnBD(string rutUsuario, string ipTorniquete, bool habilitado);
        void CambiarEstadoUsuario(string idUsuario, string ipTorniquete, bool habilitado);
        HashSet<string> ObtenerIdUsuariosFaltantes(string ipOrigen, string ipDestino);
        void RegistrarLog(string log, string ipTorniquete, string codigo);
    }
}
