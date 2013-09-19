using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace StackExchange.Profiling.Data
{
    /// <summary>
    /// The profiled database data reader.
    /// </summary>
    public class ProfiledDbDataReader : DbDataReader
    {
        /// <summary>
        /// The _reader.
        /// </summary>
        private readonly DbDataReader _reader;

        /// <summary>
        /// The _profiler.
        /// </summary>
        private readonly IDbProfiler _profiler;

        /// <summary>
        /// Initialises a new instance of the <see cref="ProfiledDbDataReader"/> class.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="profiler">The profiler.</param>
        public ProfiledDbDataReader(DbDataReader reader, DbConnection connection, IDbProfiler profiler)
        {
            _reader = reader;

            if (profiler != null)
            {
                _profiler = profiler;
            }
        }

        /// <summary>
        /// Gets the depth.
        /// </summary>
        public override int Depth
        {
            get { return _reader.Depth; }
        }

        /// <summary>
        /// Gets the field count.
        /// </summary>
        public override int FieldCount
        {
            get { return _reader.FieldCount; }
        }

        /// <summary>
        /// Gets a value indicating whether has rows.
        /// </summary>
        public override bool HasRows
        {
            get { return _reader.HasRows; }
        }

        /// <summary>
        /// Gets a value indicating whether is closed.
        /// </summary>
        public override bool IsClosed
        {
            get { return _reader.IsClosed; }
        }

        /// <summary>
        /// Gets the records affected.
        /// </summary>
        public override int RecordsAffected
        {
            get { return _reader.RecordsAffected; }
        }

        /// <summary>
        /// The 
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        public override object this[string name]
        {
            get { return _reader[name]; }
        }

        /// <summary>
        /// The 
        /// </summary>
        /// <param name="ordinal">
        /// The ordinal.
        /// </param>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        public override object this[int ordinal]
        {
            get { return _reader[ordinal]; }
        }

        /// <summary>
        /// The close.
        /// </summary>
        public override void Close()
        {
            // this can occur when we're not profiling, but we've inherited from ProfiledDbCommand and are returning a
            // an unwrapped reader from the base command
            if (_reader != null)
            {
                _reader.Close();
            }

            if (_profiler != null)
            {
                _profiler.ReaderFinish(this);
            }
        }

        /// <summary>
        /// The get boolean.
        /// </summary>
        /// <param name="ordinal">
        /// The ordinal.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public override bool GetBoolean(int ordinal)
        {
            return _reader.GetBoolean(ordinal);
        }

        /// <summary>
        /// The get byte.
        /// </summary>
        /// <param name="ordinal">
        /// The ordinal.
        /// </param>
        /// <returns>
        /// The <see cref="byte"/>.
        /// </returns>
        public override byte GetByte(int ordinal)
        {
            return _reader.GetByte(ordinal);
        }

        /// <summary>
        /// The get bytes.
        /// </summary>
        /// <param name="ordinal">
        /// The ordinal.
        /// </param>
        /// <param name="dataOffset">
        /// The data offset.
        /// </param>
        /// <param name="buffer">
        /// The buffer.
        /// </param>
        /// <param name="bufferOffset">
        /// The buffer offset.
        /// </param>
        /// <param name="length">
        /// The length.
        /// </param>
        /// <returns>
        /// The <see cref="long"/>.
        /// </returns>
        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            return _reader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
        }

        /// <summary>
        /// The get char.
        /// </summary>
        /// <param name="ordinal">
        /// The ordinal.
        /// </param>
        /// <returns>
        /// The <see cref="char"/>.
        /// </returns>
        public override char GetChar(int ordinal)
        {
            return _reader.GetChar(ordinal);
        }

        /// <summary>
        /// The get chars.
        /// </summary>
        /// <param name="ordinal">
        /// The ordinal.
        /// </param>
        /// <param name="dataOffset">
        /// The data offset.
        /// </param>
        /// <param name="buffer">
        /// The buffer.
        /// </param>
        /// <param name="bufferOffset">
        /// The buffer offset.
        /// </param>
        /// <param name="length">
        /// The length.
        /// </param>
        /// <returns>
        /// The <see cref="long"/>.
        /// </returns>
        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            return _reader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
        }

        /// <summary>
        /// The get data type name.
        /// </summary>
        /// <param name="ordinal">
        /// The ordinal.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public override string GetDataTypeName(int ordinal)
        {
            return _reader.GetDataTypeName(ordinal);
        }

        /// <summary>
        /// The get date time.
        /// </summary>
        /// <param name="ordinal">
        /// The ordinal.
        /// </param>
        /// <returns>
        /// The <see cref="DateTime"/>.
        /// </returns>
        public override DateTime GetDateTime(int ordinal)
        {
            return _reader.GetDateTime(ordinal);
        }

        /// <summary>
        /// The get decimal.
        /// </summary>
        /// <param name="ordinal">
        /// The ordinal.
        /// </param>
        /// <returns>
        /// The <see cref="decimal"/>.
        /// </returns>
        public override decimal GetDecimal(int ordinal)
        {
            return _reader.GetDecimal(ordinal);
        }

        /// <summary>
        /// The get double.
        /// </summary>
        /// <param name="ordinal">
        /// The ordinal.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public override double GetDouble(int ordinal)
        {
            return _reader.GetDouble(ordinal);
        }

        /// <summary>
        /// The get enumerator.
        /// </summary>
        /// <returns>
        /// The <see cref="IEnumerator{T}"/>.
        /// </returns>
        public override System.Collections.IEnumerator GetEnumerator()
        {
            return ((System.Collections.IEnumerable)_reader).GetEnumerator();
        }

        /// <summary>
        /// The get field type.
        /// </summary>
        /// <param name="ordinal">
        /// The ordinal.
        /// </param>
        /// <returns>
        /// The <see cref="Type"/>.
        /// </returns>
        public override Type GetFieldType(int ordinal)
        {
            return _reader.GetFieldType(ordinal);
        }

        /// <summary>
        /// The get float.
        /// </summary>
        /// <param name="ordinal">
        /// The ordinal.
        /// </param>
        /// <returns>
        /// The <see cref="float"/>.
        /// </returns>
        public override float GetFloat(int ordinal)
        {
            return _reader.GetFloat(ordinal);
        }

        /// <summary>
        /// get the GUID.
        /// </summary>
        /// <param name="ordinal">
        /// The ordinal.
        /// </param>
        /// <returns>
        /// The <see cref="Guid"/>.
        /// </returns>
        public override Guid GetGuid(int ordinal)
        {
            return _reader.GetGuid(ordinal);
        }

        /// <summary>
        /// The get integer.
        /// </summary>
        /// <param name="ordinal">The ordinal.</param>
        /// <returns>The <see cref="short"/>.</returns>
        public override short GetInt16(int ordinal)
        {
            return _reader.GetInt16(ordinal);
        }

        /// <summary>
        /// get a 32 bit integer
        /// </summary>
        /// <param name="ordinal">The ordinal.</param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public override int GetInt32(int ordinal)
        {
            return _reader.GetInt32(ordinal);
        }

        /// <summary>
        /// get a 64 bit integer (long)
        /// </summary>
        /// <param name="ordinal">
        /// The ordinal.
        /// </param>
        /// <returns>
        /// The <see cref="long"/>.
        /// </returns>
        public override long GetInt64(int ordinal)
        {
            return _reader.GetInt64(ordinal);
        }

        /// <summary>
        /// The get name.
        /// </summary>
        /// <param name="ordinal">
        /// The ordinal.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public override string GetName(int ordinal)
        {
            return _reader.GetName(ordinal);
        }

        /// <summary>
        /// The get ordinal.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public override int GetOrdinal(string name)
        {
            return _reader.GetOrdinal(name);
        }

        /// <summary>
        /// The get schema table.
        /// </summary>
        /// <returns>
        /// The <see cref="DataTable"/>.
        /// </returns>
        public override DataTable GetSchemaTable()
        {
            return _reader.GetSchemaTable();
        }

        /// <summary>
        /// The get string.
        /// </summary>
        /// <param name="ordinal">
        /// The ordinal.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public override string GetString(int ordinal)
        {
            return _reader.GetString(ordinal);
        }

        /// <summary>
        /// The get value.
        /// </summary>
        /// <param name="ordinal">
        /// The ordinal.
        /// </param>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        public override object GetValue(int ordinal)
        {
            return _reader.GetValue(ordinal);
        }

        /// <summary>
        /// The get values.
        /// </summary>
        /// <param name="values">
        /// The values.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public override int GetValues(object[] values)
        {
            return _reader.GetValues(values);
        }

        /// <summary>
        /// the database value null.
        /// </summary>
        /// <param name="ordinal">
        /// The ordinal.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public override bool IsDBNull(int ordinal)
        {
            return _reader.IsDBNull(ordinal);
        }

        /// <summary>
        /// The next result.
        /// </summary>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public override bool NextResult()
        {
            return _reader.NextResult();
        }

        /// <summary>
        /// The read.
        /// </summary>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public override bool Read()
        {
            return _reader.Read();
        }
    }
}