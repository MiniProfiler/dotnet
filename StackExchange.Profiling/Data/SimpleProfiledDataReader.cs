namespace StackExchange.Profiling.Data
{
    using System;
    using System.Data;

    /// <summary>
    /// A simple profiled data reader.
    /// </summary>
    public class SimpleProfiledDataReader : IDataReader
    {
        /// <summary>
        /// The reader.
        /// </summary>
        private readonly IDataReader _reader;

        /// <summary>
        /// The profiler.
        /// </summary>
        private readonly IDbProfiler _profiler;

        /// <summary>
        /// Initialises a new instance of the <see cref="SimpleProfiledDataReader"/> class.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="profiler">The profiler.</param>
        public SimpleProfiledDataReader(IDataReader reader, IDbProfiler profiler)
        {
            if (reader == null) throw new ArgumentNullException("reader");

            _reader = reader;

            if (profiler != null)
            {
                _profiler = profiler;
            }
        }

        /// <summary>
        /// get the name.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>a string containing the name</returns>
        public string GetName(int index)
        {
            return _reader.GetName(index);
        }

        /// <summary>
        /// get the data type name.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The <see cref="string"/>.</returns>
        public string GetDataTypeName(int index)
        {
            return _reader.GetDataTypeName(index);
        }

        /// <summary>
        /// get the field type.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The <see cref="Type"/>.</returns>
        public Type GetFieldType(int index)
        {
            return _reader.GetFieldType(index);
        }

        /// <summary>
        /// get the value.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The <see cref="object"/>.</returns>
        public object GetValue(int index)
        {
            return _reader.GetValue(index);
        }

        /// <summary>
        /// get the values.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <returns>The <see cref="int"/>.</returns>
        public int GetValues(object[] values)
        {
            return _reader.GetValues(values);
        }

        /// <summary>
        /// get the ordinal.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The <see cref="int"/>.</returns>
        public int GetOrdinal(string name)
        {
            return _reader.GetOrdinal(name);
        }

        /// <summary>
        /// The get boolean.
        /// </summary>
        /// <param name="index">
        /// The index.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool GetBoolean(int index)
        {
            return _reader.GetBoolean(index);
        }

        /// <summary>
        /// The get byte.
        /// </summary>
        /// <param name="index">
        /// The index.
        /// </param>
        /// <returns>
        /// The <see cref="byte"/>.
        /// </returns>
        public byte GetByte(int index)
        {
            return _reader.GetByte(index);
        }

        /// <summary>
        /// Gets the value for the column specified by the ordinal as an array of <see cref="T:System.Byte"/> objects.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="fieldOffset">The field Offset.</param>
        /// <param name="buffer">The buffer.</param>
        /// <param name="bufferoffset">The buffer offset.</param>
        /// <param name="length">The length.</param>
        /// <returns>The number of bytes copied.</returns>
        public long GetBytes(int index, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            return _reader.GetBytes(index, fieldOffset, buffer, bufferoffset, length);
        }

        /// <summary>
        /// The get char.
        /// </summary>
        /// <param name="index">
        /// The index.
        /// </param>
        /// <returns>
        /// The <see cref="char"/>.
        /// </returns>
        public char GetChar(int index)
        {
            return _reader.GetChar(index);
        }

        /// <summary>
        /// Gets the value for the column specified by the ordinal as an array of <see cref="T:System.Char"/> objects.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="fieldoffset">The field offset.</param>
        /// <param name="buffer">The buffer.</param>
        /// <param name="bufferoffset">The buffer offset.</param>
        /// <param name="length">The length.</param>
        /// <returns>The <see cref="long"/>.</returns>
        public long GetChars(int index, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            return _reader.GetChars(index, fieldoffset, buffer, bufferoffset, length);
        }

        /// <summary>
        /// get the GUID.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The <see cref="Guid"/>.</returns>
        public Guid GetGuid(int index)
        {
            return _reader.GetGuid(index);
        }

        /// <summary>
        /// Gets the value for the column specified by the ordinal as a <see cref="T:System.Int16"/>.
        /// </summary>
        /// <param name="index">
        /// The index.
        /// </param>
        /// <returns>
        /// The <see cref="short"/>.
        /// </returns>
        public short GetInt16(int index)
        {
            return _reader.GetInt16(index);
        }

        /// <summary>
        /// Gets the value for the column specified by the ordinal as a <see cref="T:System.Int32"/>.
        /// </summary>
        /// <param name="index">
        /// The index.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public int GetInt32(int index)
        {
            return _reader.GetInt32(index);
        }

        /// <summary>
        /// Gets the value for the column specified by the ordinal as a <see cref="T:System.Int64"/>.
        /// </summary>
        /// <param name="index">
        /// The index.
        /// </param>
        /// <returns>
        /// The <see cref="long"/>.
        /// </returns>
        public long GetInt64(int index)
        {
            return _reader.GetInt64(index);
        }

        /// <summary>
        /// The get float.
        /// </summary>
        /// <param name="index">
        /// The index.
        /// </param>
        /// <returns>
        /// The <see cref="float"/>.
        /// </returns>
        public float GetFloat(int index)
        {
            return _reader.GetFloat(index);
        }

        /// <summary>
        /// The get double.
        /// </summary>
        /// <param name="index">
        /// The index.
        /// </param>
        /// <returns>
        /// The <see cref="double"/>.
        /// </returns>
        public double GetDouble(int index)
        {
            return _reader.GetDouble(index);
        }

        /// <summary>
        /// The get string.
        /// </summary>
        /// <param name="index">
        /// The index.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public string GetString(int index)
        {
            return _reader.GetString(index);
        }

        /// <summary>
        /// The get decimal.
        /// </summary>
        /// <param name="index">
        /// The index.
        /// </param>
        /// <returns>
        /// The <see cref="decimal"/>.
        /// </returns>
        public decimal GetDecimal(int index)
        {
            return _reader.GetDecimal(index);
        }

        /// <summary>
        /// The get date time.
        /// </summary>
        /// <param name="index">
        /// The index.
        /// </param>
        /// <returns>
        /// The <see cref="DateTime"/>.
        /// </returns>
        public DateTime GetDateTime(int index)
        {
            return _reader.GetDateTime(index);
        }

        /// <summary>
        /// The get data.
        /// </summary>
        /// <param name="index">
        /// The index.
        /// </param>
        /// <returns>
        /// The <see cref="IDataReader"/>.
        /// </returns>
        public IDataReader GetData(int index)
        {
            return _reader.GetData(index);
        }

        /// <summary>
        /// Returns true if the column specified by the column ordinal parameter is null.
        /// </summary>
        /// <param name="index">
        /// The index.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool IsDBNull(int index)
        {
            return _reader.IsDBNull(index);
        }

        /// <summary>
        /// Gets the field count.
        /// </summary>
        public int FieldCount
        {
            get { return _reader.FieldCount; }
        }

        /// <summary>
        /// The 
        /// </summary>
        /// <param name="index">
        /// The index.
        /// </param>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        public object this[int index]
        {
            get { return _reader[index]; }
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
        public object this[string name]
        {
            get { return _reader[name]; }
        }

        /// <summary>
        /// The close.
        /// </summary>
        public void Close()
        {
            // this can occur when we're not profiling but we've inherited from SimpleProfiledCommand
            // and are returning an unwrapped reader from the base command
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
        /// The get schema table.
        /// </summary>
        /// <returns>
        /// The <see cref="DataTable"/>.
        /// </returns>
        public DataTable GetSchemaTable()
        {
            return _reader.GetSchemaTable();
        }

        /// <summary>
        /// The next result.
        /// </summary>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool NextResult()
        {
            return _reader.NextResult();
        }

        /// <summary>
        /// read from the underlying reader.
        /// </summary>
        /// <returns>
        /// The <see cref="bool"/>.</returns>
        public bool Read()
        {
            return _reader.Read();
        }

        /// <summary>
        /// Gets the depth.
        /// </summary>
        public int Depth
        {
            get { return _reader.Depth; }
        }

        /// <summary>
        /// Gets a value indicating whether is closed.
        /// </summary>
        public bool IsClosed
        {
            get { return _reader.IsClosed; }
        }

        /// <summary>
        /// Gets the number of records affected.
        /// </summary>
        public int RecordsAffected
        {
            get { return _reader.RecordsAffected; }
        }

        /// <summary>
        /// dispose the command / connection and profiler.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// dispose the command / connection and profiler.
        /// </summary>
        /// <param name="disposing">false if the dispose is called from a <c>finalizer</c></param>
        private void Dispose(bool disposing)
        {
            if (disposing && _reader != null)
            {
                _reader.Dispose();
            }
        }
    }
}
