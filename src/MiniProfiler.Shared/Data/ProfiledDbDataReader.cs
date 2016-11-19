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
        private readonly DbDataReader _reader;
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

        /// <summary>Gets a value indicating the depth of nesting for the current row.</summary>
        public override int Depth => _reader.Depth;

        /// <summary>Gets the number of columns in the current row.</summary>
        public override int FieldCount => _reader.FieldCount;

        /// <summary>Gets a value indicating whether the data reader has any rows.</summary>
        public override bool HasRows => _reader.HasRows;

        /// <summary>Gets a value indicating whether the data reader is closed.</summary>
        public override bool IsClosed => _reader.IsClosed;

        /// <summary>Gets the number of rows changed, inserted, or deleted by execution of the SQL statement.</summary>
        public override int RecordsAffected => _reader.RecordsAffected;

        /// <summary>The <see cref="DbDataReader"/> that is being used.</summary>
        public DbDataReader WrappedReader => _reader;

        /// <summary>Gets the column with the specified name.</summary>
        /// <param name="name">The name of the column to find.</param>
        /// <returns>The column with the specified name as an <see cref="object"/>.</returns>
        public override object this[string name] => _reader[name];

        /// <summary>Gets the column located at the specified index.</summary>
        /// <param name="ordinal">The zero-based index of the column to get.</param>
        /// <returns>The column with the specified name as an <see cref="object"/>.</returns>
        public override object this[int ordinal] => _reader[ordinal];

        /// <summary>Gets the value of the specified column as a Boolean.</summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public override bool GetBoolean(int ordinal) => _reader.GetBoolean(ordinal);

        /// <summary>Gets the 8-bit unsigned integer value of the specified column.</summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The 8-bit unsigned integer value of the specified column.</returns>
        public override byte GetByte(int ordinal) => _reader.GetByte(ordinal);

        /// <summary>Reads a stream of bytes from the specified column offset into the buffer as an array, starting at the given buffer offset.</summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <param name="dataOffset">The index within the field from which to start the read operation.</param>
        /// <param name="buffer">The buffer into which to read the stream of bytes.</param>
        /// <param name="bufferOffset">The index for buffer to start the read operation.</param>
        /// <param name="length">The number of bytes to read.</param>
        /// <returns>The actual number of bytes read.</returns>
        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length) =>
            _reader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);

        /// <summary>Gets the character value of the specified column.</summary>
        /// <param name="ordinal">The index of the field to find.</param>
        /// <returns>The character value of the specified column.</returns>
        public override char GetChar(int ordinal) => _reader.GetChar(ordinal);

        /// <summary>Reads a stream of characters from the specified column offset into the buffer as an array, starting at the given buffer offset.</summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <param name="dataOffset">The index within the row from which to start the read operation.</param>
        /// <param name="buffer">The buffer into which to read the stream of bytes.</param>
        /// <param name="bufferOffset">The index for buffer to start the read operation.</param>
        /// <param name="length">The number of bytes to read.</param>
        /// <returns>The actual number of characters read.</returns>
        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length) =>
            _reader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);

// TODO: GetData()?
/*
        /// <summary>Returns an <see cref="IDataReader"/> for the specified column ordinal.</summary>
        /// <param name="ordinal">The index of the field to find.</param>
        /// <returns>The <see cref="IDataReader"/> for the specified column ordinal.</returns>
        public override string GetData(int ordinal) => _reader.GetData(ordinal);
*/

        /// <summary>Gets the data type information for the specified field.</summary>
        /// <param name="ordinal">The index of the field to find.</param>
        /// <returns>The data type information for the specified field.</returns>
        public override string GetDataTypeName(int ordinal) => _reader.GetDataTypeName(ordinal);

        /// <summary>Gets the date and time data value of the specified field.</summary>
        /// <param name="ordinal">The index of the field to find.</param>
        /// <returns>The date and time data value of the specified field.</returns>
        public override DateTime GetDateTime(int ordinal) => _reader.GetDateTime(ordinal);

        /// <summary>Gets the fixed-position numeric value of the specified field.</summary>
        /// <param name="ordinal">The index of the field to find.</param>
        /// <returns>The fixed-position numeric value of the specified field.</returns>
        public override decimal GetDecimal(int ordinal) => _reader.GetDecimal(ordinal);

        /// <summary>Gets the double-precision floating point number of the specified field.</summary>
        /// <param name="ordinal">The index of the field to find.</param>
        /// <returns>The double-precision floating point number of the specified field.</returns>
        public override double GetDouble(int ordinal) => _reader.GetDouble(ordinal);

        /// <summary>Gets an <see cref="IEnumerator{T}"/> for the rows.</summary>
        /// <returns>The <see cref="IEnumerator{T}"/>.</returns>
        public override System.Collections.IEnumerator GetEnumerator() => ((System.Collections.IEnumerable)_reader).GetEnumerator();

        /// <summary>Gets the <see cref="Type"/> information corresponding to the type of <see cref="Object"/> that would be returned from <see cref="GetValue"/>.</summary>
        /// <param name="ordinal">The index of the field to find.</param>
        /// <returns>The <see cref="Type"/> information corresponding to the type of <see cref="Object"/> that would be returned from <see cref="GetValue"/>.</returns>
        public override Type GetFieldType(int ordinal) => _reader.GetFieldType(ordinal);

        /// <summary>Gets the single-precision floating point number of the specified field.</summary>
        /// <param name="ordinal">The index of the field to find.</param>
        /// <returns>The single-precision floating point number of the specified field.</returns>
        public override float GetFloat(int ordinal) => _reader.GetFloat(ordinal);

        /// <summary>Returns the GUID value of the specified field.</summary>
        /// <param name="ordinal">The index of the field to find.</param>
        /// <returns>The GUID value of the specified field.</returns>
        public override Guid GetGuid(int ordinal) => _reader.GetGuid(ordinal);

        /// <summary>Gets the 16-bit signed integer value of the specified field.</summary>
        /// <param name="ordinal">The index of the field to find.</param>
        /// <returns>The 16-bit signed integer value of the specified field.</returns>
        public override short GetInt16(int ordinal) => _reader.GetInt16(ordinal);

        /// <summary>Gets the 32-bit signed integer value of the specified field.</summary>
        /// <param name="ordinal">The index of the field to find.</param>
        /// <returns>The 32-bit signed integer value of the specified field.</returns>
        public override int GetInt32(int ordinal) => _reader.GetInt32(ordinal);

        /// <summary>Gets the 64-bit signed integer value of the specified field.</summary>
        /// <param name="ordinal">The index of the field to find.</param>
        /// <returns>The 64-bit signed integer value of the specified field.</returns>
        public override long GetInt64(int ordinal) => _reader.GetInt64(ordinal);

        /// <summary>Gets the name for the field to find.</summary>
        /// <param name="ordinal">The index of the field to find.</param>
        /// <returns>The name of the field or the empty string (""), if there is no value to return.</returns>
        public override string GetName(int ordinal) => _reader.GetName(ordinal);

        /// <summary>Return the index of the named field.</summary>
        /// <param name="name">The name of the field to find.</param>
        /// <returns>The index of the named field.</returns>
        public override int GetOrdinal(string name) => _reader.GetOrdinal(name);

        /// <summary>Gets the string value of the specified field.</summary>
        /// <param name="ordinal">The index of the field to find.</param>
        /// <returns>The string value of the specified field.</returns>
        public override string GetString(int ordinal) => _reader.GetString(ordinal);

        /// <summary>Return the value of the specified field.</summary>
        /// <param name="ordinal">The index of the field to find.</param>
        /// <returns>The <see cref="object"/> which will contain the field value upon return.</returns>
        public override object GetValue(int ordinal) => _reader.GetValue(ordinal);

        /// <summary>Populates an array of objects with the column values of the current record.</summary>
        /// <param name="values">An array of Object to copy the attribute fields into.</param>
        /// <returns>The number of instances of <see cref="object"/> in the array.</returns>
        public override int GetValues(object[] values) => _reader.GetValues(values);

        /// <summary>Return whether the specified field is set to null.</summary>
        /// <param name="ordinal">The index of the field to find.</param>
        /// <returns>true if the specified field is set to null; otherwise, false.</returns>
        public override bool IsDBNull(int ordinal) => _reader.IsDBNull(ordinal);

        /// <summary>Advances the data reader to the next result, when reading the results of batch SQL statements.</summary>
        /// <returns>true if there are more rows; otherwise, false.</returns>
        public override bool NextResult() => _reader.NextResult();

        /// <summary>Advances the IDataReader to the next record.</summary>
        /// <returns>true if there are more rows; otherwise, false.</returns>
        public override bool Read() => _reader.Read();

// TODO: Revisit in .Net Standard 2.0
#if NET45
        /// <summary>Closes the IDataReader Object.</summary>
        public override void Close()
        {
            // reader can be null when we're not profiling, but we've inherited from ProfiledDbCommand and are returning a
            // an unwrapped reader from the base command
            _reader?.Close();
            _profiler?.ReaderFinish(this);
        }
        
        /// <summary>Returns a <see cref="DataTable"/> that describes the column metadata of the <see cref="IDataReader"/>.</summary>
        /// <returns>A <see cref="DataTable"/> that describes the column metadata.</returns>
        public override DataTable GetSchemaTable() => _reader.GetSchemaTable();
#endif
    }
}