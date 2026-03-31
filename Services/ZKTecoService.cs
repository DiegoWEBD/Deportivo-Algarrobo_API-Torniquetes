using API_Torniquetes.Models;
using API_Torniquetes.Models.Usuarios;
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
            const int grupoUsuariosHabilitados = 1;
            const int grupoUsuariosDeshabilitados = 2;
            int grupo = habilitar ? grupoUsuariosHabilitados : grupoUsuariosDeshabilitados;

            // Deshabilitar equipo temporalmente
            if (!zk.EnableDevice(1, false))
                return "No se pudo deshabilitar el equipo";

            // Cargar usuarios
            if (!zk.ReadAllUserID(1))
            {
                zk.EnableDevice(1, true);
                return "No se pudieron leer los usuarios";
            }

            zk.RefreshData(1);

            // Cambiar grupo
            bool resultado = zk.SetUserGroup(1, int.Parse(userId), grupo);

            if (!resultado)
            {
                int error = 0;
                zk.GetLastError(ref error);
                zk.EnableDevice(1, true);
                return $"Error al cambiar estado del usuario: {error}";
            }

            zk.RefreshData(1);
            zk.EnableDevice(1, true);

            return habilitar
                ? "Usuario habilitado correctamente"
                : "Usuario deshabilitado correctamente";
        }

        public string CambiarEstadoUsuarios(List<UsuarioEstadoVencido> usuarios)
        {
            const int grupoUsuariosHabilitados = 1;
            const int grupoUsuariosDeshabilitados = 2;

            if (!zk.EnableDevice(1, false))
                return "No se pudo deshabilitar el equipo";

            if (!zk.ReadAllUserID(1))
            {
                zk.EnableDevice(1, true);
                return "No se pudieron leer usuarios";
            }

            zk.RefreshData(1);

            foreach (var usuario in usuarios)
            {
                int grupo = usuario.nuevoEstadoHabilitado
                    ? grupoUsuariosHabilitados
                    : grupoUsuariosDeshabilitados;

                bool ok = zk.SetUserGroup(1, int.Parse(usuario.idUsuario), grupo);

                if (!ok)
                {
                    int error = 0;
                    zk.GetLastError(ref error);
                    Console.WriteLine($"Error usuario {usuario.idUsuario}: {error}");
                }

                Console.WriteLine($"{DateTime.Now}. Usuario {usuario.idUsuario} {(usuario.nuevoEstadoHabilitado ? "habilitado" : "deshabilitado")} en torniquete {usuario.ipTorniquete}.");
            }

            zk.RefreshData(1);
            zk.EnableDevice(1, true);

            return "Usuarios actualizados";
        }

        public UsuarioZKTeco? ObtenerUsuarioPorId(string userId)
        {
            if (!zk.ReadAllUserID(1))
                return null;

            zk.RefreshData(1);

            string name = string.Empty;
            string password = string.Empty;
            int privilege = 0;
            bool enabled = false;
            
            if (!zk.SSR_GetUserInfo(1, userId, out name, out password, out privilege, out enabled))
                return null;

            string timezone = "0";

            if (!zk.GetUserTZStr(1, int.Parse(userId), ref timezone))
                return null;

            return new UsuarioZKTeco
            {
                UserID = userId,
                Nombre = name.Trim('\0').Trim(),
                Password = password,
                Privilegio = privilege,
                Habilitado = enabled,
                Grupo = timezone
            };
        }

        public string ActualizarNombreUsuario(string userId, string nombre)
        {
            if (!zk.ReadAllUserID(1))
                return "No se pudieron leer los usuarios";

            zk.RefreshData(1);

            string password = null;
            int privilegio = 0;
            bool habilitado = true;

            bool resultado = zk.SSR_SetUserInfo(1, userId, nombre, password, privilegio, habilitado);

            if (!resultado)
            {
                int error = 0;
                zk.GetLastError(ref error);
                return $"Error al actualizar usuario: {error}";
            }

            zk.RefreshData(1);
            return "Usuario actualizado correctamente";
        }

        public string CopiarUsuarioConHuellas(string ipOrigen, string ipDestino, string userId)
        {
            int machine = 1;

            // =========================
            // ORIGEN
            // =========================
            if (Conectar(ipOrigen).StartsWith("Error"))
                return "No se pudo conectar al equipo origen";

            var usuario = ObtenerUsuarioPorId(userId);
            if (usuario == null)
            {
                Desconectar();
                return "Usuario no existe en equipo origen";
            }

            zk.EnableDevice(machine, false);

            // ⭐ ORDEN OBLIGATORIO F18
            zk.ReadAllUserID(machine);
            zk.ReadAllTemplate(machine);
            zk.RefreshData(machine);

            var huellas = new List<(int fingerIndex, int flag, string template)>();

            for (int fingerIndex = 0; fingerIndex <= 9; fingerIndex++)
            {
                string template = "";
                int flag = 0;
                int length = 0;

                bool existe = zk.GetUserTmpExStr(
                    machine,
                    userId,
                    fingerIndex,
                    out flag,
                    out template,
                    out length
                );

                if (existe && !string.IsNullOrEmpty(template))
                {
                    huellas.Add((fingerIndex, flag, template));
                }
            }

            zk.EnableDevice(machine, true);
            Desconectar();

            // =========================
            // DESTINO
            // =========================
            if (Conectar(ipDestino).StartsWith("Error"))
                return "No se pudo conectar al equipo destino";

            zk.EnableDevice(machine, false);

            // ⭐ modo batch evita errores internos
            zk.BeginBatchUpdate(machine, 1);

            bool creado = zk.SSR_SetUserInfo(
                machine,
                usuario.UserID,
                usuario.Nombre,
                usuario.Password ?? "",
                usuario.Privilegio < 0 ? 0 : usuario.Privilegio,
                usuario.Habilitado
            );

            if (!creado)
            {
                int error = 0;
                zk.GetLastError(ref error);

                zk.EnableDevice(machine, true);
                Desconectar();

                return $"Error creando usuario destino: {error}";
            }

            // =========================
            // COPIAR HUELLAS
            // =========================
            foreach (var huella in huellas)
            {
                bool ok = zk.SetUserTmpExStr(
                    machine,
                    usuario.UserID,
                    huella.fingerIndex,
                    huella.flag,      // ⭐ CRÍTICO
                    huella.template
                );

                if (!ok)
                {
                    int error = 0;
                    zk.GetLastError(ref error);

                    zk.BatchUpdate(machine);
                    zk.EnableDevice(machine, true);
                    Desconectar();

                    return $"Error copiando dedo {huella.fingerIndex}: {error}";
                }
            }

            // cerrar batch
            zk.BatchUpdate(machine);

            zk.RefreshData(machine);
            zk.EnableDevice(machine, true);
            Desconectar();

            return "Usuario y huellas copiadas correctamente";
        }

        public string ObtenerFirmware()
        {
            string version = string.Empty;

            if (!zk.GetFirmwareVersion(1, ref version))
            {
                int error = 0;
                zk.GetLastError(ref error);
                return $"Error obteniendo firmware: {error}";
            }

            return version;
        }

        public string ObtenerAlgoritmoBiometrico()
        {
            string version = string.Empty;

            if (!zk.GetSysOption(1, "ZKFPVersion", out version))
            {
                int error = 0;
                zk.GetLastError(ref error);
                return $"Error obteniendo algoritmo: {error}";
            }

            return version;
        }
    }
}
