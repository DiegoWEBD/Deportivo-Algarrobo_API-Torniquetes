using API_Torniquetes.Models.Reserva;
using API_Torniquetes.Models.Usuarios;
using Microsoft.Data.SqlClient;
using System.Data;

namespace API_Torniquetes.Repositories.Reservas
{
    public class ReservaRepository : IReservaRepository
    {
        //private readonly string dbConnectionString = "Server=localhost\\SQLEXPRESS;Database=Torniquetes_Permisos;Trusted_Connection=True;TrustServerCertificate=True;";
        private readonly string dbConnectionString = "Server = 201.148.104.16; Database = reservas_Algarrobo; User Id = reservas_admin_redysolutions; Password=cX970htvSk; TrustServerCertificate=True;";

        public List<Reserva> ObtenerReservasActivas(DateTime fecha)
        {
            List<Reserva> reservas = new();
            using SqlConnection connection = new(dbConnectionString);
            connection.Open();

            string query = @"
                select R.IdReserva,
                       R.UserName AS RutUsuario,
                       R.FechaReserva,
                       R.Asistencia,
                       H.Horario,
                       S.Nombre AS NombreSala
                from Reserva R
                inner join Calendario C on R.FkCalendario = C.IdCalendario
                inner join Horario H on C.Fk_Horario = H.IdHorario
                inner join Clase CL on C.Fk_Clase = CL.IdClases
                left join Sala S on CL.IdSala = S.Id
                where R.FechaReserva = cast(@FechaReserva as date)
                and cast(@FechaReserva as time) 
                    between cast(left(H.Horario, 5) as time)
                    and cast(right(H.Horario, 5) as time)";

            using SqlCommand command = new(query, connection);
            command.Parameters.Add("@FechaReserva", SqlDbType.DateTime).Value = fecha;

            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                string horario = reader["Horario"].ToString();
                string[] partes = horario.Split('/');

                TimeSpan horaInicio = TimeSpan.Parse(partes[0]);
                TimeSpan horaFin = TimeSpan.Parse(partes[1]);

                DateTime fechaReserva = Convert.ToDateTime(reader["FechaReserva"]);

                reservas.Add(new Reserva
                {
                    id = Convert.ToInt32(reader["IdReserva"]),
                    idUsuario = reader["RutUsuario"].ToString(),
                    ipTorniquete = reader["NombreSala"].ToString(),
                    inicioReserva = fechaReserva.Date.Add(horaInicio),
                    finReserva = fechaReserva.Date.Add(horaFin)
                });
            }

            return reservas;
        }

        public int RegistrarUsuarioEnBD(string rutUsuario, string ipTorniquete, bool habilitado)
        {
            int filasAfectadas = 0;
            string[] partesRut = rutUsuario.Split('-');
            string idUsuario;
            string rut;

            if(partesRut.Length == 2)
            {
                idUsuario = partesRut[0];
                rut = rutUsuario;
            }
            else
            {
                idUsuario = rutUsuario;
                rut = "NULL";
            }

            using SqlConnection connection = new(dbConnectionString);
            connection.Open();

            string query = @"
                update EstadoAcceso
                set habilitado = @habilitado
                where id_usuario = @id_usuario
                and ip_torniquete = @ip_torniquete;

                if @@ROWCOUNT = 0
                begin
                    insert into EstadoAcceso (id_usuario, ip_torniquete, habilitado, rut_usuario)
                    values (@id_usuario, @ip_torniquete, @habilitado, @rut_usuario);
                end";

            using SqlCommand command = new(query, connection);

            command.Parameters.Add("@id_usuario", SqlDbType.NVarChar).Value = idUsuario;
            command.Parameters.Add("@ip_torniquete", SqlDbType.NVarChar).Value = ipTorniquete;
            command.Parameters.Add("@habilitado", SqlDbType.Bit).Value = habilitado;
            command.Parameters.Add("@rut_usuario", SqlDbType.NVarChar).Value = rut;

            filasAfectadas = command.ExecuteNonQuery();

            return filasAfectadas;
        }

        public Dictionary<string, List<UsuarioEstadoVencido>> ObtenerUsuariosConNuevoEstado()
        {
            Dictionary<string, List<UsuarioEstadoVencido>> estadosVencidos = new();
            using SqlConnection connection = new(dbConnectionString);
            connection.Open();

            string query = @"
                select *
                from (
                    select
                        ea.id_usuario,
                        ea.ip_torniquete,
                        ea.habilitado as estado_actual,

                        case
                            when u.idPerfil in (1, 2, 3) then 1

                            when exists (
                                select 1
                                from reserva r
                                inner join calendario c
                                    on r.FkCalendario = c.IdCalendario
                                inner join clase cl
                                    on c.Fk_Clase = cl.IdClases
                                inner join sala s
                                    on cl.IdSala = s.id
                                where r.UserName = ea.rut_usuario
                                  and s.ip_torniquete = ea.ip_torniquete
                                  and getdate() >= r.inicio_reserva
                                  and getdate() <= r.fin_reserva
                            ) then 1

                            else 0
                        end as estado_deseado

                    from estadoacceso ea
                    left join usuario u
                        on ea.rut_usuario = u.UserName
                ) aux
                where estado_actual <> estado_deseado";

            using SqlCommand command = new(query, connection);
            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                string ipTorniquete = reader["ip_torniquete"].ToString();

                if (!estadosVencidos.ContainsKey(ipTorniquete))
                {
                    estadosVencidos.Add(ipTorniquete, new());
                }

                List<UsuarioEstadoVencido> usuarios;
                estadosVencidos.TryGetValue(ipTorniquete, out usuarios);

                usuarios.Add(new UsuarioEstadoVencido
                {
                    idUsuario = reader["id_usuario"].ToString(),
                    ipTorniquete = ipTorniquete,
                    estadoHabilitadoActual = Boolean.Parse(reader["estado_actual"].ToString()),
                    nuevoEstadoHabilitado = reader["estado_deseado"].ToString().Equals("1")
                });
            }

            return estadosVencidos;
        }

        public void CambiarEstadoUsuario(string idUsuario, string ipTorniquete, bool habilitado)
        {
            using SqlConnection connection = new(dbConnectionString);
            connection.Open();

            string query = @"
                update EstadoAcceso
                set habilitado = @habilitado
                where id_usuario = @id_usuario
                and ip_torniquete = @ip_torniquete";

            using SqlCommand command = new(query, connection);

            command.Parameters.Add("@habilitado", SqlDbType.Bit).Value = habilitado;
            command.Parameters.Add("@id_usuario", SqlDbType.NVarChar).Value = idUsuario;
            command.Parameters.Add("@ip_torniquete", SqlDbType.NVarChar).Value = ipTorniquete;

            command.ExecuteNonQuery();
        }
    }
}
