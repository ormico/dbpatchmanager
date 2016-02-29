using Newtonsoft.Json;
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
                rc = JsonConvert.DeserializeObject<DatabaseBuildConfiguration>(_io.File.ReadAllText(_filePath));
            }
            return rc;
        }

        public void Write(DatabaseBuildConfiguration buildConfiguration)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings()
            {
                ReferenceResolverProvider = () => new PatchIdReferenceResolver()
            };
            string data = JsonConvert.SerializeObject(buildConfiguration, Formatting.Indented, settings);
            _io.File.WriteAllText(_filePath, data);
        }
    }
}
