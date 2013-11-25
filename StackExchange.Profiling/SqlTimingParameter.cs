using System;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Information about a DbParameter used in the sql statement profiled by SqlTiming.
    /// </summary>
    [DataContract]
    public class SqlTimingParameter
    {
        /// <summary>
        /// Parameter name, e.g. "@routeName"
        /// </summary>
        [DataMember(Order = 1)]
        public string Name { get; set; }

        /// <summary>
        /// The value submitted to the database.
        /// </summary>
        [DataMember(Order = 2)]
        public string Value { get; set; }

        /// <summary>
        /// System.Data.DbType, e.g. "String", "Bit"
        /// </summary>
        [DataMember(Order = 3)]
        public string DbType { get; set; }

        /// <summary>
        /// How large the type is, e.g. for string, size could be 4000
        /// </summary>
        [DataMember(Order = 4)]
        public int Size { get; set; }

        /// <summary>
        /// Returns true if this has the same parent  
        /// <see cref="Name"/> and <see cref="Value"/> as <paramref name="obj"/>.
        /// </summary>
        public override bool Equals(object obj)
        {
            var other = obj as SqlTimingParameter;
            return other != null 
                && string.Equals(Name, other.Name) 
                && string.Equals(Value, other.Value);
        }

        /// <summary>
        /// Returns the XOR of certain properties.
        /// </summary>
        public override int GetHashCode()
        {
            int hashcode = Name.GetHashCode();
            
            if (Value != null)
                hashcode ^= Value.GetHashCode();

            return hashcode;
        }

        /// <summary>
        /// Returns name and value for debugging.
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0} = {1} ({2})", Name, Value, DbType);
        }
    }
}
