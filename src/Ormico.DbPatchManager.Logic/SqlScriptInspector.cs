using System;

namespace Ormico.DbPatchManager.Logic
{
    /// <summary>
    /// Detects SQL scripts that cannot change a database because they contain nothing
    /// but whitespace, empty statements (<c>;</c>) and comments. Some ODBC drivers reject
    /// such scripts, so they are skipped instead of being sent to the database plugin.
    /// </summary>
    /// <remarks>
    /// Detection is deliberately conservative: only standard SQL comments (<c>--</c> to end
    /// of line and non-nested <c>/* */</c> blocks) are recognized. Anything else, including
    /// MySQL <c>#</c> comments, MySQL/MariaDB executable comments (<c>/*!</c>, <c>/*M!</c>),
    /// nested or unterminated block comments, is treated as executable so a script with real
    /// SQL is never skipped.
    /// </remarks>
    public static class SqlScriptInspector
    {
        /// <summary>
        /// Returns true when <paramref name="sql"/> is null, empty, or contains only
        /// whitespace, invisible formatting characters, semicolons and standard SQL comments.
        /// </summary>
        /// <param name="sql">Script text to inspect.</param>
        public static bool IsEmptyOrCommentOnly(string sql)
        {
            if (sql == null)
            {
                return true;
            }

            int i = 0;
            while (i < sql.Length)
            {
                char c = sql[i];
                // a lone ';' is an empty statement, which MySQL rejects with "Query was empty"
                if (char.IsWhiteSpace(c) || IsInvisible(c) || c == ';')
                {
                    i++;
                }
                else if (c == '-' && Next(sql, i) == '-')
                {
                    int eol = sql.IndexOf('\n', i);
                    i = eol < 0 ? sql.Length : eol + 1;
                }
                else if (c == '/' && Next(sql, i) == '*')
                {
                    int bodyStart = i + 2;
                    if (IsExecutableComment(sql, bodyStart))
                    {
                        return false;
                    }

                    int end = sql.IndexOf("*/", bodyStart, StringComparison.Ordinal);
                    if (end < 0 || sql.IndexOf("/*", bodyStart, end - bodyStart, StringComparison.Ordinal) >= 0)
                    {
                        // unterminated or nested: comment rules differ by database, let the driver decide
                        return false;
                    }
                    i = end + 2;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        static char Next(string sql, int i)
        {
            return i + 1 < sql.Length ? sql[i + 1] : '\0';
        }

        /// <summary>
        /// MySQL <c>/*!</c> and MariaDB <c>/*M!</c> comments are executed by those servers.
        /// </summary>
        static bool IsExecutableComment(string sql, int bodyStart)
        {
            return string.CompareOrdinal(sql, bodyStart, "!", 0, 1) == 0 ||
                   string.CompareOrdinal(sql, bodyStart, "M!", 0, 2) == 0;
        }

        /// <summary>
        /// Zero-width and byte order mark characters that char.IsWhiteSpace does not cover.
        /// Always write these as \u escapes: typed directly into source they cannot be seen
        /// in review and an editor or formatter could strip them.
        /// </summary>
        static bool IsInvisible(char c)
        {
            return c == '\uFEFF' || // byte order mark (zero width no-break space)
                   c == '\u200B' || // zero width space
                   c == '\u200C' || // zero width non-joiner
                   c == '\u200D' || // zero width joiner
                   c == '\u2060';   // word joiner
        }
    }
}
