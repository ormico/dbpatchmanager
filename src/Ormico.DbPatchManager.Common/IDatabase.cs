using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ormico.DbPatchManager.Common
{
    public interface IDatabase: IDisposable
    {
        void Connect(DatabaseOptions Options);

        /// <summary>
        /// Tests whether the database connection is alive without executing any schema changes.
        /// </summary>
        /// <returns>true if the connection succeeds; false otherwise.</returns>
        bool TestConnection();

        void ExecuteDDL(string commandText);

        List<InstalledPatchInfo> GetInstalledPatches();

        void LogInstalledPatch(string patchId);

        void ExecuteProgrammabilityScript(string commandText);
    }
}
