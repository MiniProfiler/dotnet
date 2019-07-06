﻿using System;
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
        public void InlineParameterNamesInParameterValues()
        {
            var formatter = new InlineFormatter();
            var parameters = new List<SqlTimingParameter>
            {
                new SqlTimingParameter() { DbType = "string", Name = "url", Value = "http://www.example.com?myid=1" },
                new SqlTimingParameter() { DbType = "string", Name = "myid", Value = "1" }
            };
            const string command = "SELECT * FROM urls WHERE url = @url OR myid = @myid";
            var formatted = formatter.FormatSql(command, parameters);
            Assert.Equal("SELECT * FROM urls WHERE url = 'http://www.example.com?myid=1' OR myid = '1'", formatted);
        }

        [Fact]
        public void EnsureVerboseSqlServerFormatterOnlyAddsInformation()
        {
            // arrange
            // overwrite the formatter
            _formatter = new VerboseSqlServerFormatter(true);
            _commandText = "select 1";
            const string expectedOutput = "-- Command Type: Text\n-- Database: TestDatabase\n\nselect 1;";
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
            const string expectedOutput = "-- Command Type: Text\n-- Database: TestDatabase\n-- Transaction Scope Iso Level: Serializable\n\nselect 1;";
            var transactionScope = new TransactionScope();
            // act
            var actualOutput = GenerateOutput();
            transactionScope.Dispose();
#else
            const string expectedOutput = "-- Command Type: Text\n-- Database: TestDatabase\n\nselect 1;";
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
            const string expectedOutput = "DECLARE @a int = 123;\n\nselect 1 from dbo.Table where x = @a;";
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
            const string expectedOutput = "DECLARE @x int = 123,\n        @y bigint = 123;\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            CreateDbCommand(CommandType.Text);
            AddDbParameter<int>(at + "x", 123);
            AddDbParameter<long>(at + "y", 123);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.Equal(expectedOutput, actualOutput);
        }

        #region Single data type tests

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithBit(string at)
        {
            // arrange
            _commandText = "select 1 from dbo.Table where x = @x, y = @y";
            const string expectedOutput = "DECLARE @x bit = 1,\n        @y bit = null;\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            CreateDbCommand(CommandType.Text);
            AddDbParameter<bool?>(at + "x", true, type: DbType.Boolean);
            AddDbParameter<bool?>(at + "y", null, type: DbType.Boolean);

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
            const string expectedOutput = "DECLARE @x varchar(20) = 'bob',\n        @y varchar(max) = 'bob2';\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            CreateDbCommand(CommandType.Text);
            AddDbParameter<string>(at + "x", "bob", size: 20, type: DbType.AnsiString);
            AddDbParameter<string>(at + "y", "bob2", size: -1, type: DbType.AnsiString);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithDate(string at)
        {
            // arrange
            _commandText = "select 1 from dbo.Table where x = @x, y = @y";
            const string expectedOutput = "DECLARE @x datetime = '2017-01-30',\n        @y datetime = '2001-01-01';\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            CreateDbCommand(CommandType.Text);
            AddDbParameter<DateTime>(at + "x", new DateTime(2017, 1, 30, 5, 13, 21), type: DbType.Date);
            AddDbParameter<DateTime>(at + "y", new DateTime(2001, 1, 1, 18, 12, 11), type: DbType.Date);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.NotEqual(expectedOutput, actualOutput); // Auto-translation of DbType.Date to DbType.DateTime breaks output
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithTime(string at)
        {
            // arrange
            _commandText = "select 1 from dbo.Table where x = @x, y = @y";
            const string expectedOutput = "DECLARE @x datetime = '05:13:21',\n        @y datetime = '18:12:11';\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            CreateDbCommand(CommandType.Text);
            AddDbParameter<DateTime>(at + "x", new DateTime(2017, 1, 30, 5, 13, 21), type: DbType.Time);
            AddDbParameter<DateTime>(at + "y", new DateTime(2001, 1, 1, 18, 12, 11), type: DbType.Time);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.NotEqual(expectedOutput, actualOutput); // Auto-translation of DbType.Time to DbType.DateTime breaks output
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithDateTime(string at)
        {
            // arrange
            _commandText = "select 1 from dbo.Table where x = @x, y = @y";
            const string expectedOutput = "DECLARE @x datetime = '2017-01-30T05:13:21',\n        @y datetime = '2001-01-01T18:12:11';\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            CreateDbCommand(CommandType.Text);
            AddDbParameter<DateTime>(at + "x", new DateTime(2017, 1, 30, 5, 13, 21), type: DbType.DateTime);
            AddDbParameter<DateTime>(at + "y", new DateTime(2001, 1, 1, 18, 12, 11), type: DbType.DateTime);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithDateTime2(string at)
        {
            // arrange
            _commandText = "select 1 from dbo.Table where x = @x, y = @y";
            const string expectedOutput = "DECLARE @x datetime2 = '2017-01-30T05:13:21',\n        @y datetime2 = '2001-01-01T18:12:11';\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            CreateDbCommand(CommandType.Text);
            AddDbParameter<DateTime>(at + "x", new DateTime(2017, 1, 30, 5, 13, 21), type: DbType.DateTime2);
            AddDbParameter<DateTime>(at + "y", new DateTime(2001, 1, 1, 18, 12, 11), type: DbType.DateTime2);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithDateTimeOffset(string at)
        {
            // arrange
            _commandText = "select 1 from dbo.Table where x = @x, y = @y";
            const string expectedOutput = "DECLARE @x datetimeoffset = '2017-01-30T05:13:21+04:30',\n        @y datetimeoffset = '2001-01-01T18:12:11-04:30';\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            CreateDbCommand(CommandType.Text);
            AddDbParameter<DateTimeOffset>(at + "x", new DateTimeOffset(2017, 1, 30, 5, 13, 21, TimeSpan.FromHours(4.5)), type: DbType.DateTimeOffset);
            AddDbParameter<DateTimeOffset>(at + "y", new DateTimeOffset(2001, 1, 1, 18, 12, 11, TimeSpan.FromHours(-4.5)), type: DbType.DateTimeOffset);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithDouble(string at)
        {
            // arrange
            _commandText = "select 1 from dbo.Table where x = @x, y = @y";
            const string expectedOutput = "DECLARE @x float = 123.45,\n        @y float = -54.321;\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            CreateDbCommand(CommandType.Text);
            AddDbParameter<double>(at + "x", 123.45, type: DbType.Double);
            AddDbParameter<double>(at + "y", -54.321, type: DbType.Double);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithSingle(string at)
        {
            // arrange
            _commandText = "select 1 from dbo.Table where x = @x, y = @y";
            const string expectedOutput = "DECLARE @x real = 123.45,\n        @y real = -54.321;\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            CreateDbCommand(CommandType.Text);
            AddDbParameter<float>(at + "x", 123.45, type: DbType.Single);
            AddDbParameter<float>(at + "y", -54.321, type: DbType.Single);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithCurrency(string at)
        {
            // arrange
            _commandText = "select 1 from dbo.Table where x = @x, y = @y";
            const string expectedOutput = "DECLARE @x money = 123.45,\n        @y money = -54.321;\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            CreateDbCommand(CommandType.Text);
            AddDbParameter<decimal>(at + "x", 123.45, type: DbType.Currency);
            AddDbParameter<decimal>(at + "y", -54.321, type: DbType.Currency);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithDecimal(string at)
        {
            // arrange
            _commandText = "select 1 from dbo.Table where x = @x, y = @y";
            const string expectedOutput = "DECLARE @x decimal(5,2) = 123.45,\n        @y decimal(5,3) = -54.321;\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            CreateDbCommand(CommandType.Text);
            AddDbParameter<decimal>(at + "x", 123.45, type: DbType.Decimal);
            AddDbParameter<decimal>(at + "y", -54.321, type: DbType.Decimal);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithDecimalNullable(string at)
        {
            // arrange
            _commandText = "select 1 from dbo.Table where x = @x, y = @y";
            const string expectedOutput = "DECLARE @x decimal(5,2) = 123.45,\n        @y decimal = null;\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            CreateDbCommand(CommandType.Text);
            AddDbParameter<decimal?>(at + "x", 123.45);
            AddDbParameter<decimal?>(at + "y", null);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithDecimalZeroPrecision(string at)
        {
            // arrange
            _commandText = "select 1 from dbo.Table where x = @x, y = @y";
            const string expectedOutput = "DECLARE @x decimal(5,0) = 12345,\n        @y decimal(5,0) = -54321;\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            CreateDbCommand(CommandType.Text);
            AddDbParameter<decimal>(at + "x", 12345.0, type: DbType.Decimal);
            AddDbParameter<decimal>(at + "y", -54321.0, type: DbType.Decimal);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.Equal(expectedOutput, actualOutput);
        }

        [Theory]
        [MemberData(nameof(GetParamPrefixes))]
        public void TableQueryWithXml(string at)
        {
            // arrange
            _commandText = "select 1 from dbo.Table where x = @x, y = @y";
            const string expectedOutput = "DECLARE @x xml = '<root></root>',\n        @y xml = '<root><node/></root>';\n\nselect 1 from dbo.Table where x = @x, y = @y;";
            CreateDbCommand(CommandType.Text);
            AddDbParameter<string>(at + "x", "<root></root>", type: DbType.Xml);
            AddDbParameter<string>(at + "y", "<root><node/></root>", type: DbType.Xml);

            // act
            var actualOutput = GenerateOutput();

            // assert
            Assert.Equal(expectedOutput, actualOutput);
        }

        #endregion

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
            const string expectedOutput = "DECLARE @x int = 123;\n\nEXEC dbo.SOMEPROCEDURE @x = @x;";
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
            const string expectedOutput = "DECLARE @x int = 123,\n        @y bigint = 123;\n\nEXEC dbo.SOMEPROCEDURE @x = @x, @y = @y;";
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
            const string expectedOutput = "DECLARE @retval int;\n\nEXEC @retval = dbo.SOMEPROCEDURE;\nSELECT @retval AS ReturnValue;";
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
            const string expectedOutput = "DECLARE @x int = 123,\n        @retval int;\n\nEXEC @retval = dbo.SOMEPROCEDURE @x = @x;\nSELECT @retval AS ReturnValue;";
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
            const string expectedOutput = "DECLARE @x int = 123;\n\nEXEC dbo.SOMEPROCEDURE @x = @x OUTPUT;\nSELECT @x AS x;";
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
            const string expectedOutput = "DECLARE @x int = 123,\n        @y int = 123;\n\nEXEC dbo.SOMEPROCEDURE @x = @x OUTPUT, @y = @y OUTPUT;\nSELECT @x AS x, @y AS y;";
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
            const string expectedOutput = "DECLARE @x int = 123,\n        @retval int;\n\nEXEC @retval = dbo.SOMEPROCEDURE @x = @x OUTPUT;\nSELECT @retval AS ReturnValue, @x AS x;";
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
            const string expectedOutput = "DECLARE @x int = 123;\n\nEXEC dbo.SOMEPROCEDURE @x = @x OUTPUT;\nSELECT @x AS x;";
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
