using NUnit.Framework;
using Ormico.DbPatchManager.Logic;
using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;

namespace Ormico.DbPatchManager.Logic.Tests
{
    internal class PatchManagerEmptySqlTests
    {
        private const string PatchesJson = @"{
  ""DatabaseType"": ""TestDatabase"",
  ""ConnectionString"": null,
  ""CodeFolder"": ""/Code"",
  ""CodeFiles"": [ ""*.sql"" ],
  ""PatchFolder"": ""/Patches"",
  ""patches"": [
    {
      ""id"": ""202401010000-0001-test-patch"",
      ""dependsOn"": []
    }
  ]
}";

        [Test]
        public void Build_WhenPatchSqlFileIsEmpty_SkipsWithoutError()
        {
            var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"/databaseproj/patches.json", new MockFileData(PatchesJson) },
                { @"/databaseproj/patches.local.json", new MockFileData("{ }") },
                { "/Code/", new MockDirectoryData() },
                { "/Patches/202401010000-0001-test-patch/", new MockDirectoryData() },
                { "/Patches/202401010000-0001-test-patch/empty.sql", new MockFileData("") }
            });

            var patchManager = new PatchManager(@"/databaseproj/patches.json", new Random(), mockFileSystem);

            Assert.DoesNotThrow(() => patchManager.Build());
        }

        [Test]
        public void Build_WhenPatchSqlFileIsWhitespaceOnly_SkipsWithoutError()
        {
            var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"/databaseproj/patches.json", new MockFileData(PatchesJson) },
                { @"/databaseproj/patches.local.json", new MockFileData("{ }") },
                { "/Code/", new MockDirectoryData() },
                { "/Patches/202401010000-0001-test-patch/", new MockDirectoryData() },
                { "/Patches/202401010000-0001-test-patch/whitespace.sql", new MockFileData("   \n\t  ") }
            });

            var patchManager = new PatchManager(@"/databaseproj/patches.json", new Random(), mockFileSystem);

            Assert.DoesNotThrow(() => patchManager.Build());
        }

        [Test]
        public void Build_WhenPatchSqlFileHasContent_CompletesSuccessfully()
        {
            var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"/databaseproj/patches.json", new MockFileData(PatchesJson) },
                { @"/databaseproj/patches.local.json", new MockFileData("{ }") },
                { "/Code/", new MockDirectoryData() },
                { "/Patches/202401010000-0001-test-patch/", new MockDirectoryData() },
                { "/Patches/202401010000-0001-test-patch/create.sql", new MockFileData("CREATE TABLE Test (Id INT)") }
            });

            var patchManager = new PatchManager(@"/databaseproj/patches.json", new Random(), mockFileSystem);

            Assert.DoesNotThrow(() => patchManager.Build());
        }

        [Test]
        public void Build_WhenPatchHasEmptyAndNonEmptySqlFiles_SkipsEmptyExecutesValid()
        {
            var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"/databaseproj/patches.json", new MockFileData(PatchesJson) },
                { @"/databaseproj/patches.local.json", new MockFileData("{ }") },
                { "/Code/", new MockDirectoryData() },
                { "/Patches/202401010000-0001-test-patch/", new MockDirectoryData() },
                { "/Patches/202401010000-0001-test-patch/empty.sql", new MockFileData("") },
                { "/Patches/202401010000-0001-test-patch/create.sql", new MockFileData("CREATE TABLE Users (Id INT)") },
                { "/Patches/202401010000-0001-test-patch/whitespace.sql", new MockFileData("   ") },
                { "/Patches/202401010000-0001-test-patch/insert.sql", new MockFileData("INSERT INTO Users VALUES (1)") }
            });

            var patchManager = new PatchManager(@"/databaseproj/patches.json", new Random(), mockFileSystem);

            Assert.DoesNotThrow(() => patchManager.Build());
        }

        [Test]
        public void Build_WhenCodeSqlFileIsEmpty_SkipsWithoutError()
        {
            var codePatchesJson = PatchesJson.Replace(
                @"""CodeFiles"": [ ""*.sql"" ]",
                @"""CodeFiles"": [ ""empty.sproc.sql"" ]"
            );

            var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"/databaseproj/patches.json", new MockFileData(codePatchesJson) },
                { @"/databaseproj/patches.local.json", new MockFileData("{ }") },
                { "/Code/", new MockDirectoryData() },
                { "/Code/empty.sproc.sql", new MockFileData("") },
                { "/Patches/", new MockDirectoryData() },
                { "/Patches/202401010000-0001-test-patch/", new MockDirectoryData() }
            });

            var patchManager = new PatchManager(@"/databaseproj/patches.json", new Random(), mockFileSystem);

            Assert.DoesNotThrow(() => patchManager.Build());
        }
    }
}
