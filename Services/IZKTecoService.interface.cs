namespace API_Torniquetes.Services
{
    public interface IZKTecoService
    {
        string Conectar(string ip, int puerto);
        void Desconectar();
    }
}
