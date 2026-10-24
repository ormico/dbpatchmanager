using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ormico.DbPatchManager.Logic
{
    public class Patch
    {
        public Patch()
        {
        }

        public Patch(string Id, List<Patch> DependsOn)
        {
            this.Id = Id;
            this.DependsOn = DependsOn;
        }

        [JsonPropertyName("id")] public string Id { get; /* protected */ set; }

        [JsonPropertyName("dependsOn")]
        public List<Patch> DependsOn { get; /* protected */ set; }

        [JsonIgnore] public List<Patch> Children { get; /* protected */ set; }
    }

    public class PatchComparer : IEqualityComparer<Patch>
    { 
        // Products are equal if their id is equal.
        public bool Equals(Patch x, Patch y)
        {
            //Check whether the compared objects reference the same data.
            if (Object.ReferenceEquals(x, y)) return true;

            //Check whether any of the compared objects is null.
            if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
                return false;

            //Check whether the products' properties are equal.
            return string.Equals(x.Id, y.Id);
        }

        // If Equals() returns true for a pair of objects 
        // then GetHashCode() must return the same value for these objects.

        public int GetHashCode(Patch product)
        {
            //Get hash code for the Name field if it is not null.
            int rc = product?.Id?.GetHashCode()??0;

            //Calculate the hash code for the product.
            return rc;
        }
    }
}
