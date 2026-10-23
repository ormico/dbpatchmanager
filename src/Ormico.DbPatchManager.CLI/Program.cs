using System;
using CommandLine;
using Ormico.DbPatchManager.Logic;
using Ormico.DbPatchManager.CLI.CommandLineOptions;
using System.Reflection;

namespace Ormico.DbPatchManager.CLI
{
    class Program
    {
        static int Main(string[] args)
        {
            int rc = 0;
            try
            {
                var result = CommandLine.Parser.Default.ParseArguments<InitCmdLineOptions, AddPatchCmdLineOptions, BuildCmdLineOptions, VersionCmdLineOptions>(args);

                rc = result.MapResult(
                    (InitCmdLineOptions o) => InitBuildSettings(o),
                    (AddPatchCmdLineOptions o) => AddPatch(o),
                    (BuildCmdLineOptions o) => Build(o),
                    (VersionCmdLineOptions o) => ShowVersion(o),
                    err => 1
                );

                return rc;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"{ex.InnerException.Message}");
                }
                return -1;
            }
        }

        private const string _patchFileName = "patches.json";
        private const string _patchLocalFileName = "patches.local.json";

        public static bool StrEq(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        static int InitBuildSettings(InitCmdLineOptions options)
        {
            int rc = 0;
            PatchManager manager = new PatchManager(_patchFileName, _patchLocalFileName);
            //todo: pass all settings
            manager.InitConfig(new InitOptions() { DbType = options.DbType });

            return rc;
        }

        static int AddPatch(AddPatchCmdLineOptions options)
        {
            int rc = 0;
            PatchManager manager = new PatchManager(_patchFileName, _patchLocalFileName);
            //todo: pass all settings
            manager.AddPatch(options.Name, new PatchOptions()
            {
            });
            return rc;
        }

        static int Build(BuildCmdLineOptions options)
        {
            int rc = 0;
            PatchManager manager = new PatchManager(_patchFileName, _patchLocalFileName);
            manager.Build();
            return rc;
        }

        static int ShowVersion(VersionCmdLineOptions options)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Console.WriteLine($"dbpatch {version}");
            return 0;
        }
    }
}
