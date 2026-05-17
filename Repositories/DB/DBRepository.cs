using Microsoft.Data.SqlClient;
using System.Data;

namespace API_Torniquetes.Repositories.DB
{
    public class DBRepository : IDBRepository
    {
        private readonly string dbConnectionString = "Server = 201.148.104.16; Database = reservas_Algarrobo; User Id = reservas_admin_redysolutions; Password=cX970htvSk; TrustServerCertificate=True;";

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
                rut = null;
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
            command.Parameters.Add("@rut_usuario", SqlDbType.NVarChar).Value = (object?)rut ?? DBNull.Value;

            filasAfectadas = command.ExecuteNonQuery();

            return filasAfectadas;
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

        public HashSet<string> ObtenerIdUsuariosFaltantes(string ipOrigen, string ipDestino)
        {
            var idUsuarios = new HashSet<string>();

            using var connection = new SqlConnection(dbConnectionString);
            connection.Open();

            string query = @"
                SELECT o.id_usuario
                FROM EstadoAcceso o
                WHERE o.ip_torniquete = @ip_origen
                AND NOT EXISTS (
                    SELECT 1
                    FROM EstadoAcceso d
                    WHERE d.id_usuario = o.id_usuario
                      AND d.ip_torniquete = @ip_destino
                )
                GROUP BY o.id_usuario";

            using var command = new SqlCommand(query, connection);

            command.Parameters.Add("@ip_origen", SqlDbType.NVarChar, 50).Value = ipOrigen;
            command.Parameters.Add("@ip_destino", SqlDbType.NVarChar, 50).Value = ipDestino;

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                if (!reader.IsDBNull(0))
                {
                    idUsuarios.Add(reader.GetString(0));
                }
            }

            return idUsuarios;
        }

        public void RegistrarLog(string log, string ipTorniquete, string codigo)
        {
            using SqlConnection connection = new(dbConnectionString);
            connection.Open();

            string query = @"
                insert into LogTorniquete(log, ip_torniquete, codigo)
                values (@log, @ip_torniquete, @codigo)";

            using SqlCommand command = new(query, connection);

            command.Parameters.Add("@log", SqlDbType.NVarChar).Value = log;
            command.Parameters.Add("@ip_torniquete", SqlDbType.NVarChar).Value = ipTorniquete;
            command.Parameters.Add("@codigo", SqlDbType.NVarChar).Value = codigo;

            command.ExecuteNonQuery();
        }
    }
}
