using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace StackExchange.Profiling.Data
{
    /// <summary>
    /// The profiled database data reader.
    /// </summary>
    public class ProfiledDbDataReader : DbDataReader
    {
        private readonly IDbProfiler? _profiler;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfiledDbDataReader"/> class (with <see cref="CommandBehavior.Default"/>).
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="profiler">The profiler.</param>
        public ProfiledDbDataReader(DbDataReader reader, IDbProfiler profiler) : this(reader, CommandBehavior.Default, profiler) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfiledDbDataReader"/> class.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="behavior">The behavior specified during command execution.</param>
        /// <param name="profiler">The profiler.</param>
        public ProfiledDbDataReader(DbDataReader reader, CommandBehavior behavior, IDbProfiler? profiler)
        {
            WrappedReader = reader;
            Behavior = behavior;
            _profiler = profiler;
        }

        /// <summary>Gets the behavior specified during command execution.</summary>
        public CommandBehavior Behavior { get; }

        /// <inheritdoc cref="DbDataReader.Depth"/>
        public override int Depth => WrappedReader.Depth;

        /// <inheritdoc cref="DbDataReader.FieldCount"/>
        public override int FieldCount => WrappedReader.FieldCount;

        /// <inheritdoc cref="DbDataReader.HasRows"/>
        public override bool HasRows => WrappedReader.HasRows;

        /// <inheritdoc cref="DbDataReader.IsClosed"/>
        public override bool IsClosed => WrappedReader.IsClosed;

        /// <inheritdoc cref="DbDataReader.RecordsAffected"/>
        public override int RecordsAffected => WrappedReader.RecordsAffected;

        /// <summary>
        /// The <see cref="DbDataReader"/> that is being used.
        /// </summary>
        public DbDataReader WrappedReader { get; }

        /// <inheritdoc cref="DbDataReader.this[string]"/>
        public override object this[string name] => WrappedReader[name];

        /// <inheritdoc cref="DbDataReader.this[int]"/>
        public override object this[int ordinal] => WrappedReader[ordinal];

        /// <inheritdoc cref="DbDataReader.GetBoolean(int)"/>
        public override bool GetBoolean(int ordinal) => WrappedReader.GetBoolean(ordinal);

        /// <inheritdoc cref="DbDataReader.GetByte(int)"/>
        public override byte GetByte(int ordinal) => WrappedReader.GetByte(ordinal);

        /// <inheritdoc cref="DbDataReader.GetBytes(int, long, byte[], int, int)"/>
        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) =>
            WrappedReader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);

        /// <inheritdoc cref="DbDataReader.GetChar(int)"/>
        public override char GetChar(int ordinal) => WrappedReader.GetChar(ordinal);

        /// <inheritdoc cref="DbDataReader.GetChars(int, long, char[], int, int)"/>
        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) =>
            WrappedReader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);

        /// <inheritdoc cref="DbDataReader.GetData(int)"/>
        public new DbDataReader GetData(int ordinal) => WrappedReader.GetData(ordinal);

        /// <inheritdoc cref="DbDataReader.GetDataTypeName(int)"/>
        public override string GetDataTypeName(int ordinal) => WrappedReader.GetDataTypeName(ordinal);

        /// <inheritdoc cref="DbDataReader.GetDateTime(int)"/>
        public override DateTime GetDateTime(int ordinal) => WrappedReader.GetDateTime(ordinal);

        /// <inheritdoc cref="DbDataReader.GetDecimal(int)"/>
        public override decimal GetDecimal(int ordinal) => WrappedReader.GetDecimal(ordinal);

        /// <inheritdoc cref="DbDataReader.GetDouble(int)"/>
        public override double GetDouble(int ordinal) => WrappedReader.GetDouble(ordinal);

        /// <inheritdoc cref="DbDataReader.GetEnumerator()"/>
        public override System.Collections.IEnumerator GetEnumerator() => ((System.Collections.IEnumerable)WrappedReader).GetEnumerator();

        /// <inheritdoc cref="DbDataReader.GetFieldType(int)"/>
        public override Type GetFieldType(int ordinal) => WrappedReader.GetFieldType(ordinal);

        /// <inheritdoc cref="DbDataReader.GetFieldValue{T}(int)"/>
        public override T GetFieldValue<T>(int ordinal) => WrappedReader.GetFieldValue<T>(ordinal);

        /// <inheritdoc cref="DbDataReader.GetFieldValueAsync{T}(int, CancellationToken)"/>
        public override Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellationToken) => WrappedReader.GetFieldValueAsync<T>(ordinal, cancellationToken);

        /// <inheritdoc cref="DbDataReader.GetFloat(int)"/>
        public override float GetFloat(int ordinal) => WrappedReader.GetFloat(ordinal);

        /// <inheritdoc cref="DbDataReader.GetGuid(int)"/>
        public override Guid GetGuid(int ordinal) => WrappedReader.GetGuid(ordinal);

        /// <inheritdoc cref="DbDataReader.GetInt16(int)"/>
        public override short GetInt16(int ordinal) => WrappedReader.GetInt16(ordinal);

        /// <inheritdoc cref="DbDataReader.GetInt32(int)"/>
        public override int GetInt32(int ordinal) => WrappedReader.GetInt32(ordinal);

        /// <inheritdoc cref="DbDataReader.GetInt64(int)"/>
        public override long GetInt64(int ordinal) => WrappedReader.GetInt64(ordinal);

        /// <inheritdoc cref="DbDataReader.GetName(int)"/>
        public override string GetName(int ordinal) => WrappedReader.GetName(ordinal);

        /// <inheritdoc cref="DbDataReader.GetOrdinal(string)"/>
        public override int GetOrdinal(string name) => WrappedReader.GetOrdinal(name);

        /// <inheritdoc cref="DbDataReader.GetString(int)"/>
        public override string GetString(int ordinal) => WrappedReader.GetString(ordinal);

        /// <inheritdoc cref="DbDataReader.GetValue(int)"/>
        public override object GetValue(int ordinal) => WrappedReader.GetValue(ordinal);

        /// <inheritdoc cref="DbDataReader.GetValues(object[])"/>
        public override int GetValues(object[] values) => WrappedReader.GetValues(values);

        /// <inheritdoc cref="DbDataReader.IsDBNull(int)"/>
        public override bool IsDBNull(int ordinal) => WrappedReader.IsDBNull(ordinal);

        /// <inheritdoc cref="DbDataReader.IsDBNullAsync(int, CancellationToken)"/>
        public override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken) => WrappedReader.IsDBNullAsync(ordinal, cancellationToken);

        /// <inheritdoc cref="DbDataReader.NextResult()"/>
        public override bool NextResult() => WrappedReader.NextResult();

        /// <inheritdoc cref="DbDataReader.NextResultAsync(CancellationToken)"/>
        public override Task<bool> NextResultAsync(CancellationToken cancellationToken) => WrappedReader.NextResultAsync(cancellationToken);

        /// <inheritdoc cref="DbDataReader.Read()"/>
        public override bool Read() => WrappedReader.Read();

        /// <inheritdoc cref="DbDataReader.ReadAsync(CancellationToken)"/>
        public override Task<bool> ReadAsync(CancellationToken cancellationToken) => WrappedReader.ReadAsync(cancellationToken);

        /// <inheritdoc cref="DbDataReader.Close()"/>
        public override void Close()
        {
            // reader can be null when we're not profiling, but we've inherited from ProfiledDbCommand and are returning a
            // an unwrapped reader from the base command
            WrappedReader?.Close();
            _profiler?.ReaderFinish(this);
        }

        /// <inheritdoc cref="DbDataReader.GetSchemaTable()"/>
        public override DataTable? GetSchemaTable() => WrappedReader.GetSchemaTable();

        /// <inheritdoc cref="DbDataReader.Dispose(bool)"/>
        protected override void Dispose(bool disposing)
        {
            // reader can be null when we're not profiling, but we've inherited from ProfiledDbCommand and are returning a
            // an unwrapped reader from the base command
            WrappedReader?.Dispose();
            base.Dispose(disposing);
        }
    }
}
