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
        public async Task handleData(Dats data,string id)
        {

            string connectionString = _connectionString;// ConfigurationManager.ConnectionStrings["localdb"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction(data.operation);
                if (!(data.content is null))
                {
                    switch (data.operation.ToLower())
                    {
                        case "insert":
                            SQLInsert(connection, data, "", transaction);
                            break;
                        case "update":
                            SQLUpdate(connection, data, "", transaction);
                            break;
                        case "delete":
                            break;
                    }
                    transaction.Commit();
                    // int reg = data.values.Count();                    
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
                    cmd.Parameters.Add(new SqlParameter("@function_name",  data.content[0].key));
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
            foreach (Contents targ in data.content)
            {
                string commandHeader = "INSERT INTO " + targ.target+ " ( ";
                string commandValues = " VALUES( ";
                string stringHeader = string.Empty;  //new string(""); ;
                string stringValues = string.Empty; // string("");
                foreach (Values val in targ.value)
                {
                    stringHeader += (string.IsNullOrEmpty(stringHeader) ? stringHeader : ", ") + val.row;
                    stringValues += (string.IsNullOrEmpty(stringValues) ? stringValues : ", ") + dataType(val);
                }
                commandHeader += stringHeader + ") ";
                commandValues += stringValues + ") ";
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
        private static void SQLUpdate(SqlConnection connection, Dats data, string target, SqlTransaction trans)
        {
            foreach (Contents targ in data.content)
            {
                string commandHeader = "UPDATE " + targ.target + " SET ";
                string commandWhere = " WHERE " +targ.key ;
                string stringHeader = string.Empty;  //new string(""); ;
                string stringValues = string.Empty; // string("");
                foreach (Values val in targ.value)
                {
                    //stringHeader += (string.IsNullOrEmpty(stringHeader) ? stringHeader : ", ") + val.row;
                    stringValues += (string.IsNullOrEmpty(stringValues) ? stringValues : " , ") + val.row +"=" +dataType(val);
                }
                commandHeader += stringHeader + ") ";
                commandValues += stringValues + ") ";
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
        private static string dataType(Values val)
        {
            switch (val.valueType.ToLower())
            {
                case "byte":
                    return val.value;
                case "integer":
                    return val.value;
                case "nvarchar":
                    return ("'" + val.value + "'");
                case "date":
                    return ("'" + val.value + "'");
                case "delete":
                    break;
            }
            return "0";
        }
    }
}
