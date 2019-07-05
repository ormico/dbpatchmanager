using CommandLine;

namespace Ormico.DbPatchManager.CLI.CommandLineOptions
{
    [Verb("addpatch", HelpText = "Create a new Patch folder and add it to odpm.json")]
    class AddPatchCmdLineOptions
    {
        public AddPatchCmdLineOptions()
        {
        }

        [Option("name", HelpText = "patch name")]
        public string Name { get; set; }
    }
}