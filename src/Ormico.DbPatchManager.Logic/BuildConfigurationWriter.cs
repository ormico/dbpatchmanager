using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Ormico.DbPatchManager.Common;

namespace Ormico.DbPatchManager.Logic
{
    /// <summary>
    /// Read and Write DatabaseBuildConfiguration to storage.
    /// </summary>
    public class BuildConfigurationWriter
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath">Path and filename for main settings file.</param>
        /// <param name="localFilePath">Path and filename for local override settings file.</param>
        /// <param name="fileSystem">File system object for unit testing.</param>
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
                var o = ConfigJson.ParseObject(_io.File.ReadAllText(_filePath), _filePath);

                var schemaVer = DetectSchemaVersion(o);
                if (schemaVer == SchemaMapper.PatchesSchemaVersionEnum.Unknown)
                    throw new DbPatchManagerException("Cannot detect patches.json schema version");

                if (_io.File.Exists(_localFilePath))
                {
                    ConfigJson.Merge(o, ConfigJson.ParseObject(_io.File.ReadAllText(_localFilePath), _localFilePath));
                }

                rc = new DatabaseBuildConfiguration();
                rc.CodeFolder = ConfigJson.ReadString(o["CodeFolder"], "CodeFolder");
                rc.CodeFiles = ReadCodeFiles(o["CodeFiles"]);
                rc.ConnectionString = ConfigJson.ReadString(o["ConnectionString"], "ConnectionString");
                rc.DatabaseType = ConfigJson.ReadString(o["DatabaseType"], "DatabaseType");
                rc.PatchFolder = ConfigJson.ReadString(o["PatchFolder"], "PatchFolder");

                // options specific to a database plugin
                rc.Options = ReadOptions(o["Options"]);

                // populate patch list
                rc.patches = ReadPatches(o["patches"]);
            }
            else
            {
                throw new ApplicationException("Configuration file does not exist. Call init first.");
            }
            return rc;
        }

        private static List<string> ReadCodeFiles(JsonNode node)
        {
            if (node == null)
            {
                return new List<string>();
            }

            if (node is not JsonArray array)
            {
                throw new DbPatchManagerException("'CodeFiles' must be an array.");
            }

            return array.Select(f => ConfigJson.ReadString(f, "CodeFiles")).ToList();
        }

        private static Dictionary<string, string> ReadOptions(JsonNode node)
        {
            if (node == null)
            {
                return null;
            }

            if (node is not JsonObject options)
            {
                throw new DbPatchManagerException("'Options' must be an object.");
            }

            return options.ToDictionary(p => p.Key, p => ConfigJson.ReadString(p.Value, p.Key));
        }

        private static List<Patch> ReadPatches(JsonNode node)
        {
            var patches = new List<Patch>();
            if (node == null)
            {
                return patches;
            }

            if (node is not JsonArray array)
            {
                throw new DbPatchManagerException("'patches' must be an array.");
            }

            // if an id is repeated then the first Patch is the one other patches link to
            var patchesById = new Dictionary<string, Patch>(StringComparer.Ordinal);
            var dependsOnById = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (var item in array)
            {
                if (item is not JsonObject patchObject)
                {
                    throw new DbPatchManagerException("'patches' must contain only objects.");
                }

                var patch = new Patch()
                {
                    Id = ConfigJson.ReadString(patchObject["id"], "id")
                };
                patches.Add(patch);

                if (patch.Id != null)
                {
                    patchesById.TryAdd(patch.Id, patch);
                    dependsOnById.TryAdd(patch.Id, new List<string>());
                    dependsOnById[patch.Id].AddRange(ReadDependsOn(patchObject["dependsOn"]));
                }
            }

            var childrenById = new Dictionary<string, List<Patch>>(StringComparer.Ordinal);
            foreach (var (id, dependsOn) in dependsOnById)
            {
                foreach (var dependsOnId in dependsOn.Where(patchesById.ContainsKey))
                {
                    childrenById.TryAdd(dependsOnId, new List<Patch>());
                    childrenById[dependsOnId].Add(patchesById[id]);
                }
            }

            foreach (var p in patches)
            {
                p.DependsOn = new List<Patch>();
                p.Children = new List<Patch>();

                if (p.Id != null)
                {
                    p.DependsOn = dependsOnById[p.Id].Where(patchesById.ContainsKey).Select(d => patchesById[d])
                        .Distinct(new PatchComparer()).ToList();

                    if (childrenById.TryGetValue(p.Id, out var children))
                    {
                        p.Children = children.Distinct(new PatchComparer()).ToList();
                    }
                }
            }

            return patches;
        }

        private static List<string> ReadDependsOn(JsonNode node)
        {
            if (node == null)
            {
                return new List<string>();
            }

            if (node is not JsonArray array)
            {
                throw new DbPatchManagerException("'dependsOn' must be an array.");
            }

            return array.Select(d => ConfigJson.ReadString(d, "dependsOn")).Where(d => d != null).ToList();
        }

        /// <summary>
        /// Detect the schema version of the contents of a patches.json file.
        /// </summary>
        /// <param name="json">Contents of a patches.json file.</param>
        /// <returns>The schema version, or Unknown if it cannot be detected.</returns>
        public SchemaMapper.PatchesSchemaVersionEnum DetectSchemaVersion(string json)
        {
            return DetectSchemaVersion(ConfigJson.ParseObject(json, _filePath));
        }

        private SchemaMapper.PatchesSchemaVersionEnum DetectSchemaVersion(JsonObject o)
        {
            //todo: write unit tests for this method and SchemaMapper class
            var mapper = new SchemaMapper();
            var rc = SchemaMapper.PatchesSchemaVersionEnum.Unknown;

            if (o.ContainsKey("schema"))
            {
                var schemaStr = ConfigJson.ReadString(o["schema"], "schema");
                if (!string.IsNullOrWhiteSpace(schemaStr))
                {
                    rc = mapper.MapSchemaVersion(schemaStr);
                }
            }
            else
            {
                // if no schemaVersion property then try to detect if v1 by checking other properties
                if (o.ContainsKey("patches"))
                {
                    rc = SchemaMapper.PatchesSchemaVersionEnum.DbPatchV1;
                }
            }

            return rc;
        }

        /// <summary>
        /// Write DatabaseBuildConfiguration data to file path passed to constructor.
        /// This method is used for all edits which currently include init and add patch.
        /// The algorithm currently used writes the entire file each time. The algorithm decides whether to 
        /// write each property by checking first to see if the property exists in the local file. If it does
        /// not exist in the local file then the property is written to the main file.
        /// </summary>
        /// <param name="buildConfiguration"></param>
        public void Write(DatabaseBuildConfiguration buildConfiguration)
        {
            string json;

            if (_io.File.Exists(_localFilePath))
            {
                using var localDoc = JsonDocument.Parse(_io.File.ReadAllText(_localFilePath));
                var localO = localDoc.RootElement;
                
                var dataObj = new Dictionary<string, string>();

                //todo: find a way to do this that isn't manual. can you loop over all values in buildConfiguration?
                if (!localO.TryGetProperty("DatabaseType", out _) && buildConfiguration.DatabaseType != null)
                {
                    dataObj["DatabaseType"] = JsonSerializer.Serialize(buildConfiguration.DatabaseType, _jsonOptions);
                }

                if (!localO.TryGetProperty("ConnectionString", out _) && buildConfiguration.ConnectionString != null)
                {
                    dataObj["ConnectionString"] = JsonSerializer.Serialize(buildConfiguration.ConnectionString, _jsonOptions);
                }

                if (!localO.TryGetProperty("CodeFolder", out _) && buildConfiguration.CodeFolder != null)
                {
                    dataObj["CodeFolder"] = JsonSerializer.Serialize(buildConfiguration.CodeFolder, _jsonOptions);
                }

                if (!localO.TryGetProperty("CodeFiles", out _) && buildConfiguration.CodeFiles != null)
                {
                    var node = JsonSerializer.SerializeToNode(buildConfiguration.CodeFiles, _jsonOptions);
                    dataObj["CodeFiles"] = node!.ToJsonString();
                }

                if (!localO.TryGetProperty("PatchFolder", out _) && buildConfiguration.PatchFolder != null)
                {
                    dataObj["PatchFolder"] = JsonSerializer.Serialize(buildConfiguration.PatchFolder, _jsonOptions);
                }

                if (!localO.TryGetProperty("Options", out _) && buildConfiguration.Options != null)
                {
                    var optArray = new List<string>();
                    foreach (var kvp in buildConfiguration.Options)
                    {
                        optArray.Add($"{{\"Key\":\"{EscapeJson(kvp.Key)}\",\"Value\":\"{EscapeJson(kvp.Value)}\"}}");
                    }
                    dataObj["Options"] = "[" + string.Join(",", optArray) + "]";
                }

                if (!localO.TryGetProperty("patches", out _) && buildConfiguration.patches != null)
                {
                    var patchItems = new List<string>();
                    foreach (var p in buildConfiguration.patches)
                    {
                        var depIds = new List<string>();
                        if (p.DependsOn != null)
                        {
                            foreach (var d in p.DependsOn.Distinct(new PatchComparer()))
                            {
                                depIds.Add($"\"{EscapeJson(d.Id)}\"");
                            }
                        }
                        patchItems.Add($"{{\"id\":\"{EscapeJson(p.Id)}\",\"dependsOn\":[{string.Join(",", depIds)}]}}");
                    }
                    dataObj["patches"] = "[" + string.Join(",", patchItems) + "]";
                }

                json = "{" + string.Join(",", dataObj.Select(kvp => $"\"{kvp.Key}\":{kvp.Value}")) + "}";
            }
            else
            {
                var allProps = new Dictionary<string, string>();
                
                if (buildConfiguration.DatabaseType != null)
                    allProps["DatabaseType"] = JsonSerializer.Serialize(buildConfiguration.DatabaseType, _jsonOptions);
                
                if (buildConfiguration.ConnectionString != null)
                    allProps["ConnectionString"] = JsonSerializer.Serialize(buildConfiguration.ConnectionString, _jsonOptions);
                
                if (buildConfiguration.CodeFolder != null)
                    allProps["CodeFolder"] = JsonSerializer.Serialize(buildConfiguration.CodeFolder, _jsonOptions);
                
                if (buildConfiguration.CodeFiles != null)
                {
                    var node = JsonSerializer.SerializeToNode(buildConfiguration.CodeFiles, _jsonOptions);
                    allProps["CodeFiles"] = node!.ToJsonString();
                }
                
                if (buildConfiguration.PatchFolder != null)
                    allProps["PatchFolder"] = JsonSerializer.Serialize(buildConfiguration.PatchFolder, _jsonOptions);
                
                if (buildConfiguration.Options != null)
                {
                    var optArray = new List<string>();
                    foreach (var kvp in buildConfiguration.Options)
                    {
                        optArray.Add($"{{\"Key\":\"{EscapeJson(kvp.Key)}\",\"Value\":\"{EscapeJson(kvp.Value)}\"}}");
                    }
                    allProps["Options"] = "[" + string.Join(",", optArray) + "]";
                }

                if (buildConfiguration.patches != null)
                {
                    var patchItems = new List<string>();
                    foreach (var p in buildConfiguration.patches.Distinct(new PatchComparer()))
                    {
                        var depIds = new List<string>();
                        if (p.DependsOn != null)
                        {
                            foreach (var d in p.DependsOn.Distinct(new PatchComparer()))
                            {
                                depIds.Add($"\"{EscapeJson(d.Id)}\"");
                            }
                        }
                        patchItems.Add($"{{\"id\":\"{EscapeJson(p.Id)}\",\"dependsOn\":[{string.Join(",", depIds)}]}}");
                    }
                    allProps["patches"] = "[" + string.Join(",", patchItems) + "]";
                }

                json = "{" + string.Join(",", allProps.Select(kvp => $"\"{kvp.Key}\":{kvp.Value}")) + "}";
            }

            // write all text creates the file if it doesn't exist
            // but it also truncates the file before writing if it does exist
            // so there will be no data left over from the original contents.
            _io.File.WriteAllText(_filePath, json);
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
