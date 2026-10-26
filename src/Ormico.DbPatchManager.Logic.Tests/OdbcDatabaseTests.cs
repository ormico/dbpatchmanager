using NUnit.Framework;
using Ormico.DbPatchManager.Common;
using Ormico.DbPatchManager.Logic;
using System;

namespace Ormico.DbPatchManager.Logic.Tests
{
    internal class OdbcDatabaseTests
    {
        static DatabaseOptions ValidOptions()
        {
            return new DatabaseOptions()
            {
                // never opened: Connect must reject bad overrides before using the connection string
                ConnectionString = "Driver={Not A Real Driver};",
                InitPatchTableSql = "CREATE TABLE InstalledPatches (PatchId varchar(100))",
                GetInstalledPatchesSql = "SELECT PatchId, InstalledDate FROM InstalledPatches",
                AddInstalledPatchSql = "INSERT INTO InstalledPatches (PatchId) VALUES (?PatchId?)"
            };
        }

        [TestCase("InitPatchTable.sql", null, TestName = "Connect_InitPatchTableOverrideMissing_ThrowsNamingFile")]
        [TestCase("InitPatchTable.sql", "", TestName = "Connect_InitPatchTableOverrideEmpty_ThrowsNamingFile")]
        [TestCase("AddInstalledPatch.sql", "  ", TestName = "Connect_AddInstalledPatchOverrideWhitespace_ThrowsNamingFile")]
        [TestCase("GetInstalledPatches.sql", "-- todo", TestName = "Connect_GetInstalledPatchesOverrideCommentOnly_ThrowsNamingFile")]
        public void Connect_ScriptOverrideHasNoExecutableContent_ThrowsNamingFile(string fileName, string sql)
        {
            var options = ValidOptions();
            switch (fileName)
            {
                case "InitPatchTable.sql": options.InitPatchTableSql = sql; break;
                case "AddInstalledPatch.sql": options.AddInstalledPatchSql = sql; break;
                case "GetInstalledPatches.sql": options.GetInstalledPatchesSql = sql; break;
            }
            var db = new OdbcDatabase();

            var ex = Assert.Throws<ApplicationException>(() => db.Connect(options));

            Assert.That(ex.Message, Does.Contain(fileName));
        }

        [TestCase("", TestName = "ExecuteDDL_EmptyScript_DoesNotExecute")]
        [TestCase("   \r\n", TestName = "ExecuteDDL_WhitespaceScript_DoesNotExecute")]
        [TestCase("-- placeholder", TestName = "ExecuteDDL_CommentOnlyScript_DoesNotExecute")]
        public void ExecuteDDL_NoExecutableContent_DoesNotExecute(string sql)
        {
            // Never connected: reaching the connection would throw NullReferenceException.
            var db = new OdbcDatabase();

            Assert.DoesNotThrow(() => db.ExecuteDDL(sql));
        }
    }
}
