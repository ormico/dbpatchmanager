using Dapper;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ormico.DbPatchManager.SqlServer
{
    public class SqlDatabase : IDatabase
    {
        public void Connect(string ConnectionString)
        {
            _con = new SqlConnection(ConnectionString);
            _con.Open();

            //todo: change database
            //if (changeDatabase)
            //{
            //    _con.ChangeDatabase(this.Database);
            //}

            _server = new Server(new ServerConnection(_con));
            _server.ConnectionContext.StatementTimeout = 0;
            InitPatchTable();
        }

        SqlConnection _con;
        Server _server;

        Lazy<string> _sqlAddInstalledPatch = new Lazy<string>(() => GetEmbededSql("Ormico.DbPatchManager.SqlServer.SqlScripts.AddInstalledPatch.sql"));
        Lazy<string> _sqlInitPatchTable = new Lazy<string>(() => GetEmbededSql("Ormico.DbPatchManager.SqlServer.SqlScripts.InitPatchTable.sql"));
        Lazy<string> _sqlGetInstalledPatches = new Lazy<string>(() => GetEmbededSql("Ormico.DbPatchManager.SqlServer.SqlScripts.GetInstalledPatches.sql"));

        public void Dispose()
        {
            if(_con != null)
            {
                _con.Dispose();
            }
        }

        public void ExecuteDDL(string commandText)
        {
            _server.ConnectionContext.ExecuteNonQuery(commandText);
        }

        public List<InstalledPatchInfo> GetInstalledPatches()
        {
            List<InstalledPatchInfo> rc = _con.Query<InstalledPatchInfo>(_sqlGetInstalledPatches.Value, null).ToList();
            return rc;
        }

        public void LogInstalledPatch(string patchId)
        {
            _con.Execute(_sqlAddInstalledPatch.Value,
                new
                {
                    PatchId = patchId
                }, commandType: System.Data.CommandType.Text);
        }

        void InitPatchTable()
        {
            ExecuteDDL(_sqlInitPatchTable.Value);
        }

        public static string GetEmbededSql(string fileName)
        {
            string rc = null;
            Assembly ass = Assembly.GetExecutingAssembly();
            using (StreamReader sr = new StreamReader(ass.GetManifestResourceStream(fileName)))
            {
                rc = sr.ReadToEnd();
            }
        
            return rc;
        }
    }
}
