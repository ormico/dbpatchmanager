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

        public Dictionary<string, string> Options { get; set; }

        public List<Patch> patches { get; set; }

        public List<Patch> GetOpenPatches()
        {
            var depends = (from p in patches
                          from d in p.DependsOn
                          select d.Id).Distinct();

            var rc = from p in patches
                             where !depends.Contains(p.Id)
                             select p;
            return rc.ToList();
        }

        public Patch GetFirstPatch()
        {
            Patch rc = null;
            var firstQuery = from f in patches
                        where f.DependsOn == null || f.DependsOn.Count() <= 0
                        select f;
            var first = firstQuery.ToList();
            if(first.Count() == 1)
            {
                rc = first.FirstOrDefault();
            }
            else if (first.Count() <= 0)
            {
                //todo: throw an exception or return null?
                //throw new ApplicationException("No starting Patch found. There should be one patch with no dependencies.");
                rc = null;
            }
            else
            {
                throw new ApplicationException("More than one starting Patch found. There should only be one Patch with no dependencies.");
            }

            return rc;
        }
    }
}
