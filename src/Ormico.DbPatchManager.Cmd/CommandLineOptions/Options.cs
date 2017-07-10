using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ormico.DbPatchManager.Cmd.CommandLineOptions
{
    //class Options
    //{
    //    public Options()
    //    {
    //        InitVerb = new InitOptions();
    //    }

    //    public InitOptions InitVerb { get; set; }

    //    public AddPatchOptions AddPatchVerb { get; set; }

    //    public BuildOptions BuildVerb { get; set; }

    //    [HelpVerbOption]
    //    public string GetUsage(string verb)
    //    {
    //        return HelpText.AutoBuild(this, verb);
    //    }
    //}

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

    [Verb("addpatch", HelpText = "Create a new Patch folder and add it to odpm.json")]
    class AddPatchCmdLineOptions
    {
        public AddPatchCmdLineOptions()
        {
        }

        [Option("name", HelpText = "patch name")]
        public string Name { get; set; }
    }

    [Verb("build", HelpText = "build")]
    class BuildCmdLineOptions
    {
        public BuildCmdLineOptions()
        {
        }
    }
}
