using System;
using System.Data;
using System.Data.Common;

namespace StackExchange.Profiling.Data
{
    /// <summary>
    /// Provides a wrapper around a native <see cref="DbDataAdapter"/>, allowing a profiled Fill operation.
    /// </summary>
    public class ProfiledDbDataAdapter : DbDataAdapter
    {
        /// <summary>
        /// This static variable is simply used as a non-null placeholder in the MiniProfiler.ExecuteFinish method.
        /// </summary>
        private static readonly DbDataReader TokenReader = new DataTableReader(new DataTable());

        private readonly IDbProfiler? _profiler;
        private IDbCommand? _selectCommand, _insertCommand, _updateCommand, _deleteCommand;

        /// <summary>
        /// Gets the underlying adapter. Useful for when APIs can't handle the wrapped adapter (e.g. CommandBuilder).
        /// </summary>
        public IDbDataAdapter InternalAdapter { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfiledDbDataAdapter"/> class.
        /// </summary>
        /// <param name="wrappedAdapter">The wrapped adapter.</param>
        /// <param name="profiler">The profiler.</param>
        /// <exception cref="ArgumentNullException">Throws when the <paramref name="wrappedAdapter"/> is <c>null</c>.</exception>
        public ProfiledDbDataAdapter(IDbDataAdapter wrappedAdapter, IDbProfiler? profiler = null)
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

        /// <inheritdoc cref="DbDataAdapter.FillSchema(DataSet, SchemaType)"/>
        public new DataTable[] FillSchema(DataSet dataSet, SchemaType schemaType) => InternalAdapter.FillSchema(dataSet, schemaType);

        /// <inheritdoc cref="DbDataAdapter.Fill(DataSet)"/>
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

            if (_profiler?.IsActive != true || _selectCommand is not DbCommand)
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

        /// <inheritdoc cref="DbDataAdapter.Fill(DataTable)"/>
        public new int Fill(DataTable dataTable)
        {
            var dbDataAdapter = InternalAdapter as DbDataAdapter
                ?? throw new InvalidOperationException("This function is only supported when profiling a DbDataAdapter object. If you are using an adapter which implements IDbDataAdapter but does not inherit from DbDataAdapter then you cannot use this function.");

            if (_profiler?.IsActive != true || _selectCommand is not DbCommand)
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

        /// <inheritdoc cref="DbDataAdapter.GetFillParameters()"/>
        public new IDataParameter[] GetFillParameters() => InternalAdapter.GetFillParameters();

        /// <inheritdoc cref="IDataAdapter.MissingMappingAction"/>
        public new MissingMappingAction MissingMappingAction
        {
            get => InternalAdapter.MissingMappingAction;
            set => InternalAdapter.MissingMappingAction = value;
        }

        /// <inheritdoc cref="IDataAdapter.MissingSchemaAction"/>
        public new MissingSchemaAction MissingSchemaAction
        {
            get => InternalAdapter.MissingSchemaAction;
            set => InternalAdapter.MissingSchemaAction = value;
        }

        /// <inheritdoc cref="IDataAdapter.TableMappings"/>
        public new ITableMappingCollection TableMappings => InternalAdapter.TableMappings;

        /// <inheritdoc cref="DbDataAdapter.SelectCommand"/>
        public new IDbCommand? SelectCommand
        {
            get => _selectCommand;
            set
            {
                _selectCommand = value;
                InternalAdapter.SelectCommand = value is ProfiledDbCommand cmd ? cmd.WrappedCommand : value;
            }
        }

        /// <inheritdoc cref="DbDataAdapter.InsertCommand"/>
        public new IDbCommand? InsertCommand
        {
            get => _insertCommand;
            set
            {
                _insertCommand = value;
                InternalAdapter.InsertCommand = value is ProfiledDbCommand cmd ? cmd.WrappedCommand : value;
            }
        }

        /// <inheritdoc cref="DbDataAdapter.UpdateCommand"/>
        public new IDbCommand? UpdateCommand
        {
            get => _updateCommand;
            set
            {
                _updateCommand = value;
                InternalAdapter.UpdateCommand = value is ProfiledDbCommand cmd ? cmd.WrappedCommand : value;
            }
        }

        /// <inheritdoc cref="DbDataAdapter.DeleteCommand"/>
        public new IDbCommand? DeleteCommand
        {
            get => _deleteCommand;
            set
            {
                _deleteCommand = value;
                InternalAdapter.DeleteCommand = value is ProfiledDbCommand cmd ? cmd.WrappedCommand : value;
            }
        }
    }
}
