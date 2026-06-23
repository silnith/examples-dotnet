using Microsoft.Data.Sqlite;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Silnith.CDB.Importer;

public class SQLiteCDB : IDisposable
{
    private static void CreateSqliteSchema(DbConnection dbConnection)
    {
        int rowsAffected;
        using DbTransaction dbTransaction = dbConnection.BeginTransaction(IsolationLevel.Serializable);

        using DbCommand dbCommand = dbConnection.CreateCommand();

        dbCommand.Transaction = dbTransaction;

        dbCommand.CommandText = createTableCDBStatement;
        rowsAffected = dbCommand.ExecuteNonQuery();

        dbCommand.CommandText = createTableMetadataStatement;
        rowsAffected = dbCommand.ExecuteNonQuery();

        // Need an index on texture name.
        dbCommand.CommandText = createTableTextureStatement;
        rowsAffected = dbCommand.ExecuteNonQuery();

        // Need an index on texture name.
        dbCommand.CommandText = createTableTextureLodStatement;
        rowsAffected = dbCommand.ExecuteNonQuery();

        // Need an index on dataset (for everything)
        // Need an index on feature_category, feature_subcategory, feature_type
        dbCommand.CommandText = createTableGTModelStatement;
        rowsAffected = dbCommand.ExecuteNonQuery();

        // Need an index on feature_category, feature_subcategory, feature_type, lod
        dbCommand.CommandText = createTableGTModelLodStatement;
        rowsAffected = dbCommand.ExecuteNonQuery();

        // Maybe an index on kind, domain, country, category.
        // Need an index on kind, domain, country, category, subcategory, specific, extra.
        dbCommand.CommandText = createTableMovingModelStatement;
        rowsAffected = dbCommand.ExecuteNonQuery();

        // Maybe an index on kind, domain, country, category.
        // Need an index on kind, domain, country, category, subcategory, specific, extra.
        dbCommand.CommandText = createTableMovingModelLodStatement;
        rowsAffected = dbCommand.ExecuteNonQuery();

        // Need an index on latitude, longitude, dataset, cs1, cs2, lod, up
        dbCommand.CommandText = CreateTableTileStatement;
        rowsAffected = dbCommand.ExecuteNonQuery();

        dbCommand.CommandText = CreateTableNavigationStatement;
        rowsAffected = dbCommand.ExecuteNonQuery();

        dbTransaction.Commit();
    }

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

    #region CDB

    private const string createTableCDBStatement = """
        create table if not exists CDB (
            name text primary key
        )
        """;

    private static DbCommand CreateInsertIntoCDBCommand(DbConnection dbConnection)
    {
        const string insertIntoCDBStatement = $"""
            insert into CDB (
                name
            ) values (
                {nameParamName}
            )
            """;

        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = insertIntoCDBStatement;
        CreateAndAttachParameter(dbCommand, nameParamName, DbType.String);
        dbCommand.Prepare();
        return dbCommand;
    }

    private static DbCommand CreateSelectFromCDBCommand(DbConnection dbConnection)
    {
        const string selectFromCDBStatement = $"""
            select name
            from CDB
            """;
        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = selectFromCDBStatement;
        dbCommand.Prepare();
        return dbCommand;
    }

    #endregion

    #region Metadata

    private const string createTableMetadataStatement = """
        create table if not exists Metadata (
            cdb text not null references CDB(name),
            name text not null,
            file_type text not null,
            content blob not null,
            primary key(
                cdb,
                name,
                file_type
            )
        )
        """;

    private static DbCommand CreateInsertIntoMetadataCommand(DbConnection dbConnection)
    {
        const string insertIntoMetadataStatement = $"""
            insert into Metadata (
                cdb,
                name,
                file_type,
                content
            ) values (
                {cdbParamName},
                {nameParamName},
                {fileTypeParamName},
                {contentParamName}
            )
            """;

        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = insertIntoMetadataStatement;
        CreateAndAttachParameter(dbCommand, cdbParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, nameParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, fileTypeParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, contentParamName, DbType.Binary);
        dbCommand.Prepare();
        return dbCommand;
    }

    private static DbCommand CreateSelectFromMetadataCommand(DbConnection dbConnection)
    {
        const string selectFromMetadataStatement = $"""
            select content
            from Metadata
            where cdb = {cdbParamName}
                and name = {nameParamName}
                and file_type = {fileTypeParamName}
            """;

        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = selectFromMetadataStatement;
        CreateAndAttachParameter(dbCommand, cdbParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, nameParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, fileTypeParamName, DbType.String);
        dbCommand.Prepare();
        return dbCommand;
    }

    #endregion

    #region Texture

    private const string createTableTextureStatement = """
        create table if not exists Texture (
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

    private static DbCommand CreateInsertIntoTextureCommand(DbConnection dbConnection)
    {
        const string insertIntoTextureStatement = $"""
            insert into Texture (
                cdb,
                dataset,
                component_selector_1,
                component_selector_2,
                texture_name,
                file_type,
                content
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
        dbCommand.CommandText = insertIntoTextureStatement;
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
        const string selectFromTextureStatement = $"""
            select content
            from Texture
            where cdb = {cdbParamName}
                and dataset = {datasetParamName}
                and component_selector_1 = {cs1ParamName}
                and component_selector_2 = {cs2ParamName}
                and texture_name = {textureNameParamName}
                and file_type = {fileTypeParamName}
            """;

        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = selectFromTextureStatement;
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

    private const string createTableTextureLodStatement = """
        create table if not exists TextureLod (
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

    private static DbCommand CreateInsertIntoTextureLodCommand(DbConnection dbConnection)
    {
        const string insertIntoTextureLodStatement = $"""
            insert into TextureLod (
                cdb,
                dataset,
                component_selector_1,
                component_selector_2,
                lod,
                texture_name,
                file_type,
                content
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
        dbCommand.CommandText = insertIntoTextureLodStatement;
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
        const string selectFromTextureLodStatement = $"""
            select content
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
        dbCommand.CommandText = selectFromTextureLodStatement;
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

    private const string createTableGTModelStatement = """
        create table if not exists GeotypicalModel (
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

    private static DbCommand CreateInsertIntoGeotypicalModelCommand(DbConnection dbConnection)
    {
        const string insertIntoGeometryStatement = $"""
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
                content
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
        dbCommand.CommandText = insertIntoGeometryStatement;
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
        const string selectFromGTModelStatement = $"""
            select content
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
        dbCommand.CommandText = selectFromGTModelStatement;
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

    #region Geotypical Model Lod

    private const string createTableGTModelLodStatement = """
        create table if not exists GeotypicalModelLod (
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

    private static DbCommand CreateInsertIntoGeotypicalModelLodCommand(DbConnection dbConnection)
    {
        const string insertIntoGTModelLodStatement = $"""
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
                content
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

        DbCommand insertIntoGeometryLodCommand = dbConnection.CreateCommand();
        insertIntoGeometryLodCommand.CommandText = insertIntoGTModelLodStatement;
        CreateAndAttachParameter(insertIntoGeometryLodCommand, cdbParamName, DbType.String);
        CreateAndAttachParameter(insertIntoGeometryLodCommand, datasetParamName, DbType.Int32);
        CreateAndAttachParameter(insertIntoGeometryLodCommand, cs1ParamName, DbType.Int32);
        CreateAndAttachParameter(insertIntoGeometryLodCommand, cs2ParamName, DbType.Int32);
        CreateAndAttachParameter(insertIntoGeometryLodCommand, lodParamName, DbType.Int32);
        CreateAndAttachParameter(insertIntoGeometryLodCommand, featureCategoryParamName, DbType.String);
        CreateAndAttachParameter(insertIntoGeometryLodCommand, featureSubcategoryParamName, DbType.String);
        CreateAndAttachParameter(insertIntoGeometryLodCommand, featureTypeParamName, DbType.Int32);
        CreateAndAttachParameter(insertIntoGeometryLodCommand, featureSubcodeParamName, DbType.Int32);
        CreateAndAttachParameter(insertIntoGeometryLodCommand, modelNameParamName, DbType.String);
        CreateAndAttachParameter(insertIntoGeometryLodCommand, fileTypeParamName, DbType.String);
        CreateAndAttachParameter(insertIntoGeometryLodCommand, contentParamName, DbType.Binary);
        insertIntoGeometryLodCommand.Prepare();
        return insertIntoGeometryLodCommand;
    }

    private static DbCommand CreateSelectFromGeotypicalModelLodCommand(DbConnection dbConnection)
    {
        const string selectFromGTModelLodStatement = $"""
            select content
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
        dbCommand.CommandText = selectFromGTModelLodStatement;
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

    private const string createTableMovingModelStatement = """
        create table if not exists MovingModel (
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

    private static DbCommand CreateInsertIntoMovingModelCommand(DbConnection dbConnection)
    {
        const string insertIntoMovingModelStatement = $"""
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
                content
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
        dbCommand.CommandText = insertIntoMovingModelStatement;
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
        const string selectFromMovingModelStatement = $"""
            select content
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
        dbCommand.CommandText = selectFromMovingModelStatement;
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

    private const string createTableMovingModelLodStatement = """
        create table if not exists MovingModelLod (
            cdb text not null references CDB(name),
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
            content blob not null,
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
        const string insertIntoMovingModelLodStatement = $"""
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
                content
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

        DbCommand insertIntoModelsLodCommand = dbConnection.CreateCommand();
        insertIntoModelsLodCommand.CommandText = insertIntoMovingModelLodStatement;
        CreateAndAttachParameter(insertIntoModelsLodCommand, cdbParamName, DbType.String);
        CreateAndAttachParameter(insertIntoModelsLodCommand, datasetParamName, DbType.Int32);
        CreateAndAttachParameter(insertIntoModelsLodCommand, cs1ParamName, DbType.Int32);
        CreateAndAttachParameter(insertIntoModelsLodCommand, cs2ParamName, DbType.Int32);
        CreateAndAttachParameter(insertIntoModelsLodCommand, lodParamName, DbType.Int32);
        CreateAndAttachParameter(insertIntoModelsLodCommand, kindParamName, DbType.Int32);
        CreateAndAttachParameter(insertIntoModelsLodCommand, domainParamName, DbType.Int32);
        CreateAndAttachParameter(insertIntoModelsLodCommand, countryParamName, DbType.Int32);
        CreateAndAttachParameter(insertIntoModelsLodCommand, categoryParamName, DbType.Int32);
        CreateAndAttachParameter(insertIntoModelsLodCommand, subcategoryParamName, DbType.Int32);
        CreateAndAttachParameter(insertIntoModelsLodCommand, specificParamName, DbType.Int32);
        CreateAndAttachParameter(insertIntoModelsLodCommand, extraParamName, DbType.Int32);
        CreateAndAttachParameter(insertIntoModelsLodCommand, fileTypeParamName, DbType.String);
        CreateAndAttachParameter(insertIntoModelsLodCommand, contentParamName, DbType.String);
        insertIntoModelsLodCommand.Prepare();
        return insertIntoModelsLodCommand;
    }

    private static DbCommand CreateSelectFromMovingModelLodCommand(DbConnection dbConnection)
    {
        const string selectFromMovingModelLodStatement = $"""
            select content
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
        dbCommand.CommandText = selectFromMovingModelLodStatement;
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

    private const string CreateTableTileStatement = """
        create table if not exists Tile (
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

    private static DbCommand CreateInsertIntoTileCommand(DbConnection dbConnection)
    {
        const string insertIntoTilesStatement = $"""
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
                content
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

        DbCommand insertIntoTilesCommand = dbConnection.CreateCommand();
        insertIntoTilesCommand.CommandText = insertIntoTilesStatement;
        CreateAndAttachParameter(insertIntoTilesCommand, cdbParamName, DbType.String);
        CreateAndAttachParameter(insertIntoTilesCommand, latitudeParamName, DbType.Int32);
        CreateAndAttachParameter(insertIntoTilesCommand, longitudeParamName, DbType.Int32);
        CreateAndAttachParameter(insertIntoTilesCommand, datasetParamName, DbType.Int32);
        CreateAndAttachParameter(insertIntoTilesCommand, cs1ParamName, DbType.Int32);
        CreateAndAttachParameter(insertIntoTilesCommand, cs2ParamName, DbType.Int32);
        CreateAndAttachParameter(insertIntoTilesCommand, lodParamName, DbType.Int32);
        CreateAndAttachParameter(insertIntoTilesCommand, upParamName, DbType.Int32);
        CreateAndAttachParameter(insertIntoTilesCommand, rightParamName, DbType.Int32);
        CreateAndAttachParameter(insertIntoTilesCommand, fileTypeParamName, DbType.String);
        CreateAndAttachParameter(insertIntoTilesCommand, contentParamName, DbType.Binary);
        insertIntoTilesCommand.Prepare();
        return insertIntoTilesCommand;
    }

    private static DbCommand CreateSelectFromTileCommand(DbConnection dbConnection)
    {
        const string selectFromTileStatement = $"""
            select content
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
        dbCommand.CommandText = selectFromTileStatement;
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

    private const string CreateTableNavigationStatement = """
        create table if not exists Navigation (
            cdb text not null references CDB(name),
            dataset integer not null,
            component_selector_1 integer not null,
            component_selector_2 integer not null,
            file_type text not null,
            content blob not null,
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
        const string InsertIntoNavigationStatement = $"""
            insert into Navigation (
                cdb,
                dataset,
                component_selector_1,
                component_selector_2,
                file_type,
                content
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
        dbCommand.CommandText = InsertIntoNavigationStatement;
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
        const string SelectFromNavigationStatement = $"""
            select content
            from Navigation
            where cdb = {cdbParamName}
                and dataset = {datasetParamName}
                and component_selector_1 = {cs1ParamName}
                and component_selector_2 = {cs2ParamName}
                and file_type = {fileTypeParamName}
            """;

        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = SelectFromNavigationStatement;
        CreateAndAttachParameter(dbCommand, cdbParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, datasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, cs2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, fileTypeParamName, DbType.String);
        dbCommand.Prepare();
        return dbCommand;
    }

    #endregion

    private readonly SqliteConnection dbConnection;

    private readonly DbCommand insertIntoCDB;

    private readonly DbCommand selectFromCDB;

    private readonly DbCommand insertIntoMetadata;

    private readonly DbCommand selectFromMetadata;

    private readonly DbCommand insertIntoTexture;

    private readonly DbCommand selectFromTexture;

    private readonly DbCommand insertIntoTextureLod;

    private readonly DbCommand selectFromTextureLod;

    private readonly DbCommand insertIntoGTModel;

    private readonly DbCommand selectFromGTModel;

    private readonly DbCommand insertIntoGTModelLod;

    private readonly DbCommand selectFromGTModelLod;

    private readonly DbCommand insertIntoMovingModel;

    private readonly DbCommand selectFromMovingModel;

    private readonly DbCommand insertIntoMovingModelLod;

    private readonly DbCommand selectFromMovingModelLod;

    private readonly DbCommand insertIntoTile;

    private readonly DbCommand selectFromTile;

    private readonly DbCommand insertIntoNavigation;

    private readonly DbCommand selectFromNavigation;

    private bool disposedValue;

    public SQLiteCDB(SqliteConnection dbConnection)
    {
        ArgumentNullException.ThrowIfNull(dbConnection);

        this.dbConnection = dbConnection;
        CreateSqliteSchema(this.dbConnection);
        this.insertIntoCDB = CreateInsertIntoCDBCommand(this.dbConnection);
        this.selectFromCDB = CreateSelectFromCDBCommand(this.dbConnection);
        this.insertIntoMetadata = CreateInsertIntoMetadataCommand(this.dbConnection);
        this.selectFromMetadata = CreateSelectFromMetadataCommand(this.dbConnection);
        this.insertIntoTexture = CreateInsertIntoTextureCommand(this.dbConnection);
        this.selectFromTexture = CreateSelectFromTextureCommand(this.dbConnection);
        this.insertIntoTextureLod = CreateInsertIntoTextureLodCommand(this.dbConnection);
        this.selectFromTextureLod = CreateSelectFromTextureLodCommand(this.dbConnection);
        this.insertIntoGTModel = CreateInsertIntoGeotypicalModelCommand(this.dbConnection);
        this.selectFromGTModel = CreateSelectFromGeotypicalModelCommand(this.dbConnection);
        this.insertIntoGTModelLod = CreateInsertIntoGeotypicalModelLodCommand(this.dbConnection);
        this.selectFromGTModelLod = CreateSelectFromGeotypicalModelLodCommand(this.dbConnection);
        this.insertIntoMovingModel = CreateInsertIntoMovingModelCommand(this.dbConnection);
        this.selectFromMovingModel = CreateSelectFromMovingModelCommand(this.dbConnection);
        this.insertIntoMovingModelLod = CreateInsertIntoMovingModelLodCommand(this.dbConnection);
        this.selectFromMovingModelLod = CreateSelectFromMovingModelLodCommand(this.dbConnection);
        this.insertIntoTile = CreateInsertIntoTileCommand(this.dbConnection);
        this.selectFromTile = CreateSelectFromTileCommand(this.dbConnection);
        this.insertIntoNavigation = CreateInsertIntoNavigationCommand(this.dbConnection);
        this.selectFromNavigation = CreateSelectFromNavigationCommand(this.dbConnection);
    }

    public int InsertIntoCDB(string cdbName)
    {
        insertIntoCDB.Parameters[nameParamName].Value = cdbName;
        return insertIntoCDB.ExecuteNonQuery();
    }

    public IEnumerable<string> SelectFromCDB()
    {
        List<string> names = new();
        using DbDataReader dbDataReader = selectFromCDB.ExecuteReader(CommandBehavior.SequentialAccess);
        do
        {
            while (dbDataReader.Read())
            {
                string name = dbDataReader.GetString("name");
                names.Add(name);
            }
        } while (dbDataReader.NextResult());
        return names;
    }

    public int InsertIntoMetadata(string cdbName, Metadata metadata, byte[] content)
    {
        insertIntoMetadata.Parameters[cdbParamName].Value = cdbName;
        insertIntoMetadata.Parameters[nameParamName].Value = metadata.Name;
        insertIntoMetadata.Parameters[fileTypeParamName].Value = metadata.FileType;
        insertIntoMetadata.Parameters[contentParamName].Value = content;

        return insertIntoMetadata.ExecuteNonQuery();
    }

    public bool TrySelectFromMetadata(string cdbName, Metadata metadata, [NotNullWhen(true)] out byte[] content)
    {
        selectFromMetadata.Parameters[cdbParamName].Value = cdbName;
        selectFromMetadata.Parameters[nameParamName].Value = metadata.Name;
        selectFromMetadata.Parameters[fileTypeParamName].Value = metadata.FileType;
        using DbDataReader dbDataReader = selectFromMetadata.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
        do
        {
            while (dbDataReader.Read())
            {
                using MemoryStream memoryStream = new();
                using Stream stream = dbDataReader.GetStream("content");
                stream.CopyTo(memoryStream);
                content = memoryStream.GetBuffer();
                return true;
            }
        } while (dbDataReader.NextResult());
        content = Array.Empty<byte>();
        return false;
    }

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

    public bool TrySelectFromTexture(string cdbName, Texture texture, [NotNullWhen(true)] out byte[] content)
    {
        selectFromTexture.Parameters[cdbParamName].Value = cdbName;
        selectFromTexture.Parameters[datasetParamName].Value = texture.Dataset.Value;
        selectFromTexture.Parameters[cs1ParamName].Value = texture.ComponentSelector1;
        selectFromTexture.Parameters[cs2ParamName].Value = texture.ComponentSelector2;
        selectFromTexture.Parameters[textureNameParamName].Value = texture.Name;
        selectFromTexture.Parameters[fileTypeParamName].Value = texture.FileType;
        using DbDataReader dbDataReader = selectFromTexture.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
        do
        {
            while (dbDataReader.Read())
            {
                using MemoryStream memoryStream = new();
                using Stream stream = dbDataReader.GetStream("content");
                stream.CopyTo(memoryStream);
                content = memoryStream.GetBuffer();
                return true;
            }
        } while (dbDataReader.NextResult());
        content = Array.Empty<byte>();
        return false;
    }

    public int InsertIntoTextureLod(string cdbName, TextureLod textureLod, byte[] content)
    {
        insertIntoTextureLod.Parameters[cdbParamName].Value = cdbName;
        insertIntoTextureLod.Parameters[datasetParamName].Value = textureLod.Dataset.Value;
        insertIntoTextureLod.Parameters[cs1ParamName].Value = textureLod.ComponentSelector1;
        insertIntoTextureLod.Parameters[cs2ParamName].Value = textureLod.ComponentSelector2;
        insertIntoTextureLod.Parameters[lodParamName].Value = textureLod.LevelOfDetail.Value;
        insertIntoTextureLod.Parameters[textureNameParamName].Value = textureLod.TextureName;
        insertIntoTextureLod.Parameters[fileTypeParamName].Value = textureLod.FileType;
        insertIntoTextureLod.Parameters[contentParamName].Value = content;

        return insertIntoTextureLod.ExecuteNonQuery();
    }

    public bool TrySelectFromTextureLod(string cdbName, TextureLod textureLod, [NotNullWhen(true)] out byte[] content)
    {
        selectFromTextureLod.Parameters[cdbParamName].Value = cdbName;
        selectFromTextureLod.Parameters[datasetParamName].Value = textureLod.Dataset.Value;
        selectFromTextureLod.Parameters[cs1ParamName].Value = textureLod.ComponentSelector1;
        selectFromTextureLod.Parameters[cs2ParamName].Value = textureLod.ComponentSelector2;
        selectFromTextureLod.Parameters[lodParamName].Value = textureLod.LevelOfDetail.Value;
        selectFromTextureLod.Parameters[textureNameParamName].Value = textureLod.TextureName;
        selectFromTextureLod.Parameters[fileTypeParamName].Value = textureLod.FileType;
        using DbDataReader dbDataReader = selectFromTextureLod.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
        do
        {
            while (dbDataReader.Read())
            {
                using MemoryStream memoryStream = new();
                using Stream stream = dbDataReader.GetStream("content");
                stream.CopyTo(memoryStream);
                content = memoryStream.GetBuffer();
                return true;
            }
        } while (dbDataReader.NextResult());
        content = Array.Empty<byte>();
        return false;
    }

    public int InsertIntoGTModel(string cdbName, GTModelGeometry geometry, byte[] content)
    {
        insertIntoGTModel.Parameters[cdbParamName].Value = cdbName;
        insertIntoGTModel.Parameters[datasetParamName].Value = geometry.Dataset.Value;
        insertIntoGTModel.Parameters[cs1ParamName].Value = geometry.ComponentSelector1;
        insertIntoGTModel.Parameters[cs2ParamName].Value = geometry.ComponentSelector2;
        insertIntoGTModel.Parameters[featureCategoryParamName].Value = geometry.FeatureCode.Category;
        insertIntoGTModel.Parameters[featureSubcategoryParamName].Value = geometry.FeatureCode.Subcategory;
        insertIntoGTModel.Parameters[featureTypeParamName].Value = geometry.FeatureCode.Type;
        insertIntoGTModel.Parameters[featureSubcodeParamName].Value = geometry.FeatureSubcode;
        insertIntoGTModel.Parameters[modelNameParamName].Value = geometry.ModelName;
        insertIntoGTModel.Parameters[fileTypeParamName].Value = geometry.FileType;
        insertIntoGTModel.Parameters[contentParamName].Value = content;

        return insertIntoGTModel.ExecuteNonQuery();
    }

    public bool TrySelectFromGTModel(string cdbName, GTModelGeometry geometry, [NotNullWhen(true)] out byte[] content)
    {
        selectFromGTModel.Parameters[cdbParamName].Value = cdbName;
        selectFromGTModel.Parameters[datasetParamName].Value = geometry.Dataset.Value;
        selectFromGTModel.Parameters[cs1ParamName].Value = geometry.ComponentSelector1;
        selectFromGTModel.Parameters[cs2ParamName].Value = geometry.ComponentSelector2;
        selectFromGTModel.Parameters[featureCategoryParamName].Value = geometry.FeatureCode.Category;
        selectFromGTModel.Parameters[featureSubcategoryParamName].Value = geometry.FeatureCode.Subcategory;
        selectFromGTModel.Parameters[featureTypeParamName].Value = geometry.FeatureCode.Type;
        selectFromGTModel.Parameters[featureSubcodeParamName].Value = geometry.FeatureSubcode;
        selectFromGTModel.Parameters[modelNameParamName].Value = geometry.ModelName;
        selectFromGTModel.Parameters[fileTypeParamName].Value = geometry.FileType;
        using DbDataReader dbDataReader = selectFromGTModel.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
        do
        {
            while (dbDataReader.Read())
            {
                using MemoryStream memoryStream = new();
                using Stream stream = dbDataReader.GetStream("content");
                stream.CopyTo(memoryStream);
                content = memoryStream.GetBuffer();
                return true;
            }
        } while (dbDataReader.NextResult());
        content = Array.Empty<byte>();
        return false;
    }

    public int InsertIntoGeotypicalModelLod(string cdbName, GTModelGeometryLod geometryLod, byte[] content)
    {
        insertIntoGTModelLod.Parameters[cdbParamName].Value = cdbName;
        insertIntoGTModelLod.Parameters[datasetParamName].Value = geometryLod.Dataset.Value;
        insertIntoGTModelLod.Parameters[cs1ParamName].Value = geometryLod.ComponentSelector1;
        insertIntoGTModelLod.Parameters[cs2ParamName].Value = geometryLod.ComponentSelector2;
        insertIntoGTModelLod.Parameters[lodParamName].Value = geometryLod.LevelOfDetail.Value;
        insertIntoGTModelLod.Parameters[featureCategoryParamName].Value = geometryLod.FeatureCode.Category;
        insertIntoGTModelLod.Parameters[featureSubcategoryParamName].Value = geometryLod.FeatureCode.Subcategory;
        insertIntoGTModelLod.Parameters[featureTypeParamName].Value = geometryLod.FeatureCode.Type;
        insertIntoGTModelLod.Parameters[featureSubcodeParamName].Value = geometryLod.FeatureSubcode;
        insertIntoGTModelLod.Parameters[modelNameParamName].Value = geometryLod.ModelName;
        insertIntoGTModelLod.Parameters[fileTypeParamName].Value = geometryLod.FileType;
        insertIntoGTModelLod.Parameters[contentParamName].Value = content;

        return insertIntoGTModelLod.ExecuteNonQuery();
    }

    public bool TrySelectFromGTModelLod(string cdbName, GTModelGeometryLod gtModelGeometryLod, [NotNullWhen(true)] out byte[] content)
    {
        selectFromGTModelLod.Parameters[cdbParamName].Value = cdbName;
        selectFromGTModelLod.Parameters[datasetParamName].Value = gtModelGeometryLod.Dataset.Value;
        selectFromGTModelLod.Parameters[cs1ParamName].Value = gtModelGeometryLod.ComponentSelector1;
        selectFromGTModelLod.Parameters[cs2ParamName].Value = gtModelGeometryLod.ComponentSelector2;
        selectFromGTModelLod.Parameters[lodParamName].Value = gtModelGeometryLod.LevelOfDetail.Value;
        selectFromGTModelLod.Parameters[featureCategoryParamName].Value = gtModelGeometryLod.FeatureCode.Category;
        selectFromGTModelLod.Parameters[featureSubcategoryParamName].Value = gtModelGeometryLod.FeatureCode.Subcategory;
        selectFromGTModelLod.Parameters[featureTypeParamName].Value = gtModelGeometryLod.FeatureCode.Type;
        selectFromGTModelLod.Parameters[featureSubcodeParamName].Value = gtModelGeometryLod.FeatureSubcode;
        selectFromGTModelLod.Parameters[modelNameParamName].Value = gtModelGeometryLod.ModelName;
        selectFromGTModelLod.Parameters[fileTypeParamName].Value = gtModelGeometryLod.FileType;
        using DbDataReader dbDataReader = selectFromGTModelLod.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
        do
        {
            while (dbDataReader.Read())
            {
                using MemoryStream memoryStream = new();
                using Stream stream = dbDataReader.GetStream("content");
                stream.CopyTo(memoryStream);
                content = memoryStream.GetBuffer();
                return true;
            }
        } while (dbDataReader.NextResult());
        content = Array.Empty<byte>();
        return false;
    }

    public int InsertIntoMovingModel(string cdbName, MovingModelGeometry movingModelGeometry, byte[] content)
    {
        insertIntoMovingModel.Parameters[cdbParamName].Value = cdbName;
        insertIntoMovingModel.Parameters[datasetParamName].Value = movingModelGeometry.Dataset.Value;
        insertIntoMovingModel.Parameters[cs1ParamName].Value = movingModelGeometry.ComponentSelector1;
        insertIntoMovingModel.Parameters[cs2ParamName].Value = movingModelGeometry.ComponentSelector2;
        insertIntoMovingModel.Parameters[kindParamName].Value = movingModelGeometry.MMDC.Kind;
        insertIntoMovingModel.Parameters[domainParamName].Value = movingModelGeometry.MMDC.Domain;
        insertIntoMovingModel.Parameters[countryParamName].Value = movingModelGeometry.MMDC.Country;
        insertIntoMovingModel.Parameters[categoryParamName].Value = movingModelGeometry.MMDC.Category;
        insertIntoMovingModel.Parameters[subcategoryParamName].Value = movingModelGeometry.MMDC.Subcategory;
        insertIntoMovingModel.Parameters[specificParamName].Value = movingModelGeometry.MMDC.Specific;
        insertIntoMovingModel.Parameters[extraParamName].Value = movingModelGeometry.MMDC.Extra;
        insertIntoMovingModel.Parameters[fileTypeParamName].Value = movingModelGeometry.FileType;
        insertIntoMovingModel.Parameters[contentParamName].Value = content;

        return insertIntoMovingModel.ExecuteNonQuery();
    }

    public bool TrySelectFromMovingModel(string cdbName, MovingModelGeometry movingModel, [NotNullWhen(true)] out byte[] content)
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
        using DbDataReader dbDataReader = selectFromMovingModel.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
        do
        {
            while (dbDataReader.Read())
            {
                using MemoryStream memoryStream = new();
                using Stream stream = dbDataReader.GetStream("content");
                stream.CopyTo(memoryStream);
                content = memoryStream.GetBuffer();
                return true;
            }
        } while (dbDataReader.NextResult());
        content = Array.Empty<byte>();
        return false;
    }

    public int InsertIntoMovingModelLod(string cdbName, MovingModelGeometryLod movingModelGeometryLod, byte[] content)
    {
        insertIntoMovingModelLod.Parameters[cdbParamName].Value = cdbName;
        insertIntoMovingModelLod.Parameters[datasetParamName].Value = movingModelGeometryLod.Dataset.Value;
        insertIntoMovingModelLod.Parameters[cs1ParamName].Value = movingModelGeometryLod.ComponentSelector1;
        insertIntoMovingModelLod.Parameters[cs2ParamName].Value = movingModelGeometryLod.ComponentSelector2;
        insertIntoMovingModelLod.Parameters[lodParamName].Value = movingModelGeometryLod.LevelOfDetail.Value;
        insertIntoMovingModelLod.Parameters[kindParamName].Value = movingModelGeometryLod.MMDC.Kind;
        insertIntoMovingModelLod.Parameters[domainParamName].Value = movingModelGeometryLod.MMDC.Domain;
        insertIntoMovingModelLod.Parameters[countryParamName].Value = movingModelGeometryLod.MMDC.Country;
        insertIntoMovingModelLod.Parameters[categoryParamName].Value = movingModelGeometryLod.MMDC.Category;
        insertIntoMovingModelLod.Parameters[subcategoryParamName].Value = movingModelGeometryLod.MMDC.Subcategory;
        insertIntoMovingModelLod.Parameters[specificParamName].Value = movingModelGeometryLod.MMDC.Specific;
        insertIntoMovingModelLod.Parameters[extraParamName].Value = movingModelGeometryLod.MMDC.Extra;
        insertIntoMovingModelLod.Parameters[fileTypeParamName].Value = movingModelGeometryLod.FileType;
        insertIntoMovingModelLod.Parameters[contentParamName].Value = content;

        return insertIntoMovingModelLod.ExecuteNonQuery();
    }

    public bool TrySelectFromMovingModelLod(string cdbName, MovingModelGeometryLod movingModelGeometryLod, [NotNullWhen(true)] out byte[] content)
    {
        selectFromMovingModelLod.Parameters[cdbParamName].Value = cdbName;
        selectFromMovingModelLod.Parameters[datasetParamName].Value = movingModelGeometryLod.Dataset.Value;
        selectFromMovingModelLod.Parameters[cs1ParamName].Value = movingModelGeometryLod.ComponentSelector1;
        selectFromMovingModelLod.Parameters[cs2ParamName].Value = movingModelGeometryLod.ComponentSelector2;
        selectFromMovingModelLod.Parameters[lodParamName].Value = movingModelGeometryLod.LevelOfDetail.Value;
        selectFromMovingModelLod.Parameters[kindParamName].Value = movingModelGeometryLod.MMDC.Kind;
        selectFromMovingModelLod.Parameters[domainParamName].Value = movingModelGeometryLod.MMDC.Domain;
        selectFromMovingModelLod.Parameters[countryParamName].Value = movingModelGeometryLod.MMDC.Country;
        selectFromMovingModelLod.Parameters[categoryParamName].Value = movingModelGeometryLod.MMDC.Category;
        selectFromMovingModelLod.Parameters[subcategoryParamName].Value = movingModelGeometryLod.MMDC.Subcategory;
        selectFromMovingModelLod.Parameters[specificParamName].Value = movingModelGeometryLod.MMDC.Specific;
        selectFromMovingModelLod.Parameters[extraParamName].Value = movingModelGeometryLod.MMDC.Extra;
        selectFromMovingModelLod.Parameters[fileTypeParamName].Value = movingModelGeometryLod.FileType;
        using DbDataReader dbDataReader = selectFromMovingModelLod.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
        do
        {
            while (dbDataReader.Read())
            {
                using MemoryStream memoryStream = new();
                using Stream stream = dbDataReader.GetStream("content");
                stream.CopyTo(memoryStream);
                content = memoryStream.GetBuffer();
                return true;
            }
        } while (dbDataReader.NextResult());
        content = Array.Empty<byte>();
        return false;
    }

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

    public bool TrySelectFromTile(string cdbName, Tile tile, [NotNullWhen(true)] out byte[] content)
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
                using MemoryStream memoryStream = new();
                using Stream stream = dbDataReader.GetStream("content");
                stream.CopyTo(memoryStream);
                content = memoryStream.GetBuffer();
                return true;
            }
        } while (dbDataReader.NextResult());
        content = Array.Empty<byte>();
        return false;
    }

    public int InsertIntoNavigation(string cdbName, Navigation navigation, byte[] contents)
    {
        insertIntoNavigation.Parameters[cdbParamName].Value = cdbName;
        insertIntoNavigation.Parameters[datasetParamName].Value = navigation.Dataset.Value;
        insertIntoNavigation.Parameters[cs1ParamName].Value = navigation.ComponentSelector1;
        insertIntoNavigation.Parameters[cs2ParamName].Value = navigation.ComponentSelector2;
        insertIntoNavigation.Parameters[fileTypeParamName].Value = navigation.FileType;
        insertIntoNavigation.Parameters[contentParamName].Value = contents;

        return insertIntoNavigation.ExecuteNonQuery();
    }

    public bool TrySelectFromNavigation(string cdbName, Navigation navigation, [NotNullWhen(true)] out byte[] content)
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
                using MemoryStream memoryStream = new();
                using Stream stream = dbDataReader.GetStream("content");
                stream.CopyTo(memoryStream);
                content = memoryStream.GetBuffer();
                return true;
            }
        } while (dbDataReader.NextResult());
        content = Array.Empty<byte>();
        return false;
    }

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
                selectFromGTModelLod.Dispose();
                insertIntoGTModelLod.Dispose();
                selectFromGTModel.Dispose();
                insertIntoGTModel.Dispose();
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
}
