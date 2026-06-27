using Microsoft.Data.Sqlite;
using Silnith.CDB.SQLite;
using System.Data.Common;

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
        builder.Services.AddSingleton<IDataStore, SQLiteDataStore>(provider =>
        {
            provider.GetRequiredService<IConfiguration>();
            SqliteConnectionStringBuilder sqliteConnectionStringBuilder = new()
            {
                Cache = SqliteCacheMode.Default,
                DataSource = "CDB.db",
                ForeignKeys = true,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = true,
                RecursiveTriggers = true
            };
            return new("CDB", new DirectoryInfo("."), sqliteConnectionStringBuilder);
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
