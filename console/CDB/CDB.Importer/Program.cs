using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silnith.CDB.SQL;
using Silnith.CDB.SQLite;
using Silnith.CDB.Visitor;
using Silnith.CDB.XML;
using System;
using System.Globalization;
using System.IO;

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
        using SQLiteDataStore sqliteDataStore = new(sqliteConnection, true);

        string cdbName = "CDB";
        DirectoryInfo cdbRoot = new(cdbName);

        sqliteDataStore.ImportDirectory(cdbName, cdbRoot, host.Services);

        using ICDB cdb = new SQLCDB(cdbName, cdbRoot, sqliteDataStore);
        CDBInformation cdbInformation = new();
        cdbInformation.Initialize(cdb);

        foreach ((int code, string name) in cdbInformation.DatasetNames)
        {
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0:D3}_{1}", code, name));
        }

        Console.WriteLine("Done");
    }
}
