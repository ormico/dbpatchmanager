﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;


namespace Ormico.DbPatchManager
{
    class PluginManager
    {
        public IDatabase LoadDatabasePlugin(string PluginType)
        {
            IDatabase rc = null;
            if(string.Equals(PluginType, "TestDatabase", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(PluginType, typeof(TestDatabase).ToString(), StringComparison.OrdinalIgnoreCase))
            {
                rc = new TestDatabase();
            }
            else if (string.Equals(PluginType, "OdbcDatabase", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(PluginType, typeof(OdbcDatabase).ToString(), StringComparison.OrdinalIgnoreCase))
            {
                rc = new OdbcDatabase();
            }
            else
            {
                string[] parts = PluginType.Split(',');
                string fileName, typeName;
                if(parts != null && parts.Length > 0)
                {
                    fileName = parts[0];
                    if(parts.Length > 1)
                    {
                        typeName = parts[1];
                        //todo: what to do if null?

                        // LoadFile() or LoadFrom()
                        Assembly pa = Assembly.LoadFrom(fileName);
                        var t = pa.GetType(typeName);
                        string interfaceName = typeof(IDatabase).ToString();
                        if (t.IsPublic && t.IsClass && t.GetInterface(interfaceName) != null)
                        {
                            rc = pa.CreateInstance(typeName) as IDatabase;
                        }

                    }
                }
            }

            return rc;
        }
    }
}
