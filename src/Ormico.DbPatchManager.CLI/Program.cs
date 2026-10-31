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
                rc = CommandLine.Parser.Default.ParseArguments(args, typeof(InitCmdLineOptions), typeof(AddPatchCmdLineOptions), typeof(BuildCmdLineOptions), typeof(TestConnectionCmdLineOptions))
                    .MapResult(
                        (InitCmdLineOptions o) => InitBuildSettings(o),
                        (AddPatchCmdLineOptions o) => AddPatch(o),
                        (BuildCmdLineOptions o) => Build(o),
                        (TestConnectionCmdLineOptions o) => TestConnection(o),
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

        static int TestConnection(TestConnectionCmdLineOptions options)
        {
            var service = new TestConnectionService();
            TestConnectionResult result;

            if (!string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                // Direct test with provided connection string
                string dbType = !string.IsNullOrWhiteSpace(options.DbType)
                    ? options.DbType
                    : "sqlserver";
                result = service.Test(options.ConnectionString, dbType);
            }
            else
            {
                // Read from existing patches.json / patches.local.json
                result = service.TestFromConfig(_patchFileName, _patchLocalFileName);
            }

            if (result.Success)
            {
                Console.WriteLine($"Connection to '{result.DatabaseType}' database established successfully.");
                if (!string.IsNullOrWhiteSpace(result.ServerInfo))
                    Console.WriteLine($"Server: {result.ServerInfo}");
                return 0;
            }
            else
            {
                Console.WriteLine("ERROR: Failed to connect to database.");
                Console.WriteLine(result.ErrorMessage);
                return 1;
            }
        }
    }
}
