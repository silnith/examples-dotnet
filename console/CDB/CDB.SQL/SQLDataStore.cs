using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silnith.CDB.Visitor;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Silnith.CDB.SQL;

/// <summary>
/// A CDB data store that uses an SQL database for its storage.
/// </summary>
public abstract class SQLDataStore : IDisposable
{
    /// <summary>
    /// Creates a parameter for a database command, sets the name and type of
    /// the parameter, and attaches the parameter to the command.
    /// </summary>
    /// <param name="dbCommand">The command to create a parameter for.</param>
    /// <param name="dbParameterName">The name of the parameter.
    /// Each database system has its own syntax for how these parameters should
    /// be named.</param>
    /// <param name="dbType">The parameter type.</param>
    private static void CreateAndAttachParameter(DbCommand dbCommand, string dbParameterName, DbType dbType)
    {
        DbParameter dbParameter = dbCommand.CreateParameter();
        dbCommand.Parameters.Add(dbParameter);
        dbParameter.DbType = dbType;
        dbParameter.ParameterName = dbParameterName;
    }

    #region Prepared Statement Data Members

    /// <summary>
    /// The SQL prepared statement that inserts a new name into the CDB table.
    /// This takes one parameter,
    /// <see cref="CdbParamName"/>.
    /// </summary>
    /// <seealso cref="InsertIntoCDBStatement"/>
    private readonly DbCommand insertIntoCDBCommand;

    /// <summary>
    /// The SQL prepared statement that selects the CDB name from the CDB table.
    /// This has no parameters.
    /// This returns one column,
    /// <see cref="CDBNameColumnName"/>.
    /// </summary>
    /// <seealso cref="SelectFromCDBStatement"/>
    private readonly DbCommand selectFromCDBCommand;

    /// <summary>
    /// The SQL prepared statement to insert a row into the Metadata table.
    /// This requires four parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="MetadataNameParamName"/>,
    /// <see cref="FileTypeParamName"/>,
    /// <see cref="ContentParamName"/>
    /// </summary>
    /// <seealso cref="InsertIntoMetadataStatement"/>
    private readonly DbCommand insertIntoMetadataCommand;

    /// <summary>
    /// The SQL prepared statement to select one row from the Metadata table.
    /// This requires three parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="MetadataNameParamName"/>,
    /// <see cref="FileTypeParamName"/>.
    /// It returns one column,
    /// <see cref="ContentColumnName"/>.
    /// </summary>
    /// <seealso cref="SelectFromMetadataStatement"/>
    private readonly DbCommand selectFromMetadataCommand;

    /// <summary>
    /// The SQL prepared statement to insert a row into the Texture table.
    /// This requires seven parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="TextureNameParamName"/>,
    /// <see cref="FileTypeParamName"/>,
    /// <see cref="ContentParamName"/>.
    /// </summary>
    /// <seealso cref="InsertIntoTextureStatement"/>
    private readonly DbCommand insertIntoTextureCommand;

    /// <summary>
    /// The SQL prepared statement to select a row from the Texture table.
    /// This requires six parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="TextureNameParamName"/>,
    /// <see cref="FileTypeParamName"/>.
    /// It returns one column,
    /// <see cref="ContentColumnName"/>.
    /// </summary>
    /// <seealso cref="SelectFromTextureStatement"/>
    private readonly DbCommand selectFromTextureCommand;

    /// <summary>
    /// The SQL prepared statement to insert a row into the Texture Level of Detail table.
    /// This requires eight parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="LevelOfDetailParamName"/>,
    /// <see cref="TextureNameParamName"/>,
    /// <see cref="FileTypeParamName"/>,
    /// <see cref="ContentParamName"/>.
    /// </summary>
    /// <seealso cref="InsertIntoTextureLodStatement"/>
    private readonly DbCommand insertIntoTextureLodCommand;

    /// <summary>
    /// The SQL prepared statement to select a row from the Texture Level of Detail table.
    /// This requires seven parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="LevelOfDetailParamName"/>,
    /// <see cref="TextureNameParamName"/>,
    /// <see cref="FileTypeParamName"/>.
    /// It returns one column,
    /// <see cref="ContentColumnName"/>.
    /// </summary>
    /// <seealso cref="SelectFromTextureLodStatement"/>
    private readonly DbCommand selectFromTextureLodCommand;

    /// <summary>
    /// The SQL prepared statement to insert a row into the Geotypical Model table.
    /// This takes eleven parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="FeatureCategoryParamName"/>,
    /// <see cref="FeatureSubcategoryParamName"/>,
    /// <see cref="FeatureTypeParamName"/>,
    /// <see cref="FeatureSubcodeParamName"/>,
    /// <see cref="ModelNameParamName"/>,
    /// <see cref="FileTypeParamName"/>,
    /// <see cref="ContentParamName"/>.
    /// </summary>
    /// <seealso cref="InsertIntoGeotypicalModelStatement"/>
    private readonly DbCommand insertIntoGeotypicalModelCommand;

    /// <summary>
    /// The SQL prepared statement to select a row from the Geotypical Model table.
    /// This takes ten parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="FeatureCategoryParamName"/>,
    /// <see cref="FeatureSubcategoryParamName"/>,
    /// <see cref="FeatureTypeParamName"/>,
    /// <see cref="FeatureSubcodeParamName"/>,
    /// <see cref="ModelNameParamName"/>,
    /// <see cref="FileTypeParamName"/>.
    /// It returns one column,
    /// <see cref="ContentColumnName"/>.
    /// </summary>
    /// <seealso cref="SelectFromGeotypicalModelStatement"/>
    private readonly DbCommand selectFromGeotypicalModelCommand;

    /// <summary>
    /// The SQL prepared statement to insert a row into the Geotypical Model Level of Detail table.
    /// This takes twelve parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="LevelOfDetailParamName"/>,
    /// <see cref="FeatureCategoryParamName"/>,
    /// <see cref="FeatureSubcategoryParamName"/>,
    /// <see cref="FeatureTypeParamName"/>,
    /// <see cref="FeatureSubcodeParamName"/>,
    /// <see cref="ModelNameParamName"/>,
    /// <see cref="FileTypeParamName"/>,
    /// <see cref="ContentParamName"/>.
    /// </summary>
    /// <seealso cref="InsertIntoGeotypicalModelLodStatement"/>
    private readonly DbCommand insertIntoGeotypicalModelLodCommand;

    /// <summary>
    /// The SQL prepared statement to select a row from the Geotypical Model Level of Detail table.
    /// This takes eleven parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="LevelOfDetailParamName"/>,
    /// <see cref="FeatureCategoryParamName"/>,
    /// <see cref="FeatureSubcategoryParamName"/>,
    /// <see cref="FeatureTypeParamName"/>,
    /// <see cref="FeatureSubcodeParamName"/>,
    /// <see cref="ModelNameParamName"/>,
    /// <see cref="FileTypeParamName"/>.
    /// It returns one column,
    /// <see cref="ContentColumnName"/>.
    /// </summary>
    /// <seealso cref="SelectFromGeotypicalModelLodStatement"/>
    private readonly DbCommand selectFromGeotypicalModelLodCommand;

    /// <summary>
    /// The SQL prepared statement to insert a row into the Moving Model table.
    /// This takes thirteen parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="DISKindParamName"/>,
    /// <see cref="DISDomainParamName"/>,
    /// <see cref="DISCountryParamName"/>,
    /// <see cref="DISCategoryParamName"/>,
    /// <see cref="DISSubcategoryParamName"/>,
    /// <see cref="DISSpecificParamName"/>,
    /// <see cref="DISExtraParamName"/>,
    /// <see cref="FileTypeParamName"/>,
    /// <see cref="ContentParamName"/>.
    /// </summary>
    /// <seealso cref="InsertIntoMovingModelStatement"/>
    private readonly DbCommand insertIntoMovingModelCommand;

    /// <summary>
    /// The SQL prepared statement to select a row from the Moving Model table.
    /// This takes twelve parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="DISKindParamName"/>,
    /// <see cref="DISDomainParamName"/>,
    /// <see cref="DISCountryParamName"/>,
    /// <see cref="DISCategoryParamName"/>,
    /// <see cref="DISSubcategoryParamName"/>,
    /// <see cref="DISSpecificParamName"/>,
    /// <see cref="DISExtraParamName"/>,
    /// <see cref="FileTypeParamName"/>.
    /// It returns one column,
    /// <see cref="ContentColumnName"/>.
    /// </summary>
    /// <seealso cref="SelectFromMovingModelStatement"/>
    private readonly DbCommand selectFromMovingModelCommand;

    /// <summary>
    /// The SQL prepared statement to insert a row into the Moving Model Level of Detail table.
    /// This takes fourteen parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="LevelOfDetailParamName"/>,
    /// <see cref="DISKindParamName"/>,
    /// <see cref="DISDomainParamName"/>,
    /// <see cref="DISCountryParamName"/>,
    /// <see cref="DISCategoryParamName"/>,
    /// <see cref="DISSubcategoryParamName"/>,
    /// <see cref="DISSpecificParamName"/>,
    /// <see cref="DISExtraParamName"/>,
    /// <see cref="FileTypeParamName"/>,
    /// <see cref="ContentParamName"/>.
    /// </summary>
    /// <seealso cref="InsertIntoMovingModelLodStatement"/>
    private readonly DbCommand insertIntoMovingModelLodCommand;

    /// <summary>
    /// The SQL prepared statement to select a row from the Moving Model Level of Detail table.
    /// This takes thirteen parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="LevelOfDetailParamName"/>,
    /// <see cref="DISKindParamName"/>,
    /// <see cref="DISDomainParamName"/>,
    /// <see cref="DISCountryParamName"/>,
    /// <see cref="DISCategoryParamName"/>,
    /// <see cref="DISSubcategoryParamName"/>,
    /// <see cref="DISSpecificParamName"/>,
    /// <see cref="DISExtraParamName"/>,
    /// <see cref="FileTypeParamName"/>.
    /// It returns one column,
    /// <see cref="ContentColumnName"/>.
    /// </summary>
    /// <seealso cref="SelectFromMovingModelLodStatement"/>
    private readonly DbCommand selectFromMovingModelLodCommand;

    /// <summary>
    /// The SQL prepared statement to insert a row into the Tile table.
    /// This takes eleven parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="LatitudeParamName"/>,
    /// <see cref="LongitudeParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="LevelOfDetailParamName"/>,
    /// <see cref="UpParamName"/>,
    /// <see cref="RightParamName"/>,
    /// <see cref="FileTypeParamName"/>,
    /// <see cref="ContentParamName"/>.
    /// </summary>
    /// <seealso cref="InsertIntoTileStatement"/>
    private readonly DbCommand insertIntoTileCommand;

    /// <summary>
    /// The SQL prepared statement to select a row from the Tile table.
    /// This takes ten parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="LatitudeParamName"/>,
    /// <see cref="LongitudeParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="LevelOfDetailParamName"/>,
    /// <see cref="UpParamName"/>,
    /// <see cref="RightParamName"/>,
    /// <see cref="FileTypeParamName"/>.
    /// It returns one column,
    /// <see cref="ContentColumnName"/>.
    /// </summary>
    /// <seealso cref="SelectFromTileStatement"/>
    private readonly DbCommand selectFromTileCommand;

    /// <summary>
    /// The SQL prepared statement to insert a row into the TileArchivedFeature table.
    /// This takes sixteen parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="LatitudeParamName"/>,
    /// <see cref="LongitudeParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="LevelOfDetailParamName"/>,
    /// <see cref="UpParamName"/>,
    /// <see cref="RightParamName"/>,
    /// <see cref="FeatureCategoryParamName"/>,
    /// <see cref="FeatureSubcategoryParamName"/>,
    /// <see cref="FeatureTypeParamName"/>,
    /// <see cref="FeatureSubcodeParamName"/>,
    /// <see cref="ModelNameParamName"/>,
    /// <see cref="FileTypeParamName"/>,
    /// <see cref="ContentParamName"/>.
    /// </summary>
    /// <seealso cref="InsertIntoTileArchivedFeatureStatement"/>
    private readonly DbCommand insertIntoTileArchivedFeatureCommand;

    /// <summary>
    /// The SQL prepared statement to select a row from the TileArchivedFeature table.
    /// This takes fifteen parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="LatitudeParamName"/>,
    /// <see cref="LongitudeParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="LevelOfDetailParamName"/>,
    /// <see cref="UpParamName"/>,
    /// <see cref="RightParamName"/>,
    /// <see cref="FeatureCategoryParamName"/>,
    /// <see cref="FeatureSubcategoryParamName"/>,
    /// <see cref="FeatureTypeParamName"/>,
    /// <see cref="FeatureSubcodeParamName"/>,
    /// <see cref="ModelNameParamName"/>,
    /// <see cref="FileTypeParamName"/>.
    /// It returns one column,
    /// <see cref="ContentColumnName"/>.
    /// </summary>
    /// <seealso cref="SelectFromTileArchivedFeatureStatement"/>
    private readonly DbCommand selectFromTileArchivedFeatureCommand;

    /// <summary>
    /// The SQL prepared statement to insert a row into the TileArchivedTexture table.
    /// This takes twelve parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="LatitudeParamName"/>,
    /// <see cref="LongitudeParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="LevelOfDetailParamName"/>,
    /// <see cref="UpParamName"/>,
    /// <see cref="RightParamName"/>,
    /// <see cref="TextureNameParamName"/>,
    /// <see cref="FileTypeParamName"/>,
    /// <see cref="ContentParamName"/>.
    /// </summary>
    /// <seealso cref="InsertIntoTileArchivedTextureStatement"/>
    private readonly DbCommand insertIntoTileArchivedTextureCommand;

    /// <summary>
    /// The SQL prepared statement to select a row from the TileArchivedTexture table.
    /// This takes eleven parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="LatitudeParamName"/>,
    /// <see cref="LongitudeParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="LevelOfDetailParamName"/>,
    /// <see cref="UpParamName"/>,
    /// <see cref="RightParamName"/>,
    /// <see cref="TextureNameParamName"/>,
    /// <see cref="FileTypeParamName"/>.
    /// It returns one column,
    /// <see cref="ContentColumnName"/>.
    /// </summary>
    /// <seealso cref="SelectFromTileArchivedTextureStatement"/>
    private readonly DbCommand selectFromTileArchivedTextureCommand;

    /// <summary>
    /// The SQL prepared statement to insert a row into the Navigation table.
    /// This takes six parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="FileTypeParamName"/>,
    /// <see cref="ContentParamName"/>.
    /// </summary>
    /// <seealso cref="InsertIntoNavigationStatement"/>
    private readonly DbCommand insertIntoNavigationCommand;

    /// <summary>
    /// The SQL prepared statement to select a row from the Navigation table.
    /// This takes five parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="FileTypeParamName"/>.
    /// It returns one column,
    /// <see cref="ContentColumnName"/>.
    /// </summary>
    /// <seealso cref="SelectFromNavigationStatement"/>
    private readonly DbCommand selectFromNavigationCommand;

    #endregion

    /// <summary>
    /// Creates a new CDB storage backend using the provided SQL connection
    /// string.
    /// </summary>
    /// <param name="dbConnection">The connection.</param>
    /// <param name="createSchema"><see langword="true"/> in order to create the schema.</param>
    protected SQLDataStore(DbConnection dbConnection, bool createSchema = false)
    {
        ArgumentNullException.ThrowIfNull(dbConnection);

        if (createSchema)
        {
            CreateSchema(dbConnection);
        }

        insertIntoCDBCommand = CreateInsertIntoCDBCommand(dbConnection);
        selectFromCDBCommand = CreateSelectFromCDBCommand(dbConnection);
        insertIntoMetadataCommand = CreateInsertIntoMetadataCommand(dbConnection);
        selectFromMetadataCommand = CreateSelectFromMetadataCommand(dbConnection);
        insertIntoTextureCommand = CreateInsertIntoTextureCommand(dbConnection);
        selectFromTextureCommand = CreateSelectFromTextureCommand(dbConnection);
        insertIntoTextureLodCommand = CreateInsertIntoTextureLodCommand(dbConnection);
        selectFromTextureLodCommand = CreateSelectFromTextureLodCommand(dbConnection);
        insertIntoGeotypicalModelCommand = CreateInsertIntoGeotypicalModelCommand(dbConnection);
        selectFromGeotypicalModelCommand = CreateSelectFromGeotypicalModelCommand(dbConnection);
        insertIntoGeotypicalModelLodCommand = CreateInsertIntoGeotypicalModelLodCommand(dbConnection);
        selectFromGeotypicalModelLodCommand = CreateSelectFromGeotypicalModelLodCommand(dbConnection);
        insertIntoMovingModelCommand = CreateInsertIntoMovingModelCommand(dbConnection);
        selectFromMovingModelCommand = CreateSelectFromMovingModelCommand(dbConnection);
        insertIntoMovingModelLodCommand = CreateInsertIntoMovingModelLodCommand(dbConnection);
        selectFromMovingModelLodCommand = CreateSelectFromMovingModelLodCommand(dbConnection);
        insertIntoTileCommand = CreateInsertIntoTileCommand(dbConnection);
        selectFromTileCommand = CreateSelectFromTileCommand(dbConnection);
        insertIntoTileArchivedFeatureCommand = CreateInsertIntoTileArchivedFeatureCommand(dbConnection);
        selectFromTileArchivedFeatureCommand = CreateSelectFromTileArchivedFeatureCommand(dbConnection);
        insertIntoTileArchivedTextureCommand = CreateInsertIntoTileArchivedTextureCommand(dbConnection);
        selectFromTileArchivedTextureCommand = CreateSelectFromTileArchivedTextureCommand(dbConnection);
        insertIntoNavigationCommand = CreateInsertIntoNavigationCommand(dbConnection);
        selectFromNavigationCommand = CreateSelectFromNavigationCommand(dbConnection);
    }

    private void CreateSchema(DbConnection dbConnection)
    {
        int rowsAffected;
        using DbTransaction dbTransaction = dbConnection.BeginTransaction(IsolationLevel.Serializable);

        using DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.Transaction = dbTransaction;

        dbCommand.CommandText = CreateTableCDBStatement;
        rowsAffected = dbCommand.ExecuteNonQuery();

        dbCommand.CommandText = CreateTableMetadataStatement;
        rowsAffected = dbCommand.ExecuteNonQuery();

        // Need an index on texture name.
        dbCommand.CommandText = CreateTableTextureStatement;
        rowsAffected = dbCommand.ExecuteNonQuery();

        // Need an index on texture name.
        dbCommand.CommandText = CreateTableTextureLodStatement;
        rowsAffected = dbCommand.ExecuteNonQuery();

        // Need an index on dataset (for everything)
        // Need an index on feature_category, feature_subcategory, feature_type
        dbCommand.CommandText = CreateTableGeotypicalModelStatement;
        rowsAffected = dbCommand.ExecuteNonQuery();

        // Need an index on feature_category, feature_subcategory, feature_type, lod
        dbCommand.CommandText = CreateTableGeotypicalModelLodStatement;
        rowsAffected = dbCommand.ExecuteNonQuery();

        // Maybe an index on kind, domain, country, category.
        // Need an index on kind, domain, country, category, subcategory, specific, extra.
        dbCommand.CommandText = CreateTableMovingModelStatement;
        rowsAffected = dbCommand.ExecuteNonQuery();

        // Maybe an index on kind, domain, country, category.
        // Need an index on kind, domain, country, category, subcategory, specific, extra.
        dbCommand.CommandText = CreateTableMovingModelLodStatement;
        rowsAffected = dbCommand.ExecuteNonQuery();

        // Need an index on latitude, longitude, dataset, cs1, cs2, lod, up
        dbCommand.CommandText = CreateTableTileStatement;
        rowsAffected = dbCommand.ExecuteNonQuery();

        // Need an index on latitude, longitude, dataset, cs1, cs2, lod, up
        dbCommand.CommandText = CreateTableTileArchivedFeatureStatement;
        rowsAffected = dbCommand.ExecuteNonQuery();

        // Need an index on latitude, longitude, dataset, cs1, cs2, lod, up
        dbCommand.CommandText = CreateTableTileArchivedTextureStatement;
        rowsAffected = dbCommand.ExecuteNonQuery();

        dbCommand.CommandText = CreateTableNavigationStatement;
        rowsAffected = dbCommand.ExecuteNonQuery();

        dbTransaction.Commit();
    }

    /*
     * TODO: Should we consolidate these?
     * MetadataNameParamName
     * TextureNameParamName
     * ModelNameParamName
     */

    #region Shared SQL Parameters

    /// <summary>
    /// The name of the SQL parameter for the CDB name.
    /// The value must be of type <see cref="DbType.String"/>.
    /// </summary>
    protected abstract string CdbParamName
    {
        get;
    }

    /// <summary>
    /// The name of the SQL parameter for the dataset.
    /// The value must be of type <see cref="DbType.Int32"/>.
    /// </summary>
    protected abstract string DatasetParamName
    {
        get;
    }

    /// <summary>
    /// The name of the SQL parameter for the component selector 1.
    /// The value must be of type <see cref="DbType.Int32"/>.
    /// </summary>
    protected abstract string ComponentSelector1ParamName
    {
        get;
    }

    /// <summary>
    /// The name of the SQL parameter for the component selector 2.
    /// The value must be of type <see cref="DbType.Int32"/>.
    /// </summary>
    protected abstract string ComponentSelector2ParamName
    {
        get;
    }

    /// <summary>
    /// The name of the SQL parameter for the level of detail.
    /// The value must be of type <see cref="DbType.Int32"/>.
    /// </summary>
    protected abstract string LevelOfDetailParamName
    {
        get;
    }

    /// <summary>
    /// The name of the SQL parameter for the file type.
    /// The value must be of type <see cref="DbType.String"/>.
    /// </summary>
    protected abstract string FileTypeParamName
    {
        get;
    }

    /// <summary>
    /// The name of the SQL parameter for the file content.
    /// The value must be of type <see cref="DbType.Binary"/>.
    /// </summary>
    protected abstract string ContentParamName
    {
        get;
    }

    /// <summary>
    /// The name of the column (in most tables) that contains the file contents.
    /// The type is <see cref="DbType.Binary"/>.
    /// </summary>
    protected abstract string ContentColumnName
    {
        get;
    }

    #endregion

    #region CDB

    /// <summary>
    /// The name of the column in the CDB table that contains the CDB name.
    /// The type is <see cref="DbType.String"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Got that?
    /// </para>
    /// </remarks>
    protected abstract string CDBNameColumnName
    {
        get;
    }

    /// <summary>
    /// The SQL DDL statement that creates the CDB table with one column for
    /// the name of the CDB instance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The name of the column is <see cref="CDBNameColumnName"/>.
    /// </para>
    /// </remarks>
    protected abstract string CreateTableCDBStatement
    {
        get;
    }

    /// <summary>
    /// The SQL statement that inserts a new name into the CDB table.
    /// This takes one parameter,
    /// <see cref="CdbParamName"/>.
    /// </summary>
    protected abstract string InsertIntoCDBStatement
    {
        get;
    }

    private DbCommand CreateInsertIntoCDBCommand(DbConnection dbConnection)
    {
        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = InsertIntoCDBStatement;
        CreateAndAttachParameter(dbCommand, CdbParamName, DbType.String);
        dbCommand.Prepare();
        return dbCommand;
    }

    /// <summary>
    /// Inserts a name into the table identifying all the unique data stores
    /// contained in the SQLite database.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An <see cref="SQLDataStore"/> is capable of holding multiple CDB data stores.
    /// Each distinct data store is identified by a name.
    /// </para>
    /// </remarks>
    /// <param name="cdbName">The name of a new CDB data store.</param>
    /// <returns>The number of database rows affected.</returns>
    public virtual int InsertIntoCDB(string cdbName)
    {
        insertIntoCDBCommand.Parameters[CdbParamName].Value = cdbName;

        return insertIntoCDBCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// Inserts a name into the table identifying all the unique data stores
    /// contained in the SQLite database.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An <see cref="SQLDataStore"/> is capable of holding multiple CDB data stores.
    /// Each distinct data store is identified by a name.
    /// </para>
    /// </remarks>
    /// <param name="cdbName">The name of a new CDB data store.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of database rows affected.</returns>
    public virtual Task<int> InsertIntoCDBAsync(string cdbName, CancellationToken cancellationToken = default)
    {
        insertIntoCDBCommand.Parameters[CdbParamName].Value = cdbName;

        return insertIntoCDBCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// The SQL statement that selects the CDB name from the CDB table.
    /// This has no parameters.
    /// This returns one column,
    /// <see cref="CDBNameColumnName"/>.
    /// </summary>
    protected abstract string SelectFromCDBStatement
    {
        get;
    }

    private DbCommand CreateSelectFromCDBCommand(DbConnection dbConnection)
    {
        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = SelectFromCDBStatement;
        dbCommand.Prepare();
        return dbCommand;
    }

    /// <summary>
    /// Returns all CDB data store names in the database.
    /// </summary>
    /// <returns>All the names of the distinct CDB data stores in the database.</returns>
    public virtual IEnumerable<string> SelectFromCDB()
    {
        using DbDataReader dbDataReader = selectFromCDBCommand.ExecuteReader(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult);
        do
        {
            while (dbDataReader.Read())
            {
                string name = dbDataReader.GetString(CDBNameColumnName);
                yield return name;
            }
        } while (dbDataReader.NextResult());
    }

    /// <summary>
    /// Returns all CDB data store names in the database.
    /// </summary>
    /// <returns>All the names of the distinct CDB data stores in the database.</returns>
    public virtual async IAsyncEnumerable<string> SelectFromCDBAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using DbDataReader dbDataReader = await selectFromCDBCommand.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult, cancellationToken);
        do
        {
            while (await dbDataReader.ReadAsync(cancellationToken))
            {
                string name = dbDataReader.GetString(CDBNameColumnName);
                yield return name;
            }
        } while (await dbDataReader.NextResultAsync(cancellationToken));
    }

    #endregion

    #region Metadata

    /// <summary>
    /// The SQL DDL statement to create the Metadata table.
    /// </summary>
    protected abstract string CreateTableMetadataStatement
    {
        get;
    }

    /// <summary>
    /// The name of the SQL parameter for the metadata name.
    /// The value must be of type <see cref="DbType.String"/>.
    /// </summary>
    protected abstract string MetadataNameParamName
    {
        get;
    }

    /// <summary>
    /// The SQL statement to insert a row in the Metadata table.
    /// This requires four parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="MetadataNameParamName"/>,
    /// <see cref="FileTypeParamName"/>,
    /// <see cref="ContentParamName"/>
    /// </summary>
    protected abstract string InsertIntoMetadataStatement
    {
        get;
    }

    private DbCommand CreateInsertIntoMetadataCommand(DbConnection dbConnection)
    {
        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = InsertIntoMetadataStatement;
        CreateAndAttachParameter(dbCommand, CdbParamName, DbType.String);
        CreateAndAttachMetadataParameters(dbCommand);
        CreateAndAttachParameter(dbCommand, ContentParamName, DbType.Binary);
        dbCommand.Prepare();
        return dbCommand;
    }

    private void CreateAndAttachMetadataParameters(DbCommand dbCommand)
    {
        CreateAndAttachParameter(dbCommand, MetadataNameParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, FileTypeParamName, DbType.String);
    }

    private void SetMetadataParameters(DbCommand dbCommand, Metadata metadata)
    {
        dbCommand.Parameters[MetadataNameParamName].Value = metadata.Name;
        dbCommand.Parameters[FileTypeParamName].Value = metadata.FileType;
    }

    /// <summary>
    /// Inserts a metadata file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="metadata">The metadata identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual int InsertIntoMetadata(string cdbName, Metadata metadata, byte[] content)
    {
        insertIntoMetadataCommand.Parameters[CdbParamName].Value = cdbName;
        SetMetadataParameters(insertIntoMetadataCommand, metadata);
        insertIntoMetadataCommand.Parameters[ContentParamName].Value = content;

        return insertIntoMetadataCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// Inserts a metadata file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="metadata">The metadata identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual int InsertIntoMetadata(string cdbName, Metadata metadata, Stream content)
    {
        insertIntoMetadataCommand.Parameters[CdbParamName].Value = cdbName;
        SetMetadataParameters(insertIntoMetadataCommand, metadata);
        insertIntoMetadataCommand.Parameters[ContentParamName].Value = content;

        return insertIntoMetadataCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// Inserts a metadata file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="metadata">The metadata identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual Task<int> InsertIntoMetadataAsync(string cdbName, Metadata metadata, byte[] content, CancellationToken cancellationToken = default)
    {
        insertIntoMetadataCommand.Parameters[CdbParamName].Value = cdbName;
        SetMetadataParameters(insertIntoMetadataCommand, metadata);
        insertIntoMetadataCommand.Parameters[ContentParamName].Value = content;

        return insertIntoMetadataCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Inserts a metadata file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="metadata">The metadata identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual Task<int> InsertIntoMetadataAsync(string cdbName, Metadata metadata, Stream content, CancellationToken cancellationToken = default)
    {
        insertIntoMetadataCommand.Parameters[CdbParamName].Value = cdbName;
        SetMetadataParameters(insertIntoMetadataCommand, metadata);
        insertIntoMetadataCommand.Parameters[ContentParamName].Value = content;

        return insertIntoMetadataCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// The SQL statement to select one row from the Metadata table.
    /// This requires three parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="MetadataNameParamName"/>,
    /// <see cref="FileTypeParamName"/>.
    /// It returns one column,
    /// <see cref="ContentColumnName"/>.
    /// </summary>
    protected abstract string SelectFromMetadataStatement
    {
        get;
    }

    private DbCommand CreateSelectFromMetadataCommand(DbConnection dbConnection)
    {
        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = SelectFromMetadataStatement;
        CreateAndAttachParameter(dbCommand, CdbParamName, DbType.String);
        CreateAndAttachMetadataParameters(dbCommand);
        dbCommand.Prepare();
        return dbCommand;
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
    public virtual bool TrySelectFromMetadata(string cdbName, Metadata metadata, [NotNullWhen(true)] out byte[] content)
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
    public virtual bool TrySelectFromMetadata(string cdbName, Metadata metadata, Stream output)
    {
        selectFromMetadataCommand.Parameters[CdbParamName].Value = cdbName;
        SetMetadataParameters(selectFromMetadataCommand, metadata);

        using DbDataReader dbDataReader = selectFromMetadataCommand.ExecuteReader(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
        do
        {
            while (dbDataReader.Read())
            {
                using Stream stream = dbDataReader.GetStream(ContentColumnName);
                stream.CopyTo(output);
                return true;
            }
        } while (dbDataReader.NextResult());
        return false;
    }

    /// <summary>
    /// Tries to find and return a metadata file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="output"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="metadata">The metadata identifier.</param>
    /// <param name="output">A stream that will receive the file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public virtual async Task<bool> TrySelectFromMetadataAsync(string cdbName, Metadata metadata, Stream output, CancellationToken cancellationToken = default)
    {
        selectFromMetadataCommand.Parameters[CdbParamName].Value = cdbName;
        SetMetadataParameters(selectFromMetadataCommand, metadata);

        await using DbDataReader dbDataReader = await selectFromMetadataCommand.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow, cancellationToken);
        do
        {
            while (await dbDataReader.ReadAsync(cancellationToken))
            {
                await using Stream stream = dbDataReader.GetStream(ContentColumnName);
                await stream.CopyToAsync(output, cancellationToken);
                return true;
            }
        } while (await dbDataReader.NextResultAsync(cancellationToken));
        return false;
    }

    /// <summary>
    /// Returns the metadata file from a CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="metadata">The metadata identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The file contents.</returns>
    /// <exception cref="FileNotFoundException">If the file was not found in the database.</exception>
    public virtual async Task<byte[]> SelectFromMetadataAsync(string cdbName, Metadata metadata, CancellationToken cancellationToken = default)
    {
        await using MemoryStream memoryStream = new();
        if (await TrySelectFromMetadataAsync(cdbName, metadata, memoryStream, cancellationToken))
        {
            return memoryStream.ToArray();
        }
        else
        {
            throw new FileNotFoundException("Metadata file not found.", metadata.Filename);
        }
    }

    #endregion

    #region Texture

    /// <summary>
    /// The name of the SQL parameter for the texture name.
    /// The value must be of type <see cref="DbType.String"/>.
    /// </summary>
    protected abstract string TextureNameParamName
    {
        get;
    }

    /// <summary>
    /// The SQL DDL statement to create the Texture table.
    /// </summary>
    protected abstract string CreateTableTextureStatement
    {
        get;
    }

    /// <summary>
    /// The SQL statement to insert a row into the Texture table.
    /// This requires seven parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="TextureNameParamName"/>,
    /// <see cref="FileTypeParamName"/>,
    /// <see cref="ContentParamName"/>.
    /// </summary>
    protected abstract string InsertIntoTextureStatement
    {
        get;
    }

    private DbCommand CreateInsertIntoTextureCommand(DbConnection dbConnection)
    {
        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = InsertIntoTextureStatement;
        CreateAndAttachParameter(dbCommand, CdbParamName, DbType.String);
        CreateAndAttachTextureParameters(dbCommand);
        CreateAndAttachParameter(dbCommand, ContentParamName, DbType.Binary);
        dbCommand.Prepare();
        return dbCommand;
    }

    private void CreateAndAttachTextureParameters(DbCommand dbCommand)
    {
        CreateAndAttachParameter(dbCommand, DatasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, TextureNameParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, FileTypeParamName, DbType.String);
    }

    private void SetTextureParameters(DbCommand dbCommand, Texture texture)
    {
        dbCommand.Parameters[DatasetParamName].Value = texture.Dataset.Value;
        dbCommand.Parameters[ComponentSelector1ParamName].Value = texture.ComponentSelector1;
        dbCommand.Parameters[ComponentSelector2ParamName].Value = texture.ComponentSelector2;
        dbCommand.Parameters[TextureNameParamName].Value = texture.Name;
        dbCommand.Parameters[FileTypeParamName].Value = texture.FileType;
    }

    /// <summary>
    /// Inserts a texture file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="texture">The texture identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual int InsertIntoTexture(string cdbName, Texture texture, byte[] content)
    {
        insertIntoTextureCommand.Parameters[CdbParamName].Value = cdbName;
        SetTextureParameters(insertIntoTextureCommand, texture);
        insertIntoTextureCommand.Parameters[ContentParamName].Value = content;

        return insertIntoTextureCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// Inserts a texture file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="texture">The texture identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual int InsertIntoTexture(string cdbName, Texture texture, Stream content)
    {
        insertIntoTextureCommand.Parameters[CdbParamName].Value = cdbName;
        SetTextureParameters(insertIntoTextureCommand, texture);
        insertIntoTextureCommand.Parameters[ContentParamName].Value = content;

        return insertIntoTextureCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// Inserts a texture file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="texture">The texture identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual Task<int> InsertIntoTextureAsync(string cdbName, Texture texture, byte[] content, CancellationToken cancellationToken = default)
    {
        insertIntoTextureCommand.Parameters[CdbParamName].Value = cdbName;
        SetTextureParameters(insertIntoTextureCommand, texture);
        insertIntoTextureCommand.Parameters[ContentParamName].Value = content;

        return insertIntoTextureCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Inserts a texture file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="texture">The texture identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual Task<int> InsertIntoTextureAsync(string cdbName, Texture texture, Stream content, CancellationToken cancellationToken = default)
    {
        insertIntoTextureCommand.Parameters[CdbParamName].Value = cdbName;
        SetTextureParameters(insertIntoTextureCommand, texture);
        insertIntoTextureCommand.Parameters[ContentParamName].Value = content;

        return insertIntoTextureCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// The SQL statement to select a row from the Texture table.
    /// This requires six parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="TextureNameParamName"/>,
    /// <see cref="FileTypeParamName"/>.
    /// It returns one column,
    /// <see cref="ContentColumnName"/>.
    /// </summary>
    protected abstract string SelectFromTextureStatement
    {
        get;
    }

    private DbCommand CreateSelectFromTextureCommand(DbConnection dbConnection)
    {
        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = SelectFromTextureStatement;
        CreateAndAttachParameter(dbCommand, CdbParamName, DbType.String);
        CreateAndAttachTextureParameters(dbCommand);
        dbCommand.Prepare();
        return dbCommand;
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
    public virtual bool TrySelectFromTexture(string cdbName, Texture texture, [NotNullWhen(true)] out byte[] content)
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
    public virtual bool TrySelectFromTexture(string cdbName, Texture texture, Stream output)
    {
        selectFromTextureCommand.Parameters[CdbParamName].Value = cdbName;
        SetTextureParameters(selectFromTextureCommand, texture);

        using DbDataReader dbDataReader = selectFromTextureCommand.ExecuteReader(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
        do
        {
            while (dbDataReader.Read())
            {
                using Stream stream = dbDataReader.GetStream(ContentColumnName);
                stream.CopyTo(output);
                return true;
            }
        } while (dbDataReader.NextResult());
        return false;
    }

    /// <summary>
    /// Tries to find and return a texture file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="output"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="texture">The texture identifier.</param>
    /// <param name="output">A stream that will receive the file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public virtual async Task<bool> TrySelectFromTextureAsync(string cdbName, Texture texture, Stream output, CancellationToken cancellationToken = default)
    {
        selectFromTextureCommand.Parameters[CdbParamName].Value = cdbName;
        SetTextureParameters(selectFromTextureCommand, texture);

        await using DbDataReader dbDataReader = await selectFromTextureCommand.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow, cancellationToken);
        do
        {
            while (await dbDataReader.ReadAsync(cancellationToken))
            {
                await using Stream stream = dbDataReader.GetStream(ContentColumnName);
                await stream.CopyToAsync(output, cancellationToken);
                return true;
            }
        } while (await dbDataReader.NextResultAsync(cancellationToken));
        return false;
    }

    /// <summary>
    /// Reads a texture file from a CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="texture">The texture identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The file contents.</returns>
    /// <exception cref="FileNotFoundException">If the file was not found in the database.</exception>
    public virtual async Task<byte[]> SelectFromTextureAsync(string cdbName, Texture texture, CancellationToken cancellationToken = default)
    {
        await using MemoryStream memoryStream = new();
        if (await TrySelectFromTextureAsync(cdbName, texture, memoryStream, cancellationToken))
        {
            return memoryStream.ToArray();
        }
        else
        {
            throw new FileNotFoundException("Texture file not found.", texture.Filename);
        }
    }

    #endregion

    #region Texture LOD

    /// <summary>
    /// The SQL DDL statement to create the Texture Level of Detail table.
    /// </summary>
    protected abstract string CreateTableTextureLodStatement
    {
        get;
    }

    /// <summary>
    /// The SQL statement to insert a row into the Texture Level of Detail table.
    /// This requires eight parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="LevelOfDetailParamName"/>,
    /// <see cref="TextureNameParamName"/>,
    /// <see cref="FileTypeParamName"/>,
    /// <see cref="ContentParamName"/>.
    /// </summary>
    protected abstract string InsertIntoTextureLodStatement
    {
        get;
    }

    private DbCommand CreateInsertIntoTextureLodCommand(DbConnection dbConnection)
    {
        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = InsertIntoTextureLodStatement;
        CreateAndAttachParameter(dbCommand, CdbParamName, DbType.String);
        CreateAndAttachTextureLodParameters(dbCommand);
        CreateAndAttachParameter(dbCommand, ContentParamName, DbType.Binary);
        dbCommand.Prepare();
        return dbCommand;
    }

    private void CreateAndAttachTextureLodParameters(DbCommand dbCommand)
    {
        CreateAndAttachParameter(dbCommand, DatasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, LevelOfDetailParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, TextureNameParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, FileTypeParamName, DbType.String);
    }

    private void SetTextureLodParameters(DbCommand dbCommand, TextureLod textureLod)
    {
        dbCommand.Parameters[DatasetParamName].Value = textureLod.Dataset.Value;
        dbCommand.Parameters[ComponentSelector1ParamName].Value = textureLod.ComponentSelector1;
        dbCommand.Parameters[ComponentSelector2ParamName].Value = textureLod.ComponentSelector2;
        dbCommand.Parameters[LevelOfDetailParamName].Value = textureLod.LevelOfDetail.Value;
        dbCommand.Parameters[TextureNameParamName].Value = textureLod.Name;
        dbCommand.Parameters[FileTypeParamName].Value = textureLod.FileType;
    }

    /// <summary>
    /// Inserts a texture mipmap file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="textureLod">The texture mipmap identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual int InsertIntoTextureLod(string cdbName, TextureLod textureLod, byte[] content)
    {
        insertIntoTextureLodCommand.Parameters[CdbParamName].Value = cdbName;
        SetTextureLodParameters(insertIntoTextureLodCommand, textureLod);
        insertIntoTextureLodCommand.Parameters[ContentParamName].Value = content;

        return insertIntoTextureLodCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// Inserts a texture mipmap file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="textureLod">The texture mipmap identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual int InsertIntoTextureLod(string cdbName, TextureLod textureLod, Stream content)
    {
        insertIntoTextureLodCommand.Parameters[CdbParamName].Value = cdbName;
        SetTextureLodParameters(insertIntoTextureLodCommand, textureLod);
        insertIntoTextureLodCommand.Parameters[ContentParamName].Value = content;

        return insertIntoTextureLodCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// Inserts a texture mipmap file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="textureLod">The texture mipmap identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual Task<int> InsertIntoTextureLodAsync(string cdbName, TextureLod textureLod, byte[] content, CancellationToken cancellationToken = default)
    {
        insertIntoTextureLodCommand.Parameters[CdbParamName].Value = cdbName;
        SetTextureLodParameters(insertIntoTextureLodCommand, textureLod);
        insertIntoTextureLodCommand.Parameters[ContentParamName].Value = content;

        return insertIntoTextureLodCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Inserts a texture mipmap file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="textureLod">The texture mipmap identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual Task<int> InsertIntoTextureLodAsync(string cdbName, TextureLod textureLod, Stream content, CancellationToken cancellationToken = default)
    {
        insertIntoTextureLodCommand.Parameters[CdbParamName].Value = cdbName;
        SetTextureLodParameters(insertIntoTextureLodCommand, textureLod);
        insertIntoTextureLodCommand.Parameters[ContentParamName].Value = content;

        return insertIntoTextureLodCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// The SQL statement to select a row from the Texture Level of Detail table.
    /// This requires seven parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="LevelOfDetailParamName"/>,
    /// <see cref="TextureNameParamName"/>,
    /// <see cref="FileTypeParamName"/>.
    /// It returns one column,
    /// <see cref="ContentColumnName"/>.
    /// </summary>
    protected abstract string SelectFromTextureLodStatement
    {
        get;
    }

    private DbCommand CreateSelectFromTextureLodCommand(DbConnection dbConnection)
    {
        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = SelectFromTextureLodStatement;
        CreateAndAttachParameter(dbCommand, CdbParamName, DbType.String);
        CreateAndAttachTextureLodParameters(dbCommand);
        dbCommand.Prepare();
        return dbCommand;
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
    public virtual bool TrySelectFromTextureLod(string cdbName, TextureLod textureLod, [NotNullWhen(true)] out byte[] content)
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
    public virtual bool TrySelectFromTextureLod(string cdbName, TextureLod textureLod, Stream output)
    {
        selectFromTextureLodCommand.Parameters[CdbParamName].Value = cdbName;
        SetTextureLodParameters(selectFromTextureLodCommand, textureLod);

        using DbDataReader dbDataReader = selectFromTextureLodCommand.ExecuteReader(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
        do
        {
            while (dbDataReader.Read())
            {
                using Stream stream = dbDataReader.GetStream(ContentColumnName);
                stream.CopyTo(output);
                return true;
            }
        } while (dbDataReader.NextResult());
        return false;
    }

    /// <summary>
    /// Tries to find and return a texture mipmap file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="output"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="textureLod">The texture mipmap identifier.</param>
    /// <param name="output">A stream that will receive the file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public virtual async Task<bool> TrySelectFromTextureLodAsync(string cdbName, TextureLod textureLod, Stream output, CancellationToken cancellationToken = default)
    {
        selectFromTextureLodCommand.Parameters[CdbParamName].Value = cdbName;
        SetTextureLodParameters(selectFromTextureLodCommand, textureLod);

        await using DbDataReader dbDataReader = await selectFromTextureLodCommand.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow, cancellationToken);
        do
        {
            while (await dbDataReader.ReadAsync(cancellationToken))
            {
                await using Stream stream = dbDataReader.GetStream(ContentColumnName);
                await stream.CopyToAsync(output, cancellationToken);
                return true;
            }
        } while (await dbDataReader.NextResultAsync(cancellationToken));
        return false;
    }

    /// <summary>
    /// Returns a texture mipmap file from a CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="textureLod">The texture mipmap identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The file contents.</returns>
    /// <exception cref="FileNotFoundException">If the file was not found in the database.</exception>
    public virtual async Task<byte[]> SelectFromTextureLodAsync(string cdbName, TextureLod textureLod, CancellationToken cancellationToken = default)
    {
        await using MemoryStream memoryStream = new();
        if (await TrySelectFromTextureLodAsync(cdbName, textureLod, memoryStream, cancellationToken))
        {
            return memoryStream.ToArray();
        }
        else
        {
            throw new FileNotFoundException("Texture mipmap file not found.", textureLod.Filename);
        }
    }

    #endregion

    #region Geotypical Model

    #region Feature Code Parameters

    /// <summary>
    /// The name of the SQL parameter for the Feature Code category.
    /// The value must be of type <see cref="DbType.String"/>.
    /// </summary>
    protected abstract string FeatureCategoryParamName
    {
        get;
    }

    /// <summary>
    /// The name of the SQL parameter for the Feature Code subcategory.
    /// The value must be of type <see cref="DbType.String"/>.
    /// </summary>
    protected abstract string FeatureSubcategoryParamName
    {
        get;
    }

    /// <summary>
    /// The name of the SQL parameter for the Feature Code type.
    /// The value must be of type <see cref="DbType.Int32"/>.
    /// </summary>
    protected abstract string FeatureTypeParamName
    {
        get;
    }

    /// <summary>
    /// The name of the SQL parameter for the Feature Code subcode.
    /// The value must be of type <see cref="DbType.Int32"/>.
    /// </summary>
    protected abstract string FeatureSubcodeParamName
    {
        get;
    }

    #endregion

    /// <summary>
    /// The name of the SQL parameter for the geotypical model name.
    /// The value must be of type <see cref="DbType.String"/>.
    /// </summary>
    protected abstract string ModelNameParamName
    {
        get;
    }

    /// <summary>
    /// The SQL DDL statement to create the Geotypical Model table.
    /// </summary>
    protected abstract string CreateTableGeotypicalModelStatement
    {
        get;
    }

    /// <summary>
    /// The SQL statement to insert a row into the Geotypical Model table.
    /// This takes eleven parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="FeatureCategoryParamName"/>,
    /// <see cref="FeatureSubcategoryParamName"/>,
    /// <see cref="FeatureTypeParamName"/>,
    /// <see cref="FeatureSubcodeParamName"/>,
    /// <see cref="ModelNameParamName"/>,
    /// <see cref="FileTypeParamName"/>,
    /// <see cref="ContentParamName"/>.
    /// </summary>
    protected abstract string InsertIntoGeotypicalModelStatement
    {
        get;
    }

    private DbCommand CreateInsertIntoGeotypicalModelCommand(DbConnection dbConnection)
    {
        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = InsertIntoGeotypicalModelStatement;
        CreateAndAttachParameter(dbCommand, CdbParamName, DbType.String);
        CreateAndAttachGeotypicalModelParameters(dbCommand);
        CreateAndAttachParameter(dbCommand, ContentParamName, DbType.Binary);
        dbCommand.Prepare();
        return dbCommand;
    }

    private void CreateAndAttachGeotypicalModelParameters(DbCommand dbCommand)
    {
        CreateAndAttachParameter(dbCommand, DatasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, FeatureCategoryParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, FeatureSubcategoryParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, FeatureTypeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, FeatureSubcodeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ModelNameParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, FileTypeParamName, DbType.String);
    }

    private void SetGeotypicalModelParameters(DbCommand dbCommand, GeotypicalModel geotypicalModel)
    {
        dbCommand.Parameters[DatasetParamName].Value = geotypicalModel.Dataset.Value;
        dbCommand.Parameters[ComponentSelector1ParamName].Value = geotypicalModel.ComponentSelector1;
        dbCommand.Parameters[ComponentSelector2ParamName].Value = geotypicalModel.ComponentSelector2;
        dbCommand.Parameters[FeatureCategoryParamName].Value = geotypicalModel.FeatureCode.Category;
        dbCommand.Parameters[FeatureSubcategoryParamName].Value = geotypicalModel.FeatureCode.Subcategory;
        dbCommand.Parameters[FeatureTypeParamName].Value = geotypicalModel.FeatureCode.Type;
        dbCommand.Parameters[FeatureSubcodeParamName].Value = geotypicalModel.FeatureSubcode;
        dbCommand.Parameters[ModelNameParamName].Value = geotypicalModel.Name;
        dbCommand.Parameters[FileTypeParamName].Value = geotypicalModel.FileType;
    }

    /// <summary>
    /// Inserts a geotypical model file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="geotypicalModel">The geotypical model identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual int InsertIntoGeotypicalModel(string cdbName, GeotypicalModel geotypicalModel, byte[] content)
    {
        insertIntoGeotypicalModelCommand.Parameters[CdbParamName].Value = cdbName;
        SetGeotypicalModelParameters(insertIntoGeotypicalModelCommand, geotypicalModel);
        insertIntoGeotypicalModelCommand.Parameters[ContentParamName].Value = content;

        return insertIntoGeotypicalModelCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// Inserts a geotypical model file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="geotypicalModel">The geotypical model identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual int InsertIntoGeotypicalModel(string cdbName, GeotypicalModel geotypicalModel, Stream content)
    {
        insertIntoGeotypicalModelCommand.Parameters[CdbParamName].Value = cdbName;
        SetGeotypicalModelParameters(insertIntoGeotypicalModelCommand, geotypicalModel);
        insertIntoGeotypicalModelCommand.Parameters[ContentParamName].Value = content;

        return insertIntoGeotypicalModelCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// Inserts a geotypical model file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="geotypicalModel">The geotypical model identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual Task<int> InsertIntoGeotypicalModelAsync(string cdbName, GeotypicalModel geotypicalModel, byte[] content, CancellationToken cancellationToken = default)
    {
        insertIntoGeotypicalModelCommand.Parameters[CdbParamName].Value = cdbName;
        SetGeotypicalModelParameters(insertIntoGeotypicalModelCommand, geotypicalModel);
        insertIntoGeotypicalModelCommand.Parameters[ContentParamName].Value = content;

        return insertIntoGeotypicalModelCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Inserts a geotypical model file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="geotypicalModel">The geotypical model identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual Task<int> InsertIntoGeotypicalModelAsync(string cdbName, GeotypicalModel geotypicalModel, Stream content, CancellationToken cancellationToken = default)
    {
        insertIntoGeotypicalModelCommand.Parameters[CdbParamName].Value = cdbName;
        SetGeotypicalModelParameters(insertIntoGeotypicalModelCommand, geotypicalModel);
        insertIntoGeotypicalModelCommand.Parameters[ContentParamName].Value = content;

        return insertIntoGeotypicalModelCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// The SQL statement to select a row from the Geotypical Model table.
    /// This takes ten parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="FeatureCategoryParamName"/>,
    /// <see cref="FeatureSubcategoryParamName"/>,
    /// <see cref="FeatureTypeParamName"/>,
    /// <see cref="FeatureSubcodeParamName"/>,
    /// <see cref="ModelNameParamName"/>,
    /// <see cref="FileTypeParamName"/>.
    /// It returns one column,
    /// <see cref="ContentColumnName"/>.
    /// </summary>
    protected abstract string SelectFromGeotypicalModelStatement
    {
        get;
    }

    private DbCommand CreateSelectFromGeotypicalModelCommand(DbConnection dbConnection)
    {
        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = SelectFromGeotypicalModelStatement;
        CreateAndAttachParameter(dbCommand, CdbParamName, DbType.String);
        CreateAndAttachGeotypicalModelParameters(dbCommand);
        dbCommand.Prepare();
        return dbCommand;
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
    public virtual bool TrySelectFromGeotypicalModel(string cdbName, GeotypicalModel geotypicalModel, [NotNullWhen(true)] out byte[] content)
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
    public virtual bool TrySelectFromGeotypicalModel(string cdbName, GeotypicalModel geotypicalModel, Stream output)
    {
        selectFromGeotypicalModelCommand.Parameters[CdbParamName].Value = cdbName;
        SetGeotypicalModelParameters(selectFromGeotypicalModelCommand, geotypicalModel);

        using DbDataReader dbDataReader = selectFromGeotypicalModelCommand.ExecuteReader(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
        do
        {
            while (dbDataReader.Read())
            {
                using Stream stream = dbDataReader.GetStream(ContentColumnName);
                stream.CopyTo(output);
                return true;
            }
        } while (dbDataReader.NextResult());
        return false;
    }

    /// <summary>
    /// Tries to find and return a geotypical model file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="output"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="geotypicalModel">The geotypical model identifier.</param>
    /// <param name="output">A stream that will receive the file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public virtual async Task<bool> TrySelectFromGeotypicalModelAsync(string cdbName, GeotypicalModel geotypicalModel, Stream output, CancellationToken cancellationToken = default)
    {
        selectFromGeotypicalModelCommand.Parameters[CdbParamName].Value = cdbName;
        SetGeotypicalModelParameters(selectFromGeotypicalModelCommand, geotypicalModel);

        await using DbDataReader dbDataReader = await selectFromGeotypicalModelCommand.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow, cancellationToken);
        do
        {
            while (await dbDataReader.ReadAsync(cancellationToken))
            {
                await using Stream stream = dbDataReader.GetStream(ContentColumnName);
                await stream.CopyToAsync(output, cancellationToken);
                return true;
            }
        } while (await dbDataReader.NextResultAsync(cancellationToken));
        return false;
    }

    /// <summary>
    /// Returns a geotypical model file from a CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="geotypicalModel">The geotypical model identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException">If the file was not found in the database.</exception>
    public virtual async Task<byte[]> SelectFromGeotypicalModelAsync(string cdbName, GeotypicalModel geotypicalModel, CancellationToken cancellationToken = default)
    {
        await using MemoryStream memoryStream = new();
        if (await TrySelectFromGeotypicalModelAsync(cdbName, geotypicalModel, memoryStream, cancellationToken))
        {
            return memoryStream.ToArray();
        }
        else
        {
            throw new FileNotFoundException("Geotypical model file not found.", geotypicalModel.Filename);
        }
    }

    #endregion

    #region Geotypical Model LOD

    /// <summary>
    /// The SQL DDL statement to create the Geotypical Model Level of Detail table.
    /// </summary>
    protected abstract string CreateTableGeotypicalModelLodStatement
    {
        get;
    }

    /// <summary>
    /// The SQL statement to insert a row into the Geotypical Model Level of Detail table.
    /// This takes twelve parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="LevelOfDetailParamName"/>,
    /// <see cref="FeatureCategoryParamName"/>,
    /// <see cref="FeatureSubcategoryParamName"/>,
    /// <see cref="FeatureTypeParamName"/>,
    /// <see cref="FeatureSubcodeParamName"/>,
    /// <see cref="ModelNameParamName"/>,
    /// <see cref="FileTypeParamName"/>,
    /// <see cref="ContentParamName"/>.
    /// </summary>
    protected abstract string InsertIntoGeotypicalModelLodStatement
    {
        get;
    }

    private DbCommand CreateInsertIntoGeotypicalModelLodCommand(DbConnection dbConnection)
    {
        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = InsertIntoGeotypicalModelLodStatement;
        CreateAndAttachParameter(dbCommand, CdbParamName, DbType.String);
        CreateAndAttachGeotypicalModelLodParameters(dbCommand);
        CreateAndAttachParameter(dbCommand, ContentParamName, DbType.Binary);
        dbCommand.Prepare();
        return dbCommand;
    }

    private void CreateAndAttachGeotypicalModelLodParameters(DbCommand dbCommand)
    {
        CreateAndAttachParameter(dbCommand, DatasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, LevelOfDetailParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, FeatureCategoryParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, FeatureSubcategoryParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, FeatureTypeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, FeatureSubcodeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ModelNameParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, FileTypeParamName, DbType.String);
    }

    private void SetGeotypicalModelLodParameters(DbCommand dbCommand, GeotypicalModelLod geotypicalModelLod)
    {
        dbCommand.Parameters[DatasetParamName].Value = geotypicalModelLod.Dataset.Value;
        dbCommand.Parameters[ComponentSelector1ParamName].Value = geotypicalModelLod.ComponentSelector1;
        dbCommand.Parameters[ComponentSelector2ParamName].Value = geotypicalModelLod.ComponentSelector2;
        dbCommand.Parameters[LevelOfDetailParamName].Value = geotypicalModelLod.LevelOfDetail.Value;
        dbCommand.Parameters[FeatureCategoryParamName].Value = geotypicalModelLod.FeatureCode.Category;
        dbCommand.Parameters[FeatureSubcategoryParamName].Value = geotypicalModelLod.FeatureCode.Subcategory;
        dbCommand.Parameters[FeatureTypeParamName].Value = geotypicalModelLod.FeatureCode.Type;
        dbCommand.Parameters[FeatureSubcodeParamName].Value = geotypicalModelLod.FeatureSubcode;
        dbCommand.Parameters[ModelNameParamName].Value = geotypicalModelLod.Name;
        dbCommand.Parameters[FileTypeParamName].Value = geotypicalModelLod.FileType;
    }

    /// <summary>
    /// Inserts a geotypical model level of detail file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="geotypicalModelLod">The geotypical model level of detail identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual int InsertIntoGeotypicalModelLod(string cdbName, GeotypicalModelLod geotypicalModelLod, byte[] content)
    {
        insertIntoGeotypicalModelLodCommand.Parameters[CdbParamName].Value = cdbName;
        SetGeotypicalModelLodParameters(insertIntoGeotypicalModelLodCommand, geotypicalModelLod);
        insertIntoGeotypicalModelLodCommand.Parameters[ContentParamName].Value = content;

        return insertIntoGeotypicalModelLodCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// Inserts a geotypical model level of detail file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="geotypicalModelLod">The geotypical model level of detail identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual int InsertIntoGeotypicalModelLod(string cdbName, GeotypicalModelLod geotypicalModelLod, Stream content)
    {
        insertIntoGeotypicalModelLodCommand.Parameters[CdbParamName].Value = cdbName;
        SetGeotypicalModelLodParameters(insertIntoGeotypicalModelLodCommand, geotypicalModelLod);
        insertIntoGeotypicalModelLodCommand.Parameters[ContentParamName].Value = content;

        return insertIntoGeotypicalModelLodCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// Inserts a geotypical model level of detail file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="geotypicalModelLod">The geotypical model level of detail identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual Task<int> InsertIntoGeotypicalModelLodAsync(string cdbName, GeotypicalModelLod geotypicalModelLod, byte[] content, CancellationToken cancellationToken = default)
    {
        insertIntoGeotypicalModelLodCommand.Parameters[CdbParamName].Value = cdbName;
        SetGeotypicalModelLodParameters(insertIntoGeotypicalModelLodCommand, geotypicalModelLod);
        insertIntoGeotypicalModelLodCommand.Parameters[ContentParamName].Value = content;

        return insertIntoGeotypicalModelLodCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Inserts a geotypical model level of detail file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="geotypicalModelLod">The geotypical model level of detail identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual Task<int> InsertIntoGeotypicalModelLodAsync(string cdbName, GeotypicalModelLod geotypicalModelLod, Stream content, CancellationToken cancellationToken = default)
    {
        insertIntoGeotypicalModelLodCommand.Parameters[CdbParamName].Value = cdbName;
        SetGeotypicalModelLodParameters(insertIntoGeotypicalModelLodCommand, geotypicalModelLod);
        insertIntoGeotypicalModelLodCommand.Parameters[ContentParamName].Value = content;

        return insertIntoGeotypicalModelLodCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// The SQL statement to select a row from the Geotypical Model Level of Detail table.
    /// This takes eleven parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="LevelOfDetailParamName"/>,
    /// <see cref="FeatureCategoryParamName"/>,
    /// <see cref="FeatureSubcategoryParamName"/>,
    /// <see cref="FeatureTypeParamName"/>,
    /// <see cref="FeatureSubcodeParamName"/>,
    /// <see cref="ModelNameParamName"/>,
    /// <see cref="FileTypeParamName"/>.
    /// It returns one column,
    /// <see cref="ContentColumnName"/>.
    /// </summary>
    protected abstract string SelectFromGeotypicalModelLodStatement
    {
        get;
    }

    private DbCommand CreateSelectFromGeotypicalModelLodCommand(DbConnection dbConnection)
    {
        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = SelectFromGeotypicalModelLodStatement;
        CreateAndAttachParameter(dbCommand, CdbParamName, DbType.String);
        CreateAndAttachGeotypicalModelLodParameters(dbCommand);
        dbCommand.Prepare();
        return dbCommand;
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
    public virtual bool TrySelectFromGeotypicalModelLod(string cdbName, GeotypicalModelLod geotypicalModelLod, [NotNullWhen(true)] out byte[] content)
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
    public virtual bool TrySelectFromGeotypicalModelLod(string cdbName, GeotypicalModelLod geotypicalModelLod, Stream output)
    {
        selectFromGeotypicalModelLodCommand.Parameters[CdbParamName].Value = cdbName;
        SetGeotypicalModelLodParameters(selectFromGeotypicalModelLodCommand, geotypicalModelLod);

        using DbDataReader dbDataReader = selectFromGeotypicalModelLodCommand.ExecuteReader(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
        do
        {
            while (dbDataReader.Read())
            {
                using Stream stream = dbDataReader.GetStream(ContentColumnName);
                stream.CopyTo(output);
                return true;
            }
        } while (dbDataReader.NextResult());
        return false;
    }

    /// <summary>
    /// Tries to find and return a geotypical model level of detail file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="output"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="geotypicalModelLod">The geotypical model level of detail identifier.</param>
    /// <param name="output">A stream that will receive the file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public virtual async Task<bool> TrySelectFromGeotypicalModelLodAsync(string cdbName, GeotypicalModelLod geotypicalModelLod, Stream output, CancellationToken cancellationToken = default)
    {
        selectFromGeotypicalModelLodCommand.Parameters[CdbParamName].Value = cdbName;
        SetGeotypicalModelLodParameters(selectFromGeotypicalModelLodCommand, geotypicalModelLod);

        await using DbDataReader dbDataReader = await selectFromGeotypicalModelLodCommand.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow, cancellationToken);
        do
        {
            while (await dbDataReader.ReadAsync(cancellationToken))
            {
                await using Stream stream = dbDataReader.GetStream(ContentColumnName);
                await stream.CopyToAsync(output, cancellationToken);
                return true;
            }
        } while (await dbDataReader.NextResultAsync(cancellationToken));
        return false;
    }

    /// <summary>
    /// Returns a geotypical model level of detail file from a CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="geotypicalModelLod">The geotypical model level of detail identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The file contents.</returns>
    /// <exception cref="FileNotFoundException">If the file was not found in the database.</exception>
    public virtual async Task<byte[]> SelectFromGeotypicalModelLodAsync(string cdbName, GeotypicalModelLod geotypicalModelLod, CancellationToken cancellationToken = default)
    {
        await using MemoryStream memoryStream = new();
        if (await TrySelectFromGeotypicalModelLodAsync(cdbName, geotypicalModelLod, memoryStream, cancellationToken))
        {
            return memoryStream.ToArray();
        }
        else
        {
            throw new FileNotFoundException("Geotypical model level of detail file not found.", geotypicalModelLod.Filename);
        }
    }

    #endregion

    #region Moving Model

    #region DIS Code Parameters

    /// <summary>
    /// The name of the SQL parameter for the DIS Code component "kind".
    /// The value must be of type <see cref="DbType.Int32"/>.
    /// </summary>
    protected abstract string DISKindParamName
    {
        get;
    }

    /// <summary>
    /// The name of the SQL parameter for the DIS Code component "domain".
    /// The value must be of type <see cref="DbType.Int32"/>.
    /// </summary>
    protected abstract string DISDomainParamName
    {
        get;
    }

    /// <summary>
    /// The name of the SQL parameter for the DIS Code component "country".
    /// The value must be of type <see cref="DbType.Int32"/>.
    /// </summary>
    protected abstract string DISCountryParamName
    {
        get;
    }

    /// <summary>
    /// The name of the SQL parameter for the DIS Code component "category".
    /// The value must be of type <see cref="DbType.Int32"/>.
    /// </summary>
    protected abstract string DISCategoryParamName
    {
        get;
    }

    /// <summary>
    /// The name of the SQL parameter for the DIS Code component "subcategory".
    /// The value must be of type <see cref="DbType.Int32"/>.
    /// </summary>
    protected abstract string DISSubcategoryParamName
    {
        get;
    }

    /// <summary>
    /// The name of the SQL parameter for the DIS Code component "specific".
    /// The value must be of type <see cref="DbType.Int32"/>.
    /// </summary>
    protected abstract string DISSpecificParamName
    {
        get;
    }

    /// <summary>
    /// The name of the SQL parameter for the DIS Code component "extra".
    /// The value must be of type <see cref="DbType.Int32"/>.
    /// </summary>
    protected abstract string DISExtraParamName
    {
        get;
    }

    #endregion

    /// <summary>
    /// The SQL DDL statement to create the Moving Model table.
    /// </summary>
    protected abstract string CreateTableMovingModelStatement
    {
        get;
    }

    /// <summary>
    /// The SQL statement to insert a row into the Moving Model table.
    /// This takes thirteen parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="DISKindParamName"/>,
    /// <see cref="DISDomainParamName"/>,
    /// <see cref="DISCountryParamName"/>,
    /// <see cref="DISCategoryParamName"/>,
    /// <see cref="DISSubcategoryParamName"/>,
    /// <see cref="DISSpecificParamName"/>,
    /// <see cref="DISExtraParamName"/>,
    /// <see cref="FileTypeParamName"/>,
    /// <see cref="ContentParamName"/>.
    /// </summary>
    protected abstract string InsertIntoMovingModelStatement
    {
        get;
    }

    private DbCommand CreateInsertIntoMovingModelCommand(DbConnection dbConnection)
    {
        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = InsertIntoMovingModelStatement;
        CreateAndAttachParameter(dbCommand, CdbParamName, DbType.String);
        CreateAndAttachMovingModelParameters(dbCommand);
        CreateAndAttachParameter(dbCommand, ContentParamName, DbType.String);
        dbCommand.Prepare();
        return dbCommand;
    }

    private void CreateAndAttachMovingModelParameters(DbCommand dbCommand)
    {
        CreateAndAttachParameter(dbCommand, DatasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, DISKindParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, DISDomainParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, DISCountryParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, DISCategoryParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, DISSubcategoryParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, DISSpecificParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, DISExtraParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, FileTypeParamName, DbType.String);
    }

    private void SetMovingModelParameters(DbCommand dbCommand, MovingModel movingModel)
    {
        dbCommand.Parameters[DatasetParamName].Value = movingModel.Dataset.Value;
        dbCommand.Parameters[ComponentSelector1ParamName].Value = movingModel.ComponentSelector1;
        dbCommand.Parameters[ComponentSelector2ParamName].Value = movingModel.ComponentSelector2;
        dbCommand.Parameters[DISKindParamName].Value = movingModel.MMDC.Kind;
        dbCommand.Parameters[DISDomainParamName].Value = movingModel.MMDC.Domain;
        dbCommand.Parameters[DISCountryParamName].Value = movingModel.MMDC.Country;
        dbCommand.Parameters[DISCategoryParamName].Value = movingModel.MMDC.Category;
        dbCommand.Parameters[DISSubcategoryParamName].Value = movingModel.MMDC.Subcategory;
        dbCommand.Parameters[DISSpecificParamName].Value = movingModel.MMDC.Specific;
        dbCommand.Parameters[DISExtraParamName].Value = movingModel.MMDC.Extra;
        dbCommand.Parameters[FileTypeParamName].Value = movingModel.FileType;
    }

    /// <summary>
    /// Inserts a moving model file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="movingModel">The moving model identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual int InsertIntoMovingModel(string cdbName, MovingModel movingModel, byte[] content)
    {
        insertIntoMovingModelCommand.Parameters[CdbParamName].Value = cdbName;
        SetMovingModelParameters(insertIntoMovingModelCommand, movingModel);
        insertIntoMovingModelCommand.Parameters[ContentParamName].Value = content;

        return insertIntoMovingModelCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// Inserts a moving model file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="movingModel">The moving model identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual int InsertIntoMovingModel(string cdbName, MovingModel movingModel, Stream content)
    {
        insertIntoMovingModelCommand.Parameters[CdbParamName].Value = cdbName;
        SetMovingModelParameters(insertIntoMovingModelCommand, movingModel);
        insertIntoMovingModelCommand.Parameters[ContentParamName].Value = content;

        return insertIntoMovingModelCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// Inserts a moving model file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="movingModel">The moving model identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual Task<int> InsertIntoMovingModelAsync(string cdbName, MovingModel movingModel, byte[] content, CancellationToken cancellationToken = default)
    {
        insertIntoMovingModelCommand.Parameters[CdbParamName].Value = cdbName;
        SetMovingModelParameters(insertIntoMovingModelCommand, movingModel);
        insertIntoMovingModelCommand.Parameters[ContentParamName].Value = content;

        return insertIntoMovingModelCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Inserts a moving model file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="movingModel">The moving model identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual Task<int> InsertIntoMovingModelAsync(string cdbName, MovingModel movingModel, Stream content, CancellationToken cancellationToken = default)
    {
        insertIntoMovingModelCommand.Parameters[CdbParamName].Value = cdbName;
        SetMovingModelParameters(insertIntoMovingModelCommand, movingModel);
        insertIntoMovingModelCommand.Parameters[ContentParamName].Value = content;

        return insertIntoMovingModelCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// The SQL statement to select a row from the Moving Model table.
    /// This takes twelve parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="DISKindParamName"/>,
    /// <see cref="DISDomainParamName"/>,
    /// <see cref="DISCountryParamName"/>,
    /// <see cref="DISCategoryParamName"/>,
    /// <see cref="DISSubcategoryParamName"/>,
    /// <see cref="DISSpecificParamName"/>,
    /// <see cref="DISExtraParamName"/>,
    /// <see cref="FileTypeParamName"/>.
    /// It returns one column,
    /// <see cref="ContentColumnName"/>.
    /// </summary>
    protected abstract string SelectFromMovingModelStatement
    {
        get;
    }

    private DbCommand CreateSelectFromMovingModelCommand(DbConnection dbConnection)
    {
        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = SelectFromMovingModelStatement;
        CreateAndAttachParameter(dbCommand, CdbParamName, DbType.String);
        CreateAndAttachMovingModelParameters(dbCommand);
        dbCommand.Prepare();
        return dbCommand;
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
    public virtual bool TrySelectFromMovingModel(string cdbName, MovingModel movingModel, [NotNullWhen(true)] out byte[] content)
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
    public virtual bool TrySelectFromMovingModel(string cdbName, MovingModel movingModel, Stream output)
    {
        selectFromMovingModelCommand.Parameters[CdbParamName].Value = cdbName;
        SetMovingModelParameters(selectFromMovingModelCommand, movingModel);

        using DbDataReader dbDataReader = selectFromMovingModelCommand.ExecuteReader(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
        do
        {
            while (dbDataReader.Read())
            {
                using Stream stream = dbDataReader.GetStream(ContentColumnName);
                stream.CopyTo(output);
                return true;
            }
        } while (dbDataReader.NextResult());
        return false;
    }

    /// <summary>
    /// Tries to find and return a moving model file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="output"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="movingModel">The moving model identifier.</param>
    /// <param name="output">A stream that will receive the file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public virtual async Task<bool> TrySelectFromMovingModelAsync(string cdbName, MovingModel movingModel, Stream output, CancellationToken cancellationToken = default)
    {
        selectFromMovingModelCommand.Parameters[CdbParamName].Value = cdbName;
        SetMovingModelParameters(selectFromMovingModelCommand, movingModel);

        await using DbDataReader dbDataReader = await selectFromMovingModelCommand.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow, cancellationToken);
        do
        {
            while (await dbDataReader.ReadAsync(cancellationToken))
            {
                await using Stream stream = dbDataReader.GetStream(ContentColumnName);
                await stream.CopyToAsync(output, cancellationToken);
                return true;
            }
        } while (await dbDataReader.NextResultAsync(cancellationToken));
        return false;
    }

    /// <summary>
    /// Returns a moving model file from a CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="movingModel">The moving model identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The file contents.</returns>
    /// <exception cref="FileNotFoundException">If the file was not found in the database.</exception>
    public virtual async Task<byte[]> SelectFromMovingModelAsync(string cdbName, MovingModel movingModel, CancellationToken cancellationToken = default)
    {
        await using MemoryStream memoryStream = new();
        if (await TrySelectFromMovingModelAsync(cdbName, movingModel, memoryStream, cancellationToken))
        {
            return memoryStream.ToArray();
        }
        else
        {
            throw new FileNotFoundException("Moving model file not found.", movingModel.Filename);
        }
    }

    #endregion

    #region Moving Model LOD

    /// <summary>
    /// The SQL DDL statement to create the Moving Model Level of Detail table.
    /// </summary>
    protected abstract string CreateTableMovingModelLodStatement
    {
        get;
    }

    /// <summary>
    /// The SQL statement to insert a row into the Moving Model Level of Detail table.
    /// This takes fourteen parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="LevelOfDetailParamName"/>,
    /// <see cref="DISKindParamName"/>,
    /// <see cref="DISDomainParamName"/>,
    /// <see cref="DISCountryParamName"/>,
    /// <see cref="DISCategoryParamName"/>,
    /// <see cref="DISSubcategoryParamName"/>,
    /// <see cref="DISSpecificParamName"/>,
    /// <see cref="DISExtraParamName"/>,
    /// <see cref="FileTypeParamName"/>,
    /// <see cref="ContentParamName"/>.
    /// </summary>
    protected abstract string InsertIntoMovingModelLodStatement
    {
        get;
    }

    private DbCommand CreateInsertIntoMovingModelLodCommand(DbConnection dbConnection)
    {
        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = InsertIntoMovingModelLodStatement;
        CreateAndAttachParameter(dbCommand, CdbParamName, DbType.String);
        CreateAndAttachMovingModelLodParameters(dbCommand);
        CreateAndAttachParameter(dbCommand, ContentParamName, DbType.String);
        dbCommand.Prepare();
        return dbCommand;
    }

    private void CreateAndAttachMovingModelLodParameters(DbCommand dbCommand)
    {
        CreateAndAttachParameter(dbCommand, DatasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, LevelOfDetailParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, DISKindParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, DISDomainParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, DISCountryParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, DISCategoryParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, DISSubcategoryParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, DISSpecificParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, DISExtraParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, FileTypeParamName, DbType.String);
    }

    private void SetMovingModelLodParameters(DbCommand dbCommand, MovingModelLod movingModelLod)
    {
        dbCommand.Parameters[DatasetParamName].Value = movingModelLod.Dataset.Value;
        dbCommand.Parameters[ComponentSelector1ParamName].Value = movingModelLod.ComponentSelector1;
        dbCommand.Parameters[ComponentSelector2ParamName].Value = movingModelLod.ComponentSelector2;
        dbCommand.Parameters[LevelOfDetailParamName].Value = movingModelLod.LevelOfDetail.Value;
        dbCommand.Parameters[DISKindParamName].Value = movingModelLod.MMDC.Kind;
        dbCommand.Parameters[DISDomainParamName].Value = movingModelLod.MMDC.Domain;
        dbCommand.Parameters[DISCountryParamName].Value = movingModelLod.MMDC.Country;
        dbCommand.Parameters[DISCategoryParamName].Value = movingModelLod.MMDC.Category;
        dbCommand.Parameters[DISSubcategoryParamName].Value = movingModelLod.MMDC.Subcategory;
        dbCommand.Parameters[DISSpecificParamName].Value = movingModelLod.MMDC.Specific;
        dbCommand.Parameters[DISExtraParamName].Value = movingModelLod.MMDC.Extra;
        dbCommand.Parameters[FileTypeParamName].Value = movingModelLod.FileType;
    }

    /// <summary>
    /// Inserts a moving model level of detail file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="movingModelLod">The moving model level of detail identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual int InsertIntoMovingModelLod(string cdbName, MovingModelLod movingModelLod, byte[] content)
    {
        insertIntoMovingModelLodCommand.Parameters[CdbParamName].Value = cdbName;
        SetMovingModelLodParameters(insertIntoMovingModelLodCommand, movingModelLod);
        insertIntoMovingModelLodCommand.Parameters[ContentParamName].Value = content;

        return insertIntoMovingModelLodCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// Inserts a moving model level of detail file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="movingModelLod">The moving model level of detail identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual int InsertIntoMovingModelLod(string cdbName, MovingModelLod movingModelLod, Stream content)
    {
        insertIntoMovingModelLodCommand.Parameters[CdbParamName].Value = cdbName;
        SetMovingModelLodParameters(insertIntoMovingModelLodCommand, movingModelLod);
        insertIntoMovingModelLodCommand.Parameters[ContentParamName].Value = content;

        return insertIntoMovingModelLodCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// Inserts a moving model level of detail file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="movingModelLod">The moving model level of detail identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual Task<int> InsertIntoMovingModelLodAsync(string cdbName, MovingModelLod movingModelLod, byte[] content, CancellationToken cancellationToken = default)
    {
        insertIntoMovingModelLodCommand.Parameters[CdbParamName].Value = cdbName;
        SetMovingModelLodParameters(insertIntoMovingModelLodCommand, movingModelLod);
        insertIntoMovingModelLodCommand.Parameters[ContentParamName].Value = content;

        return insertIntoMovingModelLodCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Inserts a moving model level of detail file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="movingModelLod">The moving model level of detail identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual Task<int> InsertIntoMovingModelLodAsync(string cdbName, MovingModelLod movingModelLod, Stream content, CancellationToken cancellationToken = default)
    {
        insertIntoMovingModelLodCommand.Parameters[CdbParamName].Value = cdbName;
        SetMovingModelLodParameters(insertIntoMovingModelLodCommand, movingModelLod);
        insertIntoMovingModelLodCommand.Parameters[ContentParamName].Value = content;

        return insertIntoMovingModelLodCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// The SQL statement to select a row from the Moving Model Level of Detail table.
    /// This takes thirteen parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="LevelOfDetailParamName"/>,
    /// <see cref="DISKindParamName"/>,
    /// <see cref="DISDomainParamName"/>,
    /// <see cref="DISCountryParamName"/>,
    /// <see cref="DISCategoryParamName"/>,
    /// <see cref="DISSubcategoryParamName"/>,
    /// <see cref="DISSpecificParamName"/>,
    /// <see cref="DISExtraParamName"/>,
    /// <see cref="FileTypeParamName"/>.
    /// It returns one column,
    /// <see cref="ContentColumnName"/>.
    /// </summary>
    protected abstract string SelectFromMovingModelLodStatement
    {
        get;
    }

    private DbCommand CreateSelectFromMovingModelLodCommand(DbConnection dbConnection)
    {
        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = SelectFromMovingModelLodStatement;
        CreateAndAttachParameter(dbCommand, CdbParamName, DbType.String);
        CreateAndAttachMovingModelLodParameters(dbCommand);
        dbCommand.Prepare();
        return dbCommand;
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
    public virtual bool TrySelectFromMovingModelLod(string cdbName, MovingModelLod movingModelLod, [NotNullWhen(true)] out byte[] content)
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
    public virtual bool TrySelectFromMovingModelLod(string cdbName, MovingModelLod movingModelLod, Stream output)
    {
        selectFromMovingModelLodCommand.Parameters[CdbParamName].Value = cdbName;
        SetMovingModelLodParameters(selectFromMovingModelLodCommand, movingModelLod);

        using DbDataReader dbDataReader = selectFromMovingModelLodCommand.ExecuteReader(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
        do
        {
            while (dbDataReader.Read())
            {
                using Stream stream = dbDataReader.GetStream(ContentColumnName);
                stream.CopyTo(output);
                return true;
            }
        } while (dbDataReader.NextResult());
        return false;
    }

    /// <summary>
    /// Tries to find and return a moving model level of detail file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="output"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="movingModelLod">The moving model level of detail identifier.</param>
    /// <param name="output">A stream that will receive the file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public virtual async Task<bool> TrySelectFromMovingModelLodAsync(string cdbName, MovingModelLod movingModelLod, Stream output, CancellationToken cancellationToken = default)
    {
        selectFromMovingModelLodCommand.Parameters[CdbParamName].Value = cdbName;
        SetMovingModelLodParameters(selectFromMovingModelLodCommand, movingModelLod);

        await using DbDataReader dbDataReader = await selectFromMovingModelLodCommand.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow, cancellationToken);
        do
        {
            while (await dbDataReader.ReadAsync(cancellationToken))
            {
                await using Stream stream = dbDataReader.GetStream(ContentColumnName);
                await stream.CopyToAsync(output, cancellationToken);
                return true;
            }
        } while (await dbDataReader.NextResultAsync(cancellationToken));
        return false;
    }

    /// <summary>
    /// Returns a moving model level of detail file from a CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="movingModelLod">The moving model level of detail identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The file contents.</returns>
    /// <exception cref="FileNotFoundException">If the file was not found in the database.</exception>
    public virtual async Task<byte[]> SelectFromMovingModelLodAsync(string cdbName, MovingModelLod movingModelLod, CancellationToken cancellationToken = default)
    {
        await using MemoryStream memoryStream = new();
        if (await TrySelectFromMovingModelLodAsync(cdbName, movingModelLod, memoryStream, cancellationToken))
        {
            return memoryStream.ToArray();
        }
        else
        {
            throw new FileNotFoundException("Moving model level of detail file not found.", movingModelLod.Filename);
        }
    }

    #endregion

    #region Tile

    /// <summary>
    /// The SQL DDL statement to create the Tile table.
    /// </summary>
    protected abstract string CreateTableTileStatement
    {
        get;
    }

    /// <summary>
    /// The name of the SQL parameter for a Tile latitude.
    /// The value must be of type <see cref="DbType.Int32"/>.
    /// </summary>
    protected abstract string LatitudeParamName
    {
        get;
    }

    /// <summary>
    /// The name of the SQL parameter for a Tile longitude.
    /// The value must be of type <see cref="DbType.Int32"/>.
    /// </summary>
    protected abstract string LongitudeParamName
    {
        get;
    }

    /// <summary>
    /// The name of the SQL parameter for a Tile UREF.
    /// The value must be of type <see cref="DbType.Int32"/>.
    /// </summary>
    protected abstract string UpParamName
    {
        get;
    }

    /// <summary>
    /// The name of the SQL parameter for a Tile RREF.
    /// The value must be of type <see cref="DbType.Int32"/>.
    /// </summary>
    protected abstract string RightParamName
    {
        get;
    }

    /// <summary>
    /// The SQL statement to insert a row into the Tile table.
    /// This takes eleven parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="LatitudeParamName"/>,
    /// <see cref="LongitudeParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="LevelOfDetailParamName"/>,
    /// <see cref="UpParamName"/>,
    /// <see cref="RightParamName"/>,
    /// <see cref="FileTypeParamName"/>,
    /// <see cref="ContentParamName"/>.
    /// </summary>
    protected abstract string InsertIntoTileStatement
    {
        get;
    }

    private DbCommand CreateInsertIntoTileCommand(DbConnection dbConnection)
    {
        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = InsertIntoTileStatement;
        CreateAndAttachParameter(dbCommand, CdbParamName, DbType.String);
        CreateAndAttachTileParameters(dbCommand);
        CreateAndAttachParameter(dbCommand, ContentParamName, DbType.Binary);
        dbCommand.Prepare();
        return dbCommand;
    }

    private void CreateAndAttachTileParameters(DbCommand dbCommand)
    {
        CreateAndAttachParameter(dbCommand, LatitudeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, LongitudeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, DatasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, LevelOfDetailParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, UpParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, RightParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, FileTypeParamName, DbType.String);
    }

    private void SetTileParameters(DbCommand dbCommand, Tile tile)
    {
        dbCommand.Parameters[LatitudeParamName].Value = tile.LatitudeValue.Value;
        dbCommand.Parameters[LongitudeParamName].Value = tile.LongitudeValue.Value;
        dbCommand.Parameters[DatasetParamName].Value = tile.DatasetValue.Value;
        dbCommand.Parameters[ComponentSelector1ParamName].Value = tile.ComponentSelector1;
        dbCommand.Parameters[ComponentSelector2ParamName].Value = tile.ComponentSelector2;
        dbCommand.Parameters[LevelOfDetailParamName].Value = tile.Level.Value;
        dbCommand.Parameters[UpParamName].Value = tile.Up;
        dbCommand.Parameters[RightParamName].Value = tile.Right;
        dbCommand.Parameters[FileTypeParamName].Value = tile.FileType;
    }

    /// <summary>
    /// Inserts a tiled dataset file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="tile">The tile identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual int InsertIntoTile(string cdbName, Tile tile, byte[] content)
    {
        insertIntoTileCommand.Parameters[CdbParamName].Value = cdbName;
        SetTileParameters(insertIntoTileCommand, tile);
        insertIntoTileCommand.Parameters[ContentParamName].Value = content;

        return insertIntoTileCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// Inserts a tiled dataset file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="tile">The tile identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual int InsertIntoTile(string cdbName, Tile tile, Stream content)
    {
        insertIntoTileCommand.Parameters[CdbParamName].Value = cdbName;
        SetTileParameters(insertIntoTileCommand, tile);
        insertIntoTileCommand.Parameters[ContentParamName].Value = content;

        return insertIntoTileCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// Inserts a tiled dataset file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="tile">The tile identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual Task<int> InsertIntoTileAsync(string cdbName, Tile tile, byte[] content, CancellationToken cancellationToken = default)
    {
        insertIntoTileCommand.Parameters[CdbParamName].Value = cdbName;
        SetTileParameters(insertIntoTileCommand, tile);
        insertIntoTileCommand.Parameters[ContentParamName].Value = content;

        return insertIntoTileCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Inserts a tiled dataset file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="tile">The tile identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual Task<int> InsertIntoTileAsync(string cdbName, Tile tile, Stream content, CancellationToken cancellationToken = default)
    {
        insertIntoTileCommand.Parameters[CdbParamName].Value = cdbName;
        SetTileParameters(insertIntoTileCommand, tile);
        insertIntoTileCommand.Parameters[ContentParamName].Value = content;

        return insertIntoTileCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// The SQL statement to select a row from the Tile table.
    /// This takes ten parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="LatitudeParamName"/>,
    /// <see cref="LongitudeParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="LevelOfDetailParamName"/>,
    /// <see cref="UpParamName"/>,
    /// <see cref="RightParamName"/>,
    /// <see cref="FileTypeParamName"/>.
    /// It returns one column,
    /// <see cref="ContentColumnName"/>.
    /// </summary>
    protected abstract string SelectFromTileStatement
    {
        get;
    }

    private DbCommand CreateSelectFromTileCommand(DbConnection dbConnection)
    {
        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = SelectFromTileStatement;
        CreateAndAttachParameter(dbCommand, CdbParamName, DbType.String);
        CreateAndAttachTileParameters(dbCommand);
        dbCommand.Prepare();
        return dbCommand;
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
    public virtual bool TrySelectFromTile(string cdbName, Tile tile, [NotNullWhen(true)] out byte[] content)
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
    public virtual bool TrySelectFromTile(string cdbName, Tile tile, Stream output)
    {
        selectFromTileCommand.Parameters[CdbParamName].Value = cdbName;
        SetTileParameters(selectFromTileCommand, tile);

        using DbDataReader dbDataReader = selectFromTileCommand.ExecuteReader(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
        do
        {
            while (dbDataReader.Read())
            {
                using Stream stream = dbDataReader.GetStream(ContentColumnName);
                stream.CopyTo(output);
                return true;
            }
        } while (dbDataReader.NextResult());
        return false;
    }

    /// <summary>
    /// Tries to find and return a tiled dataset file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="output"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="tile">The tile identifier.</param>
    /// <param name="output">A stream that will receive the file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public virtual async Task<bool> TrySelectFromTileAsync(string cdbName, Tile tile, Stream output, CancellationToken cancellationToken = default)
    {
        selectFromTileCommand.Parameters[CdbParamName].Value = cdbName;
        SetTileParameters(selectFromTileCommand, tile);

        await using DbDataReader dbDataReader = await selectFromTileCommand.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow, cancellationToken);
        do
        {
            while (await dbDataReader.ReadAsync(cancellationToken))
            {
                await using Stream stream = dbDataReader.GetStream(ContentColumnName);
                await stream.CopyToAsync(output, cancellationToken);
                return true;
            }
        } while (await dbDataReader.NextResultAsync(cancellationToken));
        return false;
    }

    /// <summary>
    /// Returns a tiled dataset file from a CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="tile">The tile identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The file contents.</returns>
    /// <exception cref="FileNotFoundException">If the file was not found in the database.</exception>
    public virtual async Task<byte[]> SelectFromTileAsync(string cdbName, Tile tile, CancellationToken cancellationToken = default)
    {
        await using MemoryStream memoryStream = new();
        if (await TrySelectFromTileAsync(cdbName, tile, memoryStream, cancellationToken))
        {
            return memoryStream.ToArray();
        }
        else
        {
            throw new FileNotFoundException("Tiled dataset file not found.", tile.Filename);
        }
    }

    #endregion

    #region Tile Archived Feature

    /// <summary>
    /// The SQL DDL statement to create the TileArchivedFeature table.
    /// </summary>
    protected abstract string CreateTableTileArchivedFeatureStatement
    {
        get;
    }

    /// <summary>
    /// The SQL statement to insert a row into the TileArchivedFeature table.
    /// This takes sixteen parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="LatitudeParamName"/>,
    /// <see cref="LongitudeParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="LevelOfDetailParamName"/>,
    /// <see cref="UpParamName"/>,
    /// <see cref="RightParamName"/>,
    /// <see cref="FeatureCategoryParamName"/>,
    /// <see cref="FeatureSubcategoryParamName"/>,
    /// <see cref="FeatureTypeParamName"/>,
    /// <see cref="FeatureSubcodeParamName"/>,
    /// <see cref="ModelNameParamName"/>,
    /// <see cref="FileTypeParamName"/>,
    /// <see cref="ContentParamName"/>.
    /// </summary>
    protected abstract string InsertIntoTileArchivedFeatureStatement
    {
        get;
    }

    private DbCommand CreateInsertIntoTileArchivedFeatureCommand(DbConnection dbConnection)
    {
        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = InsertIntoTileArchivedFeatureStatement;
        CreateAndAttachParameter(dbCommand, CdbParamName, DbType.String);
        CreateAndAttachTileArchivedFeatureParameters(dbCommand);
        CreateAndAttachParameter(dbCommand, ContentParamName, DbType.Binary);
        dbCommand.Prepare();
        return dbCommand;
    }

    private void CreateAndAttachTileArchivedFeatureParameters(DbCommand dbCommand)
    {
        CreateAndAttachParameter(dbCommand, LatitudeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, LongitudeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, DatasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, LevelOfDetailParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, UpParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, RightParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, FeatureCategoryParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, FeatureSubcategoryParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, FeatureTypeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, FeatureSubcodeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ModelNameParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, FileTypeParamName, DbType.String);
    }

    private void SetTileArchivedFeatureParameters(DbCommand dbCommand, TileArchivedFeature tileArchivedFeature)
    {
        dbCommand.Parameters[LatitudeParamName].Value = tileArchivedFeature.LatitudeValue.Value;
        dbCommand.Parameters[LongitudeParamName].Value = tileArchivedFeature.LongitudeValue.Value;
        dbCommand.Parameters[DatasetParamName].Value = tileArchivedFeature.DatasetValue.Value;
        dbCommand.Parameters[ComponentSelector1ParamName].Value = tileArchivedFeature.ComponentSelector1;
        dbCommand.Parameters[ComponentSelector2ParamName].Value = tileArchivedFeature.ComponentSelector2;
        dbCommand.Parameters[LevelOfDetailParamName].Value = tileArchivedFeature.Level.Value;
        dbCommand.Parameters[UpParamName].Value = tileArchivedFeature.Up;
        dbCommand.Parameters[RightParamName].Value = tileArchivedFeature.Right;
        dbCommand.Parameters[FeatureCategoryParamName].Value = tileArchivedFeature.FeatureCode.Category;
        dbCommand.Parameters[FeatureSubcategoryParamName].Value = tileArchivedFeature.FeatureCode.Subcategory;
        dbCommand.Parameters[FeatureTypeParamName].Value = tileArchivedFeature.FeatureCode.Type;
        dbCommand.Parameters[FeatureSubcodeParamName].Value = tileArchivedFeature.FeatureSubcode;
        dbCommand.Parameters[ModelNameParamName].Value = tileArchivedFeature.Name;
        dbCommand.Parameters[FileTypeParamName].Value = tileArchivedFeature.FileType;
    }

    /// <summary>
    /// Inserts an un-archived tiled dataset feature file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="tileArchivedFeature">The un-archived tiled dataset feature identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual int InsertIntoTileArchivedFeature(string cdbName, TileArchivedFeature tileArchivedFeature, byte[] content)
    {
        insertIntoTileArchivedFeatureCommand.Parameters[CdbParamName].Value = cdbName;
        SetTileArchivedFeatureParameters(insertIntoTileArchivedFeatureCommand, tileArchivedFeature);
        insertIntoTileArchivedFeatureCommand.Parameters[ContentParamName].Value = content;

        return insertIntoTileArchivedFeatureCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// Inserts an un-archived tiled dataset feature file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="tileArchivedFeature">The un-archived tiled dataset feature identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual int InsertIntoTileArchivedFeature(string cdbName, TileArchivedFeature tileArchivedFeature, Stream content)
    {
        insertIntoTileArchivedFeatureCommand.Parameters[CdbParamName].Value = cdbName;
        SetTileArchivedFeatureParameters(insertIntoTileArchivedFeatureCommand, tileArchivedFeature);
        insertIntoTileArchivedFeatureCommand.Parameters[ContentParamName].Value = content;

        return insertIntoTileArchivedFeatureCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// Inserts an un-archived tiled dataset feature file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="tileArchivedFeature">The un-archived tiled dataset feature identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual Task<int> InsertIntoTileArchivedFeatureAsync(string cdbName, TileArchivedFeature tileArchivedFeature, byte[] content, CancellationToken cancellationToken = default)
    {
        insertIntoTileArchivedFeatureCommand.Parameters[CdbParamName].Value = cdbName;
        SetTileArchivedFeatureParameters(insertIntoTileArchivedFeatureCommand, tileArchivedFeature);
        insertIntoTileArchivedFeatureCommand.Parameters[ContentParamName].Value = content;

        return insertIntoTileArchivedFeatureCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Inserts an un-archived tiled dataset feature file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="tileArchivedFeature">The un-archived tiled dataset feature identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual Task<int> InsertIntoTileArchivedFeatureAsync(string cdbName, TileArchivedFeature tileArchivedFeature, Stream content, CancellationToken cancellationToken = default)
    {
        insertIntoTileArchivedFeatureCommand.Parameters[CdbParamName].Value = cdbName;
        SetTileArchivedFeatureParameters(insertIntoTileArchivedFeatureCommand, tileArchivedFeature);
        insertIntoTileArchivedFeatureCommand.Parameters[ContentParamName].Value = content;

        return insertIntoTileArchivedFeatureCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// The SQL statement to select a row from the TileArchivedFeature table.
    /// This takes fifteen parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="LatitudeParamName"/>,
    /// <see cref="LongitudeParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="LevelOfDetailParamName"/>,
    /// <see cref="UpParamName"/>,
    /// <see cref="RightParamName"/>,
    /// <see cref="FeatureCategoryParamName"/>,
    /// <see cref="FeatureSubcategoryParamName"/>,
    /// <see cref="FeatureTypeParamName"/>,
    /// <see cref="FeatureSubcodeParamName"/>,
    /// <see cref="ModelNameParamName"/>,
    /// <see cref="FileTypeParamName"/>.
    /// It returns one column,
    /// <see cref="ContentColumnName"/>.
    /// </summary>
    protected abstract string SelectFromTileArchivedFeatureStatement
    {
        get;
    }

    private DbCommand CreateSelectFromTileArchivedFeatureCommand(DbConnection dbConnection)
    {
        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = SelectFromTileArchivedFeatureStatement;
        CreateAndAttachParameter(dbCommand, CdbParamName, DbType.String);
        CreateAndAttachTileArchivedFeatureParameters(dbCommand);
        dbCommand.Prepare();
        return dbCommand;
    }

    /// <summary>
    /// Tries to find and return an un-archived tiled dataset feature file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="content"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="tileArchivedFeature">The tiled dataset feature identifier.</param>
    /// <param name="content">An output variable that will receive the file contents.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public virtual bool TrySelectFromTileArchivedFeature(string cdbName, TileArchivedFeature tileArchivedFeature, [NotNullWhen(true)] out byte[] content)
    {
        using MemoryStream memoryStream = new();
        bool succeeded = TrySelectFromTileArchivedFeature(cdbName, tileArchivedFeature, memoryStream);
        content = memoryStream.ToArray();
        return succeeded;
    }

    /// <summary>
    /// Tries to find and return an un-archived tiled dataset feature file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="output"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="tileArchivedFeature">The tiled dataset feature identifier.</param>
    /// <param name="output">A stream that will receive the file contents.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public virtual bool TrySelectFromTileArchivedFeature(string cdbName, TileArchivedFeature tileArchivedFeature, Stream output)
    {
        selectFromTileArchivedFeatureCommand.Parameters[CdbParamName].Value = cdbName;
        SetTileArchivedFeatureParameters(selectFromTileArchivedFeatureCommand, tileArchivedFeature);

        using DbDataReader dbDataReader = selectFromTileArchivedFeatureCommand.ExecuteReader(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
        do
        {
            while (dbDataReader.Read())
            {
                using Stream stream = dbDataReader.GetStream(ContentColumnName);
                stream.CopyTo(output);
                return true;
            }
        } while (dbDataReader.NextResult());
        return false;
    }

    /// <summary>
    /// Tries to find and return an un-archived tiled dataset feature file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="output"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="tileArchivedFeature">The tiled dataset feature identifier.</param>
    /// <param name="output">A stream that will receive the file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public virtual async Task<bool> TrySelectFromTileArchivedFeatureAsync(string cdbName, TileArchivedFeature tileArchivedFeature, Stream output, CancellationToken cancellationToken = default)
    {
        selectFromTileArchivedFeatureCommand.Parameters[CdbParamName].Value = cdbName;
        SetTileArchivedFeatureParameters(selectFromTileArchivedFeatureCommand, tileArchivedFeature);

        await using DbDataReader dbDataReader = await selectFromTileArchivedFeatureCommand.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow, cancellationToken);
        do
        {
            while (await dbDataReader.ReadAsync(cancellationToken))
            {
                await using Stream stream = dbDataReader.GetStream(ContentColumnName);
                await stream.CopyToAsync(output, cancellationToken);
                return true;
            }
        } while (await dbDataReader.NextResultAsync(cancellationToken));
        return false;
    }

    /// <summary>
    /// Returns an un-archived tiled dataset feature file from a CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="tileArchivedFeature">The tiled dataset feature identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The file contents.</returns>
    /// <exception cref="FileNotFoundException">If the file was not found in the database.</exception>
    public virtual async Task<byte[]> SelectFromTileArchivedFeatureAsync(string cdbName, TileArchivedFeature tileArchivedFeature, CancellationToken cancellationToken = default)
    {
        await using MemoryStream memoryStream = new();
        if (await TrySelectFromTileArchivedFeatureAsync(cdbName, tileArchivedFeature, memoryStream, cancellationToken))
        {
            return memoryStream.ToArray();
        }
        else
        {
            throw new FileNotFoundException("Tile unarchived feature file not found.", tileArchivedFeature.Filename);
        }
    }

    #endregion

    #region Tile Archived Texture

    /// <summary>
    /// The SQL DDL statement to create the TileArchivedTexture table.
    /// </summary>
    protected abstract string CreateTableTileArchivedTextureStatement
    {
        get;
    }

    /// <summary>
    /// The SQL statement to insert a row into the TileArchivedTexture table.
    /// This takes twelve parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="LatitudeParamName"/>,
    /// <see cref="LongitudeParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="LevelOfDetailParamName"/>,
    /// <see cref="UpParamName"/>,
    /// <see cref="RightParamName"/>,
    /// <see cref="TextureNameParamName"/>,
    /// <see cref="FileTypeParamName"/>,
    /// <see cref="ContentParamName"/>.
    /// </summary>
    protected abstract string InsertIntoTileArchivedTextureStatement
    {
        get;
    }

    private DbCommand CreateInsertIntoTileArchivedTextureCommand(DbConnection dbConnection)
    {
        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = InsertIntoTileArchivedTextureStatement;
        CreateAndAttachParameter(dbCommand, CdbParamName, DbType.String);
        CreateAndAttachTileArchivedTextureParameters(dbCommand);
        CreateAndAttachParameter(dbCommand, ContentParamName, DbType.Binary);
        dbCommand.Prepare();
        return dbCommand;
    }

    private void CreateAndAttachTileArchivedTextureParameters(DbCommand dbCommand)
    {
        CreateAndAttachParameter(dbCommand, LatitudeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, LongitudeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, DatasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, LevelOfDetailParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, UpParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, RightParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, TextureNameParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, FileTypeParamName, DbType.String);
    }

    private void SetTileArchivedTextureParameters(DbCommand dbCommand, TileArchivedTexture tileArchivedTexture)
    {
        dbCommand.Parameters[LatitudeParamName].Value = tileArchivedTexture.LatitudeValue.Value;
        dbCommand.Parameters[LongitudeParamName].Value = tileArchivedTexture.LongitudeValue.Value;
        dbCommand.Parameters[DatasetParamName].Value = tileArchivedTexture.DatasetValue.Value;
        dbCommand.Parameters[ComponentSelector1ParamName].Value = tileArchivedTexture.ComponentSelector1;
        dbCommand.Parameters[ComponentSelector2ParamName].Value = tileArchivedTexture.ComponentSelector2;
        dbCommand.Parameters[LevelOfDetailParamName].Value = tileArchivedTexture.Level.Value;
        dbCommand.Parameters[UpParamName].Value = tileArchivedTexture.Up;
        dbCommand.Parameters[RightParamName].Value = tileArchivedTexture.Right;
        dbCommand.Parameters[TextureNameParamName].Value = tileArchivedTexture.Name;
        dbCommand.Parameters[FileTypeParamName].Value = tileArchivedTexture.FileType;
    }

    /// <summary>
    /// Inserts an un-archived tiled dataset texture file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="tileArchivedTexture">The un-archived tiled dataset texture identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual int InsertIntoTileArchivedTexture(string cdbName, TileArchivedTexture tileArchivedTexture, byte[] content)
    {
        insertIntoTileArchivedTextureCommand.Parameters[CdbParamName].Value = cdbName;
        SetTileArchivedTextureParameters(insertIntoTileArchivedTextureCommand, tileArchivedTexture);
        insertIntoTileArchivedTextureCommand.Parameters[ContentParamName].Value = content;

        return insertIntoTileArchivedTextureCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// Inserts an un-archived tiled dataset texture file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="tileArchivedTexture">The un-archived tiled dataset texture identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual int InsertIntoTileArchivedTexture(string cdbName, TileArchivedTexture tileArchivedTexture, Stream content)
    {
        insertIntoTileArchivedTextureCommand.Parameters[CdbParamName].Value = cdbName;
        SetTileArchivedTextureParameters(insertIntoTileArchivedTextureCommand, tileArchivedTexture);
        insertIntoTileArchivedTextureCommand.Parameters[ContentParamName].Value = content;

        return insertIntoTileArchivedTextureCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// Inserts an un-archived tiled dataset texture file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="tileArchivedTexture">The un-archived tiled dataset texture identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual Task<int> InsertIntoTileArchivedTextureAsync(string cdbName, TileArchivedTexture tileArchivedTexture, byte[] content, CancellationToken cancellationToken = default)
    {
        insertIntoTileArchivedTextureCommand.Parameters[CdbParamName].Value = cdbName;
        SetTileArchivedTextureParameters(insertIntoTileArchivedTextureCommand, tileArchivedTexture);
        insertIntoTileArchivedTextureCommand.Parameters[ContentParamName].Value = content;

        return insertIntoTileArchivedTextureCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Inserts an un-archived tiled dataset texture file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="tileArchivedTexture">The un-archived tiled dataset texture identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual Task<int> InsertIntoTileArchivedTextureAsync(string cdbName, TileArchivedTexture tileArchivedTexture, Stream content, CancellationToken cancellationToken = default)
    {
        insertIntoTileArchivedTextureCommand.Parameters[CdbParamName].Value = cdbName;
        SetTileArchivedTextureParameters(insertIntoTileArchivedTextureCommand, tileArchivedTexture);
        insertIntoTileArchivedTextureCommand.Parameters[ContentParamName].Value = content;

        return insertIntoTileArchivedTextureCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// The SQL statement to select a row from the TileArchivedTexture table.
    /// This takes eleven parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="LatitudeParamName"/>,
    /// <see cref="LongitudeParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="LevelOfDetailParamName"/>,
    /// <see cref="UpParamName"/>,
    /// <see cref="RightParamName"/>,
    /// <see cref="TextureNameParamName"/>,
    /// <see cref="FileTypeParamName"/>.
    /// It returns one column,
    /// <see cref="ContentColumnName"/>.
    /// </summary>
    protected abstract string SelectFromTileArchivedTextureStatement
    {
        get;
    }

    private DbCommand CreateSelectFromTileArchivedTextureCommand(DbConnection dbConnection)
    {
        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = SelectFromTileArchivedTextureStatement;
        CreateAndAttachParameter(dbCommand, CdbParamName, DbType.String);
        CreateAndAttachTileArchivedTextureParameters(dbCommand);
        dbCommand.Prepare();
        return dbCommand;
    }

    /// <summary>
    /// Tries to find and return an un-archived tiled dataset texture file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="content"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="tileArchivedTexture">The tiled dataset texture identifier.</param>
    /// <param name="content">An output variable that will receive the file contents.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public virtual bool TrySelectFromTileArchivedTexture(string cdbName, TileArchivedTexture tileArchivedTexture, [NotNullWhen(true)] out byte[] content)
    {
        using MemoryStream memoryStream = new();
        bool succeeded = TrySelectFromTileArchivedTexture(cdbName, tileArchivedTexture, memoryStream);
        content = memoryStream.ToArray();
        return succeeded;
    }

    /// <summary>
    /// Tries to find and return an un-archived tiled dataset texture file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="output"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="tileArchivedTexture">The tiled dataset texture identifier.</param>
    /// <param name="output">A stream that will receive the file contents.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public virtual bool TrySelectFromTileArchivedTexture(string cdbName, TileArchivedTexture tileArchivedTexture, Stream output)
    {
        selectFromTileArchivedTextureCommand.Parameters[CdbParamName].Value = cdbName;
        SetTileArchivedTextureParameters(selectFromTileArchivedTextureCommand, tileArchivedTexture);

        using DbDataReader dbDataReader = selectFromTileArchivedTextureCommand.ExecuteReader(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
        do
        {
            while (dbDataReader.Read())
            {
                using Stream stream = dbDataReader.GetStream(ContentColumnName);
                stream.CopyTo(output);
                return true;
            }
        } while (dbDataReader.NextResult());
        return false;
    }

    /// <summary>
    /// Tries to find and return an un-archived tiled dataset texture file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="output"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="tileArchivedTexture">The tiled dataset texture identifier.</param>
    /// <param name="output">A stream that will receive the file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public virtual async Task<bool> TrySelectFromTileArchivedTextureAsync(string cdbName, TileArchivedTexture tileArchivedTexture, Stream output, CancellationToken cancellationToken = default)
    {
        selectFromTileArchivedTextureCommand.Parameters[CdbParamName].Value = cdbName;
        SetTileArchivedTextureParameters(selectFromTileArchivedTextureCommand, tileArchivedTexture);

        await using DbDataReader dbDataReader = await selectFromTileArchivedTextureCommand.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow, cancellationToken);
        do
        {
            while (await dbDataReader.ReadAsync(cancellationToken))
            {
                await using Stream stream = dbDataReader.GetStream(ContentColumnName);
                await stream.CopyToAsync(output, cancellationToken);
                return true;
            }
        } while (await dbDataReader.NextResultAsync(cancellationToken));
        return false;
    }

    /// <summary>
    /// Returns an un-archived tiled dataset texture file from a CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="tileArchivedTexture">The tiled dataset texture identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The file contents.</returns>
    /// <exception cref="FileNotFoundException">If the file was not found in the database.</exception>
    public virtual async Task<byte[]> SelectFromTileArchivedTextureAsync(string cdbName, TileArchivedTexture tileArchivedTexture, CancellationToken cancellationToken = default)
    {
        await using MemoryStream memoryStream = new();
        if (await TrySelectFromTileArchivedTextureAsync(cdbName, tileArchivedTexture, memoryStream, cancellationToken))
        {
            return memoryStream.ToArray();
        }
        else
        {
            throw new FileNotFoundException("Tile unarchived texture file not found.", tileArchivedTexture.Filename);
        }
    }

    #endregion

    #region Navigation

    /// <summary>
    /// The SQL DDL statement to create the Navigation table.
    /// </summary>
    protected abstract string CreateTableNavigationStatement
    {
        get;
    }

    /// <summary>
    /// The SQL statement to insert a row into the Navigation table.
    /// This takes six parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="FileTypeParamName"/>,
    /// <see cref="ContentParamName"/>.
    /// </summary>
    protected abstract string InsertIntoNavigationStatement
    {
        get;
    }

    private DbCommand CreateInsertIntoNavigationCommand(DbConnection dbConnection)
    {
        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = InsertIntoNavigationStatement;
        CreateAndAttachParameter(dbCommand, CdbParamName, DbType.String);
        CreateAndAttachNavigationParameters(dbCommand);
        CreateAndAttachParameter(dbCommand, ContentParamName, DbType.Binary);
        dbCommand.Prepare();
        return dbCommand;
    }

    private void CreateAndAttachNavigationParameters(DbCommand dbCommand)
    {
        CreateAndAttachParameter(dbCommand, DatasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, FileTypeParamName, DbType.String);
    }

    private void SetNavigationParameters(DbCommand dbCommand, Navigation navigation)
    {
        dbCommand.Parameters[DatasetParamName].Value = navigation.Dataset.Value;
        dbCommand.Parameters[ComponentSelector1ParamName].Value = navigation.ComponentSelector1;
        dbCommand.Parameters[ComponentSelector2ParamName].Value = navigation.ComponentSelector2;
        dbCommand.Parameters[FileTypeParamName].Value = navigation.FileType;
    }

    /// <summary>
    /// Inserts a navigation file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="navigation">The navigation identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual int InsertIntoNavigation(string cdbName, Navigation navigation, byte[] content)
    {
        insertIntoNavigationCommand.Parameters[CdbParamName].Value = cdbName;
        SetNavigationParameters(insertIntoNavigationCommand, navigation);
        insertIntoNavigationCommand.Parameters[ContentParamName].Value = content;

        return insertIntoNavigationCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// Inserts a navigation file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="navigation">The navigation identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual int InsertIntoNavigation(string cdbName, Navigation navigation, Stream content)
    {
        insertIntoNavigationCommand.Parameters[CdbParamName].Value = cdbName;
        SetNavigationParameters(insertIntoNavigationCommand, navigation);
        insertIntoNavigationCommand.Parameters[ContentParamName].Value = content;

        return insertIntoNavigationCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// Inserts a navigation file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="navigation">The navigation identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual Task<int> InsertIntoNavigationAsync(string cdbName, Navigation navigation, byte[] content, CancellationToken cancellationToken = default)
    {
        insertIntoNavigationCommand.Parameters[CdbParamName].Value = cdbName;
        SetNavigationParameters(insertIntoNavigationCommand, navigation);
        insertIntoNavigationCommand.Parameters[ContentParamName].Value = content;

        return insertIntoNavigationCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Inserts a navigation file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="navigation">The navigation identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    public virtual Task<int> InsertIntoNavigationAsync(string cdbName, Navigation navigation, Stream content, CancellationToken cancellationToken = default)
    {
        insertIntoNavigationCommand.Parameters[CdbParamName].Value = cdbName;
        SetNavigationParameters(insertIntoNavigationCommand, navigation);
        insertIntoNavigationCommand.Parameters[ContentParamName].Value = content;

        return insertIntoNavigationCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// The SQL statement to select a row from the Navigation table.
    /// This takes five parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="FileTypeParamName"/>.
    /// It returns one column,
    /// <see cref="ContentColumnName"/>.
    /// </summary>
    protected abstract string SelectFromNavigationStatement
    {
        get;
    }

    private DbCommand CreateSelectFromNavigationCommand(DbConnection dbConnection)
    {
        DbCommand dbCommand = dbConnection.CreateCommand();
        dbCommand.CommandText = SelectFromNavigationStatement;
        CreateAndAttachParameter(dbCommand, CdbParamName, DbType.String);
        CreateAndAttachNavigationParameters(dbCommand);
        dbCommand.Prepare();
        return dbCommand;
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
    public virtual bool TrySelectFromNavigation(string cdbName, Navigation navigation, [NotNullWhen(true)] out byte[] content)
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
    public virtual bool TrySelectFromNavigation(string cdbName, Navigation navigation, Stream output)
    {
        selectFromNavigationCommand.Parameters[CdbParamName].Value = cdbName;
        SetNavigationParameters(selectFromNavigationCommand, navigation);

        using DbDataReader dbDataReader = selectFromNavigationCommand.ExecuteReader(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow);
        do
        {
            while (dbDataReader.Read())
            {
                using Stream stream = dbDataReader.GetStream(ContentColumnName);
                stream.CopyTo(output);
                return true;
            }
        } while (dbDataReader.NextResult());
        return false;
    }

    /// <summary>
    /// Tries to find and return a navigation file from a CDB data store.
    /// Returns <see langword="true"/> if the file was found, and writes the
    /// file contents to the <paramref name="output"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="navigation">The navigation identifier.</param>
    /// <param name="output">A stream that will receive the file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public virtual async Task<bool> TrySelectFromNavigationAsync(string cdbName, Navigation navigation, Stream output, CancellationToken cancellationToken = default)
    {
        selectFromNavigationCommand.Parameters[CdbParamName].Value = cdbName;
        SetNavigationParameters(selectFromNavigationCommand, navigation);

        await using DbDataReader dbDataReader = await selectFromNavigationCommand.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow, cancellationToken);
        do
        {
            while (await dbDataReader.ReadAsync(cancellationToken))
            {
                await using Stream stream = dbDataReader.GetStream(ContentColumnName);
                await stream.CopyToAsync(output, cancellationToken);
                return true;
            }
        } while (await dbDataReader.NextResultAsync(cancellationToken));
        return false;
    }

    /// <summary>
    /// Returns a navigation file from a CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="navigation">The navigation identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// 
    /// <returns>The file contents.</returns>
    /// <exception cref="FileNotFoundException">If the file was not found in the database.</exception>
    public virtual async Task<byte[]> SelectFromNavigationAsync(string cdbName, Navigation navigation, CancellationToken cancellationToken = default)
    {
        await using MemoryStream memoryStream = new();
        if (await TrySelectFromNavigationAsync(cdbName, navigation, memoryStream, cancellationToken))
        {
            return memoryStream.ToArray();
        }
        else
        {
            throw new FileNotFoundException("Navigation file not found.", navigation.Filename);
        }
    }

    #endregion

    /// <summary>
    /// Imports a filesystem directory into the SQL database as a CDB.
    /// </summary>
    /// <param name="cdbName">The identifying name for the imported CDB.</param>
    /// <param name="cdbRoot">The CDB directory to import.</param>
    /// <param name="serviceProvider">A service provider that provides the necessary directory walkers.</param>
    public void ImportDirectory(string cdbName, DirectoryInfo cdbRoot, IServiceProvider serviceProvider)
    {
        ILogger<SQLDataStore> logger = serviceProvider.GetRequiredService<ILogger<SQLDataStore>>();

        void metadataAction(Metadata metadata, FileInfo file)
        {
            logger.LogInformation("Inserting Metadata {File}", file);
            int rowsAffected = InsertIntoMetadata(cdbName, metadata, File.ReadAllBytes(file.FullName));
        }
        void geotypicalModelAction(GeotypicalModel geotypicalModel, FileInfo file)
        {
            logger.LogInformation("Inserting Geotypical Model {File}", file);
            int rowsAffected = InsertIntoGeotypicalModel(cdbName, geotypicalModel, File.ReadAllBytes(file.FullName));
        }
        void geotypicalModelLodAction(GeotypicalModelLod geotypicalModelLod, FileInfo file)
        {
            logger.LogInformation("Inserting Geotypical Model LOD {File}", file);
            int rowsAffected = InsertIntoGeotypicalModelLod(cdbName, geotypicalModelLod, File.ReadAllBytes(file.FullName));
        }
        void textureAction(Texture texture, FileInfo file)
        {
            logger.LogInformation("Inserting Texture {File}", file);
            int rowsAffected = InsertIntoTexture(cdbName, texture, File.ReadAllBytes(file.FullName));
        }
        void textureLodAction(TextureLod textureLod, FileInfo file)
        {
            logger.LogInformation("Inserting Texture LOD {File}", file);
            int rowsAffected = InsertIntoTextureLod(cdbName, textureLod, File.ReadAllBytes(file.FullName));
        }
        void movingModelAction(MovingModel movingModel, FileInfo file)
        {
            logger.LogInformation("Inserting Moving Model {File}", file);
            int rowsAffected = InsertIntoMovingModel(cdbName, movingModel, File.ReadAllBytes(file.FullName));
        }
        void movingModelLodAction(MovingModelLod movingModelLod, FileInfo file)
        {
            logger.LogInformation("Inserting Moving Model LOD {File}", file);
            int rowsAffected = InsertIntoMovingModelLod(cdbName, movingModelLod, File.ReadAllBytes(file.FullName));
        }
        void tileAction(Tile tile, FileInfo file)
        {
            logger.LogInformation("Inserting Tile {File}", file);
            int rowsAffected = InsertIntoTile(cdbName, tile, File.ReadAllBytes(file.FullName));

            if (CultureInfo.InvariantCulture.CompareInfo.Compare(tile.FileType, "zip", CompareOptions.IgnoreCase) == 0)
            {
                using ZipArchive zipArchive = ZipFile.OpenRead(file.FullName);
                foreach (var entry in zipArchive.Entries)
                {
                    /*
                     * Unfortunately, file names that match the "feature code" pattern
                     * can also match the "texture name" pattern, because it just groups
                     * everything after the known stuff as the name of a texture.
                     * Therefore, order is crucial here.
                     */
                    Match featureMatch = TileArchivedFeature.ArchivedFilenamePattern.Match(entry.Name);
                    if (featureMatch.Success)
                    {
                        TileArchivedFeature tileArchivedFeature = TileArchivedFeature.FromArchivedFilenameMatch(featureMatch);

                        using Stream content = entry.Open();
                        InsertIntoTileArchivedFeature(cdbName, tileArchivedFeature, content);
                    }
                    else
                    {
                        Match textureMatch = TileArchivedTexture.ArchivedFilenamePattern.Match(entry.Name);
                        if (textureMatch.Success)
                        {
                            TileArchivedTexture tileArchivedTexture = TileArchivedTexture.FromArchivedFilenameMatch(textureMatch);

                            using Stream content = entry.Open();
                            InsertIntoTileArchivedTexture(cdbName, tileArchivedTexture, content);
                        }
                        else
                        {
                            // Unrecognized file, ignore it.
                        }
                    }
                }
            }
        }
        void navigationAction(Navigation navigation, FileInfo file)
        {
            logger.LogInformation("Inserting Navigation {File}", file);
            int rowsAffected = InsertIntoNavigation(cdbName, navigation, File.ReadAllBytes(file.FullName));
        }

        InsertIntoCDB(cdbName);

        // Metadata
        {
            MetadataVisitor metadataVisitor = serviceProvider.GetRequiredService<MetadataVisitor>();
            metadataVisitor.VisitMetadata(cdbRoot, metadataAction);
        }
        // GTModel
        {
            GeotypicalModelVisitor gtModelVisitor = serviceProvider.GetRequiredService<GeotypicalModelVisitor>();
            gtModelVisitor.VisitGeotypicalModels(cdbRoot,
                geotypicalModelAction,
                geotypicalModelLodAction,
                textureAction,
                textureLodAction);
        }
        // MModel
        {
            MovingModelVisitor movingModelVisitor = serviceProvider.GetRequiredService<MovingModelVisitor>();
            movingModelVisitor.VisitMovingModels(cdbRoot,
                movingModelAction,
                movingModelLodAction,
                textureAction,
                textureLodAction);
        }
        // Tiles
        {
            TileVisitor tiledDatasetVisitor = serviceProvider.GetRequiredService<TileVisitor>();
            tiledDatasetVisitor.VisitTiles(cdbRoot, tileAction);
        }
        // Navigation
        {
            NavigationVisitor navigationVisitor = serviceProvider.GetRequiredService<NavigationVisitor>();
            navigationVisitor.VisitNavigationDatasets(cdbRoot, navigationAction);
        }
    }

    /// <summary>
    /// Dumps the raw SQL statements that the data store uses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All of them.
    /// This is a complete list of every statement that the class will ever
    /// execute against the database.
    /// </para>
    /// </remarks>
    /// <param name="textWriter">The text writer to dump the statements into.</param>
    public void DumpStatements(TextWriter textWriter)
    {
        textWriter.Write(CreateTableCDBStatement);
        textWriter.WriteLine(';');
        textWriter.Write(InsertIntoCDBStatement);
        textWriter.WriteLine(';');
        textWriter.Write(SelectFromCDBStatement);
        textWriter.WriteLine(';');
        textWriter.WriteLine();
        textWriter.Write(CreateTableMetadataStatement);
        textWriter.WriteLine(';');
        textWriter.Write(InsertIntoMetadataStatement);
        textWriter.WriteLine(';');
        textWriter.Write(SelectFromMetadataStatement);
        textWriter.WriteLine(';');
        textWriter.WriteLine();
        textWriter.Write(CreateTableTextureStatement);
        textWriter.WriteLine(';');
        textWriter.Write(InsertIntoTextureStatement);
        textWriter.WriteLine(';');
        textWriter.Write(SelectFromTextureStatement);
        textWriter.WriteLine(';');
        textWriter.WriteLine();
        textWriter.Write(CreateTableTextureLodStatement);
        textWriter.WriteLine(';');
        textWriter.Write(InsertIntoTextureLodStatement);
        textWriter.WriteLine(';');
        textWriter.Write(SelectFromTextureLodStatement);
        textWriter.WriteLine(';');
        textWriter.WriteLine();
        textWriter.Write(CreateTableGeotypicalModelStatement);
        textWriter.WriteLine(';');
        textWriter.Write(InsertIntoGeotypicalModelStatement);
        textWriter.WriteLine(';');
        textWriter.Write(SelectFromGeotypicalModelStatement);
        textWriter.WriteLine(';');
        textWriter.WriteLine();
        textWriter.Write(CreateTableGeotypicalModelLodStatement);
        textWriter.WriteLine(';');
        textWriter.Write(InsertIntoGeotypicalModelLodStatement);
        textWriter.WriteLine(';');
        textWriter.Write(SelectFromGeotypicalModelLodStatement);
        textWriter.WriteLine(';');
        textWriter.WriteLine();
        textWriter.Write(CreateTableMovingModelStatement);
        textWriter.WriteLine(';');
        textWriter.Write(InsertIntoMovingModelStatement);
        textWriter.WriteLine(';');
        textWriter.Write(SelectFromMovingModelStatement);
        textWriter.WriteLine(';');
        textWriter.WriteLine();
        textWriter.Write(CreateTableMovingModelLodStatement);
        textWriter.WriteLine(';');
        textWriter.Write(InsertIntoMovingModelLodStatement);
        textWriter.WriteLine(';');
        textWriter.Write(SelectFromMovingModelLodStatement);
        textWriter.WriteLine(';');
        textWriter.WriteLine();
        textWriter.Write(CreateTableTileStatement);
        textWriter.WriteLine(';');
        textWriter.Write(InsertIntoTileStatement);
        textWriter.WriteLine(';');
        textWriter.Write(SelectFromTileStatement);
        textWriter.WriteLine(';');
        textWriter.WriteLine();
        textWriter.Write(CreateTableTileArchivedFeatureStatement);
        textWriter.WriteLine(';');
        textWriter.Write(InsertIntoTileArchivedFeatureStatement);
        textWriter.WriteLine(';');
        textWriter.Write(SelectFromTileArchivedFeatureStatement);
        textWriter.WriteLine(';');
        textWriter.WriteLine();
        textWriter.Write(CreateTableTileArchivedTextureStatement);
        textWriter.WriteLine(';');
        textWriter.Write(InsertIntoTileArchivedTextureStatement);
        textWriter.WriteLine(';');
        textWriter.Write(SelectFromTileArchivedTextureStatement);
        textWriter.WriteLine(';');
        textWriter.WriteLine();
        textWriter.Write(CreateTableNavigationStatement);
        textWriter.WriteLine(';');
        textWriter.Write(InsertIntoNavigationStatement);
        textWriter.WriteLine(';');
        textWriter.Write(SelectFromNavigationStatement);
        textWriter.WriteLine(';');
    }

    #region Dispose Pattern

    private bool disposedValue;

    /// <summary>
    /// Disposes of all resources owned by this object.
    /// </summary>
    /// <param name="disposing">Whether to dispose or not.
    /// Ask Microsoft, it's their pattern.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                selectFromNavigationCommand.Dispose();
                insertIntoNavigationCommand.Dispose();
                selectFromTileCommand.Dispose();
                insertIntoTileCommand.Dispose();
                selectFromMovingModelLodCommand.Dispose();
                insertIntoMovingModelLodCommand.Dispose();
                selectFromMovingModelCommand.Dispose();
                insertIntoMovingModelCommand.Dispose();
                selectFromGeotypicalModelLodCommand.Dispose();
                insertIntoGeotypicalModelLodCommand.Dispose();
                selectFromGeotypicalModelCommand.Dispose();
                insertIntoGeotypicalModelCommand.Dispose();
                selectFromTextureLodCommand.Dispose();
                insertIntoTextureLodCommand.Dispose();
                selectFromTextureCommand.Dispose();
                insertIntoTextureCommand.Dispose();
                selectFromMetadataCommand.Dispose();
                insertIntoMetadataCommand.Dispose();
                selectFromCDBCommand.Dispose();
                insertIntoCDBCommand.Dispose();
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
