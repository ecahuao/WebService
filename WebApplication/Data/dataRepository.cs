using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using WebApplication.Models;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
//using Microsoft.AspNetCore.Mvc;

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
        public async Task<Resp> postRepository(dynamic data)
        {
            try
            {
                string connectionString = _connectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string jsonData = JsonConvert.SerializeObject(data);
                    JObject respObj = (JObject)JsonConvert.DeserializeObject(data);
                    string model = (string)respObj.SelectToken("mec");
                    if (model != null)
                    {
                        connection.Open();
                        SqlTransaction transaction = connection.BeginTransaction(model);
                        XmlDocument document = new XmlDocument();
                        await loadFile(model, document);
                        XmlNodeList steps = document.SelectNodes("/steps/step");
                        string pathContent = steps.Item(0).Attributes["path"].Value;
                        JToken acme = respObj.SelectToken(pathContent);
                        if (acme != null)
                        {
                            foreach (XmlNode step in steps)
                            {
                                SQLInsert(connection, acme, step, transaction, document, "0");
                            }
                        }
                        transaction.Commit();
                    }
                }
                Resp resp = new Resp();
                resp.message = "{\"message\": \"Ejecutado exitosamente\"}";//{\"foo\":1,\"bar\":false}
                resp.resp = 200;
                return resp;
            }

            catch (Exception e)
            {
                Resp resp = new Resp();
                resp.resp = 500;
                resp.message = "{\"message\": \"" + e.Message + "\"}";
                return resp; 
            }
        }
        public async Task<Resp> putRepository(dynamic data)
        {
            try
            {
                string connectionString = _connectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string jsonData = JsonConvert.SerializeObject(data);
                    JObject respObj = (JObject)JsonConvert.DeserializeObject(data);
                    string model = (string)respObj.SelectToken("mec");
                    if (model != null)
                    {
                        connection.Open();
                        SqlTransaction transaction = connection.BeginTransaction(model);
                        XmlDocument document = new XmlDocument();
                        await loadFile(model, document);
                        XmlNodeList steps = document.SelectNodes("/steps/step");
                        string pathContent = steps.Item(0).Attributes["path"].Value;
                        JToken acme = respObj.SelectToken(pathContent);
                        if (acme != null)
                        {
                            foreach (XmlNode step in steps)
                            {
                                SQLUpdate(connection, acme, step, transaction, document, "0");
                            }
                        }
                        transaction.Commit();
                    }
                }

                Resp resp = new Resp();
                resp.message = "{\"message\": \"Ejecutado exitosamente\"}";//{\"foo\":1,\"bar\":false}
                resp.resp = 200;
                return resp;
            }
            catch (Exception e)
            {
                Resp resp = new Resp();
                resp.resp = 500;
                resp.message = "{\"message\": \"" + e.Message + "\"}";
                return resp;
            }
        }
        private static void SQLUpdate(SqlConnection connection, dynamic dataArray, XmlNode step, SqlTransaction trans, XmlDocument document, string itemAfected)
        {
            foreach (JObject item in dataArray)
            {
                string commandHeader = "UPDATE " + step.Attributes["target"].Value + " SET ";
                string commandValues = "  ";
                string stringHeader = string.Empty;  //new string(""); ;
                string stringValues = string.Empty; // string("");
                string commandText = string.Empty;
                string stringOutput = string.Empty;
                foreach (XmlNode field in step.ChildNodes)
                {
                    constructSQL(field, ref stringHeader, ref stringValues, ref stringOutput, item, itemAfected);
                }
            }
        }
        private static void SQLInsert(SqlConnection connection, dynamic dataArray, XmlNode step, SqlTransaction trans, XmlDocument document, string itemAfected)
        {
            foreach (JObject item in dataArray)
            {
                string commandHeader = "INSERT INTO " + step.Attributes["target"].Value + " ( ";
                string commandValues = " VALUES( ";
                string stringHeader = string.Empty;  //new string(""); ;
                string stringValues = string.Empty; // string("");
                string commandText = string.Empty;
                string stringOutput = string.Empty; 
                foreach (XmlNode field in step.ChildNodes)
                {
                    string childNext = field.Name;
                    if (childNext.ToLower() != "field")
                    {
                        commandHeader += (!string.IsNullOrEmpty(stringHeader)) ? stringHeader + ") " : "";
                        commandValues += (!string.IsNullOrEmpty(stringValues)) ? stringValues + ")" : "";
                        commandText = commandHeader + stringOutput + commandValues;
                        if (!string.IsNullOrEmpty(commandText))
                        {
                            executeSql(commandText, connection, trans, ref itemAfected);
                            commandText = string.Empty;
                            commandHeader = string.Empty;
                            commandValues = string.Empty;
                            stringOutput = string.Empty;
                            stringHeader = string.Empty;
                            stringValues = string.Empty;
                        }
                        //XmlNodeList steps = document.SelectNodes("/steps/step/"+ childNext+"[@path = '"+ field.Attributes["path"].Value + "']");
                        XmlNode stepChild = document.SelectSingleNode("/steps/step/" + childNext + "[@path = '" + field.Attributes["path"].Value + "']");
                        //foreach (XmlNode stepChild in steps)
                        //{
                        //string pathDetail = stepChild.Attributes["path"].Value;
                        string pathDetail = stepChild.Attributes["path"].Value;
                        JToken acmeDetail = item.SelectToken(pathDetail);
                        if (acmeDetail != null)
                        {
                                //SQLInsert(connection, acmeDetail, stepchild, trans, document, itemAfected);
                            SQLInsert(connection, acmeDetail, stepChild, trans, document, itemAfected);
                        }
                        //}
                    }
                    else
                    {
                        constructSQL(field, ref stringHeader, ref stringValues, ref stringOutput, item, itemAfected);
                    }
                }
                commandHeader += (!string.IsNullOrEmpty(stringHeader)) ? stringHeader + ") " : "";
                commandValues += (!string.IsNullOrEmpty(stringValues)) ?  stringValues + ")" : "";
                commandText = commandHeader+stringOutput + commandValues;
                if (!string.IsNullOrEmpty(commandText))
                    {
                    executeSql(commandText, connection, trans, ref itemAfected);
                    commandText = string.Empty;
                    commandHeader = string.Empty;
                    stringOutput = string.Empty;
                    stringHeader= string.Empty;
                    stringValues = string.Empty;
                    commandValues = string.Empty;
                }
            }
        }
        private static void constructSQL(XmlNode field, ref string stringHeader, ref string stringValues, ref string stringOutput, JObject item, string itemAfected)
        {
            //string outputHeader = string.Empty;
            string dbcol = field.Attributes["field"].Value;
            string dbtype = field.Attributes["dbtype"].Value;
            int size = int.Parse(field.Attributes["size"].Value);
            string fieldPath = field.Attributes["path"].Value;
            //var asasd = field.Attributes["parent"];
            bool keyValue = Convert.ToBoolean(field.Attributes["key"] == null ? "false" : field.Attributes["key"].Value);
            bool parentValue = Convert.ToBoolean(field.Attributes["parent"]==null? "false":field.Attributes["parent"].Value);
            if (keyValue)
            {
                stringOutput = " OUTPUT inserted." + dbcol + " ";
            }
            //bool childNext = Convert.ToBoolean((field.Attributes["child"].Value));
            //XmlNode val = null;
            if (fieldPath != "" & dbtype.ToLower() != "identity")
            {
                stringHeader += (string.IsNullOrEmpty(stringHeader) ? stringHeader : ", ")  + dbcol;
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
        private static void executeSql(string commandText, SqlConnection connection, SqlTransaction trans, ref string res)
        {
            using (SqlCommand command = new SqlCommand(commandText, connection))
            {
                command.Transaction = trans;
                //var reader = command.ExecuteReader();
                res = Convert.ToString(command.ExecuteScalar());
                if (res == "0")
                {
                    throw new Exception("SQL Command was no affected (" + commandText + ")");
                }
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


