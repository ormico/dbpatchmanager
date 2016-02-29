using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ormico.DbPatchManager.Cmd.CommandLineOptions
{
    class Options
    {
        public Options()
        {
        }

        [VerbOption("init", HelpText = "")]
        public InitOptions InitVerb { get; set; }

        [VerbOption("addpatch", HelpText = "Create a new Patch folder and add it to odpm.json")]
        public AddPatchOptions AddPatchVerb { get; set; }

        [VerbOption("build", HelpText = "")]
        public BuildOptions BuildVerb { get; set; }
    }

    class InitOptions
    {
        /// <summary>
        /// sql server, mysql, sqlite, etc
        /// </summary>
        [Option("dbtype", Required = true, HelpText = "sql server, mysql, sqlite, etc")]
        public string DbType { get; set; }

        /// <summary>
        /// folder that will contain patch folders
        /// </summary>
        [Option("patchFolder", DefaultValue = "Patches", Required = true, HelpText = "folder that will contain patch folders")]
        public string PatchFolder { get; set; }

        /// <summary>
        /// sprocs, functions, views
        /// </summary>
        [Option("codeFolder", DefaultValue = "Code", Required = true, HelpText = "sprocs, functions, views")]
        public string CodeFolder { get; set; }
    }

    class AddPatchOptions
    {
        [Option("name", HelpText = "patch name")]
        public string Name { get; set; }
    }

    class BuildOptions
    {
    }
}
