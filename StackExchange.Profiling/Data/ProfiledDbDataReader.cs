namespace StackExchange.Profiling.Data
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;

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
            this._reader = reader;

            if (profiler != null)
            {
                this._profiler = profiler;
            }
        }

        /// <summary>
        /// Gets the depth.
        /// </summary>
        public override int Depth
        {
            get { return this._reader.Depth; }
        }

        /// <summary>
        /// Gets the field count.
        /// </summary>
        public override int FieldCount
        {
            get { return this._reader.FieldCount; }
        }

        /// <summary>
        /// Gets a value indicating whether has rows.
        /// </summary>
        public override bool HasRows
        {
            get { return this._reader.HasRows; }
        }

        /// <summary>
        /// Gets a value indicating whether is closed.
        /// </summary>
        public override bool IsClosed
        {
            get { return this._reader.IsClosed; }
        }

        /// <summary>
        /// Gets the records affected.
        /// </summary>
        public override int RecordsAffected
        {
            get { return this._reader.RecordsAffected; }
        }

        /// <summary>
        /// The this.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        public override object this[string name]
        {
            get { return this._reader[name]; }
        }

        /// <summary>
        /// The this.
        /// </summary>
        /// <param name="ordinal">
        /// The ordinal.
        /// </param>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        public override object this[int ordinal]
        {
            get { return this._reader[ordinal]; }
        }

        /// <summary>
        /// The close.
        /// </summary>
        public override void Close()
        {
            // this can occur when we're not profiling, but we've inherited from ProfiledDbCommand and are returning a
            // an unwrapped reader from the base command
            if (this._reader != null)
            {
                this._reader.Close();
            }

            if (this._profiler != null)
            {
                this._profiler.ReaderFinish(this);
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
            return this._reader.GetBoolean(ordinal);
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
            return this._reader.GetByte(ordinal);
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
            return this._reader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
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
            return this._reader.GetChar(ordinal);
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
            return this._reader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
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
            return this._reader.GetDataTypeName(ordinal);
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
            return this._reader.GetDateTime(ordinal);
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
            return this._reader.GetDecimal(ordinal);
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
            return this._reader.GetDouble(ordinal);
        }

        /// <summary>
        /// The get enumerator.
        /// </summary>
        /// <returns>
        /// The <see cref="IEnumerator{T}"/>.
        /// </returns>
        public override System.Collections.IEnumerator GetEnumerator()
        {
            return ((System.Collections.IEnumerable)this._reader).GetEnumerator();
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
            return this._reader.GetFieldType(ordinal);
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
            return this._reader.GetFloat(ordinal);
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
            return this._reader.GetGuid(ordinal);
        }

        /// <summary>
        /// The get integer.
        /// </summary>
        /// <param name="ordinal">The ordinal.</param>
        /// <returns>The <see cref="short"/>.</returns>
        public override short GetInt16(int ordinal)
        {
            return this._reader.GetInt16(ordinal);
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
            return this._reader.GetInt32(ordinal);
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
            return this._reader.GetInt64(ordinal);
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
            return this._reader.GetName(ordinal);
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
            return this._reader.GetOrdinal(name);
        }

        /// <summary>
        /// The get schema table.
        /// </summary>
        /// <returns>
        /// The <see cref="DataTable"/>.
        /// </returns>
        public override DataTable GetSchemaTable()
        {
            return this._reader.GetSchemaTable();
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
            return this._reader.GetString(ordinal);
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
            return this._reader.GetValue(ordinal);
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
            return this._reader.GetValues(values);
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
            return this._reader.IsDBNull(ordinal);
        }

        /// <summary>
        /// The next result.
        /// </summary>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public override bool NextResult()
        {
            return this._reader.NextResult();
        }

        /// <summary>
        /// The read.
        /// </summary>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public override bool Read()
        {
            return this._reader.Read();
        }
    }
}