using Dapper;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ormico.DbPatchManager
{
    public class OdbcDatabase : IDatabase
    {
        public void Connect(DatabaseOptions Options)
        {
            _con = new OdbcConnection(Options.ConnectionString);
        }

        OdbcConnection _con;

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
            throw new NotImplementedException();
        }

        public void LogInstalledPatch(string patchId)
        {
            throw new NotImplementedException();
        }
    }
}
