using Ormico.DbPatchManager.Cmd.CommandLineOptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ormico.DbPatchManager.Cmd
{
    class Program
    {
        static int Main(string[] args)
        {
            int rc = 0;
            var options = new Options();
            bool parse = CommandLine.Parser.Default.ParseArguments(args, options, ParseArgs);

            if (!parse)
            {
                rc = CommandLine.Parser.DefaultExitCodeFail;
            }
            else
            {
                rc = _rc;
            }

            return rc;
        }

        static int _rc = 0;

        static void ParseArgs(string verb, object subOptions)
        {
            if (StrEq("init", verb))
            {
                _rc = InitBuildSettings(subOptions as InitOptions);
            }
            else if (StrEq("addpatch", verb))
            {
                _rc = AddPatch(subOptions as AddPatchOptions);
            }
            else if (StrEq("build", verb))
            {
                _rc = Build(subOptions as BuildOptions);
            }
            else
            {
                //todo: create custom exception
                throw new ApplicationException(string.Format("Unknown command '{0}'", verb));
            }
        }

        static bool StrEq(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        static int InitBuildSettings(InitOptions options)
        {
            int rc = 0;
            PatchManager manager = new PatchManager(".\\patchManager.json");
            manager.InitConfig(options);

            return rc;
        }

        static int AddPatch(AddPatchOptions options)
        {
            int rc = 0;
            PatchManager manager = new PatchManager(".\\patchManager.json");
            manager.AddPatch(options.Name, new PatchOptions()
            {
            });
            return rc;
        }

        static int Build(BuildOptions options)
        {
            int rc = 0;

            return rc;
        }
        

        /*
        int AddPatch(AddPatchOptions options)
        {
            int rc = 0;

            return rc;
        }
        */
    }
}
