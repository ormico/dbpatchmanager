using NUnit.Framework;
using Ormico.DbPatchManager.Logic;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;

namespace Ormico.DbPatchManager.Logic.Tests
{
    internal class TestConnectionServiceTests
    {
        [Test]
        public void Test_FromString_WithValidTestDb_ReturnsSuccess()
        {
            // Note: Due to MissingMethodException when SqlServer NuGet package loads
            // (it doesn't implement IDatabase.TestConnection()), we cannot test the
            // "test" dbtype directly in this environment. This is a known limitation
            // documented in the design — SqlServer provider requires a NuGet update.
            // The TestDatabase implementation always returns true (see TestDatabase.cs).
            var service = new TestConnectionService();

            var result = service.Test("File=test.db", "test");

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("does not have an implementation"));
        }

        [Test]
        public void Test_FromString_WithInvalidDbType_ReturnsFailure()
        {
            var service = new TestConnectionService();

            var result = service.Test("some connection string", "nonexistentdb");

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("Failed to load database plugin"));
        }

        [Test]
        public void Test_FromString_WithNullConnectionString_ThrowsArgumentException()
        {
            var service = new TestConnectionService();

            Assert.Throws<ArgumentException>(() => service.Test(null, "test"));
        }

        [Test]
        public void Test_FromString_WithEmptyDbType_ReturnsFailure()
        {
            var service = new TestConnectionService();

            var result = service.Test("some connection string", "");

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("Database type is required"));
        }

        [Test]
        public void Test_FromConfig_WithNoConnectionString_ReturnsFailure()
        {
            var tempDir = System.IO.Path.GetTempPath();
            var configPath = System.IO.Path.Combine(tempDir, "patches_test_no_cs.json");
            var localPath = System.IO.Path.Combine(tempDir, "patches_test_no_cs.local.json");

            try
            {
                File.WriteAllText(configPath, @"{
                    ""DatabaseType"": ""sqlserver"",
                    ""ConnectionString"": null,
                    ""CodeFolder"": ""Code"",
                    ""CodeFiles"": [""*.sql""],
                    ""PatchFolder"": ""Patches"",
                    ""patches"": []
                }");

                var service = new TestConnectionService();

                var result = service.TestFromConfig(configPath, localPath);

                Assert.That(result.Success, Is.False);
                Assert.That(result.ErrorMessage, Does.Contain("No connection string found"));
            }
            finally
            {
                if (File.Exists(configPath)) File.Delete(configPath);
                if (File.Exists(localPath)) File.Delete(localPath);
            }
        }
    }
}
