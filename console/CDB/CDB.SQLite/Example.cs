using System.Data;
using System.Data.Common;
using System.Data.SQLite;

namespace Silnith.CDB.SQLite;

public class Example
{
    public void DoSpecificAsync()
    {
        SQLiteConnectionStringBuilder sqliteConnectionStringBuilder = new("")
        {
            DataSource = "",
            ForeignKeys = true,
            RecursiveTriggers = true,
            Pooling = true,
            ReadOnly = false,
            SyncMode = SynchronizationModes.Normal,
        };
        using SQLiteConnection sqliteConnection = new(sqliteConnectionStringBuilder.ConnectionString);
        using SQLiteCommand sqliteCommand = sqliteConnection.CreateCommand();
        SQLiteParameter sqliteParameter = sqliteCommand.CreateParameter();
        sqliteCommand.Parameters.Add(sqliteParameter);
        sqliteCommand.Prepare();
        using SQLiteDataReader sqliteDataReader = sqliteCommand.ExecuteReader(CommandBehavior.Default);
        do
        {
            while (sqliteDataReader.Read())
            {
                using SQLiteBlob sqliteBlob = sqliteDataReader.GetBlob(0, true);
                sqliteBlob.Read(new byte[1024], 1024, 0);
            }
        } while (sqliteDataReader.NextResult());
    }

    public async Task DoStuffAsync(CancellationToken cancellationToken)
    {
        DbProviderFactory dbProviderFactory = DbProviderFactories.GetFactory("System.Data.Sqlite");
        DbConnectionStringBuilder? dbConnectionStringBuilder = dbProviderFactory.CreateConnectionStringBuilder();
        if (dbConnectionStringBuilder is null)
        {
            return;
        }

        dbConnectionStringBuilder.Add("Data Source", ":memory:");
        await using DbDataSource dbDataSource = dbProviderFactory.CreateDataSource(dbConnectionStringBuilder.ConnectionString);
        await using DbConnection dbConnection = await dbDataSource.OpenConnectionAsync(cancellationToken);
        await using DbCommand dbCommand = dbConnection.CreateCommand();
        const string name = "$foo";
        dbCommand.CommandText = $"""select foo from foo where foo = {name}""";
        CreateAndAttachParameter(dbCommand, name, DbType.String);
        await dbCommand.PrepareAsync(cancellationToken);
        dbCommand.Parameters[name].Value = "foo";
        await using DbDataReader dbDataReader = await dbCommand.ExecuteReaderAsync(cancellationToken);
        do
        {
            while (await dbDataReader.ReadAsync(cancellationToken))
            {
                string fooResult = await dbDataReader.GetFieldValueAsync<string>(name, cancellationToken);
            }
        } while (await dbDataReader.NextResultAsync(cancellationToken));
    }

    private static void CreateAndAttachParameter(DbCommand dbCommand, string name, DbType type)
    {
        DbParameter dbParameter = dbCommand.CreateParameter();
        dbCommand.Parameters.Add(dbParameter);
        dbParameter.DbType = type;
        dbParameter.ParameterName = name;
    }
}
