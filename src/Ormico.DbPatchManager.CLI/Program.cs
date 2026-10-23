using System;
using CommandLine;
using Ormico.DbPatchManager.Logic;
using Ormico.DbPatchManager.CLI.CommandLineOptions;

namespace Ormico.DbPatchManager.CLI
{
    class Program
    {
        static int Main(string[] args)
        {
            int rc = 0;
            try
            {
                rc = CommandLine.Parser.Default.ParseArguments(args, typeof(InitCmdLineOptions), typeof(AddPatchCmdLineOptions), typeof(BuildCmdLineOptions))
                    .MapResult(
                        (InitCmdLineOptions o) => InitBuildSettings(o),
                        (AddPatchCmdLineOptions o) => AddPatch(o),
                        (BuildCmdLineOptions o) => Build(o),
                        err => 1
                    );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"{ex.InnerException.Message}");
                }
                rc = -1;
            }

            return rc;
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
            try
            {
                manager.InitConfig(new InitOptions() { DbType = options.DbType });
            }
            catch (System.Data.Common.DbException dbEx)
            {
                Console.Error.WriteLine($"Database error: {dbEx.Message}");
                rc = 2;
            }
            catch (ApplicationException appEx)
            {
                Console.Error.WriteLine($"Error: {appEx.Message}");
                rc = 3;
            }

            return rc;
        }

        static int AddPatch(AddPatchCmdLineOptions options)
        {
            int rc = 0;
            PatchManager manager = new PatchManager(_patchFileName, _patchLocalFileName);
            try
            {
                manager.AddPatch(options.Name, new PatchOptions()
                {
                });
            }
            catch (System.Data.Common.DbException dbEx)
            {
                Console.Error.WriteLine($"Database error: {dbEx.Message}");
                rc = 2;
            }
            catch (ApplicationException appEx)
            {
                Console.Error.WriteLine($"Error: {appEx.Message}");
                rc = 3;
            }

            return rc;
        }

        static int Build(BuildCmdLineOptions options)
        {
            int rc = 0;
            PatchManager manager = new PatchManager(_patchFileName, _patchLocalFileName);
            try
            {
                manager.Build();
            }
            catch (System.Data.Common.DbException dbEx)
            {
                Console.Error.WriteLine($"Database error: {dbEx.Message}");
                rc = 2;
            }
            catch (ApplicationException appEx)
            {
                Console.Error.WriteLine($"Error: {appEx.Message}");
                rc = 3;
            }

            return rc;
        }
    }
}
