using Ormico.DbPatchManager.Cmd.CommandLineOptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace Ormico.DbPatchManager.Cmd
{
    class Program
    {
        static int Main(string[] args)
        {
            int rc = 0;
            try
            {
                rc = CommandLine.Parser.Default.ParseArguments<InitCmdLineOptions, AddPatchCmdLineOptions, BuildCmdLineOptions>(args)
                    .MapResult(
                        (InitCmdLineOptions o) => InitBuildSettings(o),
                        (AddPatchCmdLineOptions o) => AddPatch(o),
                        (BuildCmdLineOptions o) => Build(o),
                        err => 1
                    );
            }
            catch(Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"{ex.InnerException.Message}");
                }
            }

            return rc;
        }

        private const string _patchFileName = ".\\patches.json";
        private const string _patchLocalFileName = ".\\patches.local.json";

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
    }
}
