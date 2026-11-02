using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ormico.DbPatchManager.CLI.CommandLineOptions
{
    [Verb("build", HelpText = "build")]
    class BuildCmdLineOptions
    {
        public BuildCmdLineOptions()
        {
        }

        [Option('w', "whatif", HelpText = "Dry run — list patches and scripts without executing")]
        public bool WhatIf { get; set; }
    }
}
