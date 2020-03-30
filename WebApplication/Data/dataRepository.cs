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
        public async Task postRepository(dynamic data)
        {
            string connectionString = _connectionString;// ConfigurationManager.ConnectionStrings["localdb"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string jsonData = JsonConvert.SerializeObject(data);
                //var jToken = JObject.Parse(jsonData);
                JObject respObj = (JObject)JsonConvert.DeserializeObject(data);
                JToken acme = respObj.SelectToken("content");
                string model = (string)respObj.SelectToken("mec");
                if (model != null)
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction(model);
                    XmlDocument document = new XmlDocument();
                    await loadFile(model, document);
                    XmlNodeList steps = document.SelectNodes("/steps/step");
                    //dynamic content = new JObject(data.)
                    foreach (XmlNode step in steps)
                    {
                        SQLInsert(connection, acme, step, transaction, document,0);
                    }
                    transaction.Commit();
                }
            }
        }
        public async Task putRepository(dynamic data, string modelo)
        {
            {
                string connectionString = _connectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction(modelo);
                    XmlDocument document = new XmlDocument();
                    await loadFile(modelo, document);
                    XmlNodeList steps = document.SelectNodes("/steps/step");
                    string jsonData = JsonConvert.SerializeObject(data);
                    JArray jsonArray = JArray.Parse(jsonData);
                    foreach (XmlNode step in steps)
                    {
                        SQLUpdate(connection, jsonArray, step, transaction, document);
                    }
                    transaction.Commit();
                }

            }
        }
        private static void SQLInsert(SqlConnection connection, dynamic dataArray, XmlNode step, SqlTransaction trans, XmlDocument document, int itemAfected)
        {
            foreach (JObject item in dataArray)
            {
                string commandHeader = "INSERT INTO " + step.Attributes["target"].Value + " ( ";
                string commandValues = " VALUES( ";
                string stringHeader = string.Empty;  //new string(""); ;
                string stringValues = string.Empty; // string("");
                string commandText = string.Empty;
                //int itemAfected = -1;
                foreach (XmlNode field in step.ChildNodes)
                {
                    string childNext = field.Name;
                    if (childNext.ToLower() == "child")
                    {
                        commandHeader += stringHeader + ") ";
                        commandValues += stringValues + ") ;SELECT SCOPE_IDENTITY();";
                        executeSql(commandHeader+ commandValues, connection, trans, ref itemAfected);
                        //commandHeader = "INSERT INTO " + field.Attributes["target"].Value + " ( ";
                        //commandValues = " VALUES( ";
                        //stringHeader = string.Empty;  //new string(""); ;
                        //stringValues = string.Empty; // string("");
                        XmlNodeList steps = document.SelectNodes("/steps/step/child");
                        JToken acmeDetail = item.SelectToken("detail");
                        foreach (XmlNode stepChild in steps)
                        {
                            SQLInsert(connection, acmeDetail, stepChild, trans, document, itemAfected);
                            //constructSQL(stepChild, ref stringHeader, ref stringValues, item);
                        }
                    }
                    else
                    {
                        constructSQL(field, ref stringHeader, ref stringValues, item, itemAfected);
                        // <field field="CLAVE" dbtype ="char" size="10"  path="CLAVE"  />
                    }
                }
                commandHeader += stringHeader + ") ";
                commandValues += stringValues + ");SELECT SCOPE_IDENTITY(); ";
                commandText = commandHeader + commandValues;
                executeSql(commandText, connection, trans, ref itemAfected);
            }
        }
        private static void constructSQL(XmlNode field, ref string stringHeader, ref string stringValues, JObject item, int itemAfected)
        {
            string dbcol = field.Attributes["field"].Value;
            string dbtype = field.Attributes["dbtype"].Value;
            int size = int.Parse(field.Attributes["size"].Value);
            string fieldPath = field.Attributes["path"].Value;
            //var asasd = field.Attributes["parent"];
            bool parentValue = Convert.ToBoolean(field.Attributes["parent"]==null? "false":field.Attributes["parent"].Value);
            //bool childNext = Convert.ToBoolean((field.Attributes["child"].Value));
            //XmlNode val = null;
            if (fieldPath != "" & dbtype.ToLower() != "identity")
            {
                stringHeader += (string.IsNullOrEmpty(stringHeader) ? stringHeader : ", ") + dbcol;
                JToken value = item.SelectToken(fieldPath);
                
                if (value != null || parentValue)
                {
                    string valor =  value == null? itemAfected.ToString(): value.ToString();
                    stringValues += (string.IsNullOrEmpty(stringValues) ? stringValues : ", ") + dataType(valor, dbtype);
                }
                else
                {
                    string valorDef = field.Attributes["default"].Value;
                    stringValues += (string.IsNullOrEmpty(stringValues) ? stringValues : ", ") + dataType(valorDef, dbtype);
                }
            }

        }
        private static void executeSql(string commandText, SqlConnection connection, SqlTransaction trans, ref int res)
        {
            try
            {
                using (SqlCommand command = new SqlCommand(commandText, connection))
                {
                    command.Transaction = trans;
                    res = Convert.ToInt32(command.ExecuteScalar());
                    //res = command.ExecuteNonQuery();
                    if (res <= 0)
                    {
                        throw new Exception("SQL Command was no affected (" + commandText + ")");
                    }
                }
            }
            catch(Exception e)
            {
                string mensaje = e.Message;
            }

        }
        private static void SQLUpdate(SqlConnection connection, JArray dataArray, XmlNode step, SqlTransaction trans, XmlDocument document)
        {
            foreach (dynamic item in dataArray)
            {
                string commandHeader = "UPDATE " + step.Attributes["target"].Value + " SET ";
                string commandValues = " ";
                string stringHeader = string.Empty;  //new string(""); ;
                string stringValues = string.Empty; // string("");
                string stringWhere = string.Empty;

                foreach (XmlNode field in step.ChildNodes)
                {
                    string dbcol = field.Attributes["field"].Value;
                    string dbtype = field.Attributes["dbtype"].Value;
                    int size = int.Parse(field.Attributes["size"].Value);
                    string fieldPath = field.Attributes["path"].Value;
                    bool keyField= Convert.ToBoolean(field.Attributes["key"].Value);

                    if (fieldPath != "" & !keyField)
                    {
                        JToken value = item.SelectToken(fieldPath);
                        if (value != null)
                        {
                            string valor = value.ToString();
                            stringValues += (string.IsNullOrEmpty(stringValues) ? stringValues : ", ") + fieldPath + " = " + dataType(valor, dbtype);
                        }
                        else
                        {
                            string valorDef = field.Attributes["default"].Value;
                            stringValues += (string.IsNullOrEmpty(stringValues) ? stringValues : ", ") + fieldPath + " = " + dataType(valorDef, dbtype);
                        }
                    }
                    else if (keyField)
                    {
                        JToken value = item.SelectToken(fieldPath);
                        string valor = value.ToString();
                        stringWhere = " WHERE ";
                        stringWhere += (string.IsNullOrEmpty(stringValues) ? stringValues : " AND ") + fieldPath + " = " + dataType(valor, dbtype);
                    }
                }
                //commandHeader += stringHeader + ") ";
                //commandValues += stringValues + ") ";
                string commandText = commandHeader + stringValues+ stringWhere;
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
        private static string dataType(string val, string dbType)
        {
            switch (dbType.ToLower())
            {
                case "byte":
                case "integer":
                case "int":
                case "double":
                case "identity":
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


