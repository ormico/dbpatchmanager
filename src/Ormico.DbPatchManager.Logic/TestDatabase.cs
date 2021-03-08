using Newtonsoft.Json;
using Ormico.DbPatchManager.Common;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ormico.DbPatchManager.Logic
{

    class TestDatabase : IDatabase
    {
        /// <summary>
        /// Use System.IO.Abstraction to make testing easier.
        /// </summary>
        FileSystem _io = new FileSystem();

        string _fileName;
        TestDb _testDb;

        public void Connect(DatabaseOptions Options)
        {
            DbConnectionStringBuilder csb = new DbConnectionStringBuilder();
            csb.ConnectionString = Options.ConnectionString;
            if (csb.ContainsKey("File"))
            {
                _fileName = csb["File"] as string;
            }
            else
            {
                _fileName = "test.db";
            }
            Load();
        }

        public void ExecuteDDL(string commandText)
        {
            _testDb.DdlLog.Add(string.Format("ExecuteDDL {0}\n{1}", DateTime.Now, commandText));
        }

        public List<InstalledPatchInfo> GetInstalledPatches()
        {
            List<InstalledPatchInfo> rc = new List<InstalledPatchInfo>(_testDb.InstalledPatches);
            return rc;
        }

        public void LogInstalledPatch(string patchId)
        {
            if (!_testDb.InstalledPatches
                .Any(i => string.Equals(i.PatchId, patchId, StringComparison.OrdinalIgnoreCase)))
            {
                _testDb.InstalledPatches.Add(new InstalledPatchInfo()
                {
                    PatchId = patchId,
                    InstalledDate = DateTime.Now
                });
            }
        }

        public void Dispose()
        {
            _testDb.DdlLog.Add(string.Format("Dispose {0}", DateTime.Now));
            Save();
        }

        void Load()
        {
            if(_io.File.Exists(_fileName))
            {
                _testDb = JsonConvert.DeserializeObject<TestDb>(_io.File.ReadAllText(_fileName));
            }
            else
            {
                _testDb = new TestDb();
            }
        }

        void Save()
        {
            _io.File.WriteAllText(_fileName, JsonConvert.SerializeObject(_testDb, Formatting.Indented));
        }

        public void ExecuteProgrammabilityScript(string commandText)
        {
            ExecuteDDL(commandText);
        }

        class TestDb
        {
            public TestDb()
            {
                InstalledPatches = new List<InstalledPatchInfo>();
                DdlLog = new List<string>();
            }

            public List<InstalledPatchInfo> InstalledPatches { get; protected set; }

            public List<string> DdlLog { get; protected set; }
        }
    }
}
