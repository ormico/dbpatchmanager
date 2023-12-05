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

            var o = (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.Linq.JToken.Parse(patchesJsonWSchema);

            var result = bcw.DetectSchemaVersion(o);

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

            var o = (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.Linq.JToken.Parse(patchesJsonWUnknownSchema);

            var result = bcw.DetectSchemaVersion(o);

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

            var o = (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.Linq.JToken.Parse(patchesJson);

            var result = bcw.DetectSchemaVersion(o);

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

            var o = (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.Linq.JToken.Parse(shortPatchesJson);

            var result = bcw.DetectSchemaVersion(o);

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
    }
}
