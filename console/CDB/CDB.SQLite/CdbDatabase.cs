using Microsoft.Data.Sqlite;
using System.Data;
using System.Data.Common;

namespace Silnith.CDB.SQLite;

public class CdbDatabase
{
    private SqliteConnection dbConnection;

    public async Task ConnectAsync(SqliteConnectionStringBuilder connectionStringBuilder, CancellationToken cancellationToken = default)
    {
        DbProviderFactories.GetFactory("Microsoft.Data.Sqlite");
        DbProviderFactories.GetFactoryClasses();
        DbProviderFactories.GetProviderInvariantNames();
        if (DbProviderFactories.TryGetFactory("Microsoft.Data.Sqlite", out var factory))
        {
            DbConnectionStringBuilder? dbConnectionStringBuilder = factory.CreateConnectionStringBuilder();
            if (dbConnectionStringBuilder is not null)
            {
                DbDataSource dbDataSource = factory.CreateDataSource(dbConnectionStringBuilder.ConnectionString);
                DbConnection dbConnection1 = await dbDataSource.OpenConnectionAsync(cancellationToken);
            }
        }
        dbConnection = new SqliteConnection(connectionStringBuilder.ConnectionString);

        await dbConnection.OpenAsync(cancellationToken);
    }

    public async Task CreateSqliteSchema(CancellationToken cancellationToken = default)
    {
        int rowsAffected;
        using DbTransaction dbTransaction = await dbConnection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        using DbCommand dbCommand = dbConnection.CreateCommand();

        dbCommand.Transaction = dbTransaction;

        dbCommand.CommandText = """
create table CDB (
    name text primary key
)
""";
        rowsAffected = await dbCommand.ExecuteNonQueryAsync(cancellationToken);

        dbCommand.CommandText = """
create table Metadata (
    cdb text not null references CDB(name),
    name text not null,
    file_type text not null,
    content blob not null,
    primary key(cdb, name, file_type)
)
""";
        rowsAffected = await dbCommand.ExecuteNonQueryAsync(cancellationToken);

        // Need an index on dataset (for everything)
        // Need an index on feature_category, feature_subcategory, feature_type
        dbCommand.CommandText = """
create table Geometry (
    cdb text not null references CDB(name),
    dataset integer not null,
    component_selector_1 integer not null,
    component_selector_2 integer not null,
    feature_category text not null,
    feature_subcategory text not null,
    feature_type integer not null,
    feature_subcode integer not null,
    model_name text not null,
    file_type text not null,
    content blob not null,
    primary key(
        cdb,
        dataset,
        component_selector_1,
        component_selector_2,
        feature_category,
        feature_subcategory,
        feature_type,
        feature_subcode,
        model_name,
        file_type
    )
)
""";
        rowsAffected = await dbCommand.ExecuteNonQueryAsync(cancellationToken);

        // Need an index on feature_category, feature_subcategory, feature_type, lod
        dbCommand.CommandText = """
create table GeometryLOD (
    cdb text not null references CDB(name),
    dataset integer not null,
    component_selector_1 integer not null,
    component_selector_2 integer not null,
    lod integer not null,
    feature_category text not null,
    feature_subcategory text not null,
    feature_type integer not null,
    feature_subcode integer not null,
    model_name text not null,
    file_type text not null,
    content blob not null,
    primary key(
        cdb,
        dataset,
        component_selector_1,
        component_selector_2,
        lod,
        feature_category,
        feature_subcategory,
        feature_type,
        feature_subcode,
        model_name,
        file_type
    )
)
""";
        rowsAffected = await dbCommand.ExecuteNonQueryAsync(cancellationToken);

        // Need an index on texture name.
        dbCommand.CommandText = """
create table TextureMetadata (
    cdb text not null references CDB(name),
    dataset integer not null,
    component_selector_1 integer not null,
    component_selector_2 integer not null,
    texture_name text not null,
    file_type text not null,
    content blob not null,
    primary key(
        cdb,
        dataset,
        component_selector_1,
        component_selector_2,
        texture_name,
        file_type
    )
)
""";
        rowsAffected = await dbCommand.ExecuteNonQueryAsync(cancellationToken);

        // Need an index on texture name.
        dbCommand.CommandText = """
create table Textures (
    cdb text not null references CDB(name),
    dataset integer not null,
    component_selector_1 integer not null,
    component_selector_2 integer not null,
    lod integer not null,
    texture_name text not null,
    file_type text not null,
    content blob not null,
    primary key(
        cdb,
        dataset,
        component_selector_1,
        component_selector_2,
        lod,
        texture_name,
        file_type
    )
)
""";
        rowsAffected = await dbCommand.ExecuteNonQueryAsync(cancellationToken);

        // Maybe an index on kind, domain, country, category.
        // Need an index on kind, domain, country, category, subcategory, specific, extra.
        dbCommand.CommandText = """
create table Models (
    cdb text not null references CDB(name),
    dataset integer not null,
    component_selector_1 integer not null,
    component_selector_2 integer not null,
    kind integer not null,
    domain integer not null,
    country integer not null,
    category integer not null,
    subcategory integer not null,
    specific integer not null,
    extra integer not null,
    file_type text not null,
    content blob not null,
    primary key(
        cdb,
        dataset,
        component_selector_1,
        component_selector_2,
        kind,
        domain,
        country,
        category,
        subcategory,
        specific,
        extra,
        file_type
    )
)
""";
        rowsAffected = await dbCommand.ExecuteNonQueryAsync(cancellationToken);

        // Need an index on latitude, longitude, dataset, cs1, cs2, lod, up
        dbCommand.CommandText = """
create table Tiles (
    cdb text not null references CDB(name),
    latitude integer not null,
    longitude integer not null,
    dataset integer not null,
    component_selector_1 integer not null,
    component_selector_2 integer not null,
    lod integer not null,
    up integer not null,
    right integer not null,
    file_type text not null,
    content blob not null,
    primary key(
        cdb,
        latitude,
        longitude,
        dataset,
        component_selector_1,
        component_selector_2,
        lod,
        up,
        right,
        file_type
    )
)
""";
        rowsAffected = await dbCommand.ExecuteNonQueryAsync(cancellationToken);

        await dbTransaction.CommitAsync(cancellationToken);
    }
}
