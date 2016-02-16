﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Ormico.DbPatchManager
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

        //todo: constructor for json.net

        [JsonProperty("id")]
        public string Id { get; protected set; }

        [JsonProperty("dependsOn")]
        public List<Patch> DependsOn { get; protected set; }

        [JsonIgnore]
        public List<Patch> Children { get; protected set; }
    }
}