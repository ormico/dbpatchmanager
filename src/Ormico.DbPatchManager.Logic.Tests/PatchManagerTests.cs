using NUnit.Framework;
using Ormico.DbPatchManager.Logic;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;

namespace Ormico.DbPatchManager.Logic.Tests
{
    internal class PatchManagerTests
    {
        const string ConfigFile = @"/databaseproj/patches.json";
        const string LocalConfigFile = @"/databaseproj/patches.local.json";
        const string PatchId = "202401010000-0001-test-patch";
        const string PatchFolder = "/Patches/" + PatchId + "/";

        const string PatchesJson = @"{
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

        RecordingDatabase _db;
        TextWriter _originalOut;
        StringWriter _console;

        [SetUp]
        public void Setup()
        {
            _db = new RecordingDatabase();
            _originalOut = Console.Out;
            _console = new StringWriter();
            Console.SetOut(_console);
        }

        [TearDown]
        public void TearDown()
        {
            Console.SetOut(_originalOut);
            _console.Dispose();
        }

        /// <summary>
        /// Builds a project with one patch folder and an empty code folder, plus any extra files.
        /// </summary>
        static MockFileSystem CreateFileSystem(params (string Path, MockFileData Data)[] extraFiles)
        {
            var files = new Dictionary<string, MockFileData>
            {
                { ConfigFile, new MockFileData(PatchesJson) },
                { LocalConfigFile, new MockFileData("{ }") },
                { "/Code/", new MockDirectoryData() },
                { PatchFolder, new MockDirectoryData() }
            };
            foreach (var (path, data) in extraFiles)
            {
                files[path] = data;
            }
            return new MockFileSystem(files);
        }

        PatchManager CreatePatchManager(MockFileSystem fileSystem)
        {
            return new PatchManager(ConfigFile, LocalConfigFile, new Random(), fileSystem, _ => _db);
        }

        [TestCase("", TestName = "Build_PatchSqlFileIsEmpty_SkipsFileAndLogsPatch")]
        [TestCase("   \n\t  ", TestName = "Build_PatchSqlFileIsWhitespace_SkipsFileAndLogsPatch")]
        [TestCase("-- removed: replaced by a later patch", TestName = "Build_PatchSqlFileIsCommentOnly_SkipsFileAndLogsPatch")]
        public void Build_PatchSqlFileHasNoExecutableContent_SkipsFileAndLogsPatch(string content)
        {
            var patchManager = CreatePatchManager(CreateFileSystem(
                (PatchFolder + "noop.sql", new MockFileData(content))));

            patchManager.Build();

            Assert.That(_db.ExecutedDdl, Is.Empty);
            Assert.That(_db.LoggedPatches, Is.EqualTo(new[] { PatchId }));
        }

        [Test]
        public void Build_PatchHasEmptyAndNonEmptyFiles_ExecutesOnlyNonEmptyFilesInOrder()
        {
            var patchManager = CreatePatchManager(CreateFileSystem(
                (PatchFolder + "1-empty.sql", new MockFileData("")),
                (PatchFolder + "2-create.sql", new MockFileData("CREATE TABLE Users (Id INT)")),
                (PatchFolder + "3-whitespace.sql", new MockFileData("   ")),
                (PatchFolder + "4-comment.sql", new MockFileData("/* nothing */")),
                (PatchFolder + "5-insert.sql", new MockFileData("INSERT INTO Users VALUES (1)"))));

            patchManager.Build();

            Assert.That(_db.ExecutedDdl, Is.EqualTo(new[]
            {
                "CREATE TABLE Users (Id INT)",
                "INSERT INTO Users VALUES (1)"
            }));
            Assert.That(_db.LoggedPatches, Is.EqualTo(new[] { PatchId }));
        }

        [TestCase("", TestName = "Build_CodeFileIsEmpty_SkipsFile")]
        [TestCase("  \r\n ", TestName = "Build_CodeFileIsWhitespace_SkipsFile")]
        [TestCase("-- todo", TestName = "Build_CodeFileIsCommentOnly_SkipsFile")]
        public void Build_CodeFileHasNoExecutableContent_SkipsFile(string content)
        {
            var patchManager = CreatePatchManager(CreateFileSystem(
                ("/Code/a.sproc.sql", new MockFileData(content)),
                ("/Code/b.sproc.sql", new MockFileData("CREATE PROCEDURE B AS SELECT 1"))));

            patchManager.Build();

            Assert.That(_db.ExecutedProgrammability, Is.EqualTo(new[] { "CREATE PROCEDURE B AS SELECT 1" }));
        }

        [Test]
        public void Build_FileSkipped_WritesWarningNamingFile()
        {
            var patchManager = CreatePatchManager(CreateFileSystem(
                (PatchFolder + "noop.sql", new MockFileData("-- nothing")),
                ("/Code/empty.sproc.sql", new MockFileData(""))));

            patchManager.Build();

            var output = _console.ToString();
            Assert.That(output, Does.Match(@"Warning: skipping '.*noop\.sql'"));
            Assert.That(output, Does.Match(@"Warning: skipping '.*empty\.sproc\.sql'"));
        }

        [Test]
        public void Build_PatchFileFails_ThrowsNamingFileAndPatchWithOriginalAsInner()
        {
            var driverError = new InvalidOperationException("driver said no");
            _db.FailWhenScriptContains = "BROKEN";
            _db.FailureToThrow = driverError;
            var patchManager = CreatePatchManager(CreateFileSystem(
                (PatchFolder + "bad.sql", new MockFileData("BROKEN SQL"))));

            var ex = Assert.Throws<ApplicationException>(() => patchManager.Build());

            Assert.That(ex.Message, Does.Contain("bad.sql"));
            Assert.That(ex.Message, Does.Contain(PatchId));
            Assert.That(ex.InnerException, Is.SameAs(driverError));
            Assert.That(_db.LoggedPatches, Is.Empty);
        }

        [Test]
        public void Build_CodeFileFails_ThrowsNamingFileWithOriginalAsInner()
        {
            var driverError = new InvalidOperationException("driver said no");
            _db.FailWhenScriptContains = "BROKEN";
            _db.FailureToThrow = driverError;
            var patchManager = CreatePatchManager(CreateFileSystem(
                ("/Code/bad.sproc.sql", new MockFileData("BROKEN SQL"))));

            var ex = Assert.Throws<ApplicationException>(() => patchManager.Build());

            Assert.That(ex.Message, Does.Contain("bad.sproc.sql"));
            Assert.That(ex.InnerException, Is.SameAs(driverError));
        }

        [Test]
        public void Build_LocalConfigSetsConnectionString_PassesItToDatabase()
        {
            var fileSystem = CreateFileSystem(
                (LocalConfigFile, new MockFileData(@"{ ""ConnectionString"": ""Server=local-override"" }")));
            var patchManager = CreatePatchManager(fileSystem);

            patchManager.Build();

            Assert.That(_db.ConnectedOptions?.ConnectionString, Is.EqualTo("Server=local-override"));
        }

        [Test]
        public void Build_ScriptOverrideHasNoExecutableContent_PassesItToPluginUnchecked()
        {
            // Only some plugins use overrides (SQL Server ignores them), so the plugin decides.
            var patchManager = CreatePatchManager(CreateFileSystem(
                ("/ScriptOverrides/InitPatchTable.sql", new MockFileData("-- todo"))));

            patchManager.Build();

            Assert.That(_db.ConnectedOptions?.InitPatchTableSql, Is.EqualTo("-- todo"));
        }

        [Test]
        public void InitConfig_TestingConstructor_WritesConfigToProvidedFileSystem()
        {
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { "/databaseproj/", new MockDirectoryData() }
            });
            var patchManager = CreatePatchManager(fileSystem);

            patchManager.InitConfig(new InitOptions() { DbType = "TestDatabase" });

            Assert.That(fileSystem.File.Exists(ConfigFile), Is.True);
            Assert.That(fileSystem.Directory.Exists("Patches"), Is.True);
            Assert.That(fileSystem.Directory.Exists("Code"), Is.True);
        }
    }
}
