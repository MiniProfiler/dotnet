#if NET45
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization;

namespace StackExchange.Profiling.Helpers.Net45
{
    /// <summary>
    /// Implements interface similar to AsyncLocal which is not available until .Net 4.6.
    /// 
    /// The implementation is inspired by Ambient Context Logic from here: 
    /// https://github.com/mehdime/DbContextScope/blob/master/Mehdime.Entity/Implementations/DbContextScope.cs
    /// Unlike DbContextScope's implementation this implementation doesn't support serialization/deserialization 
    /// ths not allowing to cross app domain barrier.
    /// </summary>
    internal class AsyncLocal<T>
    where T : class
    {
        public AsyncLocal()
        {
        }

        public AsyncLocal(
            T obj
            )
        {
            Value = obj;
        }

        /// <summary>
        /// Gets or Sets the value.
        /// </summary>
        public T Value
        {
            get
            {
                return (CallContext.LogicalGetData(_id) as ObjectRef)?.Ref;
            }

            set
            {
                CallContext.LogicalSetData(_id, new ObjectRef { Ref = value });
            }
        }

        /// <summary>
        /// Identifies this instance of <see cref="AsyncLocal"/> in CallContext.
        /// </summary>
        private readonly string _id = Guid.NewGuid().ToString();

        [Serializable]
        private class ObjectRef : MarshalByRefObject, ISerializable
        {
            // The special constructor is used to deserialize values.
            public ObjectRef()
            {
            }

            // The special constructor is used to deserialize values.
            public ObjectRef(
                SerializationInfo info,
                StreamingContext context
                )
            {
            }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
            }

            public T Ref { get; set; }
        }
    }
}
#endif
