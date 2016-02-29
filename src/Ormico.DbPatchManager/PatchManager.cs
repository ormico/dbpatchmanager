using System;
using System.Collections.Generic;
using System.IO.Abstractions;
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

        /// <summary>
        /// Use System.IO.Abstraction to make testing easier.
        /// </summary>
        FileSystem _io = new FileSystem();

        public void InitConfig(InitOptions Options)
        {
            var cfgWriter = new BuildConfigurationWriter(_configFileName);
            cfgWriter.Write(new DatabaseBuildConfiguration()
            {
                DatabaseType = "Ormico.DbPatchManager.TestDatabase",
                ConnectionString = "File=testDatabase.json;",
                PatchFolder = "Patches",
                CodeFolder = "Code"
            });
        }

        public void AddPatch(string PatchID, PatchOptions Options = null)
        {
            var cfgWriter = new BuildConfigurationWriter(_configFileName);
            var cfg = cfgWriter.Read();

            //create unique id prefix 
            string prefix = string.Format("{0:yyyyMMddHHmm}-{1:0000}",
                DateTime.Now,
                (new Random()).Next(0, 9999));

            string finalId = string.Format("{0}-{1}", prefix, PatchID);

            if(!_io.Directory.Exists(finalId))
            {
                _io.Directory.CreateDirectory(finalId);

                // when adding a patch, make it dependent on all patches 
                // that don't already have a dependency.
                // need to guard against circular dependencies
                var openPatches = cfg.GetOpenPatches();
                cfg.patches.Add(new Patch(finalId, openPatches));
                cfgWriter.Write(cfg);
            }
            else
            {
                // create custom exception
                throw new ApplicationException(string.Format("a folder named {0} already exists", finalId));
            }
        }

        public void Build()
        {

        }
    }
}
