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

        string ScriptOverridesFolder = @"ScriptOverrides";
        //todo: not sure if making these .sql is best since not all databases are sql
        string AddInstalledPatchFileName = "AddInstalledPatch.sql";
        string GetInstalledPatchesFileName = "GetInstalledPatches.sql";
        string InitPatchTableFileName = "InitPatchTable.sql";

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

                // load options
                //DatabaseOptions dbopt = LoadDatabaseOptions(cfg);

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

            // load options
            DatabaseOptions dbopt = LoadDatabaseOptions(cfg);

            var first = cfg.GetFirstPatch();
            if(first != null)
            {
                PluginManager pm = new PluginManager();

                using (var db = pm.LoadDatabasePlugin(cfg.DatabaseType))
                {
                    db.Connect(dbopt);
                    var installedPatches = db.GetInstalledPatches();

                    //todo: add log/console output
                    InstallPatch(first, db, installedPatches);
                }
            }
        }

        private void InstallPatch(Patch patch, IDatabase db, List<InstalledPatchInfo> installedPatches)
        {
            LinkedList<Patch> graph = new LinkedList<Patch>();
            graph.AddLast(patch);
            bool isInstalled;
            Patch current;

            while (graph.Count() > 0)
            {
                current = graph.First.Value;
                graph.RemoveFirst();
                isInstalled = installedPatches.Any(i => string.Equals(i.PatchId, current.Id));

                List<Patch> notInstalledDependencies = new List<Patch>();
                // make sure all dependencies are installed
                if (current.DependsOn != null)
                {
                    // check each dependency and see if it is already installed
                    // add not installed dependencies to a list
                    bool isDepInstalled;
                    foreach (Patch dependency in current.DependsOn)
                    {
                        isDepInstalled = installedPatches.Any(i => string.Equals(i.PatchId, dependency.Id));
                        if(!isDepInstalled)
                        {
                            notInstalledDependencies.Add(dependency);
                        }
                    }
                }

                if(notInstalledDependencies.Count() > 0)
                {
                    // if there are dependencies to install
                    // put current back on stack and put dependencies on stack
                    // and return to top of loop
                    graph.AddFirst(current);
                    foreach(var d in notInstalledDependencies)
                    {
                        graph.AddFirst(d);
                    }
                }
                else
                {
                    // if there are no dependencies and patch is not installed then install it
                    if (!_io.Directory.Exists(current.Id))
                    {
                        throw new ApplicationException(string.Format("Patch folder '{0}' missing.", current.Id));
                    }
                    else
                    {
                        // make sure patch isn't already installed
                        if (!isInstalled)
                        {
                            var files = _io.Directory.GetFiles(current.Id);
                            foreach (var file in files)
                            {
                                var ext = _io.Path.GetExtension(file);
                                if (string.Equals(ext, ".sql", StringComparison.OrdinalIgnoreCase))
                                {
                                    //todo: check file size before reading all text
                                    string sql = _io.File.ReadAllText(file);
                                    db.ExecuteDDL(sql);
                                }
                                else if (string.Equals(ext, ".js", StringComparison.OrdinalIgnoreCase))
                                {
                                    //todo: run javascript patch
                                }
                            }
                            db.LogInstalledPatch(current.Id);
                            installedPatches.Add(new InstalledPatchInfo() { PatchId = current.Id, InstalledDate = DateTime.Now });
                        }

                        // add children of current
                        foreach (var c in current.Children)
                        {
                            graph.AddLast(c);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Create DatabaseOptions object from DatabaseCuildConfiguration and
        /// by checking to see if override files exist.
        /// </summary>
        /// <param name="cfg"></param>
        /// <returns></returns>
        DatabaseOptions LoadDatabaseOptions(DatabaseBuildConfiguration cfg)
        {
            DatabaseOptions rc = new DatabaseOptions();
            rc.ConnectionString = cfg.ConnectionString;
            rc.AdditionalOptions = cfg.Options;

            if (_io.File.Exists(_io.Path.Combine(ScriptOverridesFolder, AddInstalledPatchFileName)))
            {
                rc.AddInstalledPatchSql = _io.File.ReadAllText(_io.Path.Combine(ScriptOverridesFolder, AddInstalledPatchFileName));
            }

            if (_io.File.Exists(_io.Path.Combine(ScriptOverridesFolder, GetInstalledPatchesFileName)))
            {
                rc.GetInstalledPatchesSql = _io.File.ReadAllText(_io.Path.Combine(ScriptOverridesFolder, GetInstalledPatchesFileName));
            }

            if (_io.File.Exists(_io.Path.Combine(ScriptOverridesFolder, InitPatchTableFileName)))
            {
                rc.InitPatchTableSql = _io.File.ReadAllText(_io.Path.Combine(ScriptOverridesFolder, InitPatchTableFileName));
            }

            return rc;
        }
    }
}
