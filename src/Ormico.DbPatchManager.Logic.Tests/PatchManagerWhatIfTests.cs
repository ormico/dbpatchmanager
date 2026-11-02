using NUnit.Framework;
using Ormico.DbPatchManager.Logic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Ormico.DbPatchManager.Logic.Tests
{
    internal class PatchManagerWhatIfTests
    {
        private string _testDir;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), $"dbpatch-test-whatif-{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDir);
            Directory.CreateDirectory(Path.Combine(_testDir, "Patches"));
            Directory.CreateDirectory(Path.Combine(_testDir, "Code"));

            string codeFolder = _testDir.Replace('\\', '/');
            string patchesFolder = _testDir.Replace('\\', '/') + "/Patches";

            File.WriteAllText(Path.Combine(_testDir, "patches.json"), $@"{{
  ""DatabaseType"": ""TestDatabase"",
  ""ConnectionString"": null,
  ""CodeFolder"": ""{codeFolder}/Code"",
  ""CodeFiles"": [ ""*.sql"" ],
  ""PatchFolder"": ""{patchesFolder}"",
  ""patches"": [
    {{
      ""id"": ""20240101-0001-first-patch"",
      ""dependsOn"": []
    }},
    {{
      ""id"": ""20240102-0002-second-patch"",
      ""dependsOn"": [ ""20240101-0001-first-patch"" ]
    }}
  ]
}}");
            File.WriteAllText(Path.Combine(_testDir, "patches.local.json"), "{ }");
            Directory.CreateDirectory(Path.Combine(_testDir, "Patches", "20240101-0001-first-patch"));
            File.WriteAllText(Path.Combine(_testDir, "Patches", "20240101-0001-first-patch", "001-create.sql"), "CREATE TABLE Test (Id INT)");
            Directory.CreateDirectory(Path.Combine(_testDir, "Patches", "20240102-0002-second-patch"));
            File.WriteAllText(Path.Combine(_testDir, "Patches", "20240102-0002-second-patch", "001-insert.sql"), "INSERT INTO Test VALUES (1)");
            File.WriteAllText(Path.Combine(_testDir, "Code", "view.main.sql"), "CREATE VIEW TestView AS SELECT * FROM Test");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }

        [Test]
        public void Build_WithWhatIf_PrintsPatchesAndCodeFilesWithoutError()
        {
            // Arrange
            var patchManager = new PatchManager(
                Path.Combine(_testDir, "patches.json"),
                Path.Combine(_testDir, "patches.local.json"));

            var output = new StringWriter();
            Console.SetOut(output);

            // Act
            patchManager.Build(whatIf: true);

            // Assert
            Console.SetOut(TextWriter.Null);
            string result = output.ToString();

            Assert.That(result, Does.Contain("What-If:"));
            Assert.That(result, Does.Contain("20240101-0001-first-patch"));
            Assert.That(result, Does.Contain("20240102-0002-second-patch"));
            Assert.That(result, Does.Contain("001-create.sql"));
            Assert.That(result, Does.Contain("001-insert.sql"));
            Assert.That(result, Does.Contain("view.main.sql"));
        }

        [Test]
        public void Build_WithWhatIf_DoesNotConnectToDatabase()
        {
            // Arrange - config with no valid connection string
            var patchManager = new PatchManager(
                Path.Combine(_testDir, "patches.json"),
                Path.Combine(_testDir, "patches.local.json"));

            var output = new StringWriter();
            Console.SetOut(output);

            // Act — should not throw even though ConnectionString is null
            Assert.DoesNotThrow(() => patchManager.Build(whatIf: true));

            // Assert
            Console.SetOut(TextWriter.Null);
            string result = output.ToString();
            Assert.That(result, Does.Not.Contain("Connection"));
        }

        [Test]
        public void Build_WithoutWhatIf_NormalBehavior()
        {
            // Arrange - use absolute paths and empty patches list
            string codeFolder = _testDir.Replace('\\', '/');
            var testConfigJson = $@"{{
  ""DatabaseType"": ""TestDatabase"",
  ""ConnectionString"": """",
  ""CodeFolder"": ""{codeFolder}/Code"",
  ""CodeFiles"": [ ""*.sql"" ],
  ""PatchFolder"": ""{_testDir.Replace('\\', '/')}/Patches"",
  ""patches"": []
}}";
            File.WriteAllText(Path.Combine(_testDir, "patches.json"), testConfigJson);

            var patchManager = new PatchManager(
                Path.Combine(_testDir, "patches.json"),
                Path.Combine(_testDir, "patches.local.json"));

            var output = new StringWriter();
            Console.SetOut(output);

            // Act — should not throw with empty patches list
            Assert.DoesNotThrow(() => patchManager.Build(whatIf: false));

            Console.SetOut(TextWriter.Null);
        }
    }
}
