using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ormico.DbPatchManager
{
    public class DatabaseOptions
    {
        public DatabaseOptions()
        {
            AdditionalOptions = new Dictionary<string, string>();
        }

        public string ConnectionString { get; set; }
        public string GetInstalledPatchesSql { get; set; }
        public string AddInstalledPatchSql { get; set; }
        public string InitPatchTableSql { get; set; }


        public Dictionary<string, string> AdditionalOptions { get; set; }
    }
}
