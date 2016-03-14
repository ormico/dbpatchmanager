using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ormico.DbPatchManager
{
    /// <summary>
    /// Readn and Write DatabaseBuildConfiguration to storage.
    /// </summary>
    public class BuildConfigurationWriter
    {
        public BuildConfigurationWriter(string filePath)
        {
            _filePath = filePath;
        }

        /// <summary>
        /// Use System.IO.Abstraction to make testing easier.
        /// </summary>
        FileSystem _io = new FileSystem();

        /// <summary>
        /// Path and name of file to read and write.
        /// </summary>
        string _filePath;

        /// <summary>
        /// Read DatabaseBuildConfiguration data from file path passed to constructor.
        /// </summary>
        /// <returns></returns>
        public DatabaseBuildConfiguration Read()
        {
            DatabaseBuildConfiguration rc = null;
            if(_io.File.Exists(_filePath))
            {
                //rc = JsonConvert.DeserializeObject<DatabaseBuildConfiguration>(_io.File.ReadAllText(_filePath), _jsonSettings);
                var o = (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.Linq.JToken.Parse(_io.File.ReadAllText(_filePath));
                rc = new DatabaseBuildConfiguration();
                rc.CodeFolder = (string)o["CodeFolder"];
                rc.ConnectionString = (string)o["ConnectionString"];
                rc.DatabaseType = (string)o["DatabaseType"];
                rc.PatchFolder = (string)o["PatchFolder"];

                // options specific to a database plugin
                rc.Options = o["Options"].ToObject<Dictionary<string, string>>();

                // populate patch list
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
                    p.DependsOn = cur.ToList();
                    //todo: double check this query
                    var children = from x in o["patches"]
                                   from d in x["dependsOn"]
                                   from a in patches
                                   where (string)d == p.Id && (string)x["id"] == a.Id
                                   select a;
                    p.Children = children.ToList();
                }
                rc.patches = patches.ToList();
            }
            else
            {
                throw new ApplicationException("Configuration file does not exist. Call init first.");
            }
            return rc;
        }

        public void Write(DatabaseBuildConfiguration buildConfiguration)
        {
            //string data = JsonConvert.SerializeObject(buildConfiguration, Formatting.Indented, _jsonSettings);
            JObject data = JObject.FromObject(new
            {
                DatabaseType = buildConfiguration.DatabaseType,
                ConnectionString = buildConfiguration.ConnectionString,
                CodeFolder = buildConfiguration.CodeFolder,
                PatchFolder = buildConfiguration.PatchFolder,
                Options = buildConfiguration.Options,
                patches = from p in buildConfiguration.patches
                          select new
                          {
                              id = p.Id,
                              dependsOn = p.DependsOn != null?(from d in p.DependsOn
                                          select d.Id):null
                          }
            });

            _io.File.WriteAllText(_filePath, data.ToString());
        }
    }
}
