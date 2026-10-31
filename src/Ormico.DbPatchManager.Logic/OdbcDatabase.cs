using Dapper;
using Ormico.DbPatchManager.Common;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ormico.DbPatchManager.Logic
{
    public class OdbcDatabase : IDatabase
    {
        public void Connect(DatabaseOptions Options)
        {
            _option = Options;

            RequireScriptOverride(Options.AddInstalledPatchSql, "AddInstalledPatch.sql");
            RequireScriptOverride(Options.GetInstalledPatchesSql, "GetInstalledPatches.sql");
            RequireScriptOverride(Options.InitPatchTableSql, "InitPatchTable.sql");

            _con = new OdbcConnection(Options.ConnectionString);
            ExecuteDDL(_option.InitPatchTableSql);
        }

        OdbcConnection _con;
        DatabaseOptions _option;

        /// <summary>
        /// ODBC has no built-in scripts, so every override must exist and contain executable SQL.
        /// Skipping one is not safe: an empty AddInstalledPatch, for example, would silently
        /// stop patches from being recorded as installed.
        /// </summary>
        static void RequireScriptOverride(string sql, string fileName)
        {
            if (sql == null)
            {
                throw new ApplicationException($"ODBC Plugin requires overriding all scripts: ScriptOverrides/{fileName} is missing.");
            }
            if (SqlScriptInspector.IsEmptyOrCommentOnly(sql))
            {
                throw new ApplicationException($"ODBC Plugin requires overriding all scripts: ScriptOverrides/{fileName} is empty or contains only comments.");
            }
        }

        public void Dispose()
        {
            if(_con != null)
            {
                _con.Dispose();
            }
        }

        public bool TestConnection()
        {
            try
            {
                if (_con == null || _con.State != System.Data.ConnectionState.Open)
                {
                    _con.Open();
                }
                _con.Query<int>("SELECT 1").FirstOrDefault();
                return true;
            }
            catch (Exception ex)
            {
                throw new Common.DbPatchManagerException(
                    $"ODBC connection test failed: {ex.Message}", ex);
            }
        }

        public void ExecuteDDL(string commandText)
        {
            // some ODBC drivers reject scripts with no executable content
            if (SqlScriptInspector.IsEmptyOrCommentOnly(commandText))
            {
                return;
            }
            _con.Execute(commandText);
        }

        public List<InstalledPatchInfo> GetInstalledPatches()
        {
            List<InstalledPatchInfo> rc = _con.Query<InstalledPatchInfo>(_option.GetInstalledPatchesSql, null).ToList();
            return rc;
        }

        public void LogInstalledPatch(string patchId)
        {
            _con.Execute(_option.AddInstalledPatchSql,
               new
               {
                   PatchId = patchId
               }, commandType: System.Data.CommandType.Text);
        }

        public void ExecuteProgrammabilityScript(string commandText)
        {
            ExecuteDDL(commandText);
        }
    }
}
