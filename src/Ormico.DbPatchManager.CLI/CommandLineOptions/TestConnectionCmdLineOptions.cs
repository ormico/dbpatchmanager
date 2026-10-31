using CommandLine;

namespace Ormico.DbPatchManager.CLI.CommandLineOptions
{
    [Verb("test-connection", HelpText = "Test database connectivity without applying patches")]
    class TestConnectionCmdLineOptions
    {
        public TestConnectionCmdLineOptions()
        {
        }

        /// <summary>
        /// Database connection string to test. If omitted, reads from patches.json / patches.local.json.
        /// </summary>
        [Option('c', "connectionstring", HelpText = "Database connection string to test. If omitted, reads from config files.")]
        public string ConnectionString { get; set; }

        /// <summary>
        /// Database type: sqlserver, odbc, test, or custom plugin name. Defaults to value in patches.json if -c is used without this flag.
        /// </summary>
        [Option('d', "dbtype", HelpText = "Database type (sqlserver, odbc, test). Read from config if not specified.")]
        public string DbType { get; set; }
    }
}
