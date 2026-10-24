using NUnit.Framework;
using Ormico.DbPatchManager.Logic;
using Ormico.DbPatchManager.Common;
using System;

namespace Ormico.DbPatchManager.Logic.Tests
{
    internal class OdbcDatabaseTestConnectionTests
    {
        [Test]
        public void TestConnection_DoesNotRequireOverrideScripts_ConnectFailsGracefully()
        {
            var odbcDb = new OdbcDatabase();

            var options = new DatabaseOptions
            {
                ConnectionString = "Dsn=NonExistentDsn"
            };

            Assert.Throws<ApplicationException>(() => odbcDb.Connect(options));
        }

        [Test]
        public void TestConnection_WithInvalidOdbcDriver_ReturnsFailureViaService()
        {
            var service = new TestConnectionService();

            var result = service.Test("Dsn=NonExistentODBC", "odbc");

            Assert.That(result.Success, Is.False);
        }
    }
}
