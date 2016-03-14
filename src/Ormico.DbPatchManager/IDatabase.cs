using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ormico.DbPatchManager
{
    public interface IDatabase: IDisposable
    {
        void Connect(DatabaseOptions Options);

        void ExecuteDDL(string commandText);

        List<InstalledPatchInfo> GetInstalledPatches();

        void LogInstalledPatch(string patchId);
    }
}
