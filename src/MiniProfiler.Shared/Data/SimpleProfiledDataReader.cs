using System;
using System.Data;
using System.Data.Common;

namespace StackExchange.Profiling.Data
{
    /// <summary>
    /// A simple profiled data reader.
    /// </summary>
    public class SimpleProfiledDataReader : IDataReader
    {
        private readonly IDataReader _reader;
        private readonly IDbProfiler? _profiler;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleProfiledDataReader"/> class.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="profiler">The profiler.</param>
        /// <exception cref="ArgumentNullException">Throws when the <paramref name="reader"/> is <c>null</c>.</exception>
        public SimpleProfiledDataReader(IDataReader reader, IDbProfiler? profiler)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));

            if (profiler != null)
            {
                _profiler = profiler;
            }
        }

        /// <inheritdoc cref="IDataReader.Depth"/>
        public int Depth => _reader.Depth;

        /// <inheritdoc cref="IDataRecord.FieldCount"/>
        public int FieldCount => _reader.FieldCount;

        /// <inheritdoc cref="IDataReader.IsClosed"/>
        public bool IsClosed => _reader.IsClosed;

        /// <inheritdoc cref="IDataReader.RecordsAffected"/>
        public int RecordsAffected => _reader.RecordsAffected;

        /// <inheritdoc cref="DbDataReader.this[string]"/>
        public object this[string name] => _reader[name];

        /// <inheritdoc cref="DbDataReader.this[int]"/>
        public object this[int ordinal] => _reader[ordinal];

        /// <inheritdoc cref="IDataRecord.GetBoolean(int)"/>
        public bool GetBoolean(int ordinal) => _reader.GetBoolean(ordinal);

        /// <inheritdoc cref="IDataRecord.GetByte(int)"/>
        public byte GetByte(int ordinal) => _reader.GetByte(ordinal);

        /// <inheritdoc cref="IDataRecord.GetBytes(int, long, byte[], int, int)"/>
        public long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) =>
            _reader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);

        /// <inheritdoc cref="IDataRecord.GetChar(int)"/>
        public char GetChar(int ordinal) => _reader.GetChar(ordinal);

        /// <inheritdoc cref="IDataRecord.GetChars(int, long, char[], int, int)"/>
        public long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) =>
            _reader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);

        /// <inheritdoc cref="IDataRecord.GetData(int)"/>
        public IDataReader GetData(int ordinal) => _reader.GetData(ordinal);

        /// <inheritdoc cref="IDataRecord.GetDataTypeName(int)"/>
        public string GetDataTypeName(int ordinal) => _reader.GetDataTypeName(ordinal);

        /// <inheritdoc cref="IDataRecord.GetDateTime(int)"/>
        public DateTime GetDateTime(int ordinal) => _reader.GetDateTime(ordinal);

        /// <inheritdoc cref="IDataRecord.GetDecimal(int)"/>
        public decimal GetDecimal(int ordinal) => _reader.GetDecimal(ordinal);

        /// <inheritdoc cref="IDataRecord.GetDouble(int)"/>
        public double GetDouble(int ordinal) => _reader.GetDouble(ordinal);

        /// <inheritdoc cref="IDataRecord.GetFieldType(int)"/>
        public Type GetFieldType(int ordinal) => _reader.GetFieldType(ordinal);

        /// <inheritdoc cref="IDataRecord.GetFloat(int)"/>
        public float GetFloat(int ordinal) => _reader.GetFloat(ordinal);

        /// <inheritdoc cref="IDataRecord.GetGuid(int)"/>
        public Guid GetGuid(int ordinal) => _reader.GetGuid(ordinal);

        /// <inheritdoc cref="IDataRecord.GetInt32(int)"/>
        public short GetInt16(int ordinal) => _reader.GetInt16(ordinal);

        /// <inheritdoc cref="IDataRecord.GetInt32(int)"/>
        public int GetInt32(int ordinal) => _reader.GetInt32(ordinal);

        /// <inheritdoc cref="IDataRecord.GetInt64(int)"/>
        public long GetInt64(int ordinal) => _reader.GetInt64(ordinal);

        /// <inheritdoc cref="IDataRecord.GetName(int)"/>
        public string GetName(int ordinal) => _reader.GetName(ordinal);

        /// <inheritdoc cref="IDataRecord.GetOrdinal(string)"/>
        public int GetOrdinal(string name) => _reader.GetOrdinal(name);

        /// <inheritdoc cref="IDataRecord.GetString(int)"/>
        public string GetString(int ordinal) => _reader.GetString(ordinal);

        /// <inheritdoc cref="IDataRecord.GetValue(int)"/>
        public object GetValue(int ordinal) => _reader.GetValue(ordinal);

        /// <inheritdoc cref="IDataRecord.GetValues(object[])"/>
        public int GetValues(object[] values) => _reader.GetValues(values);

        /// <inheritdoc cref="IDataRecord.IsDBNull(int)"/>
        public bool IsDBNull(int ordinal) => _reader.IsDBNull(ordinal);

        /// <inheritdoc cref="IDataReader.Close()"/>
        public void Close()
        {
            // reader can be null when we're not profiling, but we've inherited from ProfiledDbCommand and are returning a
            // an unwrapped reader from the base command
            _reader?.Close();
            _profiler?.ReaderFinish(this);
        }

        /// <inheritdoc cref="IDataReader.GetSchemaTable()"/>
        public DataTable? GetSchemaTable() => _reader.GetSchemaTable();

        /// <inheritdoc cref="IDataReader.NextResult()"/>
        public bool NextResult() => _reader.NextResult();

        /// <inheritdoc cref="IDataReader.Read()"/>
        public bool Read() => _reader.Read();

        /// <summary>
        /// Releases all resources used by this reader.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the managed resources used by this reader and optionally releases the unmanaged resources.
        /// </summary>
        /// <param name="disposing">false if the dispose is called from a <c>finalizer</c></param>
        private void Dispose(bool disposing)
        {
            if (disposing) _reader?.Dispose();
        }
    }
}
