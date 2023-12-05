using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ormico.DbPatchManager.Logic
{
    public class SchemaMapper
    {
        public SchemaMapper()
        {
        }

        public PatchesSchemaVersionEnum MapSchemaVersion(string schemaName)
        {
            PatchesSchemaVersionEnum rc = PatchesSchemaVersionEnum.Unknown;
            if (schemaNameToEnum.ContainsKey(schemaName))
                rc = schemaNameToEnum[schemaName];
            return rc;
        }

        Dictionary<string, PatchesSchemaVersionEnum> schemaNameToEnum = new Dictionary<string, PatchesSchemaVersionEnum>()
        {
            { PatchesSchemaVersionId.DbPatchV1, PatchesSchemaVersionEnum.DbPatchV1 }
        };

        public enum PatchesSchemaVersionEnum
        {
            Unknown = 0,
            DbPatchV1 = 1
        }

        public class PatchesSchemaVersionId
        {
            public const string DbPatchV1 = "http://dbpatch.com/json-schema/ormico-dbpatch-v1.json";
        }
    }
}
