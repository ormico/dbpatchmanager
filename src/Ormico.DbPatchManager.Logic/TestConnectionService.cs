using Ormico.DbPatchManager.Common;
using System;
using System.Data.Common;

namespace Ormico.DbPatchManager.Logic
{
    /// <summary>
    /// Service for testing database connectivity without applying any patches.
    /// </summary>
    public class TestConnectionService
    {
        /// <summary>
        /// Tests connection using a configuration read from files.
        /// </summary>
        /// <param name="configFileName">Path to main config file (e.g., patches.json).</param>
        /// <param name="localConfigFileName">Path to local override file (e.g., patches.local.json).</param>
        /// <returns>TestConnectionResult with success/failure details.</returns>
        public TestConnectionResult TestFromConfig(string configFileName, string localConfigFileName)
        {
            var cfgWriter = new BuildConfigurationWriter(configFileName, localConfigFileName);
            var config = cfgWriter.Read();

            if (string.IsNullOrWhiteSpace(config.ConnectionString))
            {
                return new TestConnectionResult
                {
                    Success = false,
                    DatabaseType = config.DatabaseType ?? "unknown",
                    ErrorMessage = "No connection string found. Provide one via --connectionstring or configure it in patches.json."
                };
            }

            return Test(config.ConnectionString, config.DatabaseType);
        }

        /// <summary>
        /// Tests connection with an explicit connection string and database type.
        /// </summary>
        /// <param name="connectionString">The connection string to test.</param>
        /// <param name="dbType">Database type identifier (e.g., "sqlserver", "odbc", "test").</param>
        /// <returns>TestConnectionResult with success/failure details.</returns>
        public TestConnectionResult Test(string connectionString, string dbType)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));
            }

            if (string.IsNullOrWhiteSpace(dbType))
            {
                return new TestConnectionResult
                {
                    Success = false,
                    ErrorMessage = "Database type is required. Use --dbtype to specify (e.g., sqlserver, odbc, test)."
                };
            }

            var pluginManager = new PluginManager();
            IDatabase db;

            try
            {
                db = pluginManager.LoadDatabasePlugin(dbType);
            }
            catch (Exception ex)
            {
                return new TestConnectionResult
                {
                    Success = false,
                    DatabaseType = dbType,
                    ErrorMessage = $"Failed to load database plugin '{dbType}': {ex.Message}"
                };
            }

            if (db == null)
            {
                return new TestConnectionResult
                {
                    Success = false,
                    DatabaseType = dbType,
                    ErrorMessage = $"Unknown database type '{dbType}'. Supported types: sqlserver, odbc, test."
                };
            }

            var options = new DatabaseOptions { ConnectionString = connectionString };
            db.Connect(options);

            try
            {
                bool connected = db.TestConnection();

                if (connected)
                {
                    string serverInfo = ExtractServerInfo(connectionString, dbType);
                    return new TestConnectionResult
                    {
                        Success = true,
                        DatabaseType = dbType,
                        ServerInfo = serverInfo
                    };
                }

                return new TestConnectionResult
                {
                    Success = false,
                    DatabaseType = dbType,
                    ErrorMessage = "Database connection test returned false."
                };
            }
            catch (DbPatchManagerException ex)
            {
                return new TestConnectionResult
                {
                    Success = false,
                    DatabaseType = dbType,
                    ErrorMessage = $"Connection test failed: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                // Catches MissingMethodException for SqlServer providers that haven't implemented TestConnection() yet
                return new TestConnectionResult
                {
                    Success = false,
                    DatabaseType = dbType,
                    ErrorMessage = $"Connection test failed: {ex.Message}. If using SqlServer provider, update Ormico.DbPatchManager.SqlServer to the latest version."
                };
            }
        }

        private string ExtractServerInfo(string connectionString, string dbType)
        {
            try
            {
                var csb = new DbConnectionStringBuilder { ConnectionString = connectionString };

                if (csb.ContainsKey("Server") || csb.ContainsKey("Data Source") || csb.ContainsKey("Initial Catalog") || csb.ContainsKey("Database"))
                {
                    string server = (string)(csb["Server"] ?? csb["Data Source"] ?? "unknown");
                    string database = (string)(csb["Initial Catalog"] ?? csb["Database"] ?? "unknown");
                    return $"{server} / {database}";
                }

                if (csb.ContainsKey("DSN"))
                {
                    return $"DSN: {(string)csb["DSN"]}";
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Result of a connection test operation.
    /// </summary>
    public class TestConnectionResult
    {
        public bool Success { get; set; }
        public string DatabaseType { get; set; }
        public string ServerInfo { get; set; }
        public string ErrorMessage { get; set; }
    }
}
