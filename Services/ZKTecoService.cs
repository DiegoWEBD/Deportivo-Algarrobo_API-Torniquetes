using zkemkeeper;

namespace API_Torniquetes.Services
{
    public class ZKTecoService : IZKTecoService
    {
        private IZKEM zk = new CZKEMClass();

        public string Conectar(string ip, int puerto = 4370)
        {
            zk.Disconnect();

            bool conectado = zk.Connect_Net(ip, puerto);

            if (!conectado)
            {
                int error = 0;
                zk.GetLastError(ref error);
                return $"Error ZKTeco: {error}";
            }

            return "Conectado correctamente";
        }


        public void Desconectar()
        {
            zk.Disconnect();
        }
    }
}
