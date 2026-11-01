using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Ormico.DbPatchManager.Common;

namespace Ormico.DbPatchManager.Logic
{
    /// <summary>
    /// JSON handling for the patches.json and patches.local.json configuration files.
    /// </summary>
    internal static class ConfigJson
    {
        // these files are hand edited so accept what users are likely to type
        private static readonly JsonDocumentOptions _documentOptions = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        // these files are read by people and never embedded in html, so don't escape & + < > or non-ascii
        private static readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>
        /// Parse text which must contain a JSON object.
        /// </summary>
        /// <param name="json">Text to parse.</param>
        /// <param name="fileName">Name of the file the text came from, used in error messages.</param>
        /// <returns>The parsed object.</returns>
        /// <exception cref="DbPatchManagerException">The text is not a valid JSON object.</exception>
        public static JsonObject ParseObject(string json, string fileName)
        {
            try
            {
                using var document = JsonDocument.Parse(json, _documentOptions);
                if (ToNode(document.RootElement) is JsonObject rc)
                {
                    return rc;
                }
            }
            catch (JsonException ex)
            {
                // JsonException.Message can include the file contents, which may hold a connection string.
                // Don't pass ex as the inner exception because the CLI prints the inner exception message.
                throw new DbPatchManagerException($"{fileName} is not a valid JSON object: invalid JSON at line {ex.LineNumber + 1}, position {ex.BytePositionInLine + 1}.");
            }

            throw new DbPatchManagerException($"{fileName} is not a valid JSON object: the root value must be an object.");
        }

        // JsonNode.Parse builds objects on first use and throws then if a name is repeated.
        // Building them here means the last value of a repeated name wins, as it did with Json.NET.
        private static JsonNode ToNode(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var o = new JsonObject();
                    foreach (var property in element.EnumerateObject())
                    {
                        o[property.Name] = ToNode(property.Value);
                    }
                    return o;
                case JsonValueKind.Array:
                    var a = new JsonArray();
                    foreach (var item in element.EnumerateArray())
                    {
                        a.Add(ToNode(item));
                    }
                    return a;
                case JsonValueKind.Null:
                    return null;
                default:
                    return JsonValue.Create(element.Clone());
            }
        }

        /// <summary>
        /// Merge local into main. Objects merge recursively, arrays are combined without duplicates,
        /// other local values replace the main value, and local nulls are ignored.
        /// </summary>
        /// <param name="main">Object to merge into. This object is modified.</param>
        /// <param name="local">Object to merge from.</param>
        public static void Merge(JsonObject main, JsonObject local)
        {
            foreach (var (key, localValue) in local)
            {
                if (localValue == null)
                {
                    continue;
                }

                var mainValue = main[key];
                if (mainValue is JsonObject mainObject && localValue is JsonObject localObject)
                {
                    Merge(mainObject, localObject);
                }
                else if (mainValue is JsonArray mainArray && localValue is JsonArray localArray)
                {
                    Union(mainArray, localArray);
                }
                else
                {
                    main[key] = localValue.DeepClone();
                }
            }
        }

        private static void Union(JsonArray main, JsonArray local)
        {
            foreach (var item in local)
            {
                if (!main.Any(m => JsonNode.DeepEquals(m, item)))
                {
                    main.Add(item?.DeepClone());
                }
            }
        }

        /// <summary>
        /// Read a value as a string. Numbers and booleans are converted to their JSON text.
        /// </summary>
        /// <param name="node">Value to read. Null represents a missing value or a JSON null.</param>
        /// <param name="propertyName">Name of the property being read, used in error messages.</param>
        /// <returns>The string, or null if there is no value.</returns>
        /// <exception cref="DbPatchManagerException">The value is an object or an array.</exception>
        public static string ReadString(JsonNode node, string propertyName)
        {
            if (node == null)
            {
                return null;
            }

            switch (node.GetValueKind())
            {
                case JsonValueKind.String:
                    return node.GetValue<string>();
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return node.ToJsonString();
                default:
                    throw new DbPatchManagerException($"'{propertyName}' must be a string.");
            }
        }

        /// <summary>
        /// Convert an object to indented JSON text.
        /// </summary>
        /// <param name="o">Object to convert.</param>
        /// <returns>JSON text.</returns>
        public static string Serialize(JsonObject o)
        {
            return o.ToJsonString(_serializerOptions);
        }
    }
}
