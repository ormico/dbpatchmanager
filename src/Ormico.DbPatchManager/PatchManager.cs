using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ormico.DbPatchManager
{
    public class PatchManager
    {
        public PatchManager(string ConfigFile)
        {
            _configFileName = ConfigFile;
        }

        readonly string _configFileName;

        public void InitConfig(InitOptions Options)
        {
            
        }

        public void AddPatch(string PatchID, PatchOptions Options = null)
        {

        }

        public void Build()
        {

        }
    }
}
