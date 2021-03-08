using CommandLine;

namespace Ormico.DbPatchManager.CLI.CommandLineOptions
{
    [Verb("addpatch", HelpText = "Create a new Patch folder and add it to patches.json")]
    class AddPatchCmdLineOptions
    {
        public AddPatchCmdLineOptions()
        {
        }

        [Option('n', "name", HelpText = "patch name")]
        public string Name { get; set; }
    }
}