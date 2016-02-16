using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ormico.DbPatchManager
{
    public class PatchIdReferenceResolver : IReferenceResolver
    {
        readonly Dictionary<string, Patch> _patches = new Dictionary<string, Patch>();

        public object ResolveReference(object context, string reference)
        {
            Patch rc = null;
            _patches.TryGetValue(reference, out rc);
            return rc;
        }

        public string GetReference(object context, object value)
        {
            string rc = null;
            Patch p = value as Patch;
            if (p != null)
            {
                _patches[p.Id] = p;
                rc = p.Id;
            }
            return rc;
        }

        public bool IsReferenced(object context, object value)
        {
            bool rc = false;
            Patch p = value as Patch;
            if (p != null)
            {
                rc = _patches.ContainsKey(p.Id);
            }
            return rc;
        }

        public void AddReference(object context, string reference, object value)
        {
            Patch p = value as Patch;
            if (p != null)
            {
                _patches[reference] = p;
            }
        }
    }
}
