// Only in .NET 4.6 until netstandard 2.0 timeframe
#if !NETSTANDARD
using System;
using System.Data;
using System.Data.Common;

namespace StackExchange.Profiling.Data
{
    /// <summary>
    /// Provides a wrapper around a native <c>DbDataAdapter</c>, allowing a profiled Fill operation.
    /// </summary>
    public class ProfiledDbDataAdapter : DbDataAdapter
    {
        /// <summary>
        /// This static variable is simply used as a non-null placeholder in the MiniProfiler.ExecuteFinish method
        /// </summary>
        private static readonly DbDataReader TokenReader = new DataTableReader(new DataTable());

        private readonly IDbProfiler _profiler;
        private IDbCommand _selectCommand, _insertCommand, _updateCommand, _deleteCommand;

        /// <summary>
        /// Gets the underlying adapter.  Useful for when APIs can't handle the wrapped adapter (e.g. CommandBuilder).
        /// </summary>
        public IDbDataAdapter InternalAdapter { get; }

        /// <summary>
        /// Initialises a new instance of the <see cref="ProfiledDbDataAdapter"/> class.
        /// </summary>
        /// <param name="wrappedAdapter">The wrapped adapter.</param>
        /// <param name="profiler">The profiler.</param>
        /// <exception cref="ArgumentNullException">Throws when the <paramref name="wrappedAdapter"/> is <c>null</c>.</exception>
        public ProfiledDbDataAdapter(IDbDataAdapter wrappedAdapter, IDbProfiler profiler = null)
        {
            InternalAdapter = wrappedAdapter ?? throw new ArgumentNullException(nameof(wrappedAdapter));
            _profiler = profiler ?? MiniProfiler.Current;

            InitCommands(wrappedAdapter);
        }

        private void InitCommands(IDbDataAdapter wrappedAdapter)
        {
            if (wrappedAdapter.SelectCommand != null)
            {
                _selectCommand = wrappedAdapter.SelectCommand;
            }
            if (wrappedAdapter.DeleteCommand != null)
            {
                _deleteCommand = wrappedAdapter.DeleteCommand;
            }
            if (wrappedAdapter.UpdateCommand != null)
            {
                _updateCommand = wrappedAdapter.UpdateCommand;
            }
            if (wrappedAdapter.InsertCommand != null)
            {
                _insertCommand = wrappedAdapter.InsertCommand;
            }
        }

        /// <summary>
        /// Adds a <see cref="T:System.Data.DataTable"/> named "Table" to the specified <see cref="T:System.Data.DataSet"/> and configures the schema to match that in the data source based on the specified <see cref="T:System.Data.SchemaType"/>.
        /// </summary>
        /// <param name="dataSet">The <see cref="T:System.Data.DataSet"/> to be filled with the schema from the data source.</param>
        /// <param name="schemaType">One of the <see cref="T:System.Data.SchemaType"/> values.</param>
        /// <returns>
        /// An array of <see cref="T:System.Data.DataTable"/> objects that contain schema information returned from the data source.
        /// </returns>
        public new DataTable[] FillSchema(DataSet dataSet, SchemaType schemaType) => InternalAdapter.FillSchema(dataSet, schemaType);

        /// <summary>
        /// Adds or updates rows in the <see cref="T:System.Data.DataSet"/> to match those in the data source using the <see cref="T:System.Data.DataSet"/> name, and creates a <see cref="T:System.Data.DataTable"/> named "Table".
        /// </summary>
        /// <param name="dataSet">A <see cref="T:System.Data.DataSet"/> to fill with records and, if necessary, schema.</param>
        /// <returns>
        /// The number of rows successfully added to or refreshed in the <see cref="T:System.Data.DataSet"/>. This does not include rows affected by statements that do not return rows.
        /// </returns>
        public new int Fill(DataSet dataSet)
        {
            /* 
             * The SqlDataAdapter type requires that you use a SqlDataCommand for the various adapter commands and will throw an 
             * exception if you attempt to pass it a ProfiledDbCommand instead.  This method is a simple wrapper around the existing
             * Fill method and assumes that a single ExecuteReader method will eventually be called on the SelectCommand.  This is 
             * somewhat of a hack but appears to be working to give rough timings.
             * 
             * While I have not tested this with an oracle DataAdapter, I would guess that it works in much the same way as the 
             * SqlDataAdapter type and would thus work fine with this workaround.
             */

            if (_profiler == null || !_profiler.IsActive || !(_selectCommand is DbCommand))
            {
                return InternalAdapter.Fill(dataSet);
            }

            int result;
            var cmd = (DbCommand)_selectCommand;
            _profiler.ExecuteStart(cmd, SqlExecuteType.Reader);
            try
            {
                result = InternalAdapter.Fill(dataSet);
            }
            catch (Exception e)
            {
                _profiler.OnError(cmd, SqlExecuteType.Reader, e);
                throw;
            }
            finally
            {
                _profiler.ExecuteFinish(cmd, SqlExecuteType.Reader, TokenReader);
            }

            return result;
        }

        /// <summary>
        /// Adds or refreshes rows in a specified range in the <see cref="T:System.Data.DataTable"/>. 
        /// </summary>
        /// <param name="dataTable">The <see cref="T:System.Data.DataTable"/> to use for table mapping.</param>/// <returns>
        /// The number of rows successfully added to or refreshed in the <see cref="T:System.Data.DataSet"/>. This does not include rows affected by statements that do not return rows.
        /// </returns>
        /// <remarks>
        /// This function will only work if you are using an <see cref="T:IDbDataAdapter"/> implementation that inherits from <see cref="T:DbDataAdapter"/>. 
        /// This includes <see cref="T:SqlDataAdapter"/> and most other similar classes. 
        /// </remarks>
        /// <exception cref="InvalidOperationException">The source table is invalid or being used with an <see cref="IDbDataAdapter"/> implementation that does not inherit from <see cref="DbDataAdapter"/>.</exception>
        public new int Fill(DataTable dataTable)
        {
            var dbDataAdapter = InternalAdapter as DbDataAdapter;
            if (dbDataAdapter == null)
            {
                throw new InvalidOperationException("This function is only supported when profiling a DbDataAdapter object. " +
                    "If you are using an adapter which implements IDbDataAdapter but does not inherit from DbDataAdapter then you cannot use this function.");
            }

            if (_profiler == null || !_profiler.IsActive || !(_selectCommand is DbCommand))
            {
                return dbDataAdapter.Fill(dataTable);
            }

            int result;
            var cmd = (DbCommand)_selectCommand;
            _profiler.ExecuteStart(cmd, SqlExecuteType.Reader);
            try
            {
                result = dbDataAdapter.Fill(dataTable);
            }
            catch (Exception e)
            {
                _profiler.OnError(cmd, SqlExecuteType.Reader, e);
                throw;
            }
            finally
            {
                _profiler.ExecuteFinish(cmd, SqlExecuteType.Reader, TokenReader);
            }

            return result;
        }

        /// <summary>
        /// Gets the parameters set by the user when executing an SQL SELECT statement.
        /// </summary>
        /// <returns>
        /// An array of <see cref="T:System.Data.IDataParameter"/> objects that contains the parameters set by the user.
        /// </returns>
        public new IDataParameter[] GetFillParameters() => InternalAdapter.GetFillParameters();

        /// <summary>
        /// Calls the respective INSERT, UPDATE, or DELETE statements for each inserted, updated, or deleted row in the specified <see cref="T:System.Data.DataSet"/> from a <see cref="T:System.Data.DataTable"/> named "Table".
        /// </summary>
        /// <param name="dataSet">The <see cref="T:System.Data.DataSet"/> used to update the data source.</param>
        /// <returns>
        /// The number of rows successfully updated from the <see cref="T:System.Data.DataSet"/>.
        /// </returns>
        /// <exception cref="DBConcurrencyException">An attempt to execute an INSERT, UPDATE, or DELETE statement resulted in zero records affected.</exception>
        /// <exception cref="NotImplementedException"></exception>
        public new int Update(DataSet dataSet)
        {
            // Don't need this right now and the logic is much more complicated.  Someone else can have at it.
            //	It looks like you could use the SqlDataAdapter RowUpdating and RowUpdated events but that would be provider-specific
            //	and be a pain to maintain.
#pragma warning disable RCS1079 // Throwing of new NotImplementedException.
            throw new NotImplementedException();
#pragma warning restore RCS1079 // Throwing of new NotImplementedException.
        }

        /// <summary>
        /// Gets or sets whether unmapped source tables or columns are passed with their source names in order to be filtered or to raise an error.
        /// </summary>
        /// <returns>One of the <see cref="T:System.Data.MissingMappingAction"/> values. The default is Passthrough.</returns>
        public new MissingMappingAction MissingMappingAction
        {
            get => InternalAdapter.MissingMappingAction;
            set => InternalAdapter.MissingMappingAction = value;
        }

        /// <summary>
        /// Gets or sets whether missing source tables, columns, and their relationships are added to the dataset schema, ignored, or cause an error to be raised.
        /// </summary>
        /// <returns>One of the <see cref="T:System.Data.MissingSchemaAction"/> values. The default is Add.</returns>
        /// <exception cref="ArgumentException">The value set is not one of the <see cref="T:System.Data.MissingSchemaAction"/> values.</exception>
        public new MissingSchemaAction MissingSchemaAction
        {
            get => InternalAdapter.MissingSchemaAction;
            set => InternalAdapter.MissingSchemaAction = value;
        }

        /// <summary>
        /// Gets how a source table is mapped to a dataset table.
        /// </summary>
        /// <returns>A collection that provides the master mapping between the returned records and the <see cref="T:System.Data.DataSet"/>. The default value is an empty collection.</returns>
        public new ITableMappingCollection TableMappings => InternalAdapter.TableMappings;

        /// <summary>
        /// Gets or sets an SQL statement used to select records in the data source.
        /// </summary>
        /// <returns>An <see cref="T:System.Data.IDbCommand"/> that is used during <see cref="M:System.Data.Common.DbDataAdapter.Update(System.Data.DataSet)"/> to select records from data source for placement in the data set.</returns>
        public new IDbCommand SelectCommand
        {
            get => _selectCommand;
            set
            {
                _selectCommand = value;

                var cmd = value as ProfiledDbCommand;
                InternalAdapter.SelectCommand = cmd == null ? value : cmd.InternalCommand;
            }
        }

        /// <summary>
        /// Gets or sets an SQL statement used to insert new records into the data source.
        /// </summary>
        /// <returns>An <see cref="T:System.Data.IDbCommand"/> used during <see cref="M:System.Data.Common.DbDataAdapter.Update(System.Data.DataSet)"/> to insert records in the data source for new rows in the data set.</returns>
        public new IDbCommand InsertCommand
        {
            get => _insertCommand;
            set
            {
                _insertCommand = value;

                var cmd = value as ProfiledDbCommand;
                InternalAdapter.InsertCommand = cmd == null ? value : cmd.InternalCommand;
            }
        }

        /// <summary>
        /// Gets or sets an SQL statement used to update records in the data source.
        /// </summary>
        /// <returns>An <see cref="T:System.Data.IDbCommand"/> used during <see cref="M:System.Data.Common.DbDataAdapter.Update(System.Data.DataSet)"/> to update records in the data source for modified rows in the data set.</returns>
        public new IDbCommand UpdateCommand
        {
            get => _updateCommand;
            set
            {
                _updateCommand = value;

                var cmd = value as ProfiledDbCommand;
                InternalAdapter.UpdateCommand = cmd == null ? value : cmd.InternalCommand;
            }
        }

        /// <summary>
        /// Gets or sets an SQL statement for deleting records from the data set.
        /// </summary>
        /// <returns>An <see cref="T:System.Data.IDbCommand"/> used during <see cref="M:System.Data.Common.DbDataAdapter.Update(System.Data.DataSet)"/> to delete records in the data source for deleted rows in the data set.</returns>
        public new IDbCommand DeleteCommand
        {
            get => _deleteCommand;
            set
            {
                _deleteCommand = value;

                var cmd = value as ProfiledDbCommand;
                InternalAdapter.DeleteCommand = cmd == null ? value : cmd.InternalCommand;
            }
        }
    }
}
#endif