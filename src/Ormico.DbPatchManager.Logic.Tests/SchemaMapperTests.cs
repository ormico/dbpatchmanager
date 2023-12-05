using NUnit.Framework;
using Ormico.DbPatchManager.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ormico.DbPatchManager.Logic.Tests
{
    internal class SchemaMapperTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void MapSchemaVersion_WhenSchemaVersionExists()
        {
            var schemaMapper = new SchemaMapper();
            var enumResult = schemaMapper.MapSchemaVersion(SchemaMapper.PatchesSchemaVersionId.DbPatchV1);

            Assert.That(enumResult, Is.EqualTo(SchemaMapper.PatchesSchemaVersionEnum.DbPatchV1));
        }

        [Test]
        public void MapSchemaVersion_WhenSchemaVersionDoesNotExist()
        {
            var schemaMapper = new SchemaMapper();
            var enumResult = schemaMapper.MapSchemaVersion("https://example.com/json-schema/does-not-exist.json");

            Assert.That(enumResult, Is.EqualTo(SchemaMapper.PatchesSchemaVersionEnum.Unknown));
        }
    }
}
