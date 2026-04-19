using API_Torniquetes.Models;
using API_Torniquetes.Models.Usuarios;
using API_Torniquetes.Services.Reservas;
using zkemkeeper;

namespace API_Torniquetes.Services
{
    public class ResultadoCambioEstado
    {
        public string IdUsuario { get; set; }
        public string IpTorniquete { get; set; }
        public bool Exito { get; set; }
        public bool NuevoEstadoHabilitado { get; set; }
        public int? CodigoError { get; set; }
    }

    public class ZKTecoService : IZKTecoService
    {
        private readonly IServiceScopeFactory scopeFactory;
        private IZKEM zk = new CZKEMClass();
        private static string IP_TORNIQUETE_ENROLADOR = "192.168.1.7";
        private static int ID_GRUPO_USUARIOS_HABILITADOS = 1;
        private static int ID_GRUPO_USUARIOS_DESHABILITADOS = 2;

        public ZKTecoService(IServiceScopeFactory scopeFactory)
        {
            this.scopeFactory = scopeFactory;
        }

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

        public List<ResultadoCambioEstado> CambiarEstadoUsuarios(List<UsuarioEstadoVencido> usuarios)
        {
            var resultados = new List<ResultadoCambioEstado>();

            zk.EnableDevice(1, false);

            if (!zk.ReadAllUserID(1))
            {
                zk.EnableDevice(1, true);
                throw new Exception("No se pudieron leer los usuarios");
            }

            zk.RefreshData(1);

            foreach (var usuario in usuarios)
            {
                int grupo = usuario.nuevoEstadoHabilitado
                    ? ID_GRUPO_USUARIOS_HABILITADOS
                    : ID_GRUPO_USUARIOS_DESHABILITADOS;

                bool ok = zk.SetUserGroup(1, int.Parse(usuario.idUsuario), grupo);

                int? error = null;

                if (!ok)
                {
                    int err = 0;
                    zk.GetLastError(ref err);
                    error = err;
                }

                resultados.Add(new ResultadoCambioEstado
                {
                    IdUsuario = usuario.idUsuario,
                    IpTorniquete = usuario.ipTorniquete,
                    NuevoEstadoHabilitado = usuario.nuevoEstadoHabilitado,
                    Exito = ok,
                    CodigoError = error
                });
            }

            zk.RefreshData(1);
            zk.EnableDevice(1, true);

            return resultados;
        }

        /*
         public string CambiarEstadoUsuarios(List<UsuarioEstadoVencido> usuarios)
        {
            Console.WriteLine("Deshabilitando torniquete");
            if (!zk.EnableDevice(1, false))
            {
                Console.WriteLine("No se pudo deshabilitar el torniquete");
                return "No se pudo deshabilitar el torniquete";
            }
                

            if (!zk.ReadAllUserID(1))
            {
                Console.WriteLine("No se pudieron leer los usuarios");
                zk.EnableDevice(1, true);
                return "No se pudieron leer los usuarios";
            }

            zk.RefreshData(1);

            using var scope = scopeFactory.CreateScope();
            var reservasService = scope.ServiceProvider.GetRequiredService<IReservasService>();

            foreach (var usuario in usuarios)
            {
                int grupo = usuario.nuevoEstadoHabilitado
                    ? ID_GRUPO_USUARIOS_HABILITADOS
                    : ID_GRUPO_USUARIOS_DESHABILITADOS;

                Console.WriteLine("Cambiando al usuario de grupo");
                bool ok = zk.SetUserGroup(1, int.Parse(usuario.idUsuario), grupo);

                if (!ok)
                {
                    int error = 0;
                    zk.GetLastError(ref error);
                    Console.WriteLine($"No se pudo cambiar al usuario de grupo: Error ({error})");
                    continue;
                }

                Console.WriteLine($"{DateTime.Now}. Usuario {usuario.idUsuario} {(usuario.nuevoEstadoHabilitado ? "habilitado" : "deshabilitado")} en torniquete {usuario.ipTorniquete}.");

                reservasService.CambiarEstadoUsuario(usuario.idUsuario, usuario.ipTorniquete, usuario.nuevoEstadoHabilitado);
                Console.WriteLine($"{DateTime.Now}. Usuario {usuario.idUsuario} {(usuario.nuevoEstadoHabilitado ? "habilitado" : "deshabilitado")} en base de datos.");
            }

            zk.RefreshData(1);
            zk.EnableDevice(1, true);

            return "Usuarios actualizados";
        }
         */

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

        /*public string CopiarUsuarioConHuellas(string ipOrigen, string ipDestino, string rut)
        {
            int machine = 1;
            try
            {
                Desconectar();

                string[] partesRut = rut.Split('-');

                if (partesRut.Length != 2)
                    return "Error. Formato de rut incorrecto";

                string userId = partesRut[0];

                // Origen
                if (Conectar(ipOrigen).StartsWith("Error"))
                    return "Error. No se pudo conectar al equipo origen";

                var usuario = ObtenerUsuarioPorId(userId);
                if (usuario == null)
                {
                    Desconectar();
                    return "Error. Usuario no existe en equipo origen";
                }

                zk.EnableDevice(machine, false);
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

                // Destino
                if (Conectar(ipDestino).StartsWith("Error"))
                    return "No se pudo conectar al equipo destino";

                zk.EnableDevice(machine, false);

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

                // Copiar huellas
                foreach (var huella in huellas)
                {
                    bool ok = zk.SetUserTmpExStr(
                        machine,
                        usuario.UserID,
                        huella.fingerIndex,
                        huella.flag,
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

                using var scope = scopeFactory.CreateScope();
                var reservasService = scope.ServiceProvider.GetRequiredService<IReservasService>();
                bool habilitado = true;

                reservasService.RegistrarUsuarioEnBD(rut, ipDestino, habilitado);

                return "Usuario y huellas copiadas correctamente";
            }
            catch
            {
                zk.EnableDevice(machine, true);
                Desconectar();
                return "Error al copiar el usuario";
            }
        }*/

        public string CopiarUsuarioConHuellas(string rut, string[] ipsDestino)
        {
            int machine = 1;
            try
            {
                Desconectar();

                string[] partesRut = rut.Split('-');

                if (partesRut.Length != 2)
                    return "Error. Formato de rut incorrecto";

                string userId = partesRut[0];


                // Origen
                if (Conectar(IP_TORNIQUETE_ENROLADOR).StartsWith("Error"))
                {
                    return "Error. No se pudo conectar al torniquete enrolador";
                }

                var usuario = ObtenerUsuarioPorId(userId);
                if (usuario == null)
                {
                    Desconectar();
                    return "Error. Usuario no existe en equipo origen";
                }

                zk.EnableDevice(machine, false);
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

                using var scope = scopeFactory.CreateScope();
                var reservasService = scope.ServiceProvider.GetRequiredService<IReservasService>();

                var resultados = new List<string>();

                foreach (var ipDestino in ipsDestino)
                {
                    if (Conectar(ipDestino).StartsWith("Error"))
                    {
                        resultados.Add($"{ipDestino}: no se pudo conectar");
                        continue;
                    }

                    zk.EnableDevice(machine, false);
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
                        resultados.Add($"{ipDestino}: error creando usuario ({error})");

                        zk.BatchUpdate(machine);
                        zk.EnableDevice(machine, true);
                        Desconectar();
                        continue;
                    }

                    bool falloHuella = false;

                    foreach (var huella in huellas)
                    {
                        bool ok = zk.SetUserTmpExStr(
                            machine,
                            usuario.UserID,
                            huella.fingerIndex,
                            huella.flag,
                            huella.template
                        );

                        if (!ok)
                        {
                            int error = 0;
                            zk.GetLastError(ref error);

                            resultados.Add($"{ipDestino}: error dedo {huella.fingerIndex} ({error})");

                            falloHuella = true;
                            break;
                        }
                    }

                    zk.BatchUpdate(machine);
                    zk.RefreshData(machine);
                    zk.EnableDevice(machine, true);
                    Desconectar();

                    if (!falloHuella)
                    {
                        bool habilitado = true;
                        reservasService.RegistrarUsuarioEnBD(rut, ipDestino, habilitado);
                        resultados.Add($"{ipDestino}: OK");
                    }
                }

                return string.Join(" | ", resultados);
            }
            catch
            {
                zk.EnableDevice(machine, true);
                Desconectar();
                return "Error al copiar el usuario";
            }
        }

        public string CopiarUsuariosConHuellas(string ipOrigen, string ipDestino)
        {
            int machine = 1;

            var usuariosDestino = new HashSet<string>();

            // ==============================
            // 1. OBTENER USUARIOS DESTINO
            // ==============================
            Console.WriteLine($"{DateTime.Now}. Conectando al torniquete destino: {ipDestino}");

            if (Conectar(ipDestino).StartsWith("Error"))
            {
                Console.WriteLine($"{DateTime.Now}. No se pudo conectar al torniquete destino: {ipDestino}");
                return "Error. No se pudo conectar al equipo destino";
            }

            zk.EnableDevice(machine, false);
            zk.ReadAllUserID(machine);
            zk.RefreshData(machine);

            string userId = "";
            string nombre = "";
            string password = "";
            int privilegio = 0;
            bool habilitado = false;

            Console.WriteLine($"{DateTime.Now}. Obteniendo usuarios del torniquete destino");
            while (zk.SSR_GetAllUserInfo(
                machine,
                out userId,
                out nombre,
                out password,
                out privilegio,
                out habilitado))
            {
                usuariosDestino.Add(userId);
            }

            Console.WriteLine($"{DateTime.Now}. Desconectando torniquete destino: {ipDestino}");
            zk.EnableDevice(machine, true);
            Desconectar();


            // ==============================
            // 2. OBTENER USUARIOS + HUELLAS ORIGEN
            // ==============================
            Console.WriteLine($"{DateTime.Now}. Conectando torniquete origen: {ipOrigen}");

            if (Conectar(ipOrigen).StartsWith("Error"))
            {
                Console.WriteLine($"{DateTime.Now}. No se pudo conectar al torniquete origen: {ipOrigen}");
                return "Error. No se pudo conectar al equipo origen";
            } 

            zk.EnableDevice(machine, false);
            zk.ReadAllUserID(machine);
            zk.ReadAllTemplate(machine);
            zk.RefreshData(machine);

            int total = 0;
            var usuariosACopiar = new List<(
                string UserID,
                string Nombre,
                string Password,
                int Privilegio,
                bool Habilitado,
                List<(int FingerIndex, int Flag, string Template)> Huellas
            )>();

            Console.WriteLine($"{DateTime.Now}. Obteniendo usuarios del torniquete origen");

            while (zk.SSR_GetAllUserInfo(
                machine,
                out userId,
                out nombre,
                out password,
                out privilegio,
                out habilitado))
            {
                total++;

                if (usuariosDestino.Contains(userId))
                    continue;

                var huellas = new List<(int FingerIndex, int Flag, string Template)>();

                for (int fingerIndex = 0; fingerIndex <= 9; fingerIndex++)
                {
                    string template = "";
                    int flag = 0;
                    int length = 0;

                    Console.WriteLine($"{DateTime.Now}. Obteniendo huellas del usuario {userId}");

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

                usuariosACopiar.Add((
                    userId,
                    nombre,
                    password,
                    privilegio,
                    habilitado,
                    huellas
                ));
            }

            Console.WriteLine($"{DateTime.Now}. Deconectando del torniquete origen: {ipOrigen}");
            zk.EnableDevice(machine, true);
            Desconectar();

            if (usuariosACopiar.Count == 0)
                return "No hay usuarios nuevos para copiar";


            // ==============================
            // 3. COPIAR AL DESTINO
            // ==============================
            Console.WriteLine($"{DateTime.Now}. Conectando al torniquete destino: {ipDestino}");

            if (Conectar(ipDestino).StartsWith("Error"))
            {
                Console.WriteLine($"{DateTime.Now}. No se pudo conectar al torniquete destino: {ipDestino}");
                return "Error. No se pudo reconectar al equipo destino";
            }
                

            zk.EnableDevice(machine, false);
            zk.BeginBatchUpdate(machine, 1);

            foreach (var usuario in usuariosACopiar)
            {
                Console.WriteLine($"{DateTime.Now}. Creando usuario en el destino");
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
                    Console.WriteLine($"{DateTime.Now}. Error creando usuario {usuario.UserID} en el destino. Desconectando");
                    int error = 0;
                    zk.GetLastError(ref error);

                    zk.BatchUpdate(machine);
                    zk.EnableDevice(machine, true);
                    Desconectar();
                    
                    return $"Error creando usuario {usuario.UserID}: {error}";
                }

                foreach (var huella in usuario.Huellas)
                {
                    Console.WriteLine($"{DateTime.Now}. Copiando huellas del usuario {usuario.UserID}");

                    bool ok = zk.SetUserTmpExStr(
                        machine,
                        usuario.UserID,
                        huella.FingerIndex,
                        huella.Flag,
                        huella.Template
                    );

                    if (!ok)
                    {
                        Console.WriteLine($"{DateTime.Now}. Error copiando huella dedo {huella.FingerIndex} de usuario {usuario.UserID}. Desconectando");
                        int error = 0;
                        zk.GetLastError(ref error);

                        zk.BatchUpdate(machine);
                        zk.EnableDevice(machine, true);
                        Desconectar();

                        return $"Error copiando huella dedo {huella.FingerIndex} de usuario {usuario.UserID}: {error}";
                    }
                }
            }

            zk.BatchUpdate(machine);
            zk.RefreshData(machine);
            zk.EnableDevice(machine, true);

            Console.WriteLine($"{DateTime.Now}. Usuarios copiados correctamente: {usuariosACopiar.Count}/{total}. Desconectando del destino");
            Desconectar();

            return $"Usuarios copiados correctamente: {usuariosACopiar.Count}/{total}";
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
