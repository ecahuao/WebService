using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using WebApplication.Models;

namespace WebApplication.Data
{
    public class dataRepository
    {
        private readonly string _connectionString;

        public dataRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetValue<string>("Context");

        }
        public async Task handleData(Dats data)
        {

            string connectionString = _connectionString;// ConfigurationManager.ConnectionStrings["localdb"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction(data.operation);
                if (!(data.values is null))
                {
                    switch (data.operation.ToLower())
                    {
                        case "insert":
                            SQLInsert(connection, data, "", transaction);
                            break;
                        case "update":
                            break;
                        case "delete":
                            break;
                    }
                    int reg = data.values.Count();                    
                    try
                    {
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.CommandText = "select MotivoError from NPVFRLOGPUBLICACION where NumDocumento = '" + "' and Estatus = 'NO EXITOSO'";
                            cmd.CommandType = System.Data.CommandType.Text;
                            cmd.Transaction = transaction;
                        }
                    }
                    catch (InvalidOperationException e)
                    {

                        throw e;
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                        throw e;
                    }
                }
            }
            using (SqlConnection sql = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("spInsertFunction", sql))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@function_name",  data.key));
                    cmd.Parameters.Add(new SqlParameter("@function_ruta", data.path));
                    await sql.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                    return;
                }
            }
        }
        //private static void SQLInsert(SqlConnection connection, XmlNode step, string target, XmlNodeList rows, SqlTransaction trans)
        private static void SQLInsert(SqlConnection connection, Dats data,string target, SqlTransaction trans)
        {
            //cmd.CommandText = "insert into NPVFRLOGPUBLICACION values(GETDATE(),'" + mec.Replace("_", "") + "','" + numTienda + "','" + messageGuid + "','OMITIDO','Se omite la publicación porque el numero de idoc es inferior')";
            string commandHeader = "INSERT INTO " + data.target + "(";
            string commandValues = " VALUES( ";

            foreach (content val in data.values)
            {
                commandHeader += val.row + ",";
                commandValues += val.value + ",";
            }
            commandHeader +=  ")";
            commandValues +=  ")";
            string commandText = commandHeader + commandValues;
            using (SqlCommand command = new SqlCommand(commandText, connection))
            {
                command.Transaction = trans;
                int res = command.ExecuteNonQuery();
                if (res <= 0)
                {
                    throw new Exception("SQL Command was no affected (" + commandText + ")");
                }
            }
        }
    }
}
