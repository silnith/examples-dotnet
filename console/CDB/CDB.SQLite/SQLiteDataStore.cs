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

    private const string kindParamName = "$dis_kind";

    /// <inheritdoc/>
    protected override string KindParamName => kindParamName;

    private const string domainParamName = "$dis_domain";

    /// <inheritdoc/>
    protected override string DomainParamName => domainParamName;

    private const string countryParamName = "$dis_country";

    /// <inheritdoc/>
    protected override string CountryParamName => countryParamName;

    private const string categoryParamName = "$dis_category";

    /// <inheritdoc/>
    protected override string CategoryParamName => categoryParamName;

    private const string subcategoryParamName = "$dis_subcategory";

    /// <inheritdoc/>
    protected override string SubcategoryParamName => subcategoryParamName;

    private const string specificParamName = "$dis_specific";

    /// <inheritdoc/>
    protected override string SpecificParamName => specificParamName;

    private const string extraParamName = "$dis_extra";

    /// <inheritdoc/>
    protected override string ExtraParamName => extraParamName;

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

    private const string cdbNameColumnName = "cdb";

    /// <inheritdoc/>
    protected override string CDBNameColumnName => cdbNameColumnName;

    private const string contentColumnName = "content";

    /// <inheritdoc/>
    protected override string ContentColumnName => contentColumnName;

    private const string rowidColumnName = "rowid";

    #region CDB

    private const string createTableCDB = $"""
        create table if not exists CDB (
            {cdbNameColumnName} {varcharColumnType} primary key
        )
        """;

    /// <inheritdoc/>
    protected override string CreateTableCDBStatement => createTableCDB;

    private const string insertIntoCDB = $"""
        insert into CDB (
            {cdbNameColumnName}
        ) values (
            {cdbParamName}
        )
        """;

    /// <inheritdoc/>
    protected override string InsertIntoCDBStatement => insertIntoCDB;

    private const string selectFromCDB = $"""
        select {cdbNameColumnName}
        from CDB
        """;

    /// <inheritdoc/>
    protected override string SelectFromCDBStatement => selectFromCDB;

    #endregion

    #region Metadata

    private const string createTableMetadata = $"""
        create table if not exists Metadata (
            cdb {varcharColumnType} not null references CDB({cdbNameColumnName}) on delete cascade on update cascade,
            name {varcharColumnType} not null,
            file_type {varcharColumnType} not null,
            {contentColumnName} {blobColumnType} not null,
            primary key(
                cdb,
                name,
                file_type
            )
        )
        """;

    /// <inheritdoc/>
    protected override string CreateTableMetadataStatement => createTableMetadata;

    private const string insertIntoMetadata = $"""
        insert into Metadata (
            cdb,
            name,
            file_type,
            {contentColumnName}
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
            {contentColumnName},
            {rowidColumnName}
        from Metadata
        where cdb = {cdbParamName}
            and name = {metadataNameParamName}
            and file_type = {fileTypeParamName}
        """;

    /// <inheritdoc/>
    protected override string SelectFromMetadataStatement => selectFromMetadata;

    #endregion

    #region Texture

    private const string createTableTexture = $"""
        create table if not exists Texture (
            cdb {varcharColumnType} not null references CDB({cdbNameColumnName}) on delete cascade on update cascade,
            dataset {numeric3ColumnType} not null,
            component_selector_1 {numeric3ColumnType} not null,
            component_selector_2 {numeric3ColumnType} not null,
            texture_name {varchar32ColumnType} not null,
            file_type {varcharColumnType} not null,
            {contentColumnName} {blobColumnType} not null,
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

    /// <inheritdoc/>
    protected override string CreateTableTextureStatement => createTableTexture;

    private const string insertIntoTexture = $"""
        insert into Texture (
            cdb,
            dataset,
            component_selector_1,
            component_selector_2,
            texture_name,
            file_type,
            {contentColumnName}
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
            {contentColumnName},
            {rowidColumnName}
        from Texture
        where cdb = {cdbParamName}
            and dataset = {datasetParamName}
            and component_selector_1 = {cs1ParamName}
            and component_selector_2 = {cs2ParamName}
            and texture_name = {textureNameParamName}
            and file_type = {fileTypeParamName}
        """;

    /// <inheritdoc/>
    protected override string SelectFromTextureStatement => selectFromTexture;

    #endregion

    #region Texture LOD

    private const string createTableTextureLod = $"""
        create table if not exists TextureLod (
            cdb {varcharColumnType} not null references CDB({cdbNameColumnName}) on delete cascade on update cascade,
            dataset {numeric3ColumnType} not null,
            component_selector_1 {numeric3ColumnType} not null,
            component_selector_2 {numeric3ColumnType} not null,
            lod {numeric2ColumnType} not null,
            texture_name {varchar32ColumnType} not null,
            file_type {varcharColumnType} not null,
            {contentColumnName} {blobColumnType} not null,
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

    /// <inheritdoc/>
    protected override string CreateTableTextureLodStatement => createTableTextureLod;

    private const string insertIntoTextureLod = $"""
        insert into TextureLod (
            cdb,
            dataset,
            component_selector_1,
            component_selector_2,
            lod,
            texture_name,
            file_type,
            {contentColumnName}
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
            {contentColumnName},
            {rowidColumnName}
        from TextureLod
        where cdb = {cdbParamName}
            and dataset = {datasetParamName}
            and component_selector_1 = {cs1ParamName}
            and component_selector_2 = {cs2ParamName}
            and lod = {lodParamName}
            and texture_name = {textureNameParamName}
            and file_type = {fileTypeParamName}
        """;

    /// <inheritdoc/>
    protected override string SelectFromTextureLodStatement => selectFromTextureLod;

    #endregion

    #region Geotypical Model

    private const string createTableGeotypicalModel = $"""
        create table if not exists GeotypicalModel (
            cdb {varcharColumnType} not null references CDB({cdbNameColumnName}) on delete cascade on update cascade,
            dataset {numeric3ColumnType} not null,
            component_selector_1 {numeric3ColumnType} not null,
            component_selector_2 {numeric3ColumnType} not null,
            feature_category {char1ColumnType} not null,
            feature_subcategory {char1ColumnType} not null,
            feature_type {numeric3ColumnType} not null,
            feature_subcode {numeric3ColumnType} not null,
            model_name {varchar32ColumnType} not null,
            file_type {varcharColumnType} not null,
            {contentColumnName} {blobColumnType} not null,
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

    /// <inheritdoc/>
    protected override string CreateTableGeotypicalModelStatement => createTableGeotypicalModel;

    private const string insertIntoGeotypicalModel = $"""
        insert into GeotypicalModel (
            cdb,
            dataset,
            component_selector_1,
            component_selector_2,
            feature_category,
            feature_subcategory,
            feature_type,
            feature_subcode,
            model_name,
            file_type,
            {contentColumnName}
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
            {contentColumnName},
            {rowidColumnName}
        from GeotypicalModel
        where cdb = {cdbParamName}
            and dataset = {datasetParamName}
            and component_selector_1 = {cs1ParamName}
            and component_selector_2 = {cs2ParamName}
            and feature_category = {featureCategoryParamName}
            and feature_subcategory = {featureSubcategoryParamName}
            and feature_type = {featureTypeParamName}
            and feature_subcode = {featureSubcodeParamName}
            and model_name = {modelNameParamName}
            and file_type = {fileTypeParamName}
        """;

    /// <inheritdoc/>
    protected override string SelectFromGeotypicalModelStatement => selectFromGeotypicalModel;

    #endregion

    #region Geotypical Model LOD

    private const string createTableGeotypicalModelLod = $"""
        create table if not exists GeotypicalModelLod (
            cdb {varcharColumnType} not null references CDB({cdbNameColumnName}) on delete cascade on update cascade,
            dataset {numeric3ColumnType} not null,
            component_selector_1 {numeric3ColumnType} not null,
            component_selector_2 {numeric3ColumnType} not null,
            lod {numeric2ColumnType} not null,
            feature_category {char1ColumnType} not null,
            feature_subcategory {char1ColumnType} not null,
            feature_type {numeric3ColumnType} not null,
            feature_subcode {numeric3ColumnType} not null,
            model_name {varchar32ColumnType} not null,
            file_type {varcharColumnType} not null,
            {contentColumnName} {blobColumnType} not null,
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

    /// <inheritdoc/>
    protected override string CreateTableGeotypicalModelLodStatement => createTableGeotypicalModelLod;

    private const string insertIntoGeotypicalModelLod = $"""
        insert into GeotypicalModelLod (
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
            file_type,
            {contentColumnName}
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
            {contentColumnName},
            {rowidColumnName}
        from GeotypicalModelLod
        where cdb = {cdbParamName}
            and dataset = {datasetParamName}
            and component_selector_1 = {cs1ParamName}
            and component_selector_2 = {cs2ParamName}
            and lod = {lodParamName}
            and feature_category = {featureCategoryParamName}
            and feature_subcategory = {featureSubcategoryParamName}
            and feature_type = {featureTypeParamName}
            and feature_subcode = {featureSubcodeParamName}
            and model_name = {modelNameParamName}
            and file_type = {fileTypeParamName}
        """;

    /// <inheritdoc/>
    protected override string SelectFromGeotypicalModelLodStatement => selectFromGeotypicalModelLod;

    #endregion

    #region Moving Model

    private const string createTableMovingModel = $"""
        create table if not exists MovingModel (
            cdb {varcharColumnType} not null references CDB({cdbNameColumnName}) on delete cascade on update cascade,
            dataset {numeric3ColumnType} not null,
            component_selector_1 {numeric3ColumnType} not null,
            component_selector_2 {numeric3ColumnType} not null,
            kind {numeric3ColumnType} not null,
            domain {numeric3ColumnType} not null,
            country {numeric3ColumnType} not null,
            category {numeric3ColumnType} not null,
            subcategory {numeric3ColumnType} not null,
            specific {numeric3ColumnType} not null,
            extra {numeric3ColumnType} not null,
            file_type {varcharColumnType} not null,
            {contentColumnName} {blobColumnType} not null,
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

    /// <inheritdoc/>
    protected override string CreateTableMovingModelStatement => createTableMovingModel;

    private const string insertIntoMovingModel = $"""
        insert into MovingModel (
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
            file_type,
            {contentColumnName}
        ) values (
            {cdbParamName},
            {datasetParamName},
            {cs1ParamName},
            {cs2ParamName},
            {kindParamName},
            {domainParamName},
            {countryParamName},
            {categoryParamName},
            {subcategoryParamName},
            {specificParamName},
            {extraParamName},
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
            {contentColumnName},
            {rowidColumnName}
        from MovingModel
        where cdb = {cdbParamName}
            and dataset = {datasetParamName}
            and component_selector_1 = {cs1ParamName}
            and component_selector_2 = {cs2ParamName}
            and kind = {kindParamName}
            and domain = {domainParamName}
            and country = {countryParamName}
            and category = {categoryParamName}
            and subcategory = {subcategoryParamName}
            and specific = {specificParamName}
            and extra = {extraParamName}
            and file_type = {fileTypeParamName}
        """;

    /// <inheritdoc/>
    protected override string SelectFromMovingModelStatement => selectFromMovingModel;

    #endregion

    #region Moving Model LOD

    private const string createTableMovingModelLod = $"""
        create table if not exists MovingModelLod (
            cdb {varcharColumnType} not null references CDB({cdbNameColumnName}) on delete cascade on update cascade,
            dataset {numeric3ColumnType} not null,
            component_selector_1 {numeric3ColumnType} not null,
            component_selector_2 {numeric3ColumnType} not null,
            lod {numeric2ColumnType} not null,
            kind {numeric3ColumnType} not null,
            domain {numeric3ColumnType} not null,
            country {numeric3ColumnType} not null,
            category {numeric3ColumnType} not null,
            subcategory {numeric3ColumnType} not null,
            specific {numeric3ColumnType} not null,
            extra {numeric3ColumnType} not null,
            file_type {varcharColumnType} not null,
            {contentColumnName} {blobColumnType} not null,
            primary key(
                cdb,
                dataset,
                component_selector_1,
                component_selector_2,
                lod,
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

    /// <inheritdoc/>
    protected override string CreateTableMovingModelLodStatement => createTableMovingModelLod;

    private const string insertIntoMovingModelLod = $"""
        insert into MovingModelLod (
            cdb,
            dataset,
            component_selector_1,
            component_selector_2,
            lod,
            kind,
            domain,
            country,
            category,
            subcategory,
            specific,
            extra,
            file_type,
            {contentColumnName}
        ) values (
            {cdbParamName},
            {datasetParamName},
            {cs1ParamName},
            {cs2ParamName},
            {lodParamName},
            {kindParamName},
            {domainParamName},
            {countryParamName},
            {categoryParamName},
            {subcategoryParamName},
            {specificParamName},
            {extraParamName},
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
            {contentColumnName},
            {rowidColumnName}
        from MovingModelLod
        where cdb = {cdbParamName}
            and dataset = {datasetParamName}
            and component_selector_1 = {cs1ParamName}
            and component_selector_2 = {cs2ParamName}
            and lod = {lodParamName}
            and kind = {kindParamName}
            and domain = {domainParamName}
            and country = {countryParamName}
            and category = {categoryParamName}
            and subcategory = {subcategoryParamName}
            and specific = {specificParamName}
            and extra = {extraParamName}
            and file_type = {fileTypeParamName}
        """;

    /// <inheritdoc/>
    protected override string SelectFromMovingModelLodStatement => selectFromMovingModelLod;

    #endregion

    #region Tile

    private const string createTableTile = $"""
        create table if not exists Tile (
            cdb {varcharColumnType} not null references CDB({cdbNameColumnName}) on delete cascade on update cascade,
            latitude {numeric2ColumnType} not null,
            longitude {numeric3ColumnType} not null,
            dataset {numeric3ColumnType} not null,
            component_selector_1 {numeric3ColumnType} not null,
            component_selector_2 {numeric3ColumnType} not null,
            lod {numeric2ColumnType} not null,
            up {numeric7ColumnType} not null,
            right {numeric7ColumnType} not null,
            file_type {varcharColumnType} not null,
            {contentColumnName} {blobColumnType} not null,
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

    /// <inheritdoc/>
    protected override string CreateTableTileStatement => createTableTile;

    private const string insertIntoTile = $"""
        insert into Tile (
            cdb,
            latitude,
            longitude,
            dataset,
            component_selector_1,
            component_selector_2,
            lod,
            up,
            right,
            file_type,
            {contentColumnName}
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
            {contentColumnName},
            {rowidColumnName}
        from Tile
        where cdb = {cdbParamName}
            and latitude = {latitudeParamName}
            and longitude = {longitudeParamName}
            and dataset = {datasetParamName}
            and component_selector_1 = {cs1ParamName}
            and component_selector_2 = {cs2ParamName}
            and lod = {lodParamName}
            and up = {upParamName}
            and right = {rightParamName}
            and file_type = {fileTypeParamName}
        """;

    /// <inheritdoc/>
    protected override string SelectFromTileStatement => selectFromTile;

    #endregion

    #region Tile Archived Feature

    private const string createTableTileArchivedFeature = $"""
        create table if not exists TileArchivedFeature (
            cdb {varcharColumnType} not null references CDB({cdbNameColumnName}) on delete cascade on update cascade,
            latitude {numeric2ColumnType} not null,
            longitude {numeric3ColumnType} not null,
            dataset {numeric3ColumnType} not null,
            component_selector_1 {numeric3ColumnType} not null,
            component_selector_2 {numeric3ColumnType} not null,
            lod {numeric2ColumnType} not null,
            up {numeric7ColumnType} not null,
            right {numeric7ColumnType} not null,
            feature_category {char1ColumnType} not null,
            feature_subcategory {char1ColumnType} not null,
            feature_type {numeric3ColumnType} not null,
            feature_subcode {numeric3ColumnType} not null,
            model_name {varchar32ColumnType} not null,
            file_type {varcharColumnType} not null,
            {contentColumnName} {blobColumnType} not null,
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
                feature_category,
                feature_subcategory,
                feature_type,
                feature_subcode,
                model_name,
                file_type
            )
        )
        """;

    /// <inheritdoc/>
    protected override string CreateTableTileArchivedFeatureStatement => createTableTileArchivedFeature;

    private const string insertIntoTileArchivedFeature = $"""
        insert into TileArchivedFeature (
            cdb,
            latitude,
            longitude,
            dataset,
            component_selector_1,
            component_selector_2,
            lod,
            up,
            right,
            feature_category,
            feature_subcategory,
            feature_type,
            feature_subcode,
            model_name,
            file_type,
            {contentColumnName}
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
            {contentColumnName},
            {rowidColumnName}
        from TileArchivedFeature
        where cdb = {cdbParamName}
            and latitude = {latitudeParamName}
            and longitude = {longitudeParamName}
            and dataset = {datasetParamName}
            and component_selector_1 = {cs1ParamName}
            and component_selector_2 = {cs2ParamName}
            and lod = {lodParamName}
            and up = {upParamName}
            and right = {rightParamName}
            and feature_category = {featureCategoryParamName}
            and feature_subcategory = {featureSubcategoryParamName}
            and feature_type = {featureTypeParamName}
            and feature_subcode = {featureSubcodeParamName}
            and model_name = {modelNameParamName}
            and file_type = {fileTypeParamName}
        """;

    /// <inheritdoc/>
    protected override string SelectFromTileArchivedFeatureStatement => selectFromTileArchivedFeature;

    #endregion

    #region Tile Archived Texture

    private const string createTableTileArchivedTexture = $"""
        create table if not exists TileArchivedTexture (
            cdb {varcharColumnType} not null references CDB({cdbNameColumnName}) on delete cascade on update cascade,
            latitude {numeric2ColumnType} not null,
            longitude {numeric3ColumnType} not null,
            dataset {numeric3ColumnType} not null,
            component_selector_1 {numeric3ColumnType} not null,
            component_selector_2 {numeric3ColumnType} not null,
            lod {numeric2ColumnType} not null,
            up {numeric7ColumnType} not null,
            right {numeric7ColumnType} not null,
            texture_name {varchar32ColumnType} not null,
            file_type {varcharColumnType} not null,
            {contentColumnName} {blobColumnType} not null,
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
                texture_name,
                file_type
            )
        )
        """;

    /// <inheritdoc/>
    protected override string CreateTableTileArchivedTextureStatement => createTableTileArchivedTexture;

    private const string insertIntoTileArchivedTexture = $"""
        insert into TileArchivedTexture (
            cdb,
            latitude,
            longitude,
            dataset,
            component_selector_1,
            component_selector_2,
            lod,
            up,
            right,
            texture_name,
            file_type,
            {contentColumnName}
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
            {contentColumnName},
            {rowidColumnName}
        from TileArchivedTexture
        where cdb = {cdbParamName}
            and latitude = {latitudeParamName}
            and longitude = {longitudeParamName}
            and dataset = {datasetParamName}
            and component_selector_1 = {cs1ParamName}
            and component_selector_2 = {cs2ParamName}
            and lod = {lodParamName}
            and up = {upParamName}
            and right = {rightParamName}
            and texture_name = {textureNameParamName}
            and file_type = {fileTypeParamName}
        """;

    /// <inheritdoc/>
    protected override string SelectFromTileArchivedTextureStatement => selectFromTileArchivedTexture;

    #endregion

    #region Navigation

    private const string createTableNavigation = $"""
        create table if not exists Navigation (
            cdb {varcharColumnType} not null references CDB({cdbNameColumnName}) on delete cascade on update cascade,
            dataset {numeric3ColumnType} not null,
            component_selector_1 {numeric3ColumnType} not null,
            component_selector_2 {numeric3ColumnType} not null,
            file_type {varcharColumnType} not null,
            {contentColumnName} {blobColumnType} not null,
            primary key(
                cdb,
                dataset,
                component_selector_1,
                component_selector_2,
                file_type
            )
        )
        """;

    /// <inheritdoc/>
    protected override string CreateTableNavigationStatement => createTableNavigation;

    private const string insertIntoNavigation = $"""
        insert into Navigation (
            cdb,
            dataset,
            component_selector_1,
            component_selector_2,
            file_type,
            {contentColumnName}
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
            {contentColumnName},
            {rowidColumnName}
        from Navigation
        where cdb = {cdbParamName}
            and dataset = {datasetParamName}
            and component_selector_1 = {cs1ParamName}
            and component_selector_2 = {cs2ParamName}
            and file_type = {fileTypeParamName}
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
