using NUnit.Framework;
using Ormico.DbPatchManager.Common;
using Ormico.DbPatchManager.Logic;
using System.Linq;
using System.Text.Json.Nodes;

namespace Ormico.DbPatchManager.Logic.Tests
{
    internal class ConfigJsonTests
    {
        static JsonObject Obj(string json) => JsonNode.Parse(json).AsObject();

        [Test]
        public void ParseObject_WithCommentsAndTrailingCommas_Parses()
        {
            var result = ConfigJson.ParseObject("{ \"a\": 1, // c\n }", "patches.json");

            Assert.That(result["a"].GetValue<int>(), Is.EqualTo(1));
        }

        [TestCase("{ nope")]
        [TestCase("[1]")]
        public void ParseObject_WhenInvalid_ThrowsWithFileName(string json)
        {
            var ex = Assert.Throws<DbPatchManagerException>(() => ConfigJson.ParseObject(json, "patches.json"));

            Assert.That(ex.Message, Does.Contain("patches.json"));
        }

        [Test]
        public void Merge_WhenKeyNeedsEscaping_MergesWithoutError()
        {
            var main = Obj(@"{""Options"":{""Data\\Source"":""a""}}");

            ConfigJson.Merge(main, Obj(@"{""Options"":{""x"":""y""}}"));

            var options = main["Options"].AsObject();
            Assert.That(options.Select(p => p.Key), Is.EquivalentTo(new[] { @"Data\Source", "x" }));
        }

        [Test]
        public void Merge_WhenLocalValueIsNull_KeepsMainValue()
        {
            var main = Obj(@"{""A"":""m""}");

            ConfigJson.Merge(main, Obj(@"{""A"":null}"));

            Assert.That(main["A"].GetValue<string>(), Is.EqualTo("m"));
        }

        [Test]
        public void Merge_WhenScalarDiffers_LocalWins()
        {
            var main = Obj(@"{""A"":""m""}");

            ConfigJson.Merge(main, Obj(@"{""A"":""l""}"));

            Assert.That(main["A"].GetValue<string>(), Is.EqualTo("l"));
        }

        [Test]
        public void Merge_WhenArraysOverlapWithDifferentEscapesOrKeyOrder_UnionsWithoutDuplicates()
        {
            var main = Obj(@"{""CodeFiles"":[""*.view.sql""],""patches"":[{""id"":""p1"",""dependsOn"":[]}]}");
            var local = Obj(@"{""CodeFiles"":[""*.view.sql"",""*.udf.sql""],""patches"":[{ ""dependsOn"" : [ ], ""id"":""p1""}]}");

            ConfigJson.Merge(main, local);

            var codeFiles = main["CodeFiles"].AsArray().Select(n => n.GetValue<string>());
            Assert.That(codeFiles, Is.EqualTo(new[] { "*.view.sql", "*.udf.sql" }));
            Assert.That(main["patches"].AsArray().Count, Is.EqualTo(1));
        }

        [Test]
        public void Merge_WhenLocalHasNewKey_AddsIt()
        {
            var main = Obj("{}");

            ConfigJson.Merge(main, Obj(@"{""ConnectionString"":""x""}"));

            Assert.That(main["ConnectionString"].GetValue<string>(), Is.EqualTo("x"));
        }

        [TestCase(@"""s""", "s")]
        [TestCase("30", "30")]
        [TestCase("1.5", "1.5")]
        [TestCase("true", "true")]
        [TestCase("false", "false")]
        public void ReadString_ForScalarKinds_ReturnsString(string json, string expected)
        {
            var node = Obj($@"{{""v"":{json}}}")["v"];

            Assert.That(ConfigJson.ReadString(node, "v"), Is.EqualTo(expected));
        }

        [Test]
        public void ReadString_WhenNull_ReturnsNull()
        {
            var node = Obj(@"{""v"":null}")["v"];

            Assert.That(ConfigJson.ReadString(node, "v"), Is.Null);
        }

        [TestCase("{}")]
        [TestCase("[]")]
        public void ReadString_WhenObjectOrArray_ThrowsNamingProperty(string json)
        {
            var node = Obj($@"{{""v"":{json}}}")["v"];

            var ex = Assert.Throws<DbPatchManagerException>(() => ConfigJson.ReadString(node, "CodeFolder"));

            Assert.That(ex.Message, Does.Contain("CodeFolder"));
        }

        [Test]
        public void Serialize_WithControlAndSpecialCharacters_IsIndentedValidAndReadable()
        {
            const string value = "a&b+<x> é\tz\n";
            var o = new JsonObject { ["c"] = value };

            var result = ConfigJson.Serialize(o);

            Assert.That(result, Does.Contain(@"""a&b+<x> é\tz\n"""));
            Assert.That(result, Does.Contain("\n"));
            Assert.That(JsonNode.Parse(result)["c"].GetValue<string>(), Is.EqualTo(value));
        }
    }
}
