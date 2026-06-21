
using Microsoft.Data.Sqlite;
using System.Data.Common;

namespace CDBService;

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
        builder.Services.AddSingleton(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            string providerInvariantName = "Microsoft.Data.Sqlite";
            return DbProviderFactories.GetFactory(providerInvariantName);
        });
        builder.Services.AddSingleton(serviceProvider =>
        {
            DbProviderFactory dbProviderFactory = serviceProvider.GetRequiredService<DbProviderFactory>();
            DbConnectionStringBuilder dbConnectionStringBuilder = serviceProvider.GetRequiredService<DbConnectionStringBuilder>();
            return dbProviderFactory.CreateDataSource(dbConnectionStringBuilder.ConnectionString);
        });
        builder.Services.AddTransient(serviceProvider =>
        {
            DbDataSource dbDataSource = serviceProvider.GetRequiredService<DbDataSource>();
            return dbDataSource.OpenConnection();
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
