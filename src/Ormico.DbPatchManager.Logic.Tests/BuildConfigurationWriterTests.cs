using NUnit.Framework;
using Ormico.DbPatchManager.Logic;
using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ormico.DbPatchManager.Logic.Tests
{
    internal class BuildConfigurationWriterTests
    {
        [SetUp]
        public void Setup()
        {
        }

        const string patchesJson = @"{
  ""DatabaseType"": ""sqlserver"",
  ""ConnectionString"": null,
  ""CodeFolder"": ""Code"",
  ""CodeFiles"": [
    ""*.view.sql"",
    ""*.udf.sql"",
    ""*.view2.sql"",
    ""*.udf2.sql"",
    ""*.view3.sql"",
    ""*.udf3.sql"",
    ""*.sproc.sql"",
    ""*.sproc2.sql"",
    ""*.sproc3.sql"",
    ""*.trigger.sql"",
    ""*.trigger2.sql"",
    ""*.trigger3.sql""
  ],
  ""PatchFolder"": ""Patches"",
  ""patches"": [
    {
      ""id"": ""202311262317-6854-first-tables"",
      ""dependsOn"": []
    }
  ]
}";
        const string patchesJsonWSchema = @"{
  ""$schema"": ""http://dbpatch.com/json-schema/ormico-dbpatch-v1.json"",
  ""DatabaseType"": ""sqlserver"",
  ""ConnectionString"": null,
  ""CodeFolder"": ""Code"",
  ""CodeFiles"": [
    ""*.view.sql"",
    ""*.udf.sql"",
    ""*.view2.sql"",
    ""*.udf2.sql"",
    ""*.view3.sql"",
    ""*.udf3.sql"",
    ""*.sproc.sql"",
    ""*.sproc2.sql"",
    ""*.sproc3.sql"",
    ""*.trigger.sql"",
    ""*.trigger2.sql"",
    ""*.trigger3.sql""
  ],
  ""PatchFolder"": ""Patches"",
  ""patches"": [
    {
      ""id"": ""202311262317-6854-first-tables"",
      ""dependsOn"": []
    }
  ]
}";
        const string patchesJsonWUnknownSchema = @"{
  ""$schema"": ""http://dbpatch.com/json-schema/ormico-dbpatch-v2.json"",
  ""DatabaseType"": ""sqlserver"",
  ""PatchFolder"": ""Patches""
}";
        const string shortPatchesJson = @"{
  ""DatabaseType"": ""sqlserver"",
  ""PatchFolder"": ""Patches""
}";
        const string patchesLocalJson = @"{
  ""DatabaseType"": ""sqlserver"",
  ""ConnectionString"": ""Server=devServer;Database=DevDataBase;Trusted_Connection=True;""
}";
        private const string ExpectedReadApplicationExceptionText = "Configuration file does not exist. Call init first.";

        [Test]
        public void DetectSchemaVersion_WhenSchemaPropertyIsSet()
        {
            var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"\databaseproj\patches.json", new MockFileData("{ }") },
                { @"\databaseproj\patches.local.json", new MockFileData("{ }") }
            });
            var bcw = new BuildConfigurationWriter(@"\databaseproj\patches.json", @"\databaseproj\patches.local.json", mockFileSystem);

            var result = bcw.DetectSchemaVersion(patchesJsonWSchema);

            Assert.That(result, Is.EqualTo(SchemaMapper.PatchesSchemaVersionEnum.DbPatchV1));
        }

        [Test]
        public void DetectSchemaVersion_WhenSchemaPropertyIsSetButItIsAnUnknownSchema()
        {
            var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"\databaseproj\patches.json", new MockFileData("{ }") },
                { @"\databaseproj\patches.local.json", new MockFileData("{ }") }
            });
            var bcw = new BuildConfigurationWriter(@"\databaseproj\patches.json", @"\databaseproj\patches.local.json", mockFileSystem);

            var result = bcw.DetectSchemaVersion(patchesJsonWUnknownSchema);

            Assert.That(result, Is.EqualTo(SchemaMapper.PatchesSchemaVersionEnum.Unknown));
        }

        [Test]
        public void DetectSchemaVersion_WhenNoSchemaPropertyIsSetDetectSchemaByOtherProperties()
        {
            var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"\databaseproj\patches.json", new MockFileData("{ }") },
                { @"\databaseproj\patches.local.json", new MockFileData("{ }") }
            });
            var bcw = new BuildConfigurationWriter(@"\databaseproj\patches.json", @"\databaseproj\patches.local.json", mockFileSystem);

            var result = bcw.DetectSchemaVersion(patchesJson);

            Assert.That(result, Is.EqualTo(SchemaMapper.PatchesSchemaVersionEnum.DbPatchV1));
        }

        [Test]
        public void DetectSchemaVersion_WhenNoSchemaPropertyIsSetAndThereArentEnoughOtherPropertiesToDetect()
        {
            var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"\databaseproj\patches.json", new MockFileData("{ }") },
                { @"\databaseproj\patches.local.json", new MockFileData("{ }") }
            });
            var bcw = new BuildConfigurationWriter(@"\databaseproj\patches.json", @"\databaseproj\patches.local.json", mockFileSystem);

            var result = bcw.DetectSchemaVersion(shortPatchesJson);

            Assert.That(result, Is.EqualTo(SchemaMapper.PatchesSchemaVersionEnum.Unknown));

            Assert.Pass();
        }

        [Test]
        public void Read_WhenFileDoesNotExist()
        {
            //todo: use https://github.com/TestableIO/System.IO.Abstractions/blob/main/tests/TestableIO.System.IO.Abstractions.TestingHelpers.Tests/MockFileWriteAllTextTests.cs
            // for examples on how to uses MockFileSystem to test file system operations

            // Arrange
            var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"\databaseproj\patches.local.json", new MockFileData("{ }") }
            });

            // Act
            //todo: test if null is passed to the constructor as patch and local patch file paths
            var bcw = new BuildConfigurationWriter(@"\databaseproj\patches.json", @"\databaseproj\patches.local.json", mockFileSystem);

            // Assert
            Assert.Throws(Is.TypeOf<ApplicationException>().And.Message.EqualTo(ExpectedReadApplicationExceptionText),
                () => bcw.Read());
        }

        [Test]
        public void Read_WhenFileExists()
        {
            var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"\databaseproj\patches.json", new MockFileData(patchesJson) },
                { @"\databaseproj\patches.local.json", new MockFileData("{ }") }
            });
            var bcw = new BuildConfigurationWriter(@"\databaseproj\patches.json", @"\databaseproj\patches.local.json", mockFileSystem);

            var result = bcw.Read();

            Assert.That(result.DatabaseType, Is.EqualTo("sqlserver"));
            Assert.That(result.CodeFiles[2], Is.EqualTo("*.view2.sql"));
            Assert.That(result.CodeFolder, Is.EqualTo("Code"));
            Assert.That(result.PatchFolder, Is.EqualTo("Patches"));
            Assert.That(result.patches.Count, Is.EqualTo(1));
        }

        [Test]
        public void Read_WhenNoLocalFileExists()
        {
            var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"\databaseproj\patches.json", new MockFileData(patchesJson) }
            });
            var bcw = new BuildConfigurationWriter(@"\databaseproj\patches.json", @"\databaseproj\patches.local.json", mockFileSystem);

            var result = bcw.Read();

            Assert.That(result.DatabaseType, Is.EqualTo("sqlserver"));
            Assert.That(result.CodeFiles[2], Is.EqualTo("*.view2.sql"));
            Assert.That(result.CodeFolder, Is.EqualTo("Code"));
            Assert.That(result.PatchFolder, Is.EqualTo("Patches"));
            Assert.That(result.patches.Count, Is.EqualTo(1));
        }

        [Test]
        public void Read_WhenLocalFileExists()
        {
            var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"\databaseproj\patches.json", new MockFileData(patchesJson) },
                { @"\databaseproj\patches.local.json", new MockFileData(patchesLocalJson) }
            });
            var bcw = new BuildConfigurationWriter(@"\databaseproj\patches.json", @"\databaseproj\patches.local.json", mockFileSystem);

            var result = bcw.Read();

            Assert.That(result.DatabaseType, Is.EqualTo("sqlserver"));
            Assert.That(result.CodeFiles[2], Is.EqualTo("*.view2.sql"));
            Assert.That(result.CodeFolder, Is.EqualTo("Code"));
            Assert.That(result.PatchFolder, Is.EqualTo("Patches"));
            Assert.That(result.ConnectionString, Is.EqualTo("Server=devServer;Database=DevDataBase;Trusted_Connection=True;"));
            Assert.That(result.patches.Count, Is.EqualTo(1));
        }

        const string MainPath = @"\databaseproj\patches.json";
        const string LocalPath = @"\databaseproj\patches.local.json";

        static BuildConfigurationWriter WriterFor(string mainJson, string localJson = null)
        {
            return new BuildConfigurationWriter(MainPath, LocalPath, FileSystemFor(mainJson, localJson));
        }

        [Test]
        public void Read_WhenOptionsAndDependsOnContainNonStrings_ConvertsToStrings()
        {
            var bcw = WriterFor(@"{""patches"":[{""id"":""1"",""dependsOn"":[]},{""id"":""2"",""dependsOn"":[1]}],""Options"":{""CommandTimeout"":30,""Flag"":true}}");

            var result = bcw.Read();

            Assert.That(result.Options["CommandTimeout"], Is.EqualTo("30"));
            Assert.That(result.Options["Flag"], Is.EqualTo("true"));
            Assert.That(result.patches[1].DependsOn[0], Is.SameAs(result.patches[0]));
        }

        [Test]
        public void Read_WithDependencyChain_PopulatesDependsOnAndChildren()
        {
            var bcw = WriterFor(@"{""patches"":[
                {""id"":""a"",""dependsOn"":[]},
                {""id"":""b"",""dependsOn"":[""a""]},
                {""id"":""c"",""dependsOn"":[""b""]},
                {""id"":""d"",""dependsOn"":[""a"",""c""]}]}");

            var result = bcw.Read();

            var a = result.patches[0];
            var b = result.patches[1];
            var c = result.patches[2];
            var d = result.patches[3];
            Assert.That(a.Children, Is.EquivalentTo(new[] { b, d }));
            Assert.That(c.DependsOn, Is.EqualTo(new[] { b }));
            Assert.That(d.DependsOn, Is.EquivalentTo(new[] { a, c }));
            Assert.That(b.Children[0], Is.SameAs(c));
            Assert.That(d.DependsOn[0], Is.SameAs(a));
        }

        [Test]
        public void Read_WithManyPatches_CompletesQuickly()
        {
            const int count = 5000;
            var items = Enumerable.Range(0, count).Select(i =>
                i == 0 ? @"{""id"":""p0"",""dependsOn"":[]}" : $@"{{""id"":""p{i}"",""dependsOn"":[""p{i - 1}""]}}");
            var bcw = WriterFor(@"{""patches"":[" + string.Join(",", items) + "]}");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = bcw.Read();
            sw.Stop();

            Assert.That(sw.Elapsed, Is.LessThan(TimeSpan.FromSeconds(2)));
            Assert.That(result.patches[count - 1].DependsOn[0].Id, Is.EqualTo($"p{count - 2}"));
        }

        [Test]
        public void Read_WhenOptionsIsArray_ThrowsDbPatchManagerException()
        {
            var bcw = WriterFor(@"{""patches"":[],""Options"":[{""Key"":""a"",""Value"":""b""}]}");

            var ex = Assert.Throws<Ormico.DbPatchManager.Common.DbPatchManagerException>(() => bcw.Read());

            Assert.That(ex.Message, Does.Contain("Options"));
        }

        [Test]
        public void Read_WhenMainFileRepeatsKey_UsesLastValue()
        {
            var bcw = WriterFor(@"{""CodeFolder"":""first"",""CodeFolder"":""last"",""patches"":[]}");

            var result = bcw.Read();

            Assert.That(result.CodeFolder, Is.EqualTo("last"));
        }

        [Test]
        public void Read_WhenNestedObjectRepeatsKey_UsesLastValue()
        {
            var bcw = WriterFor(@"{""patches"":[{""id"":""first"",""id"":""last"",""dependsOn"":[]}],""Options"":{""a"":""first"",""a"":""last""}}");

            var result = bcw.Read();

            Assert.That(result.Options["a"], Is.EqualTo("last"));
            Assert.That(result.patches[0].Id, Is.EqualTo("last"));
        }

        [Test]
        public void Read_WhenLocalFileRepeatsKey_UsesLastValue()
        {
            var bcw = WriterFor(patchesJson, @"{""ConnectionString"":""first"",""ConnectionString"":""last""}");

            var result = bcw.Read();

            Assert.That(result.ConnectionString, Is.EqualTo("last"));
        }

        [Test]
        public void Write_WhenLocalFileRepeatsKey_OmitsItFromMainFile()
        {
            var fileSystem = FileSystemFor(patchesJson, @"{""ConnectionString"":""first"",""ConnectionString"":""last""}");
            var bcw = new BuildConfigurationWriter(MainPath, LocalPath, fileSystem);
            var config = bcw.Read();

            bcw.Write(config);

            Assert.That(fileSystem.File.ReadAllText(MainPath), Does.Not.Contain("ConnectionString"));
        }

        [Test]
        public void Read_WhenLocalFileHasComments_Merges()
        {
            var bcw = WriterFor(patchesJson, @"{
  // dev only
  ""ConnectionString"": ""Server=dev;"",
}");

            var result = bcw.Read();

            Assert.That(result.ConnectionString, Is.EqualTo("Server=dev;"));
        }

        const string newtonsoftFile = @"{
  ""DatabaseType"": ""sqlserver"",
  ""ConnectionString"": null,
  ""CodeFolder"": ""Code"",
  ""CodeFiles"": [
    ""*.view.sql""
  ],
  ""PatchFolder"": ""Patches"",
  ""Options"": null,
  ""patches"": [
    {
      ""id"": ""p1"",
      ""dependsOn"": []
    },
    {
      ""id"": ""p2"",
      ""dependsOn"": [
        ""p1""
      ]
    }
  ]
}";

        static MockFileSystem FileSystemFor(string mainJson, string localJson = null)
        {
            var files = new Dictionary<string, MockFileData> { { MainPath, new MockFileData(mainJson) } };
            if (localJson != null)
            {
                files.Add(LocalPath, new MockFileData(localJson));
            }
            return new MockFileSystem(files);
        }

        static string Normalize(string text) => text.Replace("\r\n", "\n");

        [TestCase(null)]
        [TestCase(@"{""ConnectionString"":""x""}")]
        public void Write_ThenRead_WithOptions_RoundTrips(string localJson)
        {
            var fileSystem = FileSystemFor(patchesJson, localJson);
            var bcw = new BuildConfigurationWriter(MainPath, LocalPath, fileSystem);
            var config = bcw.Read();
            config.Options = new Dictionary<string, string> { { "CommandTimeout", "30" } };

            bcw.Write(config);
            bcw.Write(bcw.Read());
            var result = bcw.Read();

            Assert.That(result.Options["CommandTimeout"], Is.EqualTo("30"));
            Assert.That(fileSystem.File.ReadAllText(MainPath), Does.Contain(@"""Options"": {"));
        }

        [Test]
        public void Write_WithControlCharactersInValues_ProducesValidJson()
        {
            const string script = "line1\nline2\t\"q\"\\";
            const string patchId = "patch\tone";
            var bcw = WriterFor(patchesJson);
            var config = bcw.Read();
            config.Options = new Dictionary<string, string> { { "Script", script } };
            config.patches = new List<Patch> { new Patch(patchId, new List<Patch>()) };

            bcw.Write(config);
            var result = bcw.Read();

            Assert.That(result.Options["Script"], Is.EqualTo(script));
            Assert.That(result.patches[0].Id, Is.EqualTo(patchId));
        }

        [Test]
        public void Write_WithSpecialCharacters_WritesReadableJson()
        {
            const string connectionString = "Server=a;Pwd=x&y+z<1>;Name=Zoë";
            var fileSystem = FileSystemFor(patchesJson);
            var bcw = new BuildConfigurationWriter(MainPath, LocalPath, fileSystem);
            var config = bcw.Read();
            config.ConnectionString = connectionString;

            bcw.Write(config);

            Assert.That(fileSystem.File.ReadAllText(MainPath), Does.Contain(connectionString));
        }

        [Test]
        public void Write_RoundTripOfNewtonsoftFile_IsUnchanged()
        {
            var fileSystem = FileSystemFor(newtonsoftFile);
            var bcw = new BuildConfigurationWriter(MainPath, LocalPath, fileSystem);

            bcw.Write(bcw.Read());

            Assert.That(Normalize(fileSystem.File.ReadAllText(MainPath)), Is.EqualTo(Normalize(newtonsoftFile)));
        }

        [Test]
        public void Write_WhenLocalFileHasKey_OmitsItFromMainFile()
        {
            var fileSystem = FileSystemFor(patchesJson, @"{""ConnectionString"":null}");
            var bcw = new BuildConfigurationWriter(MainPath, LocalPath, fileSystem);
            var config = bcw.Read();
            config.ConnectionString = "secret";

            bcw.Write(config);

            Assert.That(fileSystem.File.ReadAllText(MainPath), Does.Not.Contain("ConnectionString"));
        }

        [TestCase(null)]
        [TestCase("{ }")]
        public void Write_WithDuplicatePatches_WritesEachIdOnce(string localJson)
        {
            var fileSystem = FileSystemFor(patchesJson, localJson);
            var bcw = new BuildConfigurationWriter(MainPath, LocalPath, fileSystem);
            var config = bcw.Read();
            config.patches = new List<Patch> { new Patch("p1", new List<Patch>()), new Patch("p1", new List<Patch>()) };

            bcw.Write(config);

            var patches = System.Text.Json.Nodes.JsonNode.Parse(fileSystem.File.ReadAllText(MainPath))["patches"].AsArray();
            Assert.That(patches.Count, Is.EqualTo(1));
        }
    }
}
