using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

using StackExchange.Profiling.Internal;
using StackExchange.Profiling.SqlFormatters;
using Xunit;
#if NET461
using System.Transactions;
#endif

namespace StackExchange.Profiling.Tests
{
    public class SqlFormatterTests
    {
	    private const string None = "";
	    private const string At = "@";
	    private SqlServerFormatter _formatter;
        private string _commandText;
        private SqlCommand _dbCommand;
        private static Dictionary<RuntimeTypeHandle, DbType> _dbTypeMap;

        public SqlFormatterTests()
        {
            CreateDbTypeMap();
            _formatter = new SqlServerFormatter();
        }

        public static IEnumerable<object[]> GetParamPrefixes()
        {
            yield return new object[] { None };
            yield return new object[] { At };
        }

        private void CreateDbCommand(CommandType commandType)
        {
            var sqlConnection = new SqlConnection("Initial Catalog=TestDatabase");
            _dbCommand = new SqlCommand(_commandText, sqlConnection)
            {
                CommandType = commandType
            };
        }

        private string GenerateOutput()
        {
            var sqlParameters = _dbCommand.GetParameters();
            return _formatter.GetFormattedSql(_commandText, sqlParameters, _dbCommand);
        }

        private void AddDbParameter<T>(string name, object value, ParameterDirection parameterDirection = ParameterDirection.Input, int? size = null, DbType? type = null)
        {
            var parameter = _dbCommand.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            parameter.Direction = parameterDirection;
            parameter.DbType = type ?? GetDbType(typeof(T));
            if (size.HasValue)
                parameter.Size = size.Value;
            _dbCommand.Parameters.Add(parameter);
        }

        private static void CreateDbTypeMap()
        {
            #region copied from dapper
            _dbTypeMap = new Dictionary<RuntimeTypeHandle, DbType>
            {
                [typeof(byte).TypeHandle] = DbType.Byte,
                [typeof(sbyte).TypeHandle] = DbType.SByte,
                [typeof(short).TypeHandle] = DbType.Int16,
                [typeof(ushort).TypeHandle] = DbType.UInt16,
                [typeof(int).TypeHandle] = DbType.Int32,
                [typeof(uint).TypeHandle] = DbType.UInt32,
                [typeof(long).TypeHandle] = DbType.Int64,
                [typeof(ulong).TypeHandle] = DbType.UInt64,
                [typeof(float).TypeHandle] = DbType.Single,
                [typeof(double).TypeHandle] = DbType.Double,
                [typeof(decimal).TypeHandle] = DbType.Decimal,
                [typeof(bool).TypeHandle] = DbType.Boolean,
                [typeof(string).TypeHandle] = DbType.String,
                [typeof(char).TypeHandle] = DbType.StringFixedLength,
                [typeof(Guid).TypeHandle] = DbType.Guid,
                [typeof(DateTime).TypeHandle] = DbType.DateTime,
                [typeof(DateTimeOffset).TypeHandle] = DbType.DateTimeOffset,
                [typeof(byte[]).TypeHandle] = DbType.Binary,
                [typeof(byte?).TypeHandle] = DbType.Byte,
                [typeof(sbyte?).TypeHandle] = DbType.SByte,
                [typeof(short?).TypeHandle] = DbType.Int16,
                [typeof(ushort?).TypeHandle] = DbType.UInt16,
                [typeof(int?).TypeHandle] = DbType.Int32,
                [typeof(uint?).TypeHandle] = DbType.UInt32,
                [typeof(long?).TypeHandle] = DbType.Int64,
                [typeof(ulong?).TypeHandle] = DbType.UInt64,
                [typeof(float?).TypeHandle] = DbType.Single,
                [typeof(double?).TypeHandle] = DbType.Double,
                [typeof(decimal?).TypeHandle] = DbType.Decimal,
                [typeof(bool?).TypeHandle] = DbType.Boolean,
                [typeof(char?).TypeHandle] = DbType.StringFixedLength,
                [typeof(Guid?).TypeHandle] = DbType.Guid,
                [typeof(DateTime?).TypeHandle] = DbType.DateTime,
                [typeof(DateTimeOffset?).TypeHandle] = DbType.DateTimeOffset
            };
            #endregion
        }

        private static DbType GetDbType(Type type)
        {
            return _dbTypeMap[type.TypeHandle];
        }

        [Fact]
        public void EnsureVerboseSqlServerFormatterOnlyAddsInformation()
        {
            // arrange
			// overwrite the formatter
	        _formatter = new VerboseSqlServerFormatter(true);
            _commandText = "select 1";
            const string expectedOutput = "-- Command Type: Text\r\n-- Database: TestDatabase\r\n\r\nselect 1;";
            CreateDbCommand(CommandType.Text);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Fact]
        public void VerboseSqlServerFormatterAddsTransactionInformation()
        {
			// note: since we don't have an active sql connection we cannot test the transactions coupled to a connection
			// the only thing we can do is test the TransactionScope transaction

            // arrange
			// overwrite the formatter
	        _formatter = new VerboseSqlServerFormatter(true);
            _commandText = "select 1";
            CreateDbCommand(CommandType.Text);
#if NET461
            const string expectedOutput = "-- Command Type: Text\r\n-- Database: TestDatabase\r\n-- Transaction Scope Iso Level: Serializable\r\n\r\nselect 1;";
            var transactionScope = new TransactionScope();
            // act
            var actualOutput = GenerateOutput();
	        transactionScope.Dispose();
#else
            const string expectedOutput = "-- Command Type: Text\r\n-- Database: TestDatabase\r\n\r\nselect 1;";
            var actualOutput = GenerateOutput();
#endif

            // assert
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Fact]
        public void TabelQueryWithoutParameters()
        {
            // arrange
            _commandText = "select 1";
            const string expectedOutput = "select 1;";
            CreateDbCommand(CommandType.Text);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithOneParameters(string at)
        {
            // arrange
            _commandText = "select 1 from dbo.Table where x = @a";
            const string expectedOutput = "DECLARE @a int = 123;\r\n\r\nselect 1 from dbo.Table where x = @a;";
            CreateDbCommand(CommandType.Text);
            AddDbParameter<int>(at + "a", 123);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithTwoParameters(string at)
        {
            // arrange
            _commandText = "select 1 from dbo.Table where x = @x, y = @y";
            const string expectedOutput = "DECLARE @x int = 123,\r\n        @y bigint = 123;\r\n\r\nselect 1 from dbo.Table where x = @x, y = @y;";
            CreateDbCommand(CommandType.Text);
            AddDbParameter<int>(at + "x", 123);
            AddDbParameter<long>(at + "y", 123);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithVarchar(string at)
        {
            // arrange
            _commandText = "select 1 from dbo.Table where x = @x, y = @y";
            const string expectedOutput = "DECLARE @x varchar(20) = 'bob',\r\n        @y varchar(max) = 'bob2';\r\n\r\nselect 1 from dbo.Table where x = @x, y = @y;";
            CreateDbCommand(CommandType.Text);
            AddDbParameter<string>(at + "x", "bob", size: 20, type: DbType.AnsiString);
            AddDbParameter<string>(at + "y", "bob2", size: -1, type: DbType.AnsiString);
            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Fact]
        public void StoredProcedureCallWithoutParameters()
        {
            // arrange
            _formatter = new VerboseSqlServerFormatter();
            _commandText = "dbo.SOMEPROCEDURE";
            const string expectedOutput = "EXEC dbo.SOMEPROCEDURE;";
            CreateDbCommand(CommandType.StoredProcedure);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void StoredProcedureCallWithOneParameter(string at)
        {
            // arrange
            _formatter = new VerboseSqlServerFormatter();
            _commandText = "dbo.SOMEPROCEDURE";
            const string expectedOutput = "DECLARE @x int = 123;\r\n\r\nEXEC dbo.SOMEPROCEDURE @x = @x;";
            CreateDbCommand(CommandType.StoredProcedure);
            AddDbParameter<int>(at + "x", 123, ParameterDirection.Input);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void StoredProcedureCallWithTwoParameter(string at)
        {
            // arrange
            _formatter = new VerboseSqlServerFormatter();
            _commandText = "dbo.SOMEPROCEDURE";
            const string expectedOutput = "DECLARE @x int = 123,\r\n        @y bigint = 123;\r\n\r\nEXEC dbo.SOMEPROCEDURE @x = @x, @y = @y;";
            CreateDbCommand(CommandType.StoredProcedure);
            AddDbParameter<int>(at + "x", 123, ParameterDirection.Input);
            AddDbParameter<long>(at + "y", 123, ParameterDirection.Input);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void StoredProcedureCallWithOneReturnParameter(string at)
        {
            // arrange
            _formatter = new VerboseSqlServerFormatter();
            _commandText = "dbo.SOMEPROCEDURE";
            const string expectedOutput = "DECLARE @retval int;\r\n\r\nEXEC @retval = dbo.SOMEPROCEDURE;\r\nSELECT @retval AS ReturnValue;";
            CreateDbCommand(CommandType.StoredProcedure);
            AddDbParameter<int>(at + "retval", null, ParameterDirection.ReturnValue);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void StoredProcedureCallWithNormalAndReturnParameter(string at)
        {
            // arrange
            _formatter = new VerboseSqlServerFormatter();
            _commandText = "dbo.SOMEPROCEDURE";
            const string expectedOutput = "DECLARE @x int = 123,\r\n        @retval int;\r\n\r\nEXEC @retval = dbo.SOMEPROCEDURE @x = @x;\r\nSELECT @retval AS ReturnValue;";
            CreateDbCommand(CommandType.StoredProcedure);
            AddDbParameter<int>(at + "x", 123, ParameterDirection.Input);
            AddDbParameter<int>(at + "retval", null, ParameterDirection.ReturnValue);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void StoredProcedureCallWithOneOutputParameter(string at)
        {
            // arrange
            _formatter = new VerboseSqlServerFormatter();
            _commandText = "dbo.SOMEPROCEDURE";
            const string expectedOutput = "DECLARE @x int = 123;\r\n\r\nEXEC dbo.SOMEPROCEDURE @x = @x OUTPUT;\r\nSELECT @x AS x;";
            CreateDbCommand(CommandType.StoredProcedure);
            // note: since the sql-OUTPUT parameters can be read within the procedure, we need to support setting the value
            AddDbParameter<int>(at + "x", 123, ParameterDirection.Output);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void StoredProcedureCallWithTwoOutputParameter(string at)
        {
            // arrange
            _formatter = new VerboseSqlServerFormatter();
            _commandText = "dbo.SOMEPROCEDURE";
            const string expectedOutput = "DECLARE @x int = 123,\r\n        @y int = 123;\r\n\r\nEXEC dbo.SOMEPROCEDURE @x = @x OUTPUT, @y = @y OUTPUT;\r\nSELECT @x AS x, @y AS y;";
            CreateDbCommand(CommandType.StoredProcedure);
            // note: since the sql-OUTPUT parameters can be read within the procedure, we need to support setting the value
            AddDbParameter<int>(at + "x", 123, ParameterDirection.Output);
            AddDbParameter<int>(at + "y", 123, ParameterDirection.Output);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void StoredProcedureCallWithOneOutputParameterAndOneReturnParameter(string at)
        {
            // arrange
            _formatter = new VerboseSqlServerFormatter();
            _commandText = "dbo.SOMEPROCEDURE";
            const string expectedOutput = "DECLARE @x int = 123,\r\n        @retval int;\r\n\r\nEXEC @retval = dbo.SOMEPROCEDURE @x = @x OUTPUT;\r\nSELECT @retval AS ReturnValue, @x AS x;";
            CreateDbCommand(CommandType.StoredProcedure);
            // note: since the sql-OUTPUT parameters can be read within the procedure, we need to support setting the value
            AddDbParameter<int>(at + "x", 123, ParameterDirection.Output);
            AddDbParameter<int>(at + "retval", null, ParameterDirection.ReturnValue);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void StoredProcedureCallWithInOutputParameter(string at)
        {
            // arrange
            _formatter = new VerboseSqlServerFormatter();
            _commandText = "dbo.SOMEPROCEDURE";
            const string expectedOutput = "DECLARE @x int = 123;\r\n\r\nEXEC dbo.SOMEPROCEDURE @x = @x OUTPUT;\r\nSELECT @x AS x;";
            CreateDbCommand(CommandType.StoredProcedure);
            // note: since the sql-OUTPUT parameters can be read within the procedure, we need to support setting the value
            AddDbParameter<int>(at + "x", 123, ParameterDirection.InputOutput);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.Equal(expectedOutput, actualOutput);
        }
    }
}
