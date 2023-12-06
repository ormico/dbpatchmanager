using System;

namespace Ormico.DbPatchManager.Common
{
    public class DbPatchManagerException : Exception
    {
        public DbPatchManagerException() : base()
        {
        }

        public DbPatchManagerException(string message) : base(message)
        {
        }

        public DbPatchManagerException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
