using Microsoft.Data.Sqlite;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Silnith.CDB.SQLite;

/// <summary>
/// An encapsulated SQLite database that uses a schema designed for storing
/// files from a CDB data store.
/// </summary>
public class SQLiteCDB : IDisposable
{
    private static void CreateAndAttachParameter(DbCommand dbCommand, string dbParameterName, DbType dbType)
    {
        DbParameter dbParameter = dbCommand.CreateParameter();
        dbCommand.Parameters.Add(dbParameter);
        dbParameter.DbType = dbType;
        dbParameter.ParameterName = dbParameterName;
    }

    #region SQL Parameters

    const string nameParamName = "$name";

    #region Universal Parameters

    const string cdbParamName = "$cdb";
    const string datasetParamName = "$dataset";
    const string cs1ParamName = "$cs1";
    const string cs2ParamName = "$cs2";
    const string lodParamName = "$lod";
    const string fileTypeParamName = "$file_type";
    const string contentParamName = "$content";

    #endregion

    const string textureNameParamName = "$texture_name";
    const string modelNameParamName = "$model_name";

    #region Feature Code Parameters

    const string featureCategoryParamName = "$feature_category";
    const string featureSubcategoryParamName = "$feature_subcategory";
    const string featureTypeParamName = "$feature_type";
    const string featureSubcodeParamName = "$feature_subcode";

    #endregion

    #region DIS Code Parameters

    const string kindParamName = "$kind";
    const string domainParamName = "$domain";
    const string countryParamName = "$country";
    const string categoryParamName = "$category";
    const string subcategoryParamName = "$subcategory";
    const string specificParamName = "$specific";
    const string extraParamName = "$extra";

    #endregion

    #region Tile Parameters

    const string latitudeParamName = "$latitude";
    const string longitudeParamName = "$longitude";
    const string upParamName = "$up";
    const string rightParamName = "$right";

    #endregion

    #endregion

    const string nameColumnName = "name";
    const string contentColumnName = "content";
    const string rowidColumnName = "rowid";

    #region CDB

    private const string createTableCDB = $"""
        create table if not exists CDB (
            {nameColumnName} text primary key
        )
        """;

    private static DbCommand CreateInsertIntoCDBCommand(DbConnection dbConnection)
    {
        const string insertIntoCDB = $"""
            insert into CDB (
                {nameColumnName}
            ) values (
                {nameParamName}
            )
            """;

        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = insertIntoCDB;
        CreateAndAttachParameter(dbCommand, nameParamName, DbType.String);
        dbCommand.Prepare();
        return dbCommand;
    }

    private static DbCommand CreateSelectFromCDBCommand(DbConnection dbConnection)
    {
        const string selectFromCDB = $"""
            select {nameColumnName}
            from CDB
            """;
        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = selectFromCDB;
        dbCommand.Prepare();
        return dbCommand;
    }

    #endregion

    #region Metadata

    private const string createTableMetadata = $"""
        create table if not exists Metadata (
            cdb text not null references CDB({nameColumnName}) on delete cascade on update cascade,
            name text not null,
            file_type text not null,
            {contentColumnName} blob not null,
            primary key(
                cdb,
                name,
                file_type
            )
        )
        """;

    private static DbCommand CreateInsertIntoMetadataCommand(DbConnection dbConnection)
    {
        const string insertIntoMetadata = $"""
            insert into Metadata (
                cdb,
                name,
                file_type,
                {contentColumnName}
            ) values (
                {cdbParamName},
                {nameParamName},
                {fileTypeParamName},
                {contentParamName}
            )
            """;

        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = insertIntoMetadata;
        CreateAndAttachParameter(dbCommand, cdbParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, nameParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, fileTypeParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, contentParamName, DbType.Binary);
        dbCommand.Prepare();
        return dbCommand;
    }

    private static DbCommand CreateSelectFromMetadataCommand(DbConnection dbConnection)
    {
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
        const string selectFromMetadata = $"""
            select
                {contentColumnName},
                {rowidColumnName}
            from Metadata
            where cdb = {cdbParamName}
                and name = {nameParamName}
                and file_type = {fileTypeParamName}
            """;

        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = selectFromMetadata;
        CreateAndAttachParameter(dbCommand, cdbParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, nameParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, fileTypeParamName, DbType.String);
        dbCommand.Prepare();
        return dbCommand;
    }

    #endregion

    #region Texture

    private const string createTableTexture = $"""
        create table if not exists Texture (
            cdb text not null references CDB({nameColumnName}) on delete cascade on update cascade,
            dataset integer not null,
            component_selector_1 integer not null,
            component_selector_2 integer not null,
            texture_name text not null,
            file_type text not null,
            {contentColumnName} blob not null,
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

    private static DbCommand CreateInsertIntoTextureCommand(DbConnection dbConnection)
    {
        const string insertIntoTexture = $"""
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

        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = insertIntoTexture;
        CreateAndAttachParameter(dbCommand, cdbParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, datasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, textureNameParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, fileTypeParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, contentParamName, DbType.Binary);
        dbCommand.Prepare();
        return dbCommand;
    }

    private static DbCommand CreateSelectFromTextureCommand(DbConnection dbConnection)
    {
        const string selectFromTexture = $"""
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

        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = selectFromTexture;
        CreateAndAttachParameter(dbCommand, cdbParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, datasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, textureNameParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, fileTypeParamName, DbType.String);
        dbCommand.Prepare();
        return dbCommand;
    }

    #endregion

    #region Texture LOD

    private const string createTableTextureLod = $"""
        create table if not exists TextureLod (
            cdb text not null references CDB({nameColumnName}) on delete cascade on update cascade,
            dataset integer not null,
            component_selector_1 integer not null,
            component_selector_2 integer not null,
            lod integer not null,
            texture_name text not null,
            file_type text not null,
            {contentColumnName} blob not null,
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

    private static DbCommand CreateInsertIntoTextureLodCommand(DbConnection dbConnection)
    {
        const string insertIntoTextureLod = $"""
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

        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = insertIntoTextureLod;
        CreateAndAttachParameter(dbCommand, cdbParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, datasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, lodParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, textureNameParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, fileTypeParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, contentParamName, DbType.Binary);
        dbCommand.Prepare();
        return dbCommand;
    }

    private static DbCommand CreateSelectFromTextureLodCommand(DbConnection dbConnection)
    {
        const string selectFromTextureLod = $"""
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

        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = selectFromTextureLod;
        CreateAndAttachParameter(dbCommand, cdbParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, datasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, lodParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, textureNameParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, fileTypeParamName, DbType.String);
        dbCommand.Prepare();
        return dbCommand;
    }

    #endregion

    #region Geotypical Model

    private const string createTableGeotypicalModel = $"""
        create table if not exists GeotypicalModel (
            cdb text not null references CDB({nameColumnName}) on delete cascade on update cascade,
            dataset integer not null,
            component_selector_1 integer not null,
            component_selector_2 integer not null,
            feature_category text not null,
            feature_subcategory text not null,
            feature_type integer not null,
            feature_subcode integer not null,
            model_name text not null,
            file_type text not null,
            {contentColumnName} blob not null,
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

    private static DbCommand CreateInsertIntoGeotypicalModelCommand(DbConnection dbConnection)
    {
        const string insertIntoGeotypicalModel = $"""
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

        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = insertIntoGeotypicalModel;
        CreateAndAttachParameter(dbCommand, cdbParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, datasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, featureCategoryParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, featureSubcategoryParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, featureTypeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, featureSubcodeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, modelNameParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, fileTypeParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, contentParamName, DbType.Binary);
        dbCommand.Prepare();
        return dbCommand;
    }

    private static DbCommand CreateSelectFromGeotypicalModelCommand(DbConnection dbConnection)
    {
        const string selectFromGeotypicalModel = $"""
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

        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = selectFromGeotypicalModel;
        CreateAndAttachParameter(dbCommand, cdbParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, datasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, featureCategoryParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, featureSubcategoryParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, featureTypeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, featureSubcodeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, modelNameParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, fileTypeParamName, DbType.String);
        dbCommand.Prepare();
        return dbCommand;
    }

    #endregion

    #region Geotypical Model LOD

    private const string createTableGeotypicalModelLod = $"""
        create table if not exists GeotypicalModelLod (
            cdb text not null references CDB({nameColumnName}) on delete cascade on update cascade,
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
            {contentColumnName} blob not null,
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

    private static DbCommand CreateInsertIntoGeotypicalModelLodCommand(DbConnection dbConnection)
    {
        const string insertIntoGeotypicalModelLod = $"""
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

        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = insertIntoGeotypicalModelLod;
        CreateAndAttachParameter(dbCommand, cdbParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, datasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, lodParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, featureCategoryParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, featureSubcategoryParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, featureTypeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, featureSubcodeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, modelNameParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, fileTypeParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, contentParamName, DbType.Binary);
        dbCommand.Prepare();
        return dbCommand;
    }

    private static DbCommand CreateSelectFromGeotypicalModelLodCommand(DbConnection dbConnection)
    {
        const string selectFromGeotypicalModelLod = $"""
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

        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = selectFromGeotypicalModelLod;
        CreateAndAttachParameter(dbCommand, cdbParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, datasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, lodParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, featureCategoryParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, featureSubcategoryParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, featureTypeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, featureSubcodeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, modelNameParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, fileTypeParamName, DbType.String);
        dbCommand.Prepare();
        return dbCommand;
    }

    #endregion

    #region Moving Model

    private const string createTableMovingModel = $"""
        create table if not exists MovingModel (
            cdb text not null references CDB({nameColumnName}) on delete cascade on update cascade,
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
            {contentColumnName} blob not null,
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

    private static DbCommand CreateInsertIntoMovingModelCommand(DbConnection dbConnection)
    {
        const string insertIntoMovingModel = $"""
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

        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = insertIntoMovingModel;
        CreateAndAttachParameter(dbCommand, cdbParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, datasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, kindParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, domainParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, countryParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, categoryParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, subcategoryParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, specificParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, extraParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, fileTypeParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, contentParamName, DbType.String);
        dbCommand.Prepare();
        return dbCommand;
    }

    private static DbCommand CreateSelectFromMovingModelCommand(DbConnection dbConnection)
    {
        const string selectFromMovingModel = $"""
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

        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = selectFromMovingModel;
        CreateAndAttachParameter(dbCommand, cdbParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, datasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, kindParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, domainParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, countryParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, categoryParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, subcategoryParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, specificParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, extraParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, fileTypeParamName, DbType.String);
        dbCommand.Prepare();
        return dbCommand;
    }

    #endregion

    #region Moving Model LOD

    private const string createTableMovingModelLod = $"""
        create table if not exists MovingModelLod (
            cdb text not null references CDB({nameColumnName}) on delete cascade on update cascade,
            dataset integer not null,
            component_selector_1 integer not null,
            component_selector_2 integer not null,
            lod integer not null,
            kind integer not null,
            domain integer not null,
            country integer not null,
            category integer not null,
            subcategory integer not null,
            specific integer not null,
            extra integer not null,
            file_type text not null,
            {contentColumnName} blob not null,
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

    private static DbCommand CreateInsertIntoMovingModelLodCommand(DbConnection dbConnection)
    {
        const string insertIntoMovingModelLod = $"""
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

        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = insertIntoMovingModelLod;
        CreateAndAttachParameter(dbCommand, cdbParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, datasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, lodParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, kindParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, domainParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, countryParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, categoryParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, subcategoryParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, specificParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, extraParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, fileTypeParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, contentParamName, DbType.String);
        dbCommand.Prepare();
        return dbCommand;
    }

    private static DbCommand CreateSelectFromMovingModelLodCommand(DbConnection dbConnection)
    {
        const string selectFromMovingModelLod = $"""
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

        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = selectFromMovingModelLod;
        CreateAndAttachParameter(dbCommand, cdbParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, datasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, lodParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, kindParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, domainParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, countryParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, categoryParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, subcategoryParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, specificParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, extraParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, fileTypeParamName, DbType.String);
        dbCommand.Prepare();
        return dbCommand;
    }

    #endregion

    #region Tile

    private const string CreateTableTile = $"""
        create table if not exists Tile (
            cdb text not null references CDB({nameColumnName}) on delete cascade on update cascade,
            latitude integer not null,
            longitude integer not null,
            dataset integer not null,
            component_selector_1 integer not null,
            component_selector_2 integer not null,
            lod integer not null,
            up integer not null,
            right integer not null,
            file_type text not null,
            {contentColumnName} blob not null,
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

    private static DbCommand CreateInsertIntoTileCommand(DbConnection dbConnection)
    {
        const string insertIntoTile = $"""
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

        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = insertIntoTile;
        CreateAndAttachParameter(dbCommand, cdbParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, latitudeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, longitudeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, datasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, lodParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, upParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, rightParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, fileTypeParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, contentParamName, DbType.Binary);
        dbCommand.Prepare();
        return dbCommand;
    }

    private static DbCommand CreateSelectFromTileCommand(DbConnection dbConnection)
    {
        const string selectFromTile = $"""
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

        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = selectFromTile;
        CreateAndAttachParameter(dbCommand, cdbParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, latitudeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, longitudeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, datasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, lodParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, upParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, rightParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, fileTypeParamName, DbType.String);
        dbCommand.Prepare();
        return dbCommand;
    }

    #endregion

    #region Navigation

    private const string createTableNavigation = $"""
        create table if not exists Navigation (
            cdb text not null references CDB({nameColumnName}) on delete cascade on update cascade,
            dataset integer not null,
            component_selector_1 integer not null,
            component_selector_2 integer not null,
            file_type text not null,
            {contentColumnName} blob not null,
            primary key(
                cdb,
                dataset,
                component_selector_1,
                component_selector_2,
                file_type
            )
        )
        """;

    private static DbCommand CreateInsertIntoNavigationCommand(DbConnection dbConnection)
    {
        const string insertIntoNavigation = $"""
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

        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = insertIntoNavigation;
        CreateAndAttachParameter(dbCommand, cdbParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, datasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, fileTypeParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, contentParamName, DbType.Binary);
        dbCommand.Prepare();
        return dbCommand;
    }

    private static DbCommand CreateSelectFromNavigationCommand(DbConnection dbConnection)
    {
        const string selectFromNavigation = $"""
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

        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = selectFromNavigation;
        CreateAndAttachParameter(dbCommand, cdbParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, datasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, fileTypeParamName, DbType.String);
        dbCommand.Prepare();
        return dbCommand;
    }

    #endregion

    private static void CreateSchema(DbConnection dbConnection)
    {
        int rowsAffected;
        using DbTransaction dbTransaction = dbConnection.BeginTransaction(IsolationLevel.Serializable);

        using DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.Transaction = dbTransaction;

        dbCommand.CommandText = createTableCDB;
        rowsAffected = dbCommand.ExecuteNonQuery();

        dbCommand.CommandText = createTableMetadata;
        rowsAffected = dbCommand.ExecuteNonQuery();

        // Need an index on texture name.
        dbCommand.CommandText = createTableTexture;
        rowsAffected = dbCommand.ExecuteNonQuery();

        // Need an index on texture name.
        dbCommand.CommandText = createTableTextureLod;
        rowsAffected = dbCommand.ExecuteNonQuery();

        // Need an index on dataset (for everything)
        // Need an index on feature_category, feature_subcategory, feature_type
        dbCommand.CommandText = createTableGeotypicalModel;
        rowsAffected = dbCommand.ExecuteNonQuery();

        // Need an index on feature_category, feature_subcategory, feature_type, lod
        dbCommand.CommandText = createTableGeotypicalModelLod;
        rowsAffected = dbCommand.ExecuteNonQuery();

        // Maybe an index on kind, domain, country, category.
        // Need an index on kind, domain, country, category, subcategory, specific, extra.
        dbCommand.CommandText = createTableMovingModel;
        rowsAffected = dbCommand.ExecuteNonQuery();

        // Maybe an index on kind, domain, country, category.
        // Need an index on kind, domain, country, category, subcategory, specific, extra.
        dbCommand.CommandText = createTableMovingModelLod;
        rowsAffected = dbCommand.ExecuteNonQuery();

        // Need an index on latitude, longitude, dataset, cs1, cs2, lod, up
        dbCommand.CommandText = CreateTableTile;
        rowsAffected = dbCommand.ExecuteNonQuery();

        dbCommand.CommandText = createTableNavigation;
        rowsAffected = dbCommand.ExecuteNonQuery();

        dbTransaction.Commit();
    }

    private readonly DbConnection dbConnection;

    #region Prepared Statement Data Members

    private readonly DbCommand insertIntoCDB;

    private readonly DbCommand selectFromCDB;

    private readonly DbCommand insertIntoMetadata;

    private readonly DbCommand selectFromMetadata;

    private readonly DbCommand insertIntoTexture;

    private readonly DbCommand selectFromTexture;

    private readonly DbCommand insertIntoTextureLod;

    private readonly DbCommand selectFromTextureLod;

    private readonly DbCommand insertIntoGeotypicalModel;

    private readonly DbCommand selectFromGeotypicalModel;

    private readonly DbCommand insertIntoGeotypicalModelLod;

    private readonly DbCommand selectFromGeotypicalModelLod;

    private readonly DbCommand insertIntoMovingModel;

    private readonly DbCommand selectFromMovingModel;

    private readonly DbCommand insertIntoMovingModelLod;

    private readonly DbCommand selectFromMovingModelLod;

    private readonly DbCommand insertIntoTile;

    private readonly DbCommand selectFromTile;

    private readonly DbCommand insertIntoNavigation;

    private readonly DbCommand selectFromNavigation;

    #endregion

    /// <summary>
    /// Creates a new CDB storage backend using the provided SQLite connection
    /// string.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <seealso cref="SqliteConnectionStringBuilder"/>
    public SQLiteCDB(string connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);

        dbConnection = new SqliteConnection(connectionString);
        dbConnection.Open();
        CreateSchema(dbConnection);
        insertIntoCDB = CreateInsertIntoCDBCommand(dbConnection);
        selectFromCDB = CreateSelectFromCDBCommand(dbConnection);
        insertIntoMetadata = CreateInsertIntoMetadataCommand(dbConnection);
        selectFromMetadata = CreateSelectFromMetadataCommand(dbConnection);
        insertIntoTexture = CreateInsertIntoTextureCommand(dbConnection);
        selectFromTexture = CreateSelectFromTextureCommand(dbConnection);
        insertIntoTextureLod = CreateInsertIntoTextureLodCommand(dbConnection);
        selectFromTextureLod = CreateSelectFromTextureLodCommand(dbConnection);
        insertIntoGeotypicalModel = CreateInsertIntoGeotypicalModelCommand(dbConnection);
        selectFromGeotypicalModel = CreateSelectFromGeotypicalModelCommand(dbConnection);
        insertIntoGeotypicalModelLod = CreateInsertIntoGeotypicalModelLodCommand(dbConnection);
        selectFromGeotypicalModelLod = CreateSelectFromGeotypicalModelLodCommand(dbConnection);
        insertIntoMovingModel = CreateInsertIntoMovingModelCommand(dbConnection);
        selectFromMovingModel = CreateSelectFromMovingModelCommand(dbConnection);
        insertIntoMovingModelLod = CreateInsertIntoMovingModelLodCommand(dbConnection);
        selectFromMovingModelLod = CreateSelectFromMovingModelLodCommand(dbConnection);
        insertIntoTile = CreateInsertIntoTileCommand(dbConnection);
        selectFromTile = CreateSelectFromTileCommand(dbConnection);
        insertIntoNavigation = CreateInsertIntoNavigationCommand(dbConnection);
        selectFromNavigation = CreateSelectFromNavigationCommand(dbConnection);
    }

    #region CDB

    /// <summary>
    /// Inserts a name into the table identifying all the unique data stores
    /// contained in the SQLite database.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An <see cref="SQLiteCDB"/> is capable of holding multiple CDB data stores.
    /// Each distinct data store is identified by a name.
    /// </para>
    /// </remarks>
    /// <param name="cdbName">The name of a new CDB data store.</param>
    /// <returns>The number of database rows affected.</returns>
    public int InsertIntoCDB(string cdbName)
    {
        insertIntoCDB.Parameters[nameParamName].Value = cdbName;
        return insertIntoCDB.ExecuteNonQuery();
    }

    /// <summary>
    /// Returns all CDB data store names in the database.
    /// </summary>
    /// <returns>All the names of the distinct CDB data stores in the database.</returns>
    public IEnumerable<string> SelectFromCDB()
    {
        List<string> names = new();
        using DbDataReader dbDataReader = selectFromCDB.ExecuteReader(CommandBehavior.SequentialAccess);
        do
        {
            while (dbDataReader.Read())
            {
                string name = dbDataReader.GetString(nameColumnName);
                names.Add(name);
            }
        } while (dbDataReader.NextResult());
        return names;
    }

    #endregion

    #region Metadata

    /// <summary>
    /// Inserts a metadata file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="metadata">The metadata identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoMetadata(string cdbName, Metadata metadata, byte[] content)
    {
        insertIntoMetadata.Parameters[cdbParamName].Value = cdbName;
        insertIntoMetadata.Parameters[nameParamName].Value = metadata.Name;
        insertIntoMetadata.Parameters[fileTypeParamName].Value = metadata.FileType;
        insertIntoMetadata.Parameters[contentParamName].Value = content;

        return insertIntoMetadata.ExecuteNonQuery();
    }

    /// <summary>
    /// Tries to find and return a metadata file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="content"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="metadata">The metadata identifier.</param>
    /// <param name="content">An output variable that will receive the file contents.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public bool TrySelectFromMetadata(string cdbName, Metadata metadata, [NotNullWhen(true)] out byte[] content)
    {
        using MemoryStream memoryStream = new();
        bool succeeded = TrySelectFromMetadata(cdbName, metadata, memoryStream);
        content = memoryStream.ToArray();
        return succeeded;
    }

    /// <summary>
    /// Tries to find and return a metadata file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="output"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="metadata">The metadata identifier.</param>
    /// <param name="output">A stream that will receive the file contents.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public bool TrySelectFromMetadata(string cdbName, Metadata metadata, Stream output)
    {
        selectFromMetadata.Parameters[cdbParamName].Value = cdbName;
        selectFromMetadata.Parameters[nameParamName].Value = metadata.Name;
        selectFromMetadata.Parameters[fileTypeParamName].Value = metadata.FileType;
        using DbDataReader dbDataReader = selectFromMetadata.ExecuteReader(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
        do
        {
            while (dbDataReader.Read())
            {
                using Stream stream = dbDataReader.GetStream(contentColumnName);
                stream.CopyTo(output);
                return true;
            }
        } while (dbDataReader.NextResult());
        return false;
    }

    #endregion

    #region Texture

    /// <summary>
    /// Inserts a texture file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="texture">The texture identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoTexture(string cdbName, Texture texture, byte[] content)
    {
        insertIntoTexture.Parameters[cdbParamName].Value = cdbName;
        insertIntoTexture.Parameters[datasetParamName].Value = texture.Dataset.Value;
        insertIntoTexture.Parameters[cs1ParamName].Value = texture.ComponentSelector1;
        insertIntoTexture.Parameters[cs2ParamName].Value = texture.ComponentSelector2;
        insertIntoTexture.Parameters[textureNameParamName].Value = texture.Name;
        insertIntoTexture.Parameters[fileTypeParamName].Value = texture.FileType;
        insertIntoTexture.Parameters[contentParamName].Value = content;

        return insertIntoTexture.ExecuteNonQuery();
    }

    /// <summary>
    /// Tries to find and return a texture file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="content"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="texture">The texture identifier.</param>
    /// <param name="content">An output variable that will receive the file contents.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public bool TrySelectFromTexture(string cdbName, Texture texture, [NotNullWhen(true)] out byte[] content)
    {
        using MemoryStream memoryStream = new();
        bool succeeded = TrySelectFromTexture(cdbName, texture, memoryStream);
        content = memoryStream.ToArray();
        return succeeded;
    }

    /// <summary>
    /// Tries to find and return a texture file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="output"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="texture">The texture identifier.</param>
    /// <param name="output">A stream that will receive the file contents.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public bool TrySelectFromTexture(string cdbName, Texture texture, Stream output)
    {
        selectFromTexture.Parameters[cdbParamName].Value = cdbName;
        selectFromTexture.Parameters[datasetParamName].Value = texture.Dataset.Value;
        selectFromTexture.Parameters[cs1ParamName].Value = texture.ComponentSelector1;
        selectFromTexture.Parameters[cs2ParamName].Value = texture.ComponentSelector2;
        selectFromTexture.Parameters[textureNameParamName].Value = texture.Name;
        selectFromTexture.Parameters[fileTypeParamName].Value = texture.FileType;
        using DbDataReader dbDataReader = selectFromTexture.ExecuteReader(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
        do
        {
            while (dbDataReader.Read())
            {
                using Stream stream = dbDataReader.GetStream(contentColumnName);
                stream.CopyTo(output);
                return true;
            }
        } while (dbDataReader.NextResult());
        return false;
    }

    #endregion

    #region Texture LOD

    /// <summary>
    /// Inserts a texture mipmap file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="textureLod">The texture mipmap identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoTextureLod(string cdbName, TextureLod textureLod, byte[] content)
    {
        insertIntoTextureLod.Parameters[cdbParamName].Value = cdbName;
        insertIntoTextureLod.Parameters[datasetParamName].Value = textureLod.Dataset.Value;
        insertIntoTextureLod.Parameters[cs1ParamName].Value = textureLod.ComponentSelector1;
        insertIntoTextureLod.Parameters[cs2ParamName].Value = textureLod.ComponentSelector2;
        insertIntoTextureLod.Parameters[lodParamName].Value = textureLod.LevelOfDetail.Value;
        insertIntoTextureLod.Parameters[textureNameParamName].Value = textureLod.Name;
        insertIntoTextureLod.Parameters[fileTypeParamName].Value = textureLod.FileType;
        insertIntoTextureLod.Parameters[contentParamName].Value = content;

        return insertIntoTextureLod.ExecuteNonQuery();
    }

    /// <summary>
    /// Tries to find and return a texture mipmap file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="content"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="textureLod">The texture mipmap identifier.</param>
    /// <param name="content">An output variable that will receive the file contents.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public bool TrySelectFromTextureLod(string cdbName, TextureLod textureLod, [NotNullWhen(true)] out byte[] content)
    {
        using MemoryStream memoryStream = new();
        bool succeeded = TrySelectFromTextureLod(cdbName, textureLod, memoryStream);
        content = memoryStream.ToArray();
        return succeeded;
    }

    /// <summary>
    /// Tries to find and return a texture mipmap file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="output"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="textureLod">The texture mipmap identifier.</param>
    /// <param name="output">A stream that will receive the file contents.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public bool TrySelectFromTextureLod(string cdbName, TextureLod textureLod, Stream output)
    {
        selectFromTextureLod.Parameters[cdbParamName].Value = cdbName;
        selectFromTextureLod.Parameters[datasetParamName].Value = textureLod.Dataset.Value;
        selectFromTextureLod.Parameters[cs1ParamName].Value = textureLod.ComponentSelector1;
        selectFromTextureLod.Parameters[cs2ParamName].Value = textureLod.ComponentSelector2;
        selectFromTextureLod.Parameters[lodParamName].Value = textureLod.LevelOfDetail.Value;
        selectFromTextureLod.Parameters[textureNameParamName].Value = textureLod.Name;
        selectFromTextureLod.Parameters[fileTypeParamName].Value = textureLod.FileType;
        using DbDataReader dbDataReader = selectFromTextureLod.ExecuteReader(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
        do
        {
            while (dbDataReader.Read())
            {
                using Stream stream = dbDataReader.GetStream(contentColumnName);
                stream.CopyTo(output);
                return true;
            }
        } while (dbDataReader.NextResult());
        return false;
    }

    #endregion

    #region Geotypical Model

    /// <summary>
    /// Inserts a geotypical model file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="geotypicalModel">The geotypical model identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoGeotypicalModel(string cdbName, GeotypicalModel geotypicalModel, byte[] content)
    {
        insertIntoGeotypicalModel.Parameters[cdbParamName].Value = cdbName;
        insertIntoGeotypicalModel.Parameters[datasetParamName].Value = geotypicalModel.Dataset.Value;
        insertIntoGeotypicalModel.Parameters[cs1ParamName].Value = geotypicalModel.ComponentSelector1;
        insertIntoGeotypicalModel.Parameters[cs2ParamName].Value = geotypicalModel.ComponentSelector2;
        insertIntoGeotypicalModel.Parameters[featureCategoryParamName].Value = geotypicalModel.FeatureCode.Category;
        insertIntoGeotypicalModel.Parameters[featureSubcategoryParamName].Value = geotypicalModel.FeatureCode.Subcategory;
        insertIntoGeotypicalModel.Parameters[featureTypeParamName].Value = geotypicalModel.FeatureCode.Type;
        insertIntoGeotypicalModel.Parameters[featureSubcodeParamName].Value = geotypicalModel.FeatureSubcode;
        insertIntoGeotypicalModel.Parameters[modelNameParamName].Value = geotypicalModel.Name;
        insertIntoGeotypicalModel.Parameters[fileTypeParamName].Value = geotypicalModel.FileType;
        insertIntoGeotypicalModel.Parameters[contentParamName].Value = content;

        return insertIntoGeotypicalModel.ExecuteNonQuery();
    }

    /// <summary>
    /// Tries to find and return a geotypical model file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="content"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="geotypicalModel">The geotypical model identifier.</param>
    /// <param name="content">An output variable that will receive the file contents.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public bool TrySelectFromGeotypicalModel(string cdbName, GeotypicalModel geotypicalModel, [NotNullWhen(true)] out byte[] content)
    {
        using MemoryStream memoryStream = new();
        bool succeeded = TrySelectFromGeotypicalModel(cdbName, geotypicalModel, memoryStream);
        content = memoryStream.ToArray();
        return succeeded;
    }

    /// <summary>
    /// Tries to find and return a geotypical model file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="output"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="geotypicalModel">The geotypical model identifier.</param>
    /// <param name="output">A stream that will receive the file contents.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public bool TrySelectFromGeotypicalModel(string cdbName, GeotypicalModel geotypicalModel, Stream output)
    {
        selectFromGeotypicalModel.Parameters[cdbParamName].Value = cdbName;
        selectFromGeotypicalModel.Parameters[datasetParamName].Value = geotypicalModel.Dataset.Value;
        selectFromGeotypicalModel.Parameters[cs1ParamName].Value = geotypicalModel.ComponentSelector1;
        selectFromGeotypicalModel.Parameters[cs2ParamName].Value = geotypicalModel.ComponentSelector2;
        selectFromGeotypicalModel.Parameters[featureCategoryParamName].Value = geotypicalModel.FeatureCode.Category;
        selectFromGeotypicalModel.Parameters[featureSubcategoryParamName].Value = geotypicalModel.FeatureCode.Subcategory;
        selectFromGeotypicalModel.Parameters[featureTypeParamName].Value = geotypicalModel.FeatureCode.Type;
        selectFromGeotypicalModel.Parameters[featureSubcodeParamName].Value = geotypicalModel.FeatureSubcode;
        selectFromGeotypicalModel.Parameters[modelNameParamName].Value = geotypicalModel.Name;
        selectFromGeotypicalModel.Parameters[fileTypeParamName].Value = geotypicalModel.FileType;
        using DbDataReader dbDataReader = selectFromGeotypicalModel.ExecuteReader(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
        do
        {
            while (dbDataReader.Read())
            {
                using Stream stream = dbDataReader.GetStream(contentColumnName);
                stream.CopyTo(output);
                return true;
            }
        } while (dbDataReader.NextResult());
        return false;
    }

    #endregion

    #region Geotypical Model LOD

    /// <summary>
    /// Inserts a geotypical model level of detail file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="geotypicalModelLod">The geotypical model level of detail identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoGeotypicalModelLod(string cdbName, GeotypicalModelLod geotypicalModelLod, byte[] content)
    {
        insertIntoGeotypicalModelLod.Parameters[cdbParamName].Value = cdbName;
        insertIntoGeotypicalModelLod.Parameters[datasetParamName].Value = geotypicalModelLod.Dataset.Value;
        insertIntoGeotypicalModelLod.Parameters[cs1ParamName].Value = geotypicalModelLod.ComponentSelector1;
        insertIntoGeotypicalModelLod.Parameters[cs2ParamName].Value = geotypicalModelLod.ComponentSelector2;
        insertIntoGeotypicalModelLod.Parameters[lodParamName].Value = geotypicalModelLod.LevelOfDetail.Value;
        insertIntoGeotypicalModelLod.Parameters[featureCategoryParamName].Value = geotypicalModelLod.FeatureCode.Category;
        insertIntoGeotypicalModelLod.Parameters[featureSubcategoryParamName].Value = geotypicalModelLod.FeatureCode.Subcategory;
        insertIntoGeotypicalModelLod.Parameters[featureTypeParamName].Value = geotypicalModelLod.FeatureCode.Type;
        insertIntoGeotypicalModelLod.Parameters[featureSubcodeParamName].Value = geotypicalModelLod.FeatureSubcode;
        insertIntoGeotypicalModelLod.Parameters[modelNameParamName].Value = geotypicalModelLod.Name;
        insertIntoGeotypicalModelLod.Parameters[fileTypeParamName].Value = geotypicalModelLod.FileType;
        insertIntoGeotypicalModelLod.Parameters[contentParamName].Value = content;

        return insertIntoGeotypicalModelLod.ExecuteNonQuery();
    }

    /// <summary>
    /// Tries to find and return a geotypical model level of detail file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="content"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="geotypicalModelLod">The geotypical model level of detail identifier.</param>
    /// <param name="content">An output variable that will receive the file contents.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public bool TrySelectFromGeotypicalModelLod(string cdbName, GeotypicalModelLod geotypicalModelLod, [NotNullWhen(true)] out byte[] content)
    {
        using MemoryStream memoryStream = new();
        bool succeeded = TrySelectFromGeotypicalModelLod(cdbName, geotypicalModelLod, memoryStream);
        content = memoryStream.ToArray();
        return succeeded;
    }

    /// <summary>
    /// Tries to find and return a geotypical model level of detail file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="output"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="geotypicalModelLod">The geotypical model level of detail identifier.</param>
    /// <param name="output">A stream that will receive the file contents.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public bool TrySelectFromGeotypicalModelLod(string cdbName, GeotypicalModelLod geotypicalModelLod, Stream output)
    {
        selectFromGeotypicalModelLod.Parameters[cdbParamName].Value = cdbName;
        selectFromGeotypicalModelLod.Parameters[datasetParamName].Value = geotypicalModelLod.Dataset.Value;
        selectFromGeotypicalModelLod.Parameters[cs1ParamName].Value = geotypicalModelLod.ComponentSelector1;
        selectFromGeotypicalModelLod.Parameters[cs2ParamName].Value = geotypicalModelLod.ComponentSelector2;
        selectFromGeotypicalModelLod.Parameters[lodParamName].Value = geotypicalModelLod.LevelOfDetail.Value;
        selectFromGeotypicalModelLod.Parameters[featureCategoryParamName].Value = geotypicalModelLod.FeatureCode.Category;
        selectFromGeotypicalModelLod.Parameters[featureSubcategoryParamName].Value = geotypicalModelLod.FeatureCode.Subcategory;
        selectFromGeotypicalModelLod.Parameters[featureTypeParamName].Value = geotypicalModelLod.FeatureCode.Type;
        selectFromGeotypicalModelLod.Parameters[featureSubcodeParamName].Value = geotypicalModelLod.FeatureSubcode;
        selectFromGeotypicalModelLod.Parameters[modelNameParamName].Value = geotypicalModelLod.Name;
        selectFromGeotypicalModelLod.Parameters[fileTypeParamName].Value = geotypicalModelLod.FileType;
        using DbDataReader dbDataReader = selectFromGeotypicalModelLod.ExecuteReader(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
        do
        {
            while (dbDataReader.Read())
            {
                using Stream stream = dbDataReader.GetStream(contentColumnName);
                stream.CopyTo(output);
                return true;
            }
        } while (dbDataReader.NextResult());
        return false;
    }

    #endregion

    #region Moving Model

    /// <summary>
    /// Inserts a moving model file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="movingModel">The moving model identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoMovingModel(string cdbName, MovingModel movingModel, byte[] content)
    {
        insertIntoMovingModel.Parameters[cdbParamName].Value = cdbName;
        insertIntoMovingModel.Parameters[datasetParamName].Value = movingModel.Dataset.Value;
        insertIntoMovingModel.Parameters[cs1ParamName].Value = movingModel.ComponentSelector1;
        insertIntoMovingModel.Parameters[cs2ParamName].Value = movingModel.ComponentSelector2;
        insertIntoMovingModel.Parameters[kindParamName].Value = movingModel.MMDC.Kind;
        insertIntoMovingModel.Parameters[domainParamName].Value = movingModel.MMDC.Domain;
        insertIntoMovingModel.Parameters[countryParamName].Value = movingModel.MMDC.Country;
        insertIntoMovingModel.Parameters[categoryParamName].Value = movingModel.MMDC.Category;
        insertIntoMovingModel.Parameters[subcategoryParamName].Value = movingModel.MMDC.Subcategory;
        insertIntoMovingModel.Parameters[specificParamName].Value = movingModel.MMDC.Specific;
        insertIntoMovingModel.Parameters[extraParamName].Value = movingModel.MMDC.Extra;
        insertIntoMovingModel.Parameters[fileTypeParamName].Value = movingModel.FileType;
        insertIntoMovingModel.Parameters[contentParamName].Value = content;

        return insertIntoMovingModel.ExecuteNonQuery();
    }

    /// <summary>
    /// Tries to find and return a moving model file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="content"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="movingModel">The moving model identifier.</param>
    /// <param name="content">An output variable that will receive the file contents.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public bool TrySelectFromMovingModel(string cdbName, MovingModel movingModel, [NotNullWhen(true)] out byte[] content)
    {
        using MemoryStream memoryStream = new();
        bool succeeded = TrySelectFromMovingModel(cdbName, movingModel, memoryStream);
        content = memoryStream.ToArray();
        return succeeded;
    }

    /// <summary>
    /// Tries to find and return a moving model file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="output"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="movingModel">The moving model identifier.</param>
    /// <param name="output">A stream that will receive the file contents.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public bool TrySelectFromMovingModel(string cdbName, MovingModel movingModel, Stream output)
    {
        selectFromMovingModel.Parameters[cdbParamName].Value = cdbName;
        selectFromMovingModel.Parameters[datasetParamName].Value = movingModel.Dataset.Value;
        selectFromMovingModel.Parameters[cs1ParamName].Value = movingModel.ComponentSelector1;
        selectFromMovingModel.Parameters[cs2ParamName].Value = movingModel.ComponentSelector2;
        selectFromMovingModel.Parameters[kindParamName].Value = movingModel.MMDC.Kind;
        selectFromMovingModel.Parameters[domainParamName].Value = movingModel.MMDC.Domain;
        selectFromMovingModel.Parameters[countryParamName].Value = movingModel.MMDC.Country;
        selectFromMovingModel.Parameters[categoryParamName].Value = movingModel.MMDC.Category;
        selectFromMovingModel.Parameters[subcategoryParamName].Value = movingModel.MMDC.Subcategory;
        selectFromMovingModel.Parameters[specificParamName].Value = movingModel.MMDC.Specific;
        selectFromMovingModel.Parameters[extraParamName].Value = movingModel.MMDC.Extra;
        selectFromMovingModel.Parameters[fileTypeParamName].Value = movingModel.FileType;
        using DbDataReader dbDataReader = selectFromMovingModel.ExecuteReader(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
        do
        {
            while (dbDataReader.Read())
            {
                using Stream stream = dbDataReader.GetStream(contentColumnName);
                stream.CopyTo(output);
                return true;
            }
        } while (dbDataReader.NextResult());
        return false;
    }

    #endregion

    #region Moving Model LOD

    /// <summary>
    /// Inserts a moving model level of detail file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="movingModelLod">The moving model level of detail identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoMovingModelLod(string cdbName, MovingModelLod movingModelLod, byte[] content)
    {
        insertIntoMovingModelLod.Parameters[cdbParamName].Value = cdbName;
        insertIntoMovingModelLod.Parameters[datasetParamName].Value = movingModelLod.Dataset.Value;
        insertIntoMovingModelLod.Parameters[cs1ParamName].Value = movingModelLod.ComponentSelector1;
        insertIntoMovingModelLod.Parameters[cs2ParamName].Value = movingModelLod.ComponentSelector2;
        insertIntoMovingModelLod.Parameters[lodParamName].Value = movingModelLod.LevelOfDetail.Value;
        insertIntoMovingModelLod.Parameters[kindParamName].Value = movingModelLod.MMDC.Kind;
        insertIntoMovingModelLod.Parameters[domainParamName].Value = movingModelLod.MMDC.Domain;
        insertIntoMovingModelLod.Parameters[countryParamName].Value = movingModelLod.MMDC.Country;
        insertIntoMovingModelLod.Parameters[categoryParamName].Value = movingModelLod.MMDC.Category;
        insertIntoMovingModelLod.Parameters[subcategoryParamName].Value = movingModelLod.MMDC.Subcategory;
        insertIntoMovingModelLod.Parameters[specificParamName].Value = movingModelLod.MMDC.Specific;
        insertIntoMovingModelLod.Parameters[extraParamName].Value = movingModelLod.MMDC.Extra;
        insertIntoMovingModelLod.Parameters[fileTypeParamName].Value = movingModelLod.FileType;
        insertIntoMovingModelLod.Parameters[contentParamName].Value = content;

        return insertIntoMovingModelLod.ExecuteNonQuery();
    }

    /// <summary>
    /// Tries to find and return a moving model level of detail file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="content"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="movingModelLod">The moving model level of detail identifier.</param>
    /// <param name="content">An output variable that will receive the file contents.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public bool TrySelectFromMovingModelLod(string cdbName, MovingModelLod movingModelLod, [NotNullWhen(true)] out byte[] content)
    {
        using MemoryStream memoryStream = new();
        bool succeeded = TrySelectFromMovingModelLod(cdbName, movingModelLod, memoryStream);
        content = memoryStream.ToArray();
        return succeeded;
    }

    /// <summary>
    /// Tries to find and return a moving model level of detail file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="output"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="movingModelLod">The moving model level of detail identifier.</param>
    /// <param name="output">A stream that will receive the file contents.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public bool TrySelectFromMovingModelLod(string cdbName, MovingModelLod movingModelLod, Stream output)
    {
        selectFromMovingModelLod.Parameters[cdbParamName].Value = cdbName;
        selectFromMovingModelLod.Parameters[datasetParamName].Value = movingModelLod.Dataset.Value;
        selectFromMovingModelLod.Parameters[cs1ParamName].Value = movingModelLod.ComponentSelector1;
        selectFromMovingModelLod.Parameters[cs2ParamName].Value = movingModelLod.ComponentSelector2;
        selectFromMovingModelLod.Parameters[lodParamName].Value = movingModelLod.LevelOfDetail.Value;
        selectFromMovingModelLod.Parameters[kindParamName].Value = movingModelLod.MMDC.Kind;
        selectFromMovingModelLod.Parameters[domainParamName].Value = movingModelLod.MMDC.Domain;
        selectFromMovingModelLod.Parameters[countryParamName].Value = movingModelLod.MMDC.Country;
        selectFromMovingModelLod.Parameters[categoryParamName].Value = movingModelLod.MMDC.Category;
        selectFromMovingModelLod.Parameters[subcategoryParamName].Value = movingModelLod.MMDC.Subcategory;
        selectFromMovingModelLod.Parameters[specificParamName].Value = movingModelLod.MMDC.Specific;
        selectFromMovingModelLod.Parameters[extraParamName].Value = movingModelLod.MMDC.Extra;
        selectFromMovingModelLod.Parameters[fileTypeParamName].Value = movingModelLod.FileType;
        using DbDataReader dbDataReader = selectFromMovingModelLod.ExecuteReader(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
        do
        {
            while (dbDataReader.Read())
            {
                using Stream stream = dbDataReader.GetStream(contentColumnName);
                stream.CopyTo(output);
                return true;
            }
        } while (dbDataReader.NextResult());
        return false;
    }

    #endregion

    #region Tile

    /// <summary>
    /// Inserts a tiled dataset file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="tile">The tile identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoTile(string cdbName, Tile tile, byte[] content)
    {
        insertIntoTile.Parameters[cdbParamName].Value = cdbName;
        insertIntoTile.Parameters[latitudeParamName].Value = tile.LatitudeValue.Value;
        insertIntoTile.Parameters[longitudeParamName].Value = tile.LongitudeValue.Value;
        insertIntoTile.Parameters[datasetParamName].Value = tile.DatasetValue.Value;
        insertIntoTile.Parameters[cs1ParamName].Value = tile.ComponentSelector1;
        insertIntoTile.Parameters[cs2ParamName].Value = tile.ComponentSelector2;
        insertIntoTile.Parameters[lodParamName].Value = tile.Level.Value;
        insertIntoTile.Parameters[upParamName].Value = tile.Up;
        insertIntoTile.Parameters[rightParamName].Value = tile.Right;
        insertIntoTile.Parameters[fileTypeParamName].Value = tile.FileType;
        insertIntoTile.Parameters[contentParamName].Value = content;

        return insertIntoTile.ExecuteNonQuery();
    }

    /// <summary>
    /// Tries to find and return a tiled dataset file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="content"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="tile">The tile identifier.</param>
    /// <param name="content">An output variable that will receive the file contents.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public bool TrySelectFromTile(string cdbName, Tile tile, [NotNullWhen(true)] out byte[] content)
    {
        using MemoryStream memoryStream = new();
        bool succeeded = TrySelectFromTile(cdbName, tile, memoryStream);
        content = memoryStream.ToArray();
        return succeeded;
    }

    /// <summary>
    /// Tries to find and return a tiled dataset file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="output"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="tile">The tile identifier.</param>
    /// <param name="output">A stream that will receive the file contents.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public bool TrySelectFromTile(string cdbName, Tile tile, Stream output)
    {
        selectFromTile.Parameters[cdbParamName].Value = cdbName;
        selectFromTile.Parameters[latitudeParamName].Value = tile.LatitudeValue.Value;
        selectFromTile.Parameters[longitudeParamName].Value = tile.LongitudeValue.Value;
        selectFromTile.Parameters[datasetParamName].Value = tile.DatasetValue.Value;
        selectFromTile.Parameters[cs1ParamName].Value = tile.ComponentSelector1;
        selectFromTile.Parameters[cs2ParamName].Value = tile.ComponentSelector2;
        selectFromTile.Parameters[lodParamName].Value = tile.Level.Value;
        selectFromTile.Parameters[upParamName].Value = tile.Up;
        selectFromTile.Parameters[rightParamName].Value = tile.Right;
        selectFromTile.Parameters[fileTypeParamName].Value = tile.FileType;
        using DbDataReader dbDataReader = selectFromTile.ExecuteReader(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
        do
        {
            while (dbDataReader.Read())
            {
                using Stream stream = dbDataReader.GetStream(contentColumnName);
                stream.CopyTo(output);
                return true;
            }
        } while (dbDataReader.NextResult());
        return false;
    }

    #endregion

    #region Navigation

    /// <summary>
    /// Inserts a navigation file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="navigation">The navigation identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoNavigation(string cdbName, Navigation navigation, byte[] content)
    {
        insertIntoNavigation.Parameters[cdbParamName].Value = cdbName;
        insertIntoNavigation.Parameters[datasetParamName].Value = navigation.Dataset.Value;
        insertIntoNavigation.Parameters[cs1ParamName].Value = navigation.ComponentSelector1;
        insertIntoNavigation.Parameters[cs2ParamName].Value = navigation.ComponentSelector2;
        insertIntoNavigation.Parameters[fileTypeParamName].Value = navigation.FileType;
        insertIntoNavigation.Parameters[contentParamName].Value = content;

        return insertIntoNavigation.ExecuteNonQuery();
    }

    /// <summary>
    /// Tries to find and return a navigation file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="content"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="navigation">The navigation identifier.</param>
    /// <param name="content">An output variable that will receive the file contents.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public bool TrySelectFromNavigation(string cdbName, Navigation navigation, [NotNullWhen(true)] out byte[] content)
    {
        using MemoryStream memoryStream = new();
        bool succeeded = TrySelectFromNavigation(cdbName, navigation, memoryStream);
        content = memoryStream.ToArray();
        return succeeded;
    }

    /// <summary>
    /// Tries to find and return a navigation file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="output"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="navigation">The navigation identifier.</param>
    /// <param name="output">A stream that will receive the file contents.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public bool TrySelectFromNavigation(string cdbName, Navigation navigation, Stream output)
    {
        selectFromNavigation.Parameters[cdbParamName].Value = cdbName;
        selectFromNavigation.Parameters[datasetParamName].Value = navigation.Dataset.Value;
        selectFromNavigation.Parameters[cs1ParamName].Value = navigation.ComponentSelector1;
        selectFromNavigation.Parameters[cs2ParamName].Value = navigation.ComponentSelector2;
        selectFromNavigation.Parameters[fileTypeParamName].Value = navigation.FileType;
        using DbDataReader dbDataReader = selectFromNavigation.ExecuteReader(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
        do
        {
            while (dbDataReader.Read())
            {
                using Stream stream = dbDataReader.GetStream(contentColumnName);
                stream.CopyTo(output);
                return true;
            }
        } while (dbDataReader.NextResult());
        return false;
    }

    #endregion

    #region Dispose Pattern

    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                selectFromNavigation.Dispose();
                insertIntoNavigation.Dispose();
                selectFromTile.Dispose();
                insertIntoTile.Dispose();
                selectFromMovingModelLod.Dispose();
                insertIntoMovingModelLod.Dispose();
                selectFromMovingModel.Dispose();
                insertIntoMovingModel.Dispose();
                selectFromGeotypicalModelLod.Dispose();
                insertIntoGeotypicalModelLod.Dispose();
                selectFromGeotypicalModel.Dispose();
                insertIntoGeotypicalModel.Dispose();
                selectFromTextureLod.Dispose();
                insertIntoTextureLod.Dispose();
                selectFromTexture.Dispose();
                insertIntoTexture.Dispose();
                selectFromMetadata.Dispose();
                insertIntoMetadata.Dispose();
                selectFromCDB.Dispose();
                insertIntoCDB.Dispose();
                dbConnection.Dispose();
            }

            disposedValue = true;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion

}
