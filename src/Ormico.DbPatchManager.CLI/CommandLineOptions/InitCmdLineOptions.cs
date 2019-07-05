using CommandLine;

namespace Ormico.DbPatchManager.CLI.CommandLineOptions
{
    [Verb("init", HelpText = "init")]
    class InitCmdLineOptions
    {
        public InitCmdLineOptions()
        {
        }

        /// <summary>
        /// sql server, mysql, sqlite, etc
        /// </summary>
        [Option("dbtype", Required = true, HelpText = "sql server, mysql, sqlite, etc")]
        public string DbType { get; set; }

        /// <summary>
        /// folder that will contain patch folders
        /// </summary>
        [Option("patchFolder", Default = "Patches", HelpText = "folder that will contain patch folders")]
        public string PatchFolder { get; set; }

        /// <summary>
        /// sprocs, functions, views
        /// </summary>
        [Option("codeFolder", Default = "Code", HelpText = "sprocs, functions, views")]
        public string CodeFolder { get; set; }
    }
}