using Microsoft.Data.Sqlite;
using Silnith.CDB.SQL;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Silnith.CDB.SQLite;

/// <summary>
/// An encapsulated SQLite database that uses a schema designed for storing
/// files from a CDB data store.
/// </summary>
public class SQLiteDataStore : SQLDataStore
{
    private const string varcharColumnType = "text";
    private const string varchar32ColumnType = "text";
    private const string char1ColumnType = "text";
    private const string numeric2ColumnType = "integer";
    private const string numeric3ColumnType = "integer";
    private const string numeric7ColumnType = "integer";
    private const string blobColumnType = "blob";

    #region SQL Parameters

    #region Universal Parameters

    private const string cdbParamName = "$cdb";

    /// <inheritdoc/>
    protected override string CdbParamName => cdbParamName;

    private const string datasetParamName = "$dataset";

    /// <inheritdoc/>
    protected override string DatasetParamName => datasetParamName;

    private const string cs1ParamName = "$component_selector_1";

    /// <inheritdoc/>
    protected override string ComponentSelector1ParamName => cs1ParamName;

    private const string cs2ParamName = "$component_selector_2";

    /// <inheritdoc/>
    protected override string ComponentSelector2ParamName => cs2ParamName;

    private const string lodParamName = "$level_of_detail";

    /// <inheritdoc/>
    protected override string LevelOfDetailParamName => lodParamName;

    private const string fileTypeParamName = "$file_type";

    /// <inheritdoc/>
    protected override string FileTypeParamName => fileTypeParamName;

    private const string contentParamName = "$content";

    /// <inheritdoc/>
    protected override string ContentParamName => contentParamName;

    #endregion

    private const string metadataNameParamName = "$metadata_name";

    /// <inheritdoc/>
    protected override string MetadataNameParamName => metadataNameParamName;

    private const string textureNameParamName = "$texture_name";

    /// <inheritdoc/>
    protected override string TextureNameParamName => textureNameParamName;

    private const string modelNameParamName = "$model_name";

    /// <inheritdoc/>
    protected override string ModelNameParamName => modelNameParamName;

    #region Feature Code Parameters

    private const string featureCategoryParamName = "$feature_category";

    /// <inheritdoc/>
    protected override string FeatureCategoryParamName => featureCategoryParamName;

    private const string featureSubcategoryParamName = "$feature_subcategory";

    /// <inheritdoc/>
    protected override string FeatureSubcategoryParamName => featureSubcategoryParamName;

    private const string featureTypeParamName = "$feature_type";

    /// <inheritdoc/>
    protected override string FeatureTypeParamName => featureTypeParamName;

    private const string featureSubcodeParamName = "$feature_subcode";

    /// <inheritdoc/>
    protected override string FeatureSubcodeParamName => featureSubcodeParamName;

    #endregion

    #region DIS Code Parameters

    private const string disKindParamName = "$dis_kind";

    /// <inheritdoc/>
    protected override string DISKindParamName => disKindParamName;

    private const string disDomainParamName = "$dis_domain";

    /// <inheritdoc/>
    protected override string DISDomainParamName => disDomainParamName;

    private const string disCountryParamName = "$dis_country";

    /// <inheritdoc/>
    protected override string DISCountryParamName => disCountryParamName;

    private const string disCategoryParamName = "$dis_category";

    /// <inheritdoc/>
    protected override string DISCategoryParamName => disCategoryParamName;

    private const string disSubcategoryParamName = "$dis_subcategory";

    /// <inheritdoc/>
    protected override string DISSubcategoryParamName => disSubcategoryParamName;

    private const string disSpecificParamName = "$dis_specific";

    /// <inheritdoc/>
    protected override string DISSpecificParamName => disSpecificParamName;

    private const string disExtraParamName = "$dis_extra";

    /// <inheritdoc/>
    protected override string DISExtraParamName => disExtraParamName;

    #endregion

    #region Tile Parameters

    private const string latitudeParamName = "$latitude";

    /// <inheritdoc/>
    protected override string LatitudeParamName => latitudeParamName;

    private const string longitudeParamName = "$longitude";

    /// <inheritdoc/>
    protected override string LongitudeParamName => longitudeParamName;

    private const string upParamName = "$up";

    /// <inheritdoc/>
    protected override string UpParamName => upParamName;

    private const string rightParamName = "$right";

    /// <inheritdoc/>
    protected override string RightParamName => rightParamName;

    #endregion

    #endregion

    private const string cdbTableName = "CDB";
    private const string metadataTableName = "Metadata";
    private const string textureTableName = "Texture";
    private const string textureLodTableName = "TextureLevelOfDetail";
    private const string geotypicalModelTableName = "GeotypicalModel";
    private const string geotypicalModelLodTableName = "GeotypicalModelLevelOfDetail";
    private const string movingModelTableName = "MovingModel";
    private const string movingModelLodTableName = "MovingModelLevelOfDetail";
    private const string tileTableName = "Tile";
    private const string tileArchivedFeatureTableName = "TileArchivedFeature";
    private const string tileArchivedTextureTableName = "TileArchivedTexture";
    private const string navigationTableName = "Navigation";

    private const string cdbColumnName = "cdb";
    private const string metadataNameColumnName = "metadata_name";
    private const string datasetColumnName = "dataset";
    private const string cs1ColumnName = "component_selector_1";
    private const string cs2ColumnName = "component_selector_2";
    private const string textureNameColumnName = "texture_name";
    private const string lodColumnName = "level_of_detail";
    private const string featureCategoryColumnName = "feature_category";
    private const string featureSubcategoryColumnName = "feature_subcategory";
    private const string featureTypeColumnName = "feature_type";
    private const string featureSubcodeColumnName = "feature_subcode";
    private const string modelNameColumnName = "model_name";
    private const string disKindColumnName = "dis_kind";
    private const string disDomainColumnName = "dis_domain";
    private const string disCountryColumnName = "dis_country";
    private const string disCategoryColumnName = "dis_category";
    private const string disSubcategoryColumnName = "dis_subcategory";
    private const string disSpecificColumnName = "dis_specific";
    private const string disExtraColumnName = "dis_extra";
    private const string latitudeColumnName = "latitude";
    private const string longitudeColumnName = "longitude";
    private const string upColumnName = "up";
    private const string rightColumnName = "right";
    private const string fileTypeColumnName = "file_type";
    private const string contentColumnName = "content";
    private const string rowidColumnName = "rowid";

    /// <inheritdoc/>
    protected override string CDBNameColumnName => cdbColumnName;

    /// <inheritdoc/>
    protected override string ContentColumnName => contentColumnName;

    #region CDB

    private const string createTableCDB = $"""
        create table "{cdbTableName}" (
            "{cdbColumnName}" {varcharColumnType} primary key
        )
        """;

    /// <inheritdoc/>
    protected override string CreateTableCDBStatement => createTableCDB;

    private const string insertIntoCDB = $"""
        insert into "{cdbTableName}" (
            "{cdbColumnName}"
        ) values (
            {cdbParamName}
        )
        """;

    /// <inheritdoc/>
    protected override string InsertIntoCDBStatement => insertIntoCDB;

    private const string selectFromCDB = $"""
        select "{cdbColumnName}"
        from "{cdbTableName}"
        """;

    /// <inheritdoc/>
    protected override string SelectFromCDBStatement => selectFromCDB;

    #endregion

    #region Metadata

    private const string createTableMetadata = $"""
        create table "{metadataTableName}" (
            "{cdbColumnName}" {varcharColumnType} not null references "{cdbTableName}"("{cdbColumnName}") on delete cascade on update cascade,
            "{metadataNameColumnName}" {varcharColumnType} not null,
            "{fileTypeColumnName}" {varcharColumnType} not null,
            "{contentColumnName}" {blobColumnType} not null,
            primary key(
                "{cdbColumnName}",
                "{metadataNameColumnName}",
                "{fileTypeColumnName}"
            )
        )
        """;

    /// <inheritdoc/>
    protected override string CreateTableMetadataStatement => createTableMetadata;

    private const string insertIntoMetadata = $"""
        insert into "{metadataTableName}" (
            "{cdbColumnName}",
            "{metadataNameColumnName}",
            "{fileTypeColumnName}",
            "{contentColumnName}"
        ) values (
            {cdbParamName},
            {metadataNameParamName},
            {fileTypeParamName},
            {contentParamName}
        )
        """;

    /// <inheritdoc/>
    protected override string InsertIntoMetadataStatement => insertIntoMetadata;

    /*
     * SQLite cannot handle a Stream as an input object.  Therefore this
     * implementation must convert all the Streams to byte arrays.
     */

    /// <inheritdoc/>
    public override int InsertIntoMetadata(string cdbName, Metadata metadata, Stream content)
    {
        using MemoryStream memoryStream = new();
        content.CopyTo(memoryStream);
        return InsertIntoMetadata(cdbName, metadata, memoryStream.ToArray());
    }

    /// <inheritdoc/>
    public override async Task<int> InsertIntoMetadataAsync(string cdbName, Metadata metadata, Stream content, CancellationToken cancellationToken = default)
    {
        await using MemoryStream memoryStream = new();
        await content.CopyToAsync(memoryStream, cancellationToken);
        return await InsertIntoMetadataAsync(cdbName, metadata, memoryStream.ToArray(), cancellationToken);
    }

    /*
     * If a select statement for a column of type blob also includes the
     * implicit rowid column, then the SQLite driver will return the blob
     * column as type SqliteBlob, which supports streaming the blob contents.
     * 
     * If not, the driver will return the entire blob as a MemoryStream,
     * which is fully buffered in memory.
     * 
     * https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/blob-io
     */

    private const string selectFromMetadata = $"""
        select
            "{contentColumnName}",
            {rowidColumnName}
        from "{metadataTableName}"
        where "{cdbColumnName}" = {cdbParamName}
            and "{metadataNameColumnName}" = {metadataNameParamName}
            and "{fileTypeColumnName}" = {fileTypeParamName}
        """;

    /// <inheritdoc/>
    protected override string SelectFromMetadataStatement => selectFromMetadata;

    #endregion

    #region Texture

    private const string createTableTexture = $"""
        create table "{textureTableName}" (
            "{cdbColumnName}" {varcharColumnType} not null references "{cdbTableName}"("{cdbColumnName}") on delete cascade on update cascade,
            "{datasetColumnName}" {numeric3ColumnType} not null,
            "{cs1ColumnName}" {numeric3ColumnType} not null,
            "{cs2ColumnName}" {numeric3ColumnType} not null,
            "{textureNameColumnName}" {varchar32ColumnType} not null,
            "{fileTypeColumnName}" {varcharColumnType} not null,
            "{contentColumnName}" {blobColumnType} not null,
            primary key(
                "{cdbColumnName}",
                "{datasetColumnName}",
                "{cs1ColumnName}",
                "{cs2ColumnName}",
                "{textureNameColumnName}",
                "{fileTypeColumnName}"
            )
        )
        """;

    /// <inheritdoc/>
    protected override string CreateTableTextureStatement => createTableTexture;

    private const string insertIntoTexture = $"""
        insert into "{textureTableName}" (
            "{cdbColumnName}",
            "{datasetColumnName}",
            "{cs1ColumnName}",
            "{cs2ColumnName}",
            "{textureNameColumnName}",
            "{fileTypeColumnName}",
            "{contentColumnName}"
        ) values (
            {cdbParamName},
            {datasetParamName},
            {cs1ParamName},
            {cs2ParamName},
            {textureNameParamName},
            {fileTypeParamName},
            {contentParamName}
        )
        """;

    /// <inheritdoc/>
    protected override string InsertIntoTextureStatement => insertIntoTexture;

    /// <inheritdoc/>
    public override int InsertIntoTexture(string cdbName, Texture texture, Stream content)
    {
        using MemoryStream memoryStream = new();
        content.CopyTo(memoryStream);
        return InsertIntoTexture(cdbName, texture, memoryStream.ToArray());
    }

    /// <inheritdoc/>
    public override async Task<int> InsertIntoTextureAsync(string cdbName, Texture texture, Stream content, CancellationToken cancellationToken = default)
    {
        await using MemoryStream memoryStream = new();
        await content.CopyToAsync(memoryStream, cancellationToken);
        return await InsertIntoTextureAsync(cdbName, texture, memoryStream.ToArray(), cancellationToken);
    }

    private const string selectFromTexture = $"""
        select
            "{contentColumnName}",
            {rowidColumnName}
        from "{textureTableName}"
        where "{cdbColumnName}" = {cdbParamName}
            and "{datasetColumnName}" = {datasetParamName}
            and "{cs1ColumnName}" = {cs1ParamName}
            and "{cs2ColumnName}" = {cs2ParamName}
            and "{textureNameColumnName}" = {textureNameParamName}
            and "{fileTypeColumnName}" = {fileTypeParamName}
        """;

    /// <inheritdoc/>
    protected override string SelectFromTextureStatement => selectFromTexture;

    #endregion

    #region Texture LOD

    private const string createTableTextureLod = $"""
        create table "{textureLodTableName}" (
            "{cdbColumnName}" {varcharColumnType} not null references "{cdbTableName}"("{cdbColumnName}") on delete cascade on update cascade,
            "{datasetColumnName}" {numeric3ColumnType} not null,
            "{cs1ColumnName}" {numeric3ColumnType} not null,
            "{cs2ColumnName}" {numeric3ColumnType} not null,
            "{lodColumnName}" {numeric2ColumnType} not null,
            "{textureNameColumnName}" {varchar32ColumnType} not null,
            "{fileTypeColumnName}" {varcharColumnType} not null,
            "{contentColumnName}" {blobColumnType} not null,
            primary key(
                "{cdbColumnName}",
                "{datasetColumnName}",
                "{cs1ColumnName}",
                "{cs2ColumnName}",
                "{lodColumnName}",
                "{textureNameColumnName}",
                "{fileTypeColumnName}"
            )
        )
        """;

    /// <inheritdoc/>
    protected override string CreateTableTextureLodStatement => createTableTextureLod;

    private const string insertIntoTextureLod = $"""
        insert into "{textureLodTableName}" (
            "{cdbColumnName}",
            "{datasetColumnName}",
            "{cs1ColumnName}",
            "{cs2ColumnName}",
            "{lodColumnName}",
            "{textureNameColumnName}",
            "{fileTypeColumnName}",
            "{contentColumnName}"
        ) values (
            {cdbParamName},
            {datasetParamName},
            {cs1ParamName},
            {cs2ParamName},
            {lodParamName},
            {textureNameParamName},
            {fileTypeParamName},
            {contentParamName}
        )
        """;

    /// <inheritdoc/>
    protected override string InsertIntoTextureLodStatement => insertIntoTextureLod;

    /// <inheritdoc/>
    public override int InsertIntoTextureLod(string cdbName, TextureLod textureLod, Stream content)
    {
        using MemoryStream memoryStream = new();
        content.CopyTo(memoryStream);
        return InsertIntoTextureLod(cdbName, textureLod, memoryStream.ToArray());
    }

    /// <inheritdoc/>
    public override async Task<int> InsertIntoTextureLodAsync(string cdbName, TextureLod textureLod, Stream content, CancellationToken cancellationToken = default)
    {
        await using MemoryStream memoryStream = new();
        await content.CopyToAsync(memoryStream, cancellationToken);
        return await InsertIntoTextureLodAsync(cdbName, textureLod, memoryStream.ToArray(), cancellationToken);
    }

    private const string selectFromTextureLod = $"""
        select
            "{contentColumnName}",
            {rowidColumnName}
        from "{textureLodTableName}"
        where "{cdbColumnName}" = {cdbParamName}
            and "{datasetColumnName}" = {datasetParamName}
            and "{cs1ColumnName}" = {cs1ParamName}
            and "{cs2ColumnName}" = {cs2ParamName}
            and "{lodColumnName}" = {lodParamName}
            and "{textureNameColumnName}" = {textureNameParamName}
            and "{fileTypeColumnName}" = {fileTypeParamName}
        """;

    /// <inheritdoc/>
    protected override string SelectFromTextureLodStatement => selectFromTextureLod;

    #endregion

    #region Geotypical Model

    private const string createTableGeotypicalModel = $"""
        create table "{geotypicalModelTableName}" (
            "{cdbColumnName}" {varcharColumnType} not null references "{cdbTableName}"("{cdbColumnName}") on delete cascade on update cascade,
            "{datasetColumnName}" {numeric3ColumnType} not null,
            "{cs1ColumnName}" {numeric3ColumnType} not null,
            "{cs2ColumnName}" {numeric3ColumnType} not null,
            "{featureCategoryColumnName}" {char1ColumnType} not null,
            "{featureSubcategoryColumnName}" {char1ColumnType} not null,
            "{featureTypeColumnName}" {numeric3ColumnType} not null,
            "{featureSubcodeColumnName}" {numeric3ColumnType} not null,
            "{modelNameColumnName}" {varchar32ColumnType} not null,
            "{fileTypeColumnName}" {varcharColumnType} not null,
            "{contentColumnName}" {blobColumnType} not null,
            primary key(
                "{cdbColumnName}",
                "{datasetColumnName}",
                "{cs1ColumnName}",
                "{cs2ColumnName}",
                "{featureCategoryColumnName}",
                "{featureSubcategoryColumnName}",
                "{featureTypeColumnName}",
                "{featureSubcodeColumnName}",
                "{modelNameColumnName}",
                "{fileTypeColumnName}"
            )
        )
        """;

    /// <inheritdoc/>
    protected override string CreateTableGeotypicalModelStatement => createTableGeotypicalModel;

    private const string insertIntoGeotypicalModel = $"""
        insert into "{geotypicalModelTableName}" (
            "{cdbColumnName}",
            "{datasetColumnName}",
            "{cs1ColumnName}",
            "{cs2ColumnName}",
            "{featureCategoryColumnName}",
            "{featureSubcategoryColumnName}",
            "{featureTypeColumnName}",
            "{featureSubcodeColumnName}",
            "{modelNameColumnName}",
            "{fileTypeColumnName}",
            "{contentColumnName}"
        ) values (
            {cdbParamName},
            {datasetParamName},
            {cs1ParamName},
            {cs2ParamName},
            {featureCategoryParamName},
            {featureSubcategoryParamName},
            {featureTypeParamName},
            {featureSubcodeParamName},
            {modelNameParamName},
            {fileTypeParamName},
            {contentParamName}
        )
        """;

    /// <inheritdoc/>
    protected override string InsertIntoGeotypicalModelStatement => insertIntoGeotypicalModel;

    /// <inheritdoc/>
    public override int InsertIntoGeotypicalModel(string cdbName, GeotypicalModel geotypicalModel, Stream content)
    {
        using MemoryStream memoryStream = new();
        content.CopyTo(memoryStream);
        return InsertIntoGeotypicalModel(cdbName, geotypicalModel, memoryStream.ToArray());
    }

    /// <inheritdoc/>
    public override async Task<int> InsertIntoGeotypicalModelAsync(string cdbName, GeotypicalModel geotypicalModel, Stream content, CancellationToken cancellationToken = default)
    {
        await using MemoryStream memoryStream = new();
        await content.CopyToAsync(memoryStream, cancellationToken);
        return await InsertIntoGeotypicalModelAsync(cdbName, geotypicalModel, memoryStream.ToArray(), cancellationToken);
    }

    private const string selectFromGeotypicalModel = $"""
        select
            "{contentColumnName}",
            {rowidColumnName}
        from "{geotypicalModelTableName}"
        where "{cdbColumnName}" = {cdbParamName}
            and "{datasetColumnName}" = {datasetParamName}
            and "{cs1ColumnName}" = {cs1ParamName}
            and "{cs2ColumnName}" = {cs2ParamName}
            and "{featureCategoryColumnName}" = {featureCategoryParamName}
            and "{featureSubcategoryColumnName}" = {featureSubcategoryParamName}
            and "{featureTypeColumnName}" = {featureTypeParamName}
            and "{featureSubcodeColumnName}" = {featureSubcodeParamName}
            and "{modelNameColumnName}" = {modelNameParamName}
            and "{fileTypeColumnName}" = {fileTypeParamName}
        """;

    /// <inheritdoc/>
    protected override string SelectFromGeotypicalModelStatement => selectFromGeotypicalModel;

    #endregion

    #region Geotypical Model LOD

    private const string createTableGeotypicalModelLod = $"""
        create table "{geotypicalModelLodTableName}" (
            "{cdbColumnName}" {varcharColumnType} not null references "{cdbTableName}"("{cdbColumnName}") on delete cascade on update cascade,
            "{datasetColumnName}" {numeric3ColumnType} not null,
            "{cs1ColumnName}" {numeric3ColumnType} not null,
            "{cs2ColumnName}" {numeric3ColumnType} not null,
            "{lodColumnName}" {numeric2ColumnType} not null,
            "{featureCategoryColumnName}" {char1ColumnType} not null,
            "{featureSubcategoryColumnName}" {char1ColumnType} not null,
            "{featureTypeColumnName}" {numeric3ColumnType} not null,
            "{featureSubcodeColumnName}" {numeric3ColumnType} not null,
            "{modelNameColumnName}" {varchar32ColumnType} not null,
            "{fileTypeColumnName}" {varcharColumnType} not null,
            "{contentColumnName}" {blobColumnType} not null,
            primary key(
                "{cdbColumnName}",
                "{datasetColumnName}",
                "{cs1ColumnName}",
                "{cs2ColumnName}",
                "{lodColumnName}",
                "{featureCategoryColumnName}",
                "{featureSubcategoryColumnName}",
                "{featureTypeColumnName}",
                "{featureSubcodeColumnName}",
                "{modelNameColumnName}",
                "{fileTypeColumnName}"
            )
        )
        """;

    /// <inheritdoc/>
    protected override string CreateTableGeotypicalModelLodStatement => createTableGeotypicalModelLod;

    private const string insertIntoGeotypicalModelLod = $"""
        insert into "{geotypicalModelLodTableName}" (
            "{cdbColumnName}",
            "{datasetColumnName}",
            "{cs1ColumnName}",
            "{cs2ColumnName}",
            "{lodColumnName}",
            "{featureCategoryColumnName}",
            "{featureSubcategoryColumnName}",
            "{featureTypeColumnName}",
            "{featureSubcodeColumnName}",
            "{modelNameColumnName}",
            "{fileTypeColumnName}",
            "{contentColumnName}"
        ) values (
            {cdbParamName},
            {datasetParamName},
            {cs1ParamName},
            {cs2ParamName},
            {lodParamName},
            {featureCategoryParamName},
            {featureSubcategoryParamName},
            {featureTypeParamName},
            {featureSubcodeParamName},
            {modelNameParamName},
            {fileTypeParamName},
            {contentParamName}
        )
        """;

    /// <inheritdoc/>
    protected override string InsertIntoGeotypicalModelLodStatement => insertIntoGeotypicalModelLod;

    /// <inheritdoc/>
    public override int InsertIntoGeotypicalModelLod(string cdbName, GeotypicalModelLod geotypicalModelLod, Stream content)
    {
        using MemoryStream memoryStream = new();
        content.CopyTo(memoryStream);
        return InsertIntoGeotypicalModelLod(cdbName, geotypicalModelLod, memoryStream.ToArray());
    }

    /// <inheritdoc/>
    public override async Task<int> InsertIntoGeotypicalModelLodAsync(string cdbName, GeotypicalModelLod geotypicalModelLod, Stream content, CancellationToken cancellationToken = default)
    {
        await using MemoryStream memoryStream = new();
        await content.CopyToAsync(memoryStream, cancellationToken);
        return await InsertIntoGeotypicalModelLodAsync(cdbName, geotypicalModelLod, memoryStream.ToArray(), cancellationToken);
    }

    private const string selectFromGeotypicalModelLod = $"""
        select
            "{contentColumnName}",
            {rowidColumnName}
        from "{geotypicalModelLodTableName}"
        where "{cdbColumnName}" = {cdbParamName}
            and "{datasetColumnName}" = {datasetParamName}
            and "{cs1ColumnName}" = {cs1ParamName}
            and "{cs2ColumnName}" = {cs2ParamName}
            and "{lodColumnName}" = {lodParamName}
            and "{featureCategoryColumnName}" = {featureCategoryParamName}
            and "{featureSubcategoryColumnName}" = {featureSubcategoryParamName}
            and "{featureTypeColumnName}" = {featureTypeParamName}
            and "{featureSubcodeColumnName}" = {featureSubcodeParamName}
            and "{modelNameColumnName}" = {modelNameParamName}
            and "{fileTypeColumnName}" = {fileTypeParamName}
        """;

    /// <inheritdoc/>
    protected override string SelectFromGeotypicalModelLodStatement => selectFromGeotypicalModelLod;

    #endregion

    #region Moving Model

    private const string createTableMovingModel = $"""
        create table "{movingModelTableName}" (
            "{cdbColumnName}" {varcharColumnType} not null references "{cdbTableName}"("{cdbColumnName}") on delete cascade on update cascade,
            "{datasetColumnName}" {numeric3ColumnType} not null,
            "{cs1ColumnName}" {numeric3ColumnType} not null,
            "{cs2ColumnName}" {numeric3ColumnType} not null,
            "{disKindColumnName}" {numeric3ColumnType} not null,
            "{disDomainColumnName}" {numeric3ColumnType} not null,
            "{disCountryColumnName}" {numeric3ColumnType} not null,
            "{disCategoryColumnName}" {numeric3ColumnType} not null,
            "{disSubcategoryColumnName}" {numeric3ColumnType} not null,
            "{disSpecificColumnName}" {numeric3ColumnType} not null,
            "{disExtraColumnName}" {numeric3ColumnType} not null,
            "{fileTypeColumnName}" {varcharColumnType} not null,
            "{contentColumnName}" {blobColumnType} not null,
            primary key(
                "{cdbColumnName}",
                "{datasetColumnName}",
                "{cs1ColumnName}",
                "{cs2ColumnName}",
                "{disKindColumnName}",
                "{disDomainColumnName}",
                "{disCountryColumnName}",
                "{disCategoryColumnName}",
                "{disSubcategoryColumnName}",
                "{disSpecificColumnName}",
                "{disExtraColumnName}",
                "{fileTypeColumnName}"
            )
        )
        """;

    /// <inheritdoc/>
    protected override string CreateTableMovingModelStatement => createTableMovingModel;

    private const string insertIntoMovingModel = $"""
        insert into "{movingModelTableName}" (
            "{cdbColumnName}",
            "{datasetColumnName}",
            "{cs1ColumnName}",
            "{cs2ColumnName}",
            "{disKindColumnName}",
            "{disDomainColumnName}",
            "{disCountryColumnName}",
            "{disCategoryColumnName}",
            "{disSubcategoryColumnName}",
            "{disSpecificColumnName}",
            "{disExtraColumnName}",
            "{fileTypeColumnName}",
            "{contentColumnName}"
        ) values (
            {cdbParamName},
            {datasetParamName},
            {cs1ParamName},
            {cs2ParamName},
            {disKindParamName},
            {disDomainParamName},
            {disCountryParamName},
            {disCategoryParamName},
            {disSubcategoryParamName},
            {disSpecificParamName},
            {disExtraParamName},
            {fileTypeParamName},
            {contentParamName}
        )
        """;

    /// <inheritdoc/>
    protected override string InsertIntoMovingModelStatement => insertIntoMovingModel;

    /// <inheritdoc/>
    public override int InsertIntoMovingModel(string cdbName, MovingModel movingModel, Stream content)
    {
        using MemoryStream memoryStream = new();
        content.CopyTo(memoryStream);
        return InsertIntoMovingModel(cdbName, movingModel, memoryStream.ToArray());
    }

    /// <inheritdoc/>
    public override async Task<int> InsertIntoMovingModelAsync(string cdbName, MovingModel movingModel, Stream content, CancellationToken cancellationToken = default)
    {
        await using MemoryStream memoryStream = new();
        await content.CopyToAsync(memoryStream, cancellationToken);
        return await InsertIntoMovingModelAsync(cdbName, movingModel, memoryStream.ToArray(), cancellationToken);
    }

    private const string selectFromMovingModel = $"""
        select
            "{contentColumnName}",
            {rowidColumnName}
        from "{movingModelTableName}"
        where "{cdbColumnName}" = {cdbParamName}
            and "{datasetColumnName}" = {datasetParamName}
            and "{cs1ColumnName}" = {cs1ParamName}
            and "{cs2ColumnName}" = {cs2ParamName}
            and "{disKindColumnName}" = {disKindParamName}
            and "{disDomainColumnName}" = {disDomainParamName}
            and "{disCountryColumnName}" = {disCountryParamName}
            and "{disCategoryColumnName}" = {disCategoryParamName}
            and "{disSubcategoryColumnName}" = {disSubcategoryParamName}
            and "{disSpecificColumnName}" = {disSpecificParamName}
            and "{disExtraColumnName}" = {disExtraParamName}
            and "{fileTypeColumnName}" = {fileTypeParamName}
        """;

    /// <inheritdoc/>
    protected override string SelectFromMovingModelStatement => selectFromMovingModel;

    #endregion

    #region Moving Model LOD

    private const string createTableMovingModelLod = $"""
        create table "{movingModelLodTableName}" (
            "{cdbColumnName}" {varcharColumnType} not null references "{cdbTableName}"("{cdbColumnName}") on delete cascade on update cascade,
            "{datasetColumnName}" {numeric3ColumnType} not null,
            "{cs1ColumnName}" {numeric3ColumnType} not null,
            "{cs2ColumnName}" {numeric3ColumnType} not null,
            "{lodColumnName}" {numeric2ColumnType} not null,
            "{disKindColumnName}" {numeric3ColumnType} not null,
            "{disDomainColumnName}" {numeric3ColumnType} not null,
            "{disCountryColumnName}" {numeric3ColumnType} not null,
            "{disCategoryColumnName}" {numeric3ColumnType} not null,
            "{disSubcategoryColumnName}" {numeric3ColumnType} not null,
            "{disSpecificColumnName}" {numeric3ColumnType} not null,
            "{disExtraColumnName}" {numeric3ColumnType} not null,
            "{fileTypeColumnName}" {varcharColumnType} not null,
            "{contentColumnName}" {blobColumnType} not null,
            primary key(
                "{cdbColumnName}",
                "{datasetColumnName}",
                "{cs1ColumnName}",
                "{cs2ColumnName}",
                "{lodColumnName}",
                "{disKindColumnName}",
                "{disDomainColumnName}",
                "{disCountryColumnName}",
                "{disCategoryColumnName}",
                "{disSubcategoryColumnName}",
                "{disSpecificColumnName}",
                "{disExtraColumnName}",
                "{fileTypeColumnName}"
            )
        )
        """;

    /// <inheritdoc/>
    protected override string CreateTableMovingModelLodStatement => createTableMovingModelLod;

    private const string insertIntoMovingModelLod = $"""
        insert into "{movingModelLodTableName}" (
            "{cdbColumnName}",
            "{datasetColumnName}",
            "{cs1ColumnName}",
            "{cs2ColumnName}",
            "{lodColumnName}",
            "{disKindColumnName}",
            "{disDomainColumnName}",
            "{disCountryColumnName}",
            "{disCategoryColumnName}",
            "{disSubcategoryColumnName}",
            "{disSpecificColumnName}",
            "{disExtraColumnName}",
            "{fileTypeColumnName}",
            "{contentColumnName}"
        ) values (
            {cdbParamName},
            {datasetParamName},
            {cs1ParamName},
            {cs2ParamName},
            {lodParamName},
            {disKindParamName},
            {disDomainParamName},
            {disCountryParamName},
            {disCategoryParamName},
            {disSubcategoryParamName},
            {disSpecificParamName},
            {disExtraParamName},
            {fileTypeParamName},
            {contentParamName}
        )
        """;

    /// <inheritdoc/>
    protected override string InsertIntoMovingModelLodStatement => insertIntoMovingModelLod;

    /// <inheritdoc/>
    public override int InsertIntoMovingModelLod(string cdbName, MovingModelLod movingModelLod, Stream content)
    {
        using MemoryStream memoryStream = new();
        content.CopyTo(memoryStream);
        return InsertIntoMovingModelLod(cdbName, movingModelLod, memoryStream.ToArray());
    }

    /// <inheritdoc/>
    public override async Task<int> InsertIntoMovingModelLodAsync(string cdbName, MovingModelLod movingModelLod, Stream content, CancellationToken cancellationToken = default)
    {
        await using MemoryStream memoryStream = new();
        await content.CopyToAsync(memoryStream, cancellationToken);
        return await InsertIntoMovingModelLodAsync(cdbName, movingModelLod, memoryStream.ToArray(), cancellationToken);
    }

    private const string selectFromMovingModelLod = $"""
        select
            "{contentColumnName}",
            {rowidColumnName}
        from "{movingModelLodTableName}"
        where "{cdbColumnName}" = {cdbParamName}
            and "{datasetColumnName}" = {datasetParamName}
            and "{cs1ColumnName}" = {cs1ParamName}
            and "{cs2ColumnName}" = {cs2ParamName}
            and "{lodColumnName}" = {lodParamName}
            and "{disKindColumnName}" = {disKindParamName}
            and "{disDomainColumnName}" = {disDomainParamName}
            and "{disCountryColumnName}" = {disCountryParamName}
            and "{disCategoryColumnName}" = {disCategoryParamName}
            and "{disSubcategoryColumnName}" = {disSubcategoryParamName}
            and "{disSpecificColumnName}" = {disSpecificParamName}
            and "{disExtraColumnName}" = {disExtraParamName}
            and "{fileTypeColumnName}" = {fileTypeParamName}
        """;

    /// <inheritdoc/>
    protected override string SelectFromMovingModelLodStatement => selectFromMovingModelLod;

    #endregion

    #region Tile

    private const string createTableTile = $"""
        create table "{tileTableName}" (
            "{cdbColumnName}" {varcharColumnType} not null references "{cdbTableName}"("{cdbColumnName}") on delete cascade on update cascade,
            "{latitudeColumnName}" {numeric2ColumnType} not null,
            "{longitudeColumnName}" {numeric3ColumnType} not null,
            "{datasetColumnName}" {numeric3ColumnType} not null,
            "{cs1ColumnName}" {numeric3ColumnType} not null,
            "{cs2ColumnName}" {numeric3ColumnType} not null,
            "{lodColumnName}" {numeric2ColumnType} not null,
            "{upColumnName}" {numeric7ColumnType} not null,
            "{rightColumnName}" {numeric7ColumnType} not null,
            "{fileTypeColumnName}" {varcharColumnType} not null,
            "{contentColumnName}" {blobColumnType} not null,
            primary key(
                "{cdbColumnName}",
                "{latitudeColumnName}",
                "{longitudeColumnName}",
                "{datasetColumnName}",
                "{cs1ColumnName}",
                "{cs2ColumnName}",
                "{lodColumnName}",
                "{upColumnName}",
                "{rightColumnName}",
                "{fileTypeColumnName}"
            )
        )
        """;

    /// <inheritdoc/>
    protected override string CreateTableTileStatement => createTableTile;

    private const string insertIntoTile = $"""
        insert into "{tileTableName}" (
            "{cdbColumnName}",
            "{latitudeColumnName}",
            "{longitudeColumnName}",
            "{datasetColumnName}",
            "{cs1ColumnName}",
            "{cs2ColumnName}",
            "{lodColumnName}",
            "{upColumnName}",
            "{rightColumnName}",
            "{fileTypeColumnName}",
            "{contentColumnName}"
        ) values (
            {cdbParamName},
            {latitudeParamName},
            {longitudeParamName},
            {datasetParamName},
            {cs1ParamName},
            {cs2ParamName},
            {lodParamName},
            {upParamName},
            {rightParamName},
            {fileTypeParamName},
            {contentParamName}
        )
        """;

    /// <inheritdoc/>
    protected override string InsertIntoTileStatement => insertIntoTile;

    /// <inheritdoc/>
    public override int InsertIntoTile(string cdbName, Tile tile, Stream content)
    {
        using MemoryStream memoryStream = new();
        content.CopyTo(memoryStream);
        return InsertIntoTile(cdbName, tile, memoryStream.ToArray());
    }

    /// <inheritdoc/>
    public override async Task<int> InsertIntoTileAsync(string cdbName, Tile tile, Stream content, CancellationToken cancellationToken = default)
    {
        await using MemoryStream memoryStream = new();
        await content.CopyToAsync(memoryStream, cancellationToken);
        return await InsertIntoTileAsync(cdbName, tile, memoryStream.ToArray(), cancellationToken);
    }

    private const string selectFromTile = $"""
        select
            "{contentColumnName}",
            {rowidColumnName}
        from "{tileTableName}"
        where "{cdbColumnName}" = {cdbParamName}
            and "{latitudeColumnName}" = {latitudeParamName}
            and "{longitudeColumnName}" = {longitudeParamName}
            and "{datasetColumnName}" = {datasetParamName}
            and "{cs1ColumnName}" = {cs1ParamName}
            and "{cs2ColumnName}" = {cs2ParamName}
            and "{lodColumnName}" = {lodParamName}
            and "{upColumnName}" = {upParamName}
            and "{rightColumnName}" = {rightParamName}
            and "{fileTypeColumnName}" = {fileTypeParamName}
        """;

    /// <inheritdoc/>
    protected override string SelectFromTileStatement => selectFromTile;

    #endregion

    #region Tile Archived Feature

    private const string createTableTileArchivedFeature = $"""
        create table "{tileArchivedFeatureTableName}" (
            "{cdbColumnName}" {varcharColumnType} not null references "{cdbTableName}"("{cdbColumnName}") on delete cascade on update cascade,
            "{latitudeColumnName}" {numeric2ColumnType} not null,
            "{longitudeColumnName}" {numeric3ColumnType} not null,
            "{datasetColumnName}" {numeric3ColumnType} not null,
            "{cs1ColumnName}" {numeric3ColumnType} not null,
            "{cs2ColumnName}" {numeric3ColumnType} not null,
            "{lodColumnName}" {numeric2ColumnType} not null,
            "{upColumnName}" {numeric7ColumnType} not null,
            "{rightColumnName}" {numeric7ColumnType} not null,
            "{featureCategoryColumnName}" {char1ColumnType} not null,
            "{featureSubcategoryColumnName}" {char1ColumnType} not null,
            "{featureTypeColumnName}" {numeric3ColumnType} not null,
            "{featureSubcodeColumnName}" {numeric3ColumnType} not null,
            "{modelNameColumnName}" {varchar32ColumnType} not null,
            "{fileTypeColumnName}" {varcharColumnType} not null,
            "{contentColumnName}" {blobColumnType} not null,
            primary key(
                "{cdbColumnName}",
                "{latitudeColumnName}",
                "{longitudeColumnName}",
                "{datasetColumnName}",
                "{cs1ColumnName}",
                "{cs2ColumnName}",
                "{lodColumnName}",
                "{upColumnName}",
                "{rightColumnName}",
                "{featureCategoryColumnName}",
                "{featureSubcategoryColumnName}",
                "{featureTypeColumnName}",
                "{featureSubcodeColumnName}",
                "{modelNameColumnName}",
                "{fileTypeColumnName}"
            )
        )
        """;

    /// <inheritdoc/>
    protected override string CreateTableTileArchivedFeatureStatement => createTableTileArchivedFeature;

    private const string insertIntoTileArchivedFeature = $"""
        insert into "{tileArchivedFeatureTableName}" (
            "{cdbColumnName}",
            "{latitudeColumnName}",
            "{longitudeColumnName}",
            "{datasetColumnName}",
            "{cs1ColumnName}",
            "{cs2ColumnName}",
            "{lodColumnName}",
            "{upColumnName}",
            "{rightColumnName}",
            "{featureCategoryColumnName}",
            "{featureSubcategoryColumnName}",
            "{featureTypeColumnName}",
            "{featureSubcodeColumnName}",
            "{modelNameColumnName}",
            "{fileTypeColumnName}",
            "{contentColumnName}"
        ) values (
            {cdbParamName},
            {latitudeParamName},
            {longitudeParamName},
            {datasetParamName},
            {cs1ParamName},
            {cs2ParamName},
            {lodParamName},
            {upParamName},
            {rightParamName},
            {featureCategoryParamName},
            {featureSubcategoryParamName},
            {featureTypeParamName},
            {featureSubcodeParamName},
            {modelNameParamName},
            {fileTypeParamName},
            {contentParamName}
        )
        """;

    /// <inheritdoc/>
    protected override string InsertIntoTileArchivedFeatureStatement => insertIntoTileArchivedFeature;

    /// <inheritdoc/>
    public override int InsertIntoTileArchivedFeature(string cdbName, TileArchivedFeature tileArchivedFeature, Stream content)
    {
        using MemoryStream memoryStream = new();
        content.CopyTo(memoryStream);
        return InsertIntoTileArchivedFeature(cdbName, tileArchivedFeature, memoryStream.ToArray());
    }

    /// <inheritdoc/>
    public override async Task<int> InsertIntoTileArchivedFeatureAsync(string cdbName, TileArchivedFeature tileArchivedFeature, Stream content, CancellationToken cancellationToken = default)
    {
        await using MemoryStream memoryStream = new();
        await content.CopyToAsync(memoryStream, cancellationToken);
        return await InsertIntoTileArchivedFeatureAsync(cdbName, tileArchivedFeature, memoryStream.ToArray(), cancellationToken);
    }

    private const string selectFromTileArchivedFeature = $"""
        select
            "{contentColumnName}",
            {rowidColumnName}
        from "{tileArchivedFeatureTableName}"
        where "{cdbColumnName}" = {cdbParamName}
            and "{latitudeColumnName}" = {latitudeParamName}
            and "{longitudeColumnName}" = {longitudeParamName}
            and "{datasetColumnName}" = {datasetParamName}
            and "{cs1ColumnName}" = {cs1ParamName}
            and "{cs2ColumnName}" = {cs2ParamName}
            and "{lodColumnName}" = {lodParamName}
            and "{upColumnName}" = {upParamName}
            and "{rightColumnName}" = {rightParamName}
            and "{featureCategoryColumnName}" = {featureCategoryParamName}
            and "{featureSubcategoryColumnName}" = {featureSubcategoryParamName}
            and "{featureTypeColumnName}" = {featureTypeParamName}
            and "{featureSubcodeColumnName}" = {featureSubcodeParamName}
            and "{modelNameColumnName}" = {modelNameParamName}
            and "{fileTypeColumnName}" = {fileTypeParamName}
        """;

    /// <inheritdoc/>
    protected override string SelectFromTileArchivedFeatureStatement => selectFromTileArchivedFeature;

    #endregion

    #region Tile Archived Texture

    private const string createTableTileArchivedTexture = $"""
        create table "{tileArchivedTextureTableName}" (
            "{cdbColumnName}" {varcharColumnType} not null references "{cdbTableName}"("{cdbColumnName}") on delete cascade on update cascade,
            "{latitudeColumnName}" {numeric2ColumnType} not null,
            "{longitudeColumnName}" {numeric3ColumnType} not null,
            "{datasetColumnName}" {numeric3ColumnType} not null,
            "{cs1ColumnName}" {numeric3ColumnType} not null,
            "{cs2ColumnName}" {numeric3ColumnType} not null,
            "{lodColumnName}" {numeric2ColumnType} not null,
            "{upColumnName}" {numeric7ColumnType} not null,
            "{rightColumnName}" {numeric7ColumnType} not null,
            "{textureNameColumnName}" {varchar32ColumnType} not null,
            "{fileTypeColumnName}" {varcharColumnType} not null,
            "{contentColumnName}" {blobColumnType} not null,
            primary key(
                "{cdbColumnName}",
                "{latitudeColumnName}",
                "{longitudeColumnName}",
                "{datasetColumnName}",
                "{cs1ColumnName}",
                "{cs2ColumnName}",
                "{lodColumnName}",
                "{upColumnName}",
                "{rightColumnName}",
                "{textureNameColumnName}",
                "{fileTypeColumnName}"
            )
        )
        """;

    /// <inheritdoc/>
    protected override string CreateTableTileArchivedTextureStatement => createTableTileArchivedTexture;

    private const string insertIntoTileArchivedTexture = $"""
        insert into "{tileArchivedTextureTableName}" (
            "{cdbColumnName}",
            "{latitudeColumnName}",
            "{longitudeColumnName}",
            "{datasetColumnName}",
            "{cs1ColumnName}",
            "{cs2ColumnName}",
            "{lodColumnName}",
            "{upColumnName}",
            "{rightColumnName}",
            "{textureNameColumnName}",
            "{fileTypeColumnName}",
            "{contentColumnName}"
        ) values (
            {cdbParamName},
            {latitudeParamName},
            {longitudeParamName},
            {datasetParamName},
            {cs1ParamName},
            {cs2ParamName},
            {lodParamName},
            {upParamName},
            {rightParamName},
            {textureNameParamName},
            {fileTypeParamName},
            {contentParamName}
        )
        """;

    /// <inheritdoc/>
    protected override string InsertIntoTileArchivedTextureStatement => insertIntoTileArchivedTexture;

    /// <inheritdoc/>
    public override int InsertIntoTileArchivedTexture(string cdbName, TileArchivedTexture tileArchivedTexture, Stream content)
    {
        using MemoryStream memoryStream = new();
        content.CopyTo(memoryStream);
        return InsertIntoTileArchivedTexture(cdbName, tileArchivedTexture, memoryStream.ToArray());
    }

    /// <inheritdoc/>
    public override async Task<int> InsertIntoTileArchivedTextureAsync(string cdbName, TileArchivedTexture tileArchivedTexture, Stream content, CancellationToken cancellationToken = default)
    {
        await using MemoryStream memoryStream = new();
        await content.CopyToAsync(memoryStream, cancellationToken);
        return await InsertIntoTileArchivedTextureAsync(cdbName, tileArchivedTexture, memoryStream.ToArray(), cancellationToken);
    }

    private const string selectFromTileArchivedTexture = $"""
        select
            "{contentColumnName}",
            {rowidColumnName}
        from "{tileArchivedTextureTableName}"
        where "{cdbColumnName}" = {cdbParamName}
            and "{latitudeColumnName}" = {latitudeParamName}
            and "{longitudeColumnName}" = {longitudeParamName}
            and "{datasetColumnName}" = {datasetParamName}
            and "{cs1ColumnName}" = {cs1ParamName}
            and "{cs2ColumnName}" = {cs2ParamName}
            and "{lodColumnName}" = {lodParamName}
            and "{upColumnName}" = {upParamName}
            and "{rightColumnName}" = {rightParamName}
            and "{textureNameColumnName}" = {textureNameParamName}
            and "{fileTypeColumnName}" = {fileTypeParamName}
        """;

    /// <inheritdoc/>
    protected override string SelectFromTileArchivedTextureStatement => selectFromTileArchivedTexture;

    #endregion

    #region Navigation

    private const string createTableNavigation = $"""
        create table "{navigationTableName}" (
            "{cdbColumnName}" {varcharColumnType} not null references "{cdbTableName}"("{cdbColumnName}") on delete cascade on update cascade,
            "{datasetColumnName}" {numeric3ColumnType} not null,
            "{cs1ColumnName}" {numeric3ColumnType} not null,
            "{cs2ColumnName}" {numeric3ColumnType} not null,
            "{fileTypeColumnName}" {varcharColumnType} not null,
            "{contentColumnName}" {blobColumnType} not null,
            primary key(
                "{cdbColumnName}",
                "{datasetColumnName}",
                "{cs1ColumnName}",
                "{cs2ColumnName}",
                "{fileTypeColumnName}"
            )
        )
        """;

    /// <inheritdoc/>
    protected override string CreateTableNavigationStatement => createTableNavigation;

    private const string insertIntoNavigation = $"""
        insert into "{navigationTableName}" (
            "{cdbColumnName}",
            "{datasetColumnName}",
            "{cs1ColumnName}",
            "{cs2ColumnName}",
            "{fileTypeColumnName}",
            "{contentColumnName}"
        ) values (
            {cdbParamName},
            {datasetParamName},
            {cs1ParamName},
            {cs2ParamName},
            {fileTypeParamName},
            {contentParamName}
        )
        """;

    /// <inheritdoc/>
    protected override string InsertIntoNavigationStatement => insertIntoNavigation;

    /// <inheritdoc/>
    public override int InsertIntoNavigation(string cdbName, Navigation navigation, Stream content)
    {
        using MemoryStream memoryStream = new();
        content.CopyTo(memoryStream);
        return InsertIntoNavigation(cdbName, navigation, memoryStream.ToArray());
    }

    /// <inheritdoc/>
    public override async Task<int> InsertIntoNavigationAsync(string cdbName, Navigation navigation, Stream content, CancellationToken cancellationToken = default)
    {
        await using MemoryStream memoryStream = new();
        await content.CopyToAsync(memoryStream, cancellationToken);
        return await InsertIntoNavigationAsync(cdbName, navigation, memoryStream.ToArray(), cancellationToken);
    }

    private const string selectFromNavigation = $"""
        select
            "{contentColumnName}",
            {rowidColumnName}
        from "{navigationTableName}"
        where "{cdbColumnName}" = {cdbParamName}
            and "{datasetColumnName}" = {datasetParamName}
            and "{cs1ColumnName}" = {cs1ParamName}
            and "{cs2ColumnName}" = {cs2ParamName}
            and "{fileTypeColumnName}" = {fileTypeParamName}
        """;

    /// <inheritdoc/>
    protected override string SelectFromNavigationStatement => selectFromNavigation;

    #endregion

    /// <summary>
    /// Creates a new SQL data store using the provided SQLite connection.
    /// </summary>
    /// <param name="sqliteConnection">The database connection.</param>
    /// <param name="createSchema"><see langword="true"/> to run the DDL to create the schema.</param>
    public SQLiteDataStore(SqliteConnection sqliteConnection, bool createSchema = false)
        : base(sqliteConnection, createSchema)
    {
    }

}
