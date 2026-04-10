using API_Torniquetes.Models;
using API_Torniquetes.Models.Biometrics;
using API_Torniquetes.Models.Usuarios;
using zkemkeeper;
using System.Collections.Concurrent;

namespace API_Torniquetes.Services
{
    public class ZKTecoService : IZKTecoService
    {
        private static int ID_GRUPO_HABILITADOS = 1;
        private static int ID_GRUPO_DESHABILITADOS = 2;

        // 🔒 Locks por IP
        private static readonly ConcurrentDictionary<string, object> _locks = new();

        private object ObtenerLock(string ip)
        {
            return _locks.GetOrAdd(ip, _ => new object());
        }

        // ⚠️ Contexto actual (para mantener compatibilidad con tu interfaz)
        private string _ipActual;
        private int _puertoActual;

        // =========================
        // 🔌 CONEXIÓN
        // =========================

        public string Conectar(string ip, int puerto = 4370)
        {
            var zk = new CZKEMClass();

            try
            {
                zk.Disconnect();

                bool conectado = ConectarConTimeout(zk, ip, puerto);

                if (!conectado)
                    return "Error: No se pudo conectar al dispositivo";

                // guardar contexto SOLO si conecta
                _ipActual = ip;
                _puertoActual = puerto;

                return "Conectado correctamente";
            }
            catch
            {
                try { zk.Disconnect(); } catch { }
                return "Error: excepción al conectar";
            }
        }

        public void Desconectar()
        {
            // Ya no se usa (mantenido por interfaz)
        }

        // =========================
        // 🧠 CORE SEGURO
        // =========================

        private T EjecutarSeguro<T>(string ip, int puerto, Func<CZKEMClass, T> accion)
        {
            lock (ObtenerLock(ip))
            {
                // 🔥 INTENTO DE DESBLOQUEO PREVIO
                ForzarResetDispositivo(ip, puerto);

                var zk = new CZKEMClass();

                try
                {
                    zk.Disconnect();

                    bool conectado = ConectarConTimeout(zk, ip, puerto);

                    if (!conectado)
                        throw new Exception("No conecta al dispositivo");

                    return accion(zk);
                }
                finally
                {
                    try
                    {
                        zk.EnableDevice(1, true); // 🔥 CRÍTICO
                    }
                    catch { }

                    try { zk.Disconnect(); } catch { }
                }
            }
        }

        private bool ConectarConTimeout(CZKEMClass zk, string ip, int puerto, int timeoutMs = 3000)
        {
            var task = Task.Run(() => zk.Connect_Net(ip, puerto));

            if (task.Wait(timeoutMs))
                return task.Result;

            try { zk.Disconnect(); } catch { }
            return false;
        }

        private void ForzarResetDispositivo(string ip, int puerto = 4370)
        {
            var zk = new CZKEMClass();

            try
            {
                zk.Disconnect();

                bool ok = ConectarConTimeout(zk, ip, puerto);

                if (!ok) return;

                // 🔓 Reactivar equipo SIEMPRE
                zk.EnableDevice(1, true);

                // 🔄 limpiar buffers internos
                zk.RefreshData(1);

                // 🧹 intentar leer algo básico para liberar estado
                zk.ReadAllUserID(1);

                zk.EnableDevice(1, true);
            }
            catch
            {
                // ignorar
            }
            finally
            {
                try { zk.Disconnect(); } catch { }
            }
        }

        // =========================
        // 👥 USUARIOS
        // =========================

        public List<UsuarioZKTeco> ObtenerUsuarios()
        {
            return EjecutarSeguro(_ipActual, _puertoActual, zk =>
            {
                var lista = new List<UsuarioZKTeco>();

                if (!zk.ReadAllUserID(1))
                    return lista;

                zk.RefreshData(1);

                string userID, name, password;
                int privilege;
                bool enabled;

                while (zk.SSR_GetAllUserInfo(1, out userID, out name, out password, out privilege, out enabled))
                {
                    lista.Add(new UsuarioZKTeco
                    {
                        UserID = userID,
                        Nombre = name,
                        Password = password,
                        Privilegio = privilege,
                        Habilitado = enabled
                    });
                }

                return lista;
            });
        }

        public UsuarioZKTeco? ObtenerUsuarioPorId(string userId)
        {
            return EjecutarSeguro(_ipActual, _puertoActual, zk =>
            {
                if (!zk.ReadAllUserID(1))
                    return null;

                zk.RefreshData(1);

                string name, password;
                int privilege;
                bool enabled;

                if (!zk.SSR_GetUserInfo(1, userId, out name, out password, out privilege, out enabled))
                    return null;

                string grupo = "0";
                zk.GetUserTZStr(1, int.Parse(userId), ref grupo);

                return new UsuarioZKTeco
                {
                    UserID = userId,
                    Nombre = name.Trim('\0').Trim(),
                    Password = password,
                    Privilegio = privilege,
                    Habilitado = enabled,
                    Grupo = grupo
                };
            });
        }

        public string ActualizarNombreUsuario(string userId, string nombre)
        {
            return EjecutarSeguro(_ipActual, _puertoActual, zk =>
            {
                try
                {
                    zk.EnableDevice(1, false);

                    zk.ReadAllUserID(1);
                    zk.RefreshData(1);

                    bool ok = zk.SSR_SetUserInfo(1, userId, nombre, "", 0, true);

                    if (!ok)
                    {
                        int error = 0;
                        zk.GetLastError(ref error);
                        return $"Error: {error}";
                    }

                    return "Usuario actualizado correctamente";
                }
                finally
                {
                    try { zk.EnableDevice(1, true); } catch { }
                }
            });
        }

        // =========================
        // 🔄 ESTADOS
        // =========================

        public string CambiarEstadoUsuario(string userId, bool habilitar)
        {
            return EjecutarSeguro(_ipActual, _puertoActual, zk =>
            {
                int grupo = habilitar ? ID_GRUPO_HABILITADOS : ID_GRUPO_DESHABILITADOS;

                zk.EnableDevice(1, false);

                zk.ReadAllUserID(1);
                zk.RefreshData(1);

                bool ok = zk.SetUserGroup(1, int.Parse(userId), grupo);

                zk.EnableDevice(1, true);

                if (!ok)
                {
                    int error = 0;
                    zk.GetLastError(ref error);
                    return $"Error: {error}";
                }

                return habilitar ? "Usuario habilitado" : "Usuario deshabilitado";
            });
        }

        public string CambiarEstadoUsuarios(List<UsuarioEstadoVencido> usuarios)
        {
            return EjecutarSeguro(_ipActual, _puertoActual, zk =>
            {
                zk.EnableDevice(1, false);

                zk.ReadAllUserID(1);
                zk.RefreshData(1);

                foreach (var u in usuarios)
                {
                    int grupo = u.nuevoEstadoHabilitado ? ID_GRUPO_HABILITADOS : ID_GRUPO_DESHABILITADOS;

                    zk.SetUserGroup(1, int.Parse(u.idUsuario), grupo);
                }

                zk.EnableDevice(1, true);

                return "Usuarios actualizados";
            });
        }

        // =========================
        // 👆 HUELLAS
        // =========================

        public List<string> ObtenerHuellasUsuario(string userId)
        {
            return EjecutarSeguro(_ipActual, _puertoActual, zk =>
            {
                var lista = new List<string>();

                zk.EnableDevice(1, false);

                zk.ReadAllTemplate(1);
                zk.RefreshData(1);

                for (int i = 0; i <= 9; i++)
                {
                    string template;
                    int length;

                    bool ok = zk.SSR_GetUserTmpStr(1, userId, i, out template, out length);

                    if (ok)
                        lista.Add($"Dedo {i} ✔");
                }

                zk.EnableDevice(1, true);

                return lista;
            });
        }

        // =========================
        // 📦 CLONACIÓN / COPIA
        // =========================

        public string CopiarUsuarioConHuellas(string ipOrigen, string ipDestino, string userId)
        {
            var usuario = EjecutarSeguro(ipOrigen, 4370, zk =>
            {
                return ObtenerUsuarioPorId(userId);
            });

            if (usuario == null)
                return "Usuario no existe";

            return EjecutarSeguro(ipDestino, 4370, zk =>
            {
                zk.EnableDevice(1, false);

                zk.SSR_SetUserInfo(1, usuario.UserID, usuario.Nombre, usuario.Password ?? "", usuario.Privilegio, true);

                zk.SetUserGroup(1, int.Parse(usuario.UserID), ID_GRUPO_DESHABILITADOS);

                zk.EnableDevice(1, true);

                return "Usuario copiado";
            });
        }

        public string ClonarDispositivo(string ipOrigen, string ipDestino)
        {
            var usuarios = EjecutarSeguro(ipOrigen, 4370, zk =>
            {
                var lista = new List<UsuarioZKTeco>();

                zk.ReadAllUserID(1);
                zk.RefreshData(1);

                string userID, name, password;
                int privilege;
                bool enabled;

                while (zk.SSR_GetAllUserInfo(1, out userID, out name, out password, out privilege, out enabled))
                {
                    lista.Add(new UsuarioZKTeco
                    {
                        UserID = userID,
                        Nombre = name,
                        Password = password,
                        Privilegio = privilege,
                        Habilitado = enabled
                    });
                }

                return lista;
            });

            return EjecutarSeguro(ipDestino, 4370, zk =>
            {
                zk.EnableDevice(1, false);

                foreach (var u in usuarios)
                {
                    zk.SSR_SetUserInfo(1, u.UserID, u.Nombre, u.Password ?? "", u.Privilegio, true);
                    zk.SetUserGroup(1, int.Parse(u.UserID), ID_GRUPO_DESHABILITADOS);
                }

                zk.EnableDevice(1, true);

                return $"Clonación OK ({usuarios.Count} usuarios)";
            });
        }

        // =========================
        // ⚙️ OTROS
        // =========================

        public string ObtenerFirmware()
        {
            return EjecutarSeguro(_ipActual, _puertoActual, zk =>
            {
                string version = "";

                if (!zk.GetFirmwareVersion(1, ref version))
                {
                    int error = 0;
                    zk.GetLastError(ref error);
                    return $"Error: {error}";
                }

                return version;
            });
        }

        public string ReiniciarDispositivo(string ip, int puerto = 4370)
        {
            var zk = new CZKEMClass();

            try
            {
                zk.Disconnect();

                bool ok = ConectarConTimeout(zk, ip, puerto);

                if (!ok)
                    return "No conecta para reiniciar";

                bool reinicio = zk.RestartDevice(1);

                return reinicio ? "Dispositivo reiniciado" : "No se pudo reiniciar";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
            finally
            {
                try { zk.Disconnect(); } catch { }
            }
        }
    }
}