using API_Torniquetes.Models;
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

        public List<UsuarioZKTeco> ObtenerUsuarios()
        {
            var listaUsuarios = new List<UsuarioZKTeco>();

            if (!zk.ReadAllUserID(1))
                return listaUsuarios;

            zk.RefreshData(1);

            string userID = string.Empty;
            string name = string.Empty;
            string password = string.Empty;
            int privilege = 0;
            bool enabled = false;

            while (zk.SSR_GetAllUserInfo(1, out userID, out name, out password, out privilege, out enabled))
            {
                listaUsuarios.Add(new UsuarioZKTeco
                {
                    UserID = userID,
                    Nombre = name,
                    Password = password,
                    Privilegio = privilege,
                    Habilitado = enabled
                });
            }

            return listaUsuarios;
        }

        public void Desconectar()
        {
            zk.Disconnect();
        }

        public string CambiarEstadoUsuario(string userId, bool habilitar)
        {
            if (!zk.ReadAllUserID(1))
                return "No se pudieron leer los usuarios";

            zk.RefreshData(1);

            string name = string.Empty;
            string password = string.Empty;
            int privilege = 0;
            bool enabled = false;

            if (!zk.SSR_GetUserInfo(1, userId, out name, out password, out privilege, out enabled))
                return "Usuario no encontrado";

            bool resultado = zk.SSR_SetUserInfo(1, userId, name, password, privilege, habilitar);

            if (!resultado)
            {
                int error = 0;
                zk.GetLastError(ref error);
                return $"Error al actualizar usuario: {error}";
            }

            zk.RefreshData(1);
            return habilitar ? "Usuario habilitado correctamente" : "Usuario deshabilitado correctamente";
        }

        public string CrearTimeZone(
            int tzIndex,
            int sh1, int sm1, int eh1, int em1,
            int sh2, int sm2, int eh2, int em2,
            int sh3, int sm3, int eh3, int em3)
        {
            string bloque1 = $"{sh1:D2}{sm1:D2}-{eh1:D2}{em1:D2}";
            string bloque2 = $"{sh2:D2}{sm2:D2}-{eh2:D2}{em2:D2}";
            string bloque3 = $"{sh3:D2}{sm3:D2}-{eh3:D2}{em3:D2}";

            string tzString = $"{bloque1}";

            bool resultado = zk.SetTZInfo(1, tzIndex, tzString);

            if (!resultado)
            {
                int error = 0;
                zk.GetLastError(ref error);
                return $"Error al crear TimeZone: {error}";
            }

            return "TimeZone creado correctamente";
        }

        public List<object> ObtenerMarcajes()
{
    var lista = new List<object>();

    if (!zk.ReadGeneralLogData(1))
        return lista;

    int userId = 0;
    int verifyMode = 0;
    int inOutMode = 0;
    int year = 0, month = 0, day = 0;
    int hour = 0, minute = 0, second = 0;
    int workCode = 0;

    while (zk.GetGeneralLogData(
        1,
        ref userId,
        ref verifyMode,
        ref inOutMode,
        ref year,
        ref month,
        ref day,
        ref hour,
        ref minute,
        ref second,
        ref workCode))
    {
        if (year < 2000 || year > 2100)
            continue;

        if (month < 1 || month > 12)
            continue;

        if (day < 1 || day > 31)
            continue;

        if (hour < 0 || hour > 23)
            continue;

        if (minute < 0 || minute > 59)
            continue;

        if (second < 0 || second > 59)
            continue;

        string fechaStr = $"{year}-{month}-{day} {hour}:{minute}:{second}";

        lista.Add(new
        {
            UserID = userId.ToString(),
            FechaHora = fechaStr,
            TipoVerificacion = verifyMode,
            TipoEntradaSalida = inOutMode,
            WorkCode = workCode
        });
    }

    return lista.OrderBy(x => ((dynamic)x).FechaHora).ToList();
}
    }
}
