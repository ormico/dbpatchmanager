using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ormico.DbPatchManager
{

    class TestDatabase : IDatabase
    {
        string _fileName;
        TestDb _testDb;

        public void Connect(string ConnectionString)
        {
            DbConnectionStringBuilder csb = new DbConnectionStringBuilder();
            csb.ConnectionString = ConnectionString;
            _fileName = csb["FileName"] as string;

            Load();
        }

        public void ExecuteDDL(string commandText)
        {
            _testDb.DdlLog.Add(string.Format("ExecuteDDL {0}\n{1}", DateTime.Now, commandText));
        }

        public List<InstalledPatchInfo> GetInstalledPatches()
        {
            List<InstalledPatchInfo> rc = _testDb.InstalledPatches;
            return rc;
        }

        public void LogInstalledPatch(string patchId)
        {
            if (!_testDb.InstalledPatches
                .Any(i => string.Equals(i.Id, patchId, StringComparison.OrdinalIgnoreCase)))
            {
                _testDb.InstalledPatches.Add(new InstalledPatchInfo()
                {
                    Id = patchId,
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
            if(File.Exists(_fileName))
            {
                _testDb = JsonConvert.DeserializeObject<TestDb>(File.ReadAllText(_fileName));
            }
            else
            {
                _testDb = new TestDb();
            }
        }

        void Save()
        {
            File.WriteAllText(_fileName, JsonConvert.SerializeObject(_testDb));
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
