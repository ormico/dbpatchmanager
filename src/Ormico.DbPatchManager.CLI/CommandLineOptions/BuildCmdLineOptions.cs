using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommandLine;
using CommandLine.Text;

namespace Ormico.DbPatchManager.CLI.CommandLineOptions
{
    [Verb("build", HelpText = "build")]
    class BuildCmdLineOptions
    {
        public BuildCmdLineOptions()
        {
        }
    }
}