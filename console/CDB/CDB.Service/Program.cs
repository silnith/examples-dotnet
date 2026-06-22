using Microsoft.Data.Sqlite;
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
        builder.Services.AddSingleton<DbConnectionStringBuilder, SqliteConnectionStringBuilder>(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            return new()
            {
                DataSource = ":memory:",
                Mode = SqliteOpenMode.ReadOnly,
                Cache = SqliteCacheMode.Private,
                ForeignKeys = true,
                RecursiveTriggers = true,
                Pooling = true,
            };
        });
        builder.Services.AddTransient<DbConnection, SqliteConnection>(serviceProvider =>
        {
            DbConnectionStringBuilder dbConnectionStringBuilder = serviceProvider.GetRequiredService<DbConnectionStringBuilder>();
            SqliteConnection sqliteConnection = new(dbConnectionStringBuilder.ConnectionString);
            sqliteConnection.Open();
            return sqliteConnection;
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
