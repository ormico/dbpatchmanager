using NUnit.Framework;
using Ormico.DbPatchManager.Common;
using Ormico.DbPatchManager.Logic;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Text.Json.Nodes;

namespace Ormico.DbPatchManager.Logic.Tests
{
    internal class TestDatabaseTests
    {
        const string DbPath = @"C:\db\test.db";
        static readonly DatabaseOptions Options = new DatabaseOptions() { ConnectionString = @"File=C:\db\test.db" };

        [Test]
        public void Connect_WhenDbFileHasInstalledPatches_LoadsThem()
        {
            var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { DbPath, new MockFileData(@"{""InstalledPatches"":[{""PatchId"":""p1"",""InstalledDate"":""2026-01-01T00:00:00""}],""DdlLog"":[""x""]}") }
            });
            var db = new TestDatabase(mockFileSystem);

            db.Connect(Options);
            var result = db.GetInstalledPatches();

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].PatchId, Is.EqualTo("p1"));
        }

        [Test]
        public void Dispose_ThenConnect_PreservesInstalledPatchesAndDdlLog()
        {
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddDirectory(@"C:\db");
            var first = new TestDatabase(mockFileSystem);
            first.Connect(Options);
            first.LogInstalledPatch("p1");
            first.ExecuteDDL("CREATE");
            first.Dispose();

            var second = new TestDatabase(mockFileSystem);
            second.Connect(Options);
            var installed = second.GetInstalledPatches();
            second.Dispose();

            Assert.That(installed.Count, Is.EqualTo(1));
            Assert.That(installed[0].PatchId, Is.EqualTo("p1"));
            var saved = JsonNode.Parse(mockFileSystem.File.ReadAllText(DbPath));
            Assert.That(saved["DdlLog"].AsArray().Count, Is.GreaterThanOrEqualTo(3));
        }
    }
}
