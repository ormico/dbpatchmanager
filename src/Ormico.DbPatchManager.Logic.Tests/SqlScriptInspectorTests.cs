using NUnit.Framework;
using Ormico.DbPatchManager.Logic;

namespace Ormico.DbPatchManager.Logic.Tests
{
    internal class SqlScriptInspectorTests
    {
        [TestCase(null, TestName = "IsEmptyOrCommentOnly_Null_ReturnsTrue")]
        [TestCase("", TestName = "IsEmptyOrCommentOnly_Empty_ReturnsTrue")]
        [TestCase("   \r\n\t  ", TestName = "IsEmptyOrCommentOnly_Whitespace_ReturnsTrue")]
        [TestCase("﻿​‌‍⁠ ", TestName = "IsEmptyOrCommentOnly_InvisibleCharacters_ReturnsTrue")]
        [TestCase("-- placeholder", TestName = "IsEmptyOrCommentOnly_LineCommentWithoutNewline_ReturnsTrue")]
        [TestCase("/* TODO */", TestName = "IsEmptyOrCommentOnly_BlockComment_ReturnsTrue")]
        [TestCase("-- a\r\n/* b\r\n c */\r\n  -- d\n", TestName = "IsEmptyOrCommentOnly_MixedComments_ReturnsTrue")]
        [TestCase("﻿-- removed: replaced by patch 202410250000-0001-fix", TestName = "IsEmptyOrCommentOnly_InvisibleCharacterThenComment_ReturnsTrue")]
        [TestCase(";", TestName = "IsEmptyOrCommentOnly_SemicolonOnly_ReturnsTrue")]
        [TestCase("-- removed\r\n;\r\n ; ;", TestName = "IsEmptyOrCommentOnly_CommentsAndEmptyStatements_ReturnsTrue")]
        public void IsEmptyOrCommentOnly_NoExecutableContent_ReturnsTrue(string sql)
        {
            var result = SqlScriptInspector.IsEmptyOrCommentOnly(sql);

            Assert.That(result, Is.True);
        }

        [TestCase("CREATE TABLE T (Id INT)", TestName = "IsEmptyOrCommentOnly_Statement_ReturnsFalse")]
        [TestCase("-- lead\r\nSELECT 1", TestName = "IsEmptyOrCommentOnly_CommentThenStatement_ReturnsFalse")]
        [TestCase("SELECT 1 -- trailing", TestName = "IsEmptyOrCommentOnly_StatementThenComment_ReturnsFalse")]
        [TestCase("/* a */ SELECT 1", TestName = "IsEmptyOrCommentOnly_BlockCommentThenStatement_ReturnsFalse")]
        [TestCase("; SELECT 1", TestName = "IsEmptyOrCommentOnly_SemicolonThenStatement_ReturnsFalse")]
        [TestCase("# mysql comment", TestName = "IsEmptyOrCommentOnly_HashComment_ReturnsFalse")]
        [TestCase("/*!50003 CREATE TABLE T (Id INT) */", TestName = "IsEmptyOrCommentOnly_MySqlExecutableComment_ReturnsFalse")]
        [TestCase("/*M!100100 CREATE TABLE T (Id INT) */", TestName = "IsEmptyOrCommentOnly_MariaDbExecutableComment_ReturnsFalse")]
        [TestCase("/* outer /* inner */ still outer */", TestName = "IsEmptyOrCommentOnly_NestedBlockComment_ReturnsFalse")]
        [TestCase("/* never closed", TestName = "IsEmptyOrCommentOnly_UnterminatedBlockComment_ReturnsFalse")]
        [TestCase("-", TestName = "IsEmptyOrCommentOnly_SingleDash_ReturnsFalse")]
        public void IsEmptyOrCommentOnly_PossiblyExecutableContent_ReturnsFalse(string sql)
        {
            var result = SqlScriptInspector.IsEmptyOrCommentOnly(sql);

            Assert.That(result, Is.False);
        }
    }
}
