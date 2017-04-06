// Entity Framework 6 needs ICloneable
#if !NETSTANDARD
using System;
using System.Data.Common;

namespace StackExchange.Profiling.Data
{
    public partial class ProfiledDbCommand : ICloneable
    {
        /// <summary>
        /// Clone the command, Entity Framework expects this behaviour.
        /// </summary>
        /// <returns>The <see cref="ProfiledDbCommand"/>.</returns>
        object ICloneable.Clone()
        {
            var tail = _command as ICloneable;
            if (tail == null) throw new NotSupportedException("Underlying " + _command.GetType().Name + " is not cloneable");
            return new ProfiledDbCommand((DbCommand)tail.Clone(), _connection, MiniProfiler.Current);
        }
    }
}
#endif
