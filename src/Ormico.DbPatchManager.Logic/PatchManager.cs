using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ormico.DbPatchManager.Logic
{
    public class PatchManager
    {
        public PatchManager(string ConfigFile, string LocalConfigFile)
        {
            _configFileName = ConfigFile;
            _configLocalFileName = LocalConfigFile;
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

        DatabaseBuildConfiguration _configuration;
        string ScriptOverridesFolder = @"ScriptOverrides";
        //todo: not sure if making these .sql is best since not all databases are sql
        string AddInstalledPatchFileName = "AddInstalledPatch.sql";
        string GetInstalledPatchesFileName = "GetInstalledPatches.sql";
        string InitPatchTableFileName = "InitPatchTable.sql";

        readonly string _configFileName;
        readonly string _configLocalFileName;
        readonly Random _rand;
        
        /// <summary>
        /// Use System.IO.Abstraction to make testing easier.
        /// </summary>
        readonly FileSystem _io;

        public void InitConfig(InitOptions Options)
        {
            var cfgWriter = new BuildConfigurationWriter(_configFileName, _configLocalFileName);
            DatabaseBuildConfiguration databaseBuildConfiguration = new DatabaseBuildConfiguration()
            {
                DatabaseType = Options?.DbType,
                ConnectionString = null,
                PatchFolder = "Patches",
                CodeFolder = "Code"
            };

            if (!_io.Directory.Exists(databaseBuildConfiguration.PatchFolder))
            {
                _io.Directory.CreateDirectory(databaseBuildConfiguration.PatchFolder);
            }

            if (!_io.Directory.Exists(databaseBuildConfiguration.CodeFolder))
            {
                _io.Directory.CreateDirectory(databaseBuildConfiguration.CodeFolder);
            }

            cfgWriter.Write(databaseBuildConfiguration);
        }

        public void AddPatch(string patchName, PatchOptions Options = null)
        {
            if(string.IsNullOrWhiteSpace(patchName))
            {
                throw new ApplicationException("Patch Name required");
            }
            else
            {
                var cfgWriter = new BuildConfigurationWriter(_configFileName, _configLocalFileName);
                var cfg = cfgWriter.Read();

                // load options
                //DatabaseOptions dbopt = LoadDatabaseOptions(cfg);

                //create unique id prefix to avoid collisions
                string prefix = string.Format("{0:yyyyMMddHHmm}-{1:0000}",
                    DateTime.Now,
                    _rand.Next(0, 9999));

                // patch names are limited to 50 char total at present, but this could be a property of the plugin
                string finalId = $"{prefix}-{patchName.Trim()}".Substring(0, 50);
                string patchPath = _io.Path.Combine(cfg.PatchFolder, finalId);

                if(!_io.Directory.Exists(patchPath))
                {
                    _io.Directory.CreateDirectory(patchPath);

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
                    throw new ApplicationException($"A folder named '{finalId}' already exists");
                }
            }
        }

        public void Build()
        {
            var cfgWriter = new BuildConfigurationWriter(_configFileName, _configLocalFileName);
            _configuration = cfgWriter.Read();

            // load options
            DatabaseOptions dbopt = LoadDatabaseOptions(_configuration);

            List<string> codeFileNames = GetSortedCodeFileNames(_configuration, dbopt);

            var first = _configuration.GetFirstPatch();
            if(first != null)
            {
                PluginManager pm = new PluginManager();

                using (var db = pm.LoadDatabasePlugin(_configuration.DatabaseType))
                {
                    db.Connect(dbopt);
                    var installedPatches = db.GetInstalledPatches();

                    //todo: if drop-all-sprocs-then-add-back option, then run drop all script

                    //todo: add log/console output
                    InstallPatch(first, db, installedPatches);

                    // install code files
                    InstallProgrammability(codeFileNames, db);
                }
            }
        }

        void InstallProgrammability(List<string> codeFileNames, IDatabase db)
        {
            foreach (var fn in codeFileNames)
            {
                //todo: add log/console output
                Console.WriteLine($"Installing: {fn}");
                string sql = _io.File.ReadAllText(fn);
                db.ExecuteProgrammabilityScript(sql);
            }
        }

        private List<string> GetSortedCodeFileNames(DatabaseBuildConfiguration cfg, DatabaseOptions dbopt)
        {
            List<string> rc = new List<string>();
            
            //todo: exclude if starts with "!" or add a glob library that includes this option

            // loop through each file name or pattern in CodeFiles, adding to rc in order
            // only add files once
            foreach (var p in cfg.CodeFiles)
            {
                var currentFiles = _io.Directory.GetFiles(cfg.CodeFolder, p, 
                    System.IO.SearchOption.TopDirectoryOnly);
                var filteredList = from f in currentFiles
                                   where !rc.Contains(f)
                                   select f;
                rc.AddRange(filteredList);
            }

            return rc;
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
                    string folder = _io.Path.Combine(_configuration.PatchFolder, current.Id);

                    // if there are no dependencies and patch is not installed then install it
                    if (!_io.Directory.Exists(folder))
                    {
                        throw new ApplicationException(string.Format("Patch folder '{0}' missing.", current.Id));
                    }
                    else
                    {
                        // make sure patch isn't already installed
                        if (isInstalled)
                        {
                            Console.WriteLine("{0} <already installed>", current.Id);
                        }
                        else
                        {
                            Console.WriteLine(current.Id);

                            var files = _io.Directory.GetFiles(folder);
                            foreach (var file in files)
                            {
                                Console.WriteLine(file);

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
