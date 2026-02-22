using API_Torniquetes.Models.Reserva;
using Microsoft.Data.SqlClient;
using System.Data;

namespace API_Torniquetes.Repositories.Reservas
{
    public class ReservaRepository : IReservaRepository
    {
        private readonly string dbConnectionString = "Server=localhost\\SQLEXPRESS;Database=Torniquetes_Permisos;Trusted_Connection=True;TrustServerCertificate=True;";

        public Reserva Add(Reserva reserva)
        {
            using SqlConnection connection = new(dbConnectionString);
            connection.Open();

            string query = @"
                insert into Reserva(id_usuario, ip_torniquete, inicio_reserva, fin_reserva)
                output inserted.id
                values (@id_usuario, @ip_torniquete, @inicio_reserva, @fin_reserva)";

            using SqlCommand command = new(query, connection);

            command.Parameters.Add("@id_usuario", SqlDbType.NVarChar).Value = reserva.idUsuario;
            command.Parameters.Add("@ip_torniquete", SqlDbType.NVarChar).Value = reserva.ipTorniquete;
            command.Parameters.Add("@inicio_reserva", SqlDbType.DateTime).Value = reserva.inicioReserva;
            command.Parameters.Add("@fin_reserva", SqlDbType.DateTime).Value = reserva.finReserva;

            int idGenerado = (int)command.ExecuteScalar();

            reserva.id = idGenerado;
            

            return reserva;
        }
    }
}
