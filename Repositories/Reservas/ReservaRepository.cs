using API_Torniquetes.Models.Reserva;
using API_Torniquetes.Models.Usuarios;
using Microsoft.Data.SqlClient;
using System.Data;

namespace API_Torniquetes.Repositories.Reservas
{
    public class ReservaRepository : IReservaRepository
    {
        private readonly string dbConnectionString = "Server=localhost\\SQLEXPRESS;Database=Torniquetes_Permisos;Trusted_Connection=True;TrustServerCertificate=True;";
        //private readonly string dbConnectionString = "Server = 201.148.104.16; Database = reservas_Algarrobo_v2; User Id = reservas_admin_redysolutions; Password=cX970htvSk; TrustServerCertificate=True;";

        public Reserva Add(Reserva reserva)
        {
            using SqlConnection connection = new(dbConnectionString);
            connection.Open();

            using SqlTransaction transaction = connection.BeginTransaction();

            try
            {
                // reserva
                string queryReserva = @"
                    insert into Reserva(id, id_usuario, ip_torniquete, inicio_reserva, fin_reserva)
                    values (@id, @id_usuario, @ip_torniquete, @inicio_reserva, @fin_reserva)
                ";

                using SqlCommand commandReserva = new(queryReserva, connection, transaction);

                commandReserva.Parameters.Add("@id", SqlDbType.Int).Value = reserva.id;
                commandReserva.Parameters.Add("@id_usuario", SqlDbType.NVarChar).Value = reserva.idUsuario;
                commandReserva.Parameters.Add("@ip_torniquete", SqlDbType.NVarChar).Value = reserva.ipTorniquete;
                commandReserva.Parameters.Add("@inicio_reserva", SqlDbType.DateTime).Value = reserva.inicioReserva;
                commandReserva.Parameters.Add("@fin_reserva", SqlDbType.DateTime).Value = reserva.finReserva;

                commandReserva.ExecuteScalar();

                // estado acceso
                string queryEstado = @"
                    insert into EstadoAcceso(id_usuario, ip_torniquete, habilitado)
                    select @id_usuario, @ip_torniquete, @habilitado
                    where not exists (
                        select 1
                        from EstadoAcceso
                        where id_usuario = @id_usuario
                        and ip_torniquete = @ip_torniquete
                    )
                ";

                using SqlCommand commandEstado = new(queryEstado, connection, transaction);

                commandEstado.Parameters.Add("@id_usuario", SqlDbType.NVarChar).Value = reserva.idUsuario;
                commandEstado.Parameters.Add("@ip_torniquete", SqlDbType.NVarChar).Value = reserva.ipTorniquete;
                commandEstado.Parameters.Add("@habilitado", SqlDbType.Bit).Value = false;

                commandEstado.ExecuteNonQuery();

                transaction.Commit();

                return reserva;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

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

        public int RegistrarUsuarioEnBD(string idUsuario, string ipTorniquete, bool habilitado)
        {
            using SqlConnection connection = new(dbConnectionString);
            connection.Open();

            string query = @"
                insert into EstadoAcceso(id_usuario, ip_torniquete, habilitado)
                select @id_usuario, @ip_torniquete, @habilitado
                where not exists (
                    select 1
                    from EstadoAcceso
                    where id_usuario = @id_usuario
                    and ip_torniquete = @ip_torniquete
                )";

            using SqlCommand command = new(query, connection);

            command.Parameters.Add("@id_usuario", SqlDbType.NVarChar).Value = idUsuario;
            command.Parameters.Add("@ip_torniquete", SqlDbType.NVarChar).Value = ipTorniquete;
            command.Parameters.Add("@habilitado", SqlDbType.Bit).Value = habilitado;

            int filasAfectadas = command.ExecuteNonQuery();

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
                        EA.id_usuario,
                        EA.ip_torniquete,
                        EA.habilitado as estado_actual,

                        case
                            when exists (
                                select 1
                                from Reserva R
                                where R.id_usuario = EA.id_usuario
                                  and R.ip_torniquete = EA.ip_torniquete
                                  and getdate() >= R.inicio_reserva
                                  and getdate() <= R.fin_reserva
                            )
                            then 1
                            else 0
                        end as estado_deseado

                    from EstadoAcceso EA
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
