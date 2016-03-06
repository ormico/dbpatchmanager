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
            _rand = new Random();
            _io = new FileSystem();
        }

        /// <summary>
        /// Testing constructor. This constructor allows setting
        /// the the Random and FileSystem objects in addition to the normal 
        /// constructor parameter(s).
        /// </summary>
        /// <param name="ConfigFile"></param>
        /// <param name="Rand"></param>
        /// <param name="Io"></param>
        public PatchManager(string ConfigFile, Random Rand, FileSystem Io)
        {
            _configFileName = ConfigFile;
            _rand = Rand;
            _io = Io;
        }

        readonly string _configFileName;
        readonly Random _rand;

        /// <summary>
        /// Use System.IO.Abstraction to make testing easier.
        /// </summary>
        readonly FileSystem _io;

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
            if(string.IsNullOrWhiteSpace(PatchID))
            {
                throw new ApplicationException("Patch ID required");
            }
            else
            {
                var cfgWriter = new BuildConfigurationWriter(_configFileName);
                var cfg = cfgWriter.Read();

                //create unique id prefix to avoid collisions
                string prefix = string.Format("{0:yyyyMMddHHmm}-{1:0000}",
                    DateTime.Now,
                    _rand.Next(0, 9999));
                string finalId = string.Format("{0}-{1}", prefix, PatchID.Trim());

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
                    throw new ApplicationException(string.Format("A folder named '{0}' already exists", finalId));
                }
            }
        }

        public void Build()
        {
            var cfgWriter = new BuildConfigurationWriter(_configFileName);
            var cfg = cfgWriter.Read();
            var first = cfg.GetFirstPatch();
            if(first != null)
            {
                using (var db = new TestDatabase())
                {
                    var installedPatches = db.GetInstalledPatches();
                    db.Connect(cfg.ConnectionString);

                    //todo: log or console first patch
                    InstallPatch(first, db, installedPatches);


                }
            }
        }

        private void InstallPatch(Patch patch, TestDatabase db, List<InstalledPatchInfo> installedPatches)
        {
            bool isInstalled = installedPatches.Any(i => string.Equals(i.Id, patch.Id));
            if (!isInstalled)
            {
                var files = _io.Directory.GetFiles(patch.Id);
                foreach (var file in files)
                {
                    var ext = _io.Path.GetExtension(file);
                    if (string.Equals(ext, "sql", StringComparison.OrdinalIgnoreCase))
                    {
                        //todo: check file size before reading all text
                        string sql = _io.File.ReadAllText(file);
                        db.ExecuteDDL(sql);
                    }
                    else if (string.Equals(ext, "js", StringComparison.OrdinalIgnoreCase))
                    {

                    }
                }
                db.LogInstalledPatch(patch.Id);
            }
            else
            {
                //todo: log or console that patch already installed
            }
        }
    }
}
