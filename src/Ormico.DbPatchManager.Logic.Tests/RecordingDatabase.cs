using Ormico.DbPatchManager.Common;
using System;
using System.Collections.Generic;

namespace Ormico.DbPatchManager.Logic.Tests
{
    /// <summary>
    /// In-memory IDatabase that records every call so tests can assert on
    /// exactly what PatchManager sent to the database plugin.
    /// </summary>
    internal class RecordingDatabase : IDatabase
    {
        public DatabaseOptions? ConnectedOptions { get; private set; }

        public List<string> ExecutedDdl { get; } = new List<string>();

        public List<string> ExecutedProgrammability { get; } = new List<string>();

        public List<string> LoggedPatches { get; } = new List<string>();

        public List<InstalledPatchInfo> InstalledPatches { get; } = new List<InstalledPatchInfo>();

        /// <summary>
        /// When set, any executed script containing this text throws <see cref="FailureToThrow"/>.
        /// </summary>
        public string FailWhenScriptContains { get; set; }

        public Exception FailureToThrow { get; set; } = new InvalidOperationException("simulated driver failure");

        public void Connect(DatabaseOptions Options)
        {
            ConnectedOptions = Options;
        }

        public void ExecuteDDL(string commandText)
        {
            ThrowIfConfigured(commandText);
            ExecutedDdl.Add(commandText);
        }

        public void ExecuteProgrammabilityScript(string commandText)
        {
            ThrowIfConfigured(commandText);
            ExecutedProgrammability.Add(commandText);
        }

        public List<InstalledPatchInfo> GetInstalledPatches()
        {
            return new List<InstalledPatchInfo>(InstalledPatches);
        }

        public void LogInstalledPatch(string patchId)
        {
            LoggedPatches.Add(patchId);
        }

        public void Dispose()
        {
        }

        void ThrowIfConfigured(string commandText)
        {
            if (FailWhenScriptContains != null && commandText.Contains(FailWhenScriptContains))
            {
                throw FailureToThrow;
            }
        }
    }
}
