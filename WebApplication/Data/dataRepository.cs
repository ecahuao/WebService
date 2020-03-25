using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using WebApplication.Models;
//using System.Web.Services;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace WebApplication.Data
{
    public class dataRepository
    {
        private readonly string _connectionString;
        private readonly IHostingEnvironment _env;

        public dataRepository(IConfiguration configuration, IHostingEnvironment env)
        {
            _connectionString = configuration.GetValue<string>("Context");
            _env = env;
        }

        public async Task postRepository(dynamic data, string modelo)
        {
            string connectionString = _connectionString;// ConfigurationManager.ConnectionStrings["localdb"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction(modelo);
                XmlDocument document = new XmlDocument();
                await loadFile(modelo, document);
                XmlNodeList steps = document.SelectNodes("/steps/step");
                string jsonData = JsonConvert.SerializeObject(data);
                JArray o = JArray.Parse(jsonData);
                foreach (XmlNode step in steps)
                {
                    SQLInsert(connection, o, step, transaction, document);
                }
                transaction.Commit();
            }
        }
        public async Task putRepository(dynamic data, string modelo)
        {
            {
                string connectionString = _connectionString;// ConfigurationManager.ConnectionStrings["localdb"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction(modelo);
                    XmlDocument document = new XmlDocument();
                    await loadFile(modelo, document);
                    XmlNodeList steps = document.SelectNodes("/steps/step");
                    string jsonData = JsonConvert.SerializeObject(data);
                    JArray o = JArray.Parse(jsonData);
                    foreach (XmlNode step in steps)
                    {
                        SQLInsert(connection, o, step, transaction, document);
                    }
                    transaction.Commit();
                }

            }
        }
        public async Task handleData(Dats data, string id)
        {

            string connectionString = _connectionString;// ConfigurationManager.ConnectionStrings["localdb"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction(data.mec);
                XmlDocument document = new XmlDocument();
                await loadFile(data.mec, document);



                /*                if (!(data.content is null))
                                {
                                    switch (data.operation.ToLower())
                                    {
                                        case "insert":
                                            //SQLInsert(connection, data, "", transaction);
                                            break;
                                        case "update":
                                            SQLUpdate(connection, data, "", transaction, id);
                                            break;
                                        case "delete":
                                            break;
                                    }*/

                // int reg = data.values.Count();                    
                /*try
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
                }*/
            }
        }

        //private static void SQLInsert(SqlConnection connection, XmlNode step, string target, XmlNodeList rows, SqlTransaction trans)
        private static void SQLInsert(SqlConnection connection, JArray dataArray, XmlNode step, SqlTransaction trans, XmlDocument document)
        {
            //cmd.CommandText = "insert into NPVFRLOGPUBLICACION values(GETDATE(),'" + mec.Replace("_", "") + "','" + numTienda + "','" + messageGuid + "','OMITIDO','Se omite la publicación porque el numero de idoc es inferior')";

            foreach (dynamic item in dataArray)
            {
                string commandHeader = "INSERT INTO " + step.Attributes["target"].Value + " ( ";
                string commandValues = " VALUES( ";
                string stringHeader = string.Empty;  //new string(""); ;
                string stringValues = string.Empty; // string("");
                foreach (XmlNode field in step.ChildNodes)
                {
                    // <field field="CLAVE" dbtype ="char" size="10"  path="CLAVE"  />
                    string dbcol = field.Attributes["field"].Value;
                    string dbtype = field.Attributes["dbtype"].Value;
                    int size = int.Parse(field.Attributes["size"].Value);
                    string fieldPath = field.Attributes["path"].Value;

                    //XmlNode val = null;
                    if (fieldPath != "" & dbtype.ToLower() != "identity")
                    {
                        stringHeader += (string.IsNullOrEmpty(stringHeader) ? stringHeader : ", ") + dbcol;
                        //string jsonData = JsonConvert.SerializeObject(dataArray);
                        //JObject o = JObject.Parse(jsonData);
                        //JToken acme = dataArray.SelectToken("$.value[?(@.'" + fieldPath + "')]");//.SelectToken("value");
                        //object asdasd = dataArray[0].SelectToken("first_name"); 
                        //JToken value = dataArray.SelectToken( fieldPath );
                        JToken value = item.SelectToken(fieldPath);
                        //string valor = dataArray.SelectToken(fieldPath).ToString();
                        if (value != null)
                        {
                            string valor = value.ToString();
                            stringValues += (string.IsNullOrEmpty(stringValues) ? stringValues : ", ") + dataType(valor, dbtype);
                        }
                        else
                        {
                            string valorDef = field.Attributes["default"].Value;
                            stringValues += (string.IsNullOrEmpty(stringValues) ? stringValues : ", ") + dataType(valorDef, dbtype);
                        }
                    }
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
        /* private static void SQLUpdate(SqlConnection connection, Dats data, string target, SqlTransaction trans, string id)
         {
             foreach (Contents targ in data.content)
             {
                 string commandHeader = "UPDATE " + targ.target + " SET ";
                 string commandWhere = " WHERE " + targ.key + "=";
                 string stringHeader = string.Empty;  //new string(""); ;
                 string stringValues = string.Empty;
                 string stringWhere = string.Empty;// string("");
                 foreach (Values val in targ.value)
                 {
                     //stringHeader += (string.IsNullOrEmpty(stringHeader) ? stringHeader : ", ") + val.row;
                     stringValues += (string.IsNullOrEmpty(stringValues) ? stringValues : " , ") + val.row + "=" + dataType(val);
                 }
                 commandHeader += stringHeader + "=" + stringValues;

                 string commandText = commandHeader + commandWhere + id;
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
         }*/
        private static string dataType(string val, string dbType)
        {
            switch (dbType.ToLower())
            {
                case "byte":
                case ("integer"):
                case ("int"):
                case ("double"):
                    return val;
                case "nvarchar":
                case "varchar":
                case "date":
                case "datetime":
                    return ("'" + val + "'");
                case "null":
                    return ("'null'");
                default:
                    break;
            }
            return "0";
        }
        public async Task loadFile(string file, XmlDocument Xml)
        {

            var path = Path.Combine(_env.ContentRootPath, "xml", file + ".xml");
            Xml.Load(path);
            //XmlNodeList steps = Xml.SelectNodes("/steps/step");
            //string tables = string.Empty;
        }
    }
}


