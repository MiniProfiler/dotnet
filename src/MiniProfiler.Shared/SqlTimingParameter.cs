using System.Runtime.Serialization;

namespace StackExchange.Profiling
{
    /// <summary>
    /// Information about a DbParameter used in the sql statement profiled by SqlTiming.
    /// </summary>
    [DataContract]
    public class SqlTimingParameter
    {
        /// <summary>
        /// Parameter name, e.g. "routeName"
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
        /// System.Data.ParameterDirection: "Input", "Output", "InputOutput", "ReturnValue"
        /// </summary>
        [DataMember(Order = 5)]
        public string Direction { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether the parameter accepts null values.
        /// </summary>
        [DataMember(Order = 6)]
        public bool IsNullable { get; set; }

        /// <summary>
        /// Returns true if this has the same parent  
        /// <see cref="Name"/> and <see cref="Value"/> as <paramref name="obj"/>.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare.</param>
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
        public override string ToString() => $"{Name} = {Value} ({DbType})";
    }
}
