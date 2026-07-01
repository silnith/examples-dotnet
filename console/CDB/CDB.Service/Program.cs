using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silnith.CDB.SQL;
using Silnith.CDB.SQLite;
using System.IO;
using System.Threading.Tasks;

namespace Silnith.CDB.Service;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        //builder.Services.AddSingleton<IDataStore, FileSystemDataStore>(provider => null);
        builder.Services.AddSingleton(provider =>
        {
            IConfiguration configuration = provider.GetRequiredService<IConfiguration>();
            SqliteConnectionStringBuilder sqliteConnectionStringBuilder = new()
            {
                Cache = SqliteCacheMode.Default,
                DataSource = "CDB.db",
                ForeignKeys = true,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = true,
                RecursiveTriggers = true
            };
            SqliteConnection sqliteConnection = new(sqliteConnectionStringBuilder.ConnectionString);
            sqliteConnection.Open();
            return sqliteConnection;
        });
        builder.Services.AddSingleton<SQLDataStore, SQLiteDataStore>(provider =>
        {
            SqliteConnection sqliteConnection = provider.GetRequiredService<SqliteConnection>();
            return new(sqliteConnection, false);
        });
        builder.Services.AddSingleton<ICDB, SQLCDB>(provider =>
        {
            SQLDataStore sqlCDB = provider.GetRequiredService<SQLDataStore>();
            return new("CDB", new DirectoryInfo("."), sqlCDB);
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        app.UseSwagger();
        app.UseSwaggerUI();

        //app.UseHttpsRedirection();

        //app.UseAuthorization();


        app.MapControllers();

        await app.RunAsync();
    }
}
