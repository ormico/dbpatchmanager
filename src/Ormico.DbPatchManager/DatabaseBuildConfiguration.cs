using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ormico.DbPatchManager
{
    public class DatabaseBuildConfiguration
    {
        public string DatabaseType { get; set; }

        public string ConnectionString { get; set; }

        public string PatchFolder { get; set; }

        public string CodeFolder { get; set; }

        public List<Patch> patches { get; set; }
    }
}
