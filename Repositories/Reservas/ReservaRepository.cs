using API_Torniquetes.Models.Reserva;
using Microsoft.Data.SqlClient;
using System.Data;

namespace API_Torniquetes.Repositories.Reservas
{
    public class ReservaRepository : IReservaRepository
    {
        //private readonly string dbConnectionString = "Server=localhost\\SQLEXPRESS;Database=Torniquetes_Permisos;Trusted_Connection=True;TrustServerCertificate=True;";
        private readonly string dbConnectionString = "Server = 201.148.104.16; Database = reservas_Algarrobo_v2; User Id = reservas_admin_redysolutions; Password=cX970htvSk; TrustServerCertificate=True;";

        /*public Reserva Add(Reserva reserva)
        {
            using SqlConnection connection = new(dbConnectionString);
            connection.Open();

            string query = @"
                insert into Reserva(id_usuario, ip_torniquete, inicio_reserva, fin_reserva)
                output inserted.id
                values (@id_usuario, @ip_torniquete, @inicio_reserva, @fin_reserva)";

            using SqlCommand command = new(query, connection);

            command.Parameters.Add("@id_usuario", SqlDbType.NVarChar).Value = reserva.idUsuario;
            command.Parameters.Add("@ip_torniquete", SqlDbType.NVarChar).Value = reserva.nombreSala;
            command.Parameters.Add("@inicio_reserva", SqlDbType.DateTime).Value = reserva.inicioReserva;
            command.Parameters.Add("@fin_reserva", SqlDbType.DateTime).Value = reserva.finReserva;

            int idGenerado = (int)command.ExecuteScalar();

            reserva.id = idGenerado;
            

            return reserva;
        }*/

        public Reserva[] ObtenerReservasActivas(DateTime fecha)
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
                    nombreSala = reader["NombreSala"].ToString(),
                    inicioReserva = fechaReserva.Date.Add(horaInicio),
                    finReserva = fechaReserva.Date.Add(horaFin)
                });
            }

            return reservas.ToArray();
        }
    }
}
