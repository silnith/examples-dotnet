using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silnith.CDB.SQLite;
using Silnith.CDB.Visitor;
using Silnith.CDB.XML;
using System.Globalization;

namespace Silnith.CDB.Importer;

internal class Program
{
    private static IHost Setup(string[] args)
    {
        HostApplicationBuilder hostApplicationBuilder = Host.CreateApplicationBuilder(args);

        hostApplicationBuilder.Services.AddSingleton<DISEntityDirectoryWalker>();
        hostApplicationBuilder.Services.AddSingleton<FeatureCodeDirectoryWalker>();
        hostApplicationBuilder.Services.AddSingleton<LevelOfDetailDirectoryWalker>();
        hostApplicationBuilder.Services.AddSingleton<TextureDirectoryVisitor>();
        hostApplicationBuilder.Services.AddSingleton<TileVisitor>();

        hostApplicationBuilder.Services.AddSingleton<MetadataVisitor>();
        hostApplicationBuilder.Services.AddSingleton<GeotypicalModelVisitor>();
        hostApplicationBuilder.Services.AddSingleton<MovingModelVisitor>();
        hostApplicationBuilder.Services.AddSingleton<TileVisitor>();
        hostApplicationBuilder.Services.AddSingleton<NavigationVisitor>();

        return hostApplicationBuilder.Build();
    }

    static void Main(string[] args)
    {
        using var host = Setup(args);

        SqliteConnectionStringBuilder connectionStringBuilder = new()
        {
            //DataSource = "CDB.db",
            DataSource = ":memory:",
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Default,
            ForeignKeys = true,
            RecursiveTriggers = true,
            Pooling = true,
        };
        using SqliteConnection sqliteConnection = new(connectionStringBuilder.ConnectionString);
        sqliteConnection.Open();
        using SQLiteCDB sqliteCDB = new(sqliteConnection, true);

        string cdbName = "CDB";
        DirectoryInfo cdbRoot = new(cdbName);

        sqliteCDB.ImportDirectory(cdbName, cdbRoot, host.Services);

        using IDataStore dataStore = new SQLDataStore(cdbName, cdbRoot, sqliteCDB);
        DataStoreInformation dataStoreInformation = new();
        dataStoreInformation.Initialize(dataStore);

        foreach ((int code, string name) in dataStoreInformation.DatasetNames)
        {
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0:D3}_{1}", code, name));
        }

        Console.WriteLine("Done");
    }
}
