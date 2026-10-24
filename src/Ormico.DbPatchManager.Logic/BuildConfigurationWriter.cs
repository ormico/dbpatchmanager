using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text.Json;
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

        private static JsonElement MergeJson(JsonElement main, JsonElement local)
        {
            using var mainDoc = JsonDocument.Parse(main.GetRawText());
            using var localDoc = JsonDocument.Parse(local.GetRawText());
            return MergeElements(mainDoc.RootElement, localDoc.RootElement);
        }

        private static JsonElement? TryGetProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop))
                return prop;
            return null;
        }

        private static JsonElement MergeElements(JsonElement main, JsonElement local)
        {
            if (main.ValueKind == JsonValueKind.Object && local.ValueKind == JsonValueKind.Object)
            {
                var merged = new Dictionary<string, string>();
                
                foreach (var prop in main.EnumerateObject())
                {
                    var localProp = TryGetProperty(local, prop.Name);
                    if (localProp.HasValue && localProp.Value.ValueKind != JsonValueKind.Null)
                    {
                        merged[prop.Name] = MergeElements(prop.Value, localProp.Value).GetRawText();
                    }
                    else
                    {
                        merged[prop.Name] = prop.Value.GetRawText();
                    }
                }

                foreach (var prop in local.EnumerateObject())
                {
                    if (!merged.ContainsKey(prop.Name))
                    {
                        merged[prop.Name] = prop.Value.GetRawText();
                    }
                }

                return JsonDocument.Parse("{" + string.Join(",", merged.Select(kvp => $"\"{kvp.Key}\":{kvp.Value}")) + "}").RootElement;
            }

            if (main.ValueKind == JsonValueKind.Array && local.ValueKind == JsonValueKind.Array)
            {
                var mainItems = new List<string>();
                foreach (var item in main.EnumerateArray())
                    mainItems.Add(item.GetRawText());

                var seen = new HashSet<string>(mainItems);
                foreach (var item in local.EnumerateArray())
                {
                    var raw = item.GetRawText();
                    if (!seen.Contains(raw))
                    {
                        seen.Add(raw);
                        mainItems.Add(raw);
                    }
                }

                return JsonDocument.Parse("[" + string.Join(",", mainItems) + "]").RootElement;
            }

            return local.ValueKind != JsonValueKind.Null ? local : main;
        }

        /// <summary>
        /// Read DatabaseBuildConfiguration data from file path passed to constructor.
        /// </summary>
        /// <returns></returns>
        public DatabaseBuildConfiguration Read()
        {
            DatabaseBuildConfiguration rc = null;
            if(_io.File.Exists(_filePath))
            {
                using var mainDoc = JsonDocument.Parse(_io.File.ReadAllText(_filePath));
                var o = mainDoc.RootElement.Clone();

                var schemaVer = DetectSchemaVersion(o);
                if (schemaVer == SchemaMapper.PatchesSchemaVersionEnum.Unknown)
                    throw new DbPatchManagerException("Cannot detect patches.json schema version");

                if (_io.File.Exists(_localFilePath))
                {
                    using var localDoc = JsonDocument.Parse(_io.File.ReadAllText(_localFilePath));
                    o = MergeElements(o, localDoc.RootElement);
                }

                rc = new DatabaseBuildConfiguration();
                
                var codeFolderProp = TryGetProperty(o, "CodeFolder");
                rc.CodeFolder = codeFolderProp?.GetString();

                var codeFilesProp = TryGetProperty(o, "CodeFiles");
                rc.CodeFiles = codeFilesProp?.Deserialize<List<string>>(_jsonOptions) ?? new List<string>();

                var connStringProp = TryGetProperty(o, "ConnectionString");
                rc.ConnectionString = connStringProp?.GetString();

                var dbTypeProp = TryGetProperty(o, "DatabaseType");
                rc.DatabaseType = dbTypeProp?.GetString();

                var patchFolderProp = TryGetProperty(o, "PatchFolder");
                rc.PatchFolder = patchFolderProp?.GetString();

                // options specific to a database plugin
                var optionsProp = TryGetProperty(o, "Options");
                if (optionsProp.HasValue)
                {
                    rc.Options = optionsProp.Value.Deserialize<Dictionary<string, string>>(_jsonOptions);
                }

                // populate patch list
                var patchesProp = TryGetProperty(o, "patches");
                if (!patchesProp.HasValue || patchesProp.Value.ValueKind == JsonValueKind.Null)
                {
                    rc.patches = new List<Patch>();
                }
                else
                {   
                    var patchesArray = patchesProp.Value;
                    var patches = new List<Patch>();
                    
                    foreach (var patchElem in patchesArray.EnumerateArray())
                    {
                        var idProp = TryGetProperty(patchElem, "id");
                        patches.Add(new Patch()
                        {
                            Id = idProp?.GetString()
                        });
                    }

                    // populate DependsOn
                    foreach(var p in patches)
                    {
                        var dependsOnList = new List<Patch>();
                        
                        for (int i = 0; i < patchesArray.GetArrayLength(); i++)
                        {
                            var patchObj = patchesArray[i];
                            var idProp = TryGetProperty(patchObj, "id");
                            var curId = idProp?.GetString();

                            if (curId != p.Id) continue;

                            var dependsOnProp = TryGetProperty(patchObj, "dependsOn");
                            if (dependsOnProp.HasValue && dependsOnProp.Value.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var depElem in dependsOnProp.Value.EnumerateArray())
                                {
                                    var depId = depElem.GetString();
                                    var matched = patches.Where(a => a.Id == depId).Distinct(new PatchComparer()).ToList();
                                    dependsOnList.AddRange(matched);
                                }
                            }
                        }

                        p.DependsOn = dependsOnList.Distinct(new PatchComparer()).ToList();

                        // populate Children
                        var childrenList = new List<Patch>();
                        
                        for (int i = 0; i < patchesArray.GetArrayLength(); i++)
                        {
                            var patchObj = patchesArray[i];
                            var idProp = TryGetProperty(patchObj, "id");
                            var curId = idProp?.GetString();

                            var dependsOnProp = TryGetProperty(patchObj, "dependsOn");
                            if (dependsOnProp.HasValue && dependsOnProp.Value.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var depElem in dependsOnProp.Value.EnumerateArray())
                                {
                                    var depId = depElem.GetString();
                                    if (depId == p.Id)
                                    {
                                        var matched = patches.Where(a => a.Id == curId).Distinct(new PatchComparer()).ToList();
                                        childrenList.AddRange(matched);
                                    }
                                }
                            }
                        }

                        p.Children = childrenList.Distinct(new PatchComparer()).ToList();
                    }

                    rc.patches = patches;
                }
            }
            else
            {
                throw new ApplicationException("Configuration file does not exist. Call init first.");
            }
            return rc;
        }

        public SchemaMapper.PatchesSchemaVersionEnum DetectSchemaVersion(JsonElement o)
        {
            //todo: write unit tests for this method and SchemaMapper class
            var mapper = new SchemaMapper();
            var rc = SchemaMapper.PatchesSchemaVersionEnum.Unknown;
            
            if (o.TryGetProperty("schema", out var schemaVersionProperty))
            {
                var schemaStr = schemaVersionProperty.GetString();
                if (!string.IsNullOrWhiteSpace(schemaStr))
                {
                    rc = mapper.MapSchemaVersion(schemaStr);
                }
            }
            else
            {
                // if no schemaVersion property then try to detect if v1 by checking other properties
                if (o.TryGetProperty("patches", out var patchesProperty))
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
