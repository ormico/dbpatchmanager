using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ormico.DbPatchManager.Common;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;

namespace Ormico.DbPatchManager.Logic
{
    /// <summary>
    /// Readn and Write DatabaseBuildConfiguration to storage.
    /// </summary>
    public class BuildConfigurationWriter
    {
        public BuildConfigurationWriter(string filePath, string localFilePath, IFileSystem fileSystem = null)
        {
            _filePath = filePath;
            _localFilePath = localFilePath;
            _io = fileSystem ?? new FileSystem();
        }

        /// <summary>
        /// Use System.IO.Abstraction to make testing easier.
        /// </summary>
        IFileSystem _io;

        /// <summary>
        /// Path and name of file to read and write.
        /// </summary>
        string _filePath;

        /// <summary>
        /// Path and name of secondary file to read and write.
        /// The local file is intended to contains settings which the user does not want to 
        /// place in the main file. For example, a user may not wish to put the connection string
        /// in the main file which is checked into source control. The connection string could
        /// be placed in he local file which is not checked into source control.
        /// </summary>
        string _localFilePath;

        /// <summary>
        /// Read DatabaseBuildConfiguration data from file path passed to constructor.
        /// </summary>
        /// <returns></returns>
        public DatabaseBuildConfiguration Read()
        {
            DatabaseBuildConfiguration rc = null;
            if(_io.File.Exists(_filePath))
            {
                var o = (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.Linq.JToken.Parse(_io.File.ReadAllText(_filePath));

                var schemaVer = DetectSchemaVersion(o);
                if (schemaVer == SchemaMapper.PatchesSchemaVersionEnum.Unknown)
                    throw new DbPatchManagerException("Cannot detect patches.json schema version");

                if (_io.File.Exists(_localFilePath))
                {
                    var localO = (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.Linq.JToken.Parse(_io.File.ReadAllText(_localFilePath));
                    o.Merge(localO, new JsonMergeSettings()
                    {
                        MergeArrayHandling = MergeArrayHandling.Union
                    });
                }

                rc = new DatabaseBuildConfiguration();
                rc.CodeFolder = (string)o["CodeFolder"];
                rc.CodeFiles = o["CodeFiles"].ToObject<List<string>>();
                rc.ConnectionString = (string)o["ConnectionString"];
                rc.DatabaseType = (string)o["DatabaseType"];
                rc.PatchFolder = (string)o["PatchFolder"];

                // options specific to a database plugin
                rc.Options = o["Options"]?.ToObject<Dictionary<string, string>>();

                // populate patch list
                if (o["patches"] == null)
                {
                    rc.patches = new List<Patch>();
                }
                else
                {   
                    var patches = (from p in o["patches"]
                                 select new Patch()
                                 {
                                     Id = (string)p["id"]
                                 }).ToList();
                    // populate DependsOn
                    foreach(var p in patches)
                    {
                        var cur = from x in o["patches"]
                                    from d in x["dependsOn"]
                                    from a in patches
                                    where (string)x["id"] == p.Id &&
                                        a.Id == (string)d
                                    select a;
                        p.DependsOn = cur.Distinct(new PatchComparer()).ToList();
                        //todo: double check this query
                        var children = from x in o["patches"]
                                       from d in x["dependsOn"]
                                       from a in patches
                                       where (string)d == p.Id && (string)x["id"] == a.Id
                                       select a;
                        p.Children = children.Distinct(new PatchComparer()).ToList();
                    }
                    rc.patches = patches.ToList();
                }
            }
            else
            {
                throw new ApplicationException("Configuration file does not exist. Call init first.");
            }
            return rc;
        }

        public SchemaMapper.PatchesSchemaVersionEnum DetectSchemaVersion(Newtonsoft.Json.Linq.JObject o)
        {
            //todo: write unit tests for this method and SchemaMapper class
            var mapper = new SchemaMapper();
            var rc = SchemaMapper.PatchesSchemaVersionEnum.Unknown;
            var schemaVersionProperty = o.Property("schema");

            if (schemaVersionProperty != null)
            {
                if (!string.IsNullOrWhiteSpace((string)schemaVersionProperty.Value))
                {
                    string schemaStr = (string)schemaVersionProperty.Value;
                    rc = mapper.MapSchemaVersion(schemaStr);
                }
            }
            else
            {
                // if no schemaVersion property then try to detect if v1 by checking other properties
                var patchesProperty = o.Property("patches");

                if (patchesProperty != null)
                {
                    rc = SchemaMapper.PatchesSchemaVersionEnum.DbPatchV1;
                }
            }

            return rc;
        }

        public void Write(DatabaseBuildConfiguration buildConfiguration)
        {
            JObject data;

            //todo: if local file exists, don't write values to patches.json if value exists in patches.local.json
            if (_io.File.Exists(_localFilePath))
            {
                var localO = (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.Linq.JToken.Parse(_io.File.ReadAllText(_localFilePath));
                data = new JObject();

                //todo: find a way to do this that isn't manual. can you loop over all values in buildConfiguration?
                if (localO["DatabaseType"] == null && buildConfiguration.DatabaseType != null)
                {
                    data["DatabaseType"] = buildConfiguration.DatabaseType;
                }

                if (localO["ConnectionString"] == null && buildConfiguration.ConnectionString != null)
                {
                    data["ConnectionString"] = buildConfiguration.ConnectionString;
                }

                if (localO["CodeFolder"] == null && buildConfiguration.CodeFolder != null)
                {
                    data["CodeFolder"] = buildConfiguration.CodeFolder;
                }

                if (localO["CodeFiles"] == null && buildConfiguration.CodeFiles != null)
                {
                    data["CodeFiles"] = JArray.FromObject(buildConfiguration.CodeFiles);
                }

                if (localO["PatchFolder"] == null && buildConfiguration.PatchFolder != null)
                {
                    data["PatchFolder"] = buildConfiguration.PatchFolder;
                }

                if (localO["Options"] == null && buildConfiguration.Options != null)
                {
                    data["Options"] = JArray.FromObject(buildConfiguration.Options);
                }

                if (localO["patches"] == null && buildConfiguration.patches != null)
                {
                    data["patches"] = JArray.FromObject(from p in buildConfiguration.patches
                                      select new
                                      {
                                        id = p.Id,
                                        dependsOn = p.DependsOn != null ? (from d in p.DependsOn
                                            select d.Id) : null
                                      });
                }
            }
            else
            {
                //string data = JsonConvert.SerializeObject(buildConfiguration, Formatting.Indented, _jsonSettings);
                data = JObject.FromObject(new
                {
                    DatabaseType = buildConfiguration.DatabaseType,
                    ConnectionString = buildConfiguration.ConnectionString,
                    CodeFolder = buildConfiguration.CodeFolder,
                    CodeFiles = buildConfiguration.CodeFiles,
                    PatchFolder = buildConfiguration.PatchFolder,
                    Options = buildConfiguration.Options,
                    patches = from p in buildConfiguration.patches
                              select new
                              {
                                  id = p.Id,
                                  dependsOn = p.DependsOn != null?(from d in p.DependsOn.Distinct(new PatchComparer())
                                              select d.Id):null
                              }
                });
            }

            _io.File.WriteAllText(_filePath, data.ToString());
        }
    }
}
