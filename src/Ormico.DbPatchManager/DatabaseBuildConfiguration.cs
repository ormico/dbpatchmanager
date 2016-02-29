using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ormico.DbPatchManager
{
    public class DatabaseBuildConfiguration
    {
        public DatabaseBuildConfiguration()
        {
            patches = new List<Patch>();
        }

        public string DatabaseType { get; set; }

        public string ConnectionString { get; set; }

        public string PatchFolder { get; set; }

        public string CodeFolder { get; set; }

        public List<Patch> patches { get; set; }

        public List<Patch> GetOpenPatches()
        {
            var rc = from p in patches
                             where p.DependsOn == null || p.DependsOn.Count() > 0
                             select p;
            return rc.ToList();
        }
    }
}
