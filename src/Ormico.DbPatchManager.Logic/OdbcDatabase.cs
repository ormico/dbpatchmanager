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

            if(Options.AddInstalledPatchSql == null ||
                Options.GetInstalledPatchesSql == null ||
                Options.InitPatchTableSql == null)
            {
                throw new ApplicationException("ODBC Plugin requires overriding all scripts");
            }

            _con = new OdbcConnection(Options.ConnectionString);
            ExecuteDDL(_option.InitPatchTableSql);
        }

        OdbcConnection _con;
        DatabaseOptions _option;

        public void Dispose()
        {
            if(_con != null)
            {
                _con.Dispose();
            }
        }

        public void ExecuteDDL(string commandText)
        {
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
