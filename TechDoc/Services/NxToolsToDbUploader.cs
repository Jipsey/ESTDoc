using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;
using NXOpen;
using NXOpen.CAM;
using TechDocNS.Model;

namespace TechDocNS.Services
{
    public class NxToolsToDbUploaderService
    {
        private NxToolsToDbUploaderOptions _options;
        private readonly string _connectionString;
        private readonly string _commandCheckOperationInDb;
        private readonly string _commandAddToolToDb;
        private readonly string _commandDeleteToolFromDb;
        private readonly NxSession _context;

        private const string ConnectionConstString
            = "Data Source=max3;Initial Catalog=DiakontProgs;Integrated Security=False;User ID=maxmast;Password=maxplus;Connect Timeout=15;Encrypt=False;TrustServerCertificate=False;";

        private NxToolsToDbUploaderService()
        {
            _commandCheckOperationInDb = "ugs_check_opers_new";
            _commandAddToolToDb = "instr_ugs_add";
            _commandDeleteToolFromDb = "instr_ugs_del";

            var stringBuilder = new SqlConnectionStringBuilder(ConnectionConstString) { ApplicationName = "NxToolsToDbUploader" };
            _connectionString = stringBuilder.ConnectionString;
        }

        public NxToolsToDbUploaderService(NxSession data)
            : this()
        {
            _context = data;
        }

        public void UploadNxToolsToDb()
        {
            if (_context == null) return;
            NxLogger.Log("Начинаю выгрузку инструмента в базу...");

            NxLogger.Log("Получаю объекты из контекста...");
            _options = new NxToolsToDbUploaderOptions(_context);
            NxLogger.Log("Завершено.");
            NxLogger.Log("Получено инструментов: " + _options.Tools.Count());
            foreach (var tool in _options.Tools)
                NxLogger.Log(tool.Name);

            NxLogger.Log("Начинаю проверку операции...");
            var checkResult = CheckOperationInDb(_options);
            NxLogger.Log("Результат проверки: " + checkResult);
            NxLogger.Log("Завершено.");

            if (!string.IsNullOrEmpty(checkResult) && checkResult != "1"
                && MessageBox.Show(checkResult, "Необходимо принять решение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            {
                // удаляем записи для инструментов
                NxLogger.Log("Начинаю удаление операции...");
                DeleteToolFromDb(_options);
                NxLogger.Log("Завершено.");
            }

            // добавляем инструмент
            NxLogger.Log("Начинаю добавлять инструмент в базу...");
            AddToolToDb(_options);
            NxLogger.Log("Завершено.");
            NxLogger.Log("Закончил выгрузку инструмента.");
        }

        private void AddToolToDb(NxToolsToDbUploaderOptions options)
        {
            if (options == null || string.IsNullOrEmpty(options.PartNumber)) return;

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(_commandAddToolToDb, connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@u_item", SqlDbType.Char).Value = options.PartNumber;
                command.Parameters.Add("@u_routeid", SqlDbType.Char).Value = options.RouteNumber;
                command.Parameters.Add("@u_nom_oper", SqlDbType.Char).Value = options.OperationNumber;
                command.Parameters.Add("@u_user", SqlDbType.Char).Value = options.Login;

                command.Parameters.Add("@u_kod_instr", SqlDbType.Char).Value = "";
                command.Parameters.Add("@u_kol_instr", SqlDbType.Float).Value = 0;
                command.Parameters.Add("@u_vrem_tr", SqlDbType.Char).Value = "";
                command.Parameters.Add("@u_st_instr", SqlDbType.Char).Value = "";
                command.Parameters.Add("@u_kol_plas", SqlDbType.Char).Value = "";
                command.Parameters.Add("@u_material", SqlDbType.Char).Value = "";
                command.Parameters.Add("@u_tip_oper", SqlDbType.Char).Value = "";

                if (options.Tools == null) return;

                var toolIds = options.Tools
                    .Where(tool => tool.HasUserAttribute("ID_TOOL", NXObject.AttributeType.String, -1))
                    .Select(tool => tool.GetUserAttribute("ID_TOOL", NXObject.AttributeType.String, -1).StringValue)
                    .Where(value => !string.IsNullOrEmpty(value));

#if DEBUG
                {
                    foreach (var toolId in toolIds)
                        NxLogger.Log("ID_TOOL: " + toolId);
                    NxLogger.Log("AddToolToDb command.ExecuteNonQuery()");
                }
#else
                {
                    connection.Open();
                    foreach (var u_kod_instr in toolIds)
                    {
                        command.Parameters["@u_kod_instr"].Value = u_kod_instr;
                        command.ExecuteNonQuery();
                    }

                }
#endif
            }
        }

        private void DeleteToolFromDb(NxToolsToDbUploaderOptions options)
        {
            if (options == null || string.IsNullOrEmpty(options.PartNumber)) return;

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(_commandDeleteToolFromDb, connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@u_item", SqlDbType.Char).Value = options.PartNumber;
                command.Parameters.Add("@u_routeid", SqlDbType.Char).Value = options.RouteNumber;
                command.Parameters.Add("@u_opno", SqlDbType.Char).Value = options.OperationNumber;
                command.Parameters.Add("@u_user", SqlDbType.Char).Value = options.Login;
#if DEBUG
                {
                    NxLogger.Log("DeleteToolFromDb command.ExecuteNonQuery()");
                }
#else
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                }
#endif
            }
        }

        private string CheckOperationInDb(NxToolsToDbUploaderOptions options)
        {
            if (options == null || string.IsNullOrEmpty(options.PartNumber)) return null;

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(_commandCheckOperationInDb, connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@type", SqlDbType.Int).Value = 1;
                command.Parameters.Add("@item", SqlDbType.Char).Value = options.PartNumber;
                command.Parameters.Add("@route", SqlDbType.Char).Value = options.RouteNumber;
                command.Parameters.Add("@old_opno", SqlDbType.Char).Value = options.OperationNumber;
                command.Parameters.Add("@new_opno", SqlDbType.Char).Value = options.OperationNumber;
#if DEBUG
                {
                    NxLogger.Log("CheckOperationInDb command.ExecuteScalar() as string");
                    return "1";
                }
#else
                {
                    connection.Open();
                    return command.ExecuteScalar() as string;
                }
#endif
            }
        }
    }

    internal class NxToolsToDbUploaderOptions
    {
        private string _login;
        private readonly NxSession _context;
        private IEnumerable<NxOperation> _operations;
        private IEnumerable<Tool> _tools;

        public NxToolsToDbUploaderOptions(NxSession context)
        {
            if (context == null)
                throw new Exception("Не могу работать! Не могу создать объект для выгрузки в базу!");

            if (context.NxOperationGroups == null || !context.NxOperationGroups.Any())
                throw new Exception("Не могу работать! Не нашел выделенных групп операций!");

            _context = context;
        }

        private IEnumerable<NxOperation> Operations
        {
            get
            {
                if (_operations == null && _context != null && _context.NxOperationGroups != null)
                    _operations = _context.NxOperationGroups.SelectMany(gr => gr.NxOperations);
                return _operations;
            }
        }

        public IEnumerable<Tool> Tools
        {
            get
            {
                if (_tools == null && Operations != null)
                    _tools = Operations.GroupBy(op => op.Tool).Select(gr =>gr.Key);
                return _tools;
            }
        }

        public string PartNumber
        {
            get { return _context != null && _context.Attributes != null ? _context.Attributes.PartNumber : string.Empty; }
        }

        public string RouteNumber
        {
            get { return _context != null && _context.Additional != null ? string.Format("М{0}", _context.Additional.RouteNumber) : string.Empty; }
        }

        public string OperationNumber
        {
            get { return _context != null && _context.Additional != null ? string.Format("{0:D3}", _context.Additional.OperationNumber) : string.Empty; }
        }

        public string Login
        {
            get
            {
                if (string.IsNullOrEmpty(_login)) _login = Environment.UserName;
                if (_login.Length > 30) _login = _login.Remove(30);
                return _login;
            }
        }
    }
}
