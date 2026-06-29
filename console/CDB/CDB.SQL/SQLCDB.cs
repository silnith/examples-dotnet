using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silnith.CDB.Visitor;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace Silnith.CDB.SQL;

/// <summary>
/// An encapsulated SQLite database that uses a schema designed for storing
/// files from a CDB data store.
/// </summary>
public abstract class SQLCDB : IDisposable
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
    /// <see cref="KindParamName"/>,
    /// <see cref="DomainParamName"/>,
    /// <see cref="CountryParamName"/>,
    /// <see cref="CategoryParamName"/>,
    /// <see cref="SubcategoryParamName"/>,
    /// <see cref="SpecificParamName"/>,
    /// <see cref="ExtraParamName"/>,
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
    /// <see cref="KindParamName"/>,
    /// <see cref="DomainParamName"/>,
    /// <see cref="CountryParamName"/>,
    /// <see cref="CategoryParamName"/>,
    /// <see cref="SubcategoryParamName"/>,
    /// <see cref="SpecificParamName"/>,
    /// <see cref="ExtraParamName"/>,
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
    /// <see cref="KindParamName"/>,
    /// <see cref="DomainParamName"/>,
    /// <see cref="CountryParamName"/>,
    /// <see cref="CategoryParamName"/>,
    /// <see cref="SubcategoryParamName"/>,
    /// <see cref="SpecificParamName"/>,
    /// <see cref="ExtraParamName"/>,
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
    /// <see cref="KindParamName"/>,
    /// <see cref="DomainParamName"/>,
    /// <see cref="CountryParamName"/>,
    /// <see cref="CategoryParamName"/>,
    /// <see cref="SubcategoryParamName"/>,
    /// <see cref="SpecificParamName"/>,
    /// <see cref="ExtraParamName"/>,
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
    public SQLCDB(DbConnection dbConnection, bool createSchema = false)
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
    /// An <see cref="SQLCDB"/> is capable of holding multiple CDB data stores.
    /// Each distinct data store is identified by a name.
    /// </para>
    /// </remarks>
    /// <param name="cdbName">The name of a new CDB data store.</param>
    /// <returns>The number of database rows affected.</returns>
    public int InsertIntoCDB(string cdbName)
    {
        insertIntoCDBCommand.Parameters[CdbParamName].Value = cdbName;
        return insertIntoCDBCommand.ExecuteNonQuery();
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
    public IEnumerable<string> SelectFromCDB()
    {
        List<string> names = new();
        using DbDataReader dbDataReader = selectFromCDBCommand.ExecuteReader(
            CommandBehavior.SequentialAccess | CommandBehavior.SingleResult);
        do
        {
            while (dbDataReader.Read())
            {
                string name = dbDataReader.GetString(CDBNameColumnName);
                names.Add(name);
            }
        } while (dbDataReader.NextResult());
        return names;
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
        CreateAndAttachParameter(dbCommand, MetadataNameParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, FileTypeParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, ContentParamName, DbType.Binary);
        dbCommand.Prepare();
        return dbCommand;
    }

    /// <summary>
    /// Inserts a metadata file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="metadata">The metadata identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoMetadata(string cdbName, Metadata metadata, Stream content)
    {
        using MemoryStream memoryStream = new();
        content.CopyTo(memoryStream);
        return InsertIntoMetadata(cdbName, metadata, memoryStream.ToArray());
    }

    /// <summary>
    /// Inserts a metadata file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="metadata">The metadata identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoMetadata(string cdbName, Metadata metadata, byte[] content)
    {
        insertIntoMetadataCommand.Parameters[CdbParamName].Value = cdbName;
        insertIntoMetadataCommand.Parameters[MetadataNameParamName].Value = metadata.Name;
        insertIntoMetadataCommand.Parameters[FileTypeParamName].Value = metadata.FileType;
        insertIntoMetadataCommand.Parameters[ContentParamName].Value = content;

        return insertIntoMetadataCommand.ExecuteNonQuery();
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
        CreateAndAttachParameter(dbCommand, MetadataNameParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, FileTypeParamName, DbType.String);
        dbCommand.Prepare();
        return dbCommand;
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
        selectFromMetadataCommand.Parameters[CdbParamName].Value = cdbName;
        selectFromMetadataCommand.Parameters[MetadataNameParamName].Value = metadata.Name;
        selectFromMetadataCommand.Parameters[FileTypeParamName].Value = metadata.FileType;
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
        CreateAndAttachParameter(dbCommand, DatasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, TextureNameParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, FileTypeParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, ContentParamName, DbType.Binary);
        dbCommand.Prepare();
        return dbCommand;
    }

    /// <summary>
    /// Inserts a texture file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="texture">The texture identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoTexture(string cdbName, Texture texture, Stream content)
    {
        using MemoryStream memoryStream = new();
        content.CopyTo(memoryStream);
        return InsertIntoTexture(cdbName, texture, memoryStream.ToArray());
    }

    /// <summary>
    /// Inserts a texture file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="texture">The texture identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoTexture(string cdbName, Texture texture, byte[] content)
    {
        insertIntoTextureCommand.Parameters[CdbParamName].Value = cdbName;
        insertIntoTextureCommand.Parameters[DatasetParamName].Value = texture.Dataset.Value;
        insertIntoTextureCommand.Parameters[ComponentSelector1ParamName].Value = texture.ComponentSelector1;
        insertIntoTextureCommand.Parameters[ComponentSelector2ParamName].Value = texture.ComponentSelector2;
        insertIntoTextureCommand.Parameters[TextureNameParamName].Value = texture.Name;
        insertIntoTextureCommand.Parameters[FileTypeParamName].Value = texture.FileType;
        insertIntoTextureCommand.Parameters[ContentParamName].Value = content;

        return insertIntoTextureCommand.ExecuteNonQuery();
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
        CreateAndAttachParameter(dbCommand, DatasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, TextureNameParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, FileTypeParamName, DbType.String);
        dbCommand.Prepare();
        return dbCommand;
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
        selectFromTextureCommand.Parameters[CdbParamName].Value = cdbName;
        selectFromTextureCommand.Parameters[DatasetParamName].Value = texture.Dataset.Value;
        selectFromTextureCommand.Parameters[ComponentSelector1ParamName].Value = texture.ComponentSelector1;
        selectFromTextureCommand.Parameters[ComponentSelector2ParamName].Value = texture.ComponentSelector2;
        selectFromTextureCommand.Parameters[TextureNameParamName].Value = texture.Name;
        selectFromTextureCommand.Parameters[FileTypeParamName].Value = texture.FileType;
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
        CreateAndAttachParameter(dbCommand, DatasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, LevelOfDetailParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, TextureNameParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, FileTypeParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, ContentParamName, DbType.Binary);
        dbCommand.Prepare();
        return dbCommand;
    }

    /// <summary>
    /// Inserts a texture mipmap file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="textureLod">The texture mipmap identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoTextureLod(string cdbName, TextureLod textureLod, Stream content)
    {
        using MemoryStream memoryStream = new();
        content.CopyTo(memoryStream);
        return InsertIntoTextureLod(cdbName, textureLod, memoryStream.ToArray());
    }

    /// <summary>
    /// Inserts a texture mipmap file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="textureLod">The texture mipmap identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoTextureLod(string cdbName, TextureLod textureLod, byte[] content)
    {
        insertIntoTextureLodCommand.Parameters[CdbParamName].Value = cdbName;
        insertIntoTextureLodCommand.Parameters[DatasetParamName].Value = textureLod.Dataset.Value;
        insertIntoTextureLodCommand.Parameters[ComponentSelector1ParamName].Value = textureLod.ComponentSelector1;
        insertIntoTextureLodCommand.Parameters[ComponentSelector2ParamName].Value = textureLod.ComponentSelector2;
        insertIntoTextureLodCommand.Parameters[LevelOfDetailParamName].Value = textureLod.LevelOfDetail.Value;
        insertIntoTextureLodCommand.Parameters[TextureNameParamName].Value = textureLod.Name;
        insertIntoTextureLodCommand.Parameters[FileTypeParamName].Value = textureLod.FileType;
        insertIntoTextureLodCommand.Parameters[ContentParamName].Value = content;

        return insertIntoTextureLodCommand.ExecuteNonQuery();
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
        CreateAndAttachParameter(dbCommand, DatasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, LevelOfDetailParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, TextureNameParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, FileTypeParamName, DbType.String);
        dbCommand.Prepare();
        return dbCommand;
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
        selectFromTextureLodCommand.Parameters[CdbParamName].Value = cdbName;
        selectFromTextureLodCommand.Parameters[DatasetParamName].Value = textureLod.Dataset.Value;
        selectFromTextureLodCommand.Parameters[ComponentSelector1ParamName].Value = textureLod.ComponentSelector1;
        selectFromTextureLodCommand.Parameters[ComponentSelector2ParamName].Value = textureLod.ComponentSelector2;
        selectFromTextureLodCommand.Parameters[LevelOfDetailParamName].Value = textureLod.LevelOfDetail.Value;
        selectFromTextureLodCommand.Parameters[TextureNameParamName].Value = textureLod.Name;
        selectFromTextureLodCommand.Parameters[FileTypeParamName].Value = textureLod.FileType;
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
        CreateAndAttachParameter(dbCommand, DatasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, FeatureCategoryParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, FeatureSubcategoryParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, FeatureTypeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, FeatureSubcodeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ModelNameParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, FileTypeParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, ContentParamName, DbType.Binary);
        dbCommand.Prepare();
        return dbCommand;
    }

    /// <summary>
    /// Inserts a geotypical model file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="geotypicalModel">The geotypical model identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoGeotypicalModel(string cdbName, GeotypicalModel geotypicalModel, Stream content)
    {
        using MemoryStream memoryStream = new();
        content.CopyTo(memoryStream);
        return InsertIntoGeotypicalModel(cdbName, geotypicalModel, memoryStream.ToArray());
    }

    /// <summary>
    /// Inserts a geotypical model file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="geotypicalModel">The geotypical model identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoGeotypicalModel(string cdbName, GeotypicalModel geotypicalModel, byte[] content)
    {
        insertIntoGeotypicalModelCommand.Parameters[CdbParamName].Value = cdbName;
        insertIntoGeotypicalModelCommand.Parameters[DatasetParamName].Value = geotypicalModel.Dataset.Value;
        insertIntoGeotypicalModelCommand.Parameters[ComponentSelector1ParamName].Value = geotypicalModel.ComponentSelector1;
        insertIntoGeotypicalModelCommand.Parameters[ComponentSelector2ParamName].Value = geotypicalModel.ComponentSelector2;
        insertIntoGeotypicalModelCommand.Parameters[FeatureCategoryParamName].Value = geotypicalModel.FeatureCode.Category;
        insertIntoGeotypicalModelCommand.Parameters[FeatureSubcategoryParamName].Value = geotypicalModel.FeatureCode.Subcategory;
        insertIntoGeotypicalModelCommand.Parameters[FeatureTypeParamName].Value = geotypicalModel.FeatureCode.Type;
        insertIntoGeotypicalModelCommand.Parameters[FeatureSubcodeParamName].Value = geotypicalModel.FeatureSubcode;
        insertIntoGeotypicalModelCommand.Parameters[ModelNameParamName].Value = geotypicalModel.Name;
        insertIntoGeotypicalModelCommand.Parameters[FileTypeParamName].Value = geotypicalModel.FileType;
        insertIntoGeotypicalModelCommand.Parameters[ContentParamName].Value = content;

        return insertIntoGeotypicalModelCommand.ExecuteNonQuery();
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
        CreateAndAttachParameter(dbCommand, DatasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, FeatureCategoryParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, FeatureSubcategoryParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, FeatureTypeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, FeatureSubcodeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ModelNameParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, FileTypeParamName, DbType.String);
        dbCommand.Prepare();
        return dbCommand;
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
        selectFromGeotypicalModelCommand.Parameters[CdbParamName].Value = cdbName;
        selectFromGeotypicalModelCommand.Parameters[DatasetParamName].Value = geotypicalModel.Dataset.Value;
        selectFromGeotypicalModelCommand.Parameters[ComponentSelector1ParamName].Value = geotypicalModel.ComponentSelector1;
        selectFromGeotypicalModelCommand.Parameters[ComponentSelector2ParamName].Value = geotypicalModel.ComponentSelector2;
        selectFromGeotypicalModelCommand.Parameters[FeatureCategoryParamName].Value = geotypicalModel.FeatureCode.Category;
        selectFromGeotypicalModelCommand.Parameters[FeatureSubcategoryParamName].Value = geotypicalModel.FeatureCode.Subcategory;
        selectFromGeotypicalModelCommand.Parameters[FeatureTypeParamName].Value = geotypicalModel.FeatureCode.Type;
        selectFromGeotypicalModelCommand.Parameters[FeatureSubcodeParamName].Value = geotypicalModel.FeatureSubcode;
        selectFromGeotypicalModelCommand.Parameters[ModelNameParamName].Value = geotypicalModel.Name;
        selectFromGeotypicalModelCommand.Parameters[FileTypeParamName].Value = geotypicalModel.FileType;
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
        CreateAndAttachParameter(dbCommand, ContentParamName, DbType.Binary);
        dbCommand.Prepare();
        return dbCommand;
    }

    /// <summary>
    /// Inserts a geotypical model level of detail file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="geotypicalModelLod">The geotypical model level of detail identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoGeotypicalModelLod(string cdbName, GeotypicalModelLod geotypicalModelLod, Stream content)
    {
        using MemoryStream memoryStream = new();
        content.CopyTo(memoryStream);
        return InsertIntoGeotypicalModelLod(cdbName, geotypicalModelLod, memoryStream.ToArray());
    }

    /// <summary>
    /// Inserts a geotypical model level of detail file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="geotypicalModelLod">The geotypical model level of detail identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoGeotypicalModelLod(string cdbName, GeotypicalModelLod geotypicalModelLod, byte[] content)
    {
        insertIntoGeotypicalModelLodCommand.Parameters[CdbParamName].Value = cdbName;
        insertIntoGeotypicalModelLodCommand.Parameters[DatasetParamName].Value = geotypicalModelLod.Dataset.Value;
        insertIntoGeotypicalModelLodCommand.Parameters[ComponentSelector1ParamName].Value = geotypicalModelLod.ComponentSelector1;
        insertIntoGeotypicalModelLodCommand.Parameters[ComponentSelector2ParamName].Value = geotypicalModelLod.ComponentSelector2;
        insertIntoGeotypicalModelLodCommand.Parameters[LevelOfDetailParamName].Value = geotypicalModelLod.LevelOfDetail.Value;
        insertIntoGeotypicalModelLodCommand.Parameters[FeatureCategoryParamName].Value = geotypicalModelLod.FeatureCode.Category;
        insertIntoGeotypicalModelLodCommand.Parameters[FeatureSubcategoryParamName].Value = geotypicalModelLod.FeatureCode.Subcategory;
        insertIntoGeotypicalModelLodCommand.Parameters[FeatureTypeParamName].Value = geotypicalModelLod.FeatureCode.Type;
        insertIntoGeotypicalModelLodCommand.Parameters[FeatureSubcodeParamName].Value = geotypicalModelLod.FeatureSubcode;
        insertIntoGeotypicalModelLodCommand.Parameters[ModelNameParamName].Value = geotypicalModelLod.Name;
        insertIntoGeotypicalModelLodCommand.Parameters[FileTypeParamName].Value = geotypicalModelLod.FileType;
        insertIntoGeotypicalModelLodCommand.Parameters[ContentParamName].Value = content;

        return insertIntoGeotypicalModelLodCommand.ExecuteNonQuery();
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
        dbCommand.Prepare();
        return dbCommand;
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
        selectFromGeotypicalModelLodCommand.Parameters[CdbParamName].Value = cdbName;
        selectFromGeotypicalModelLodCommand.Parameters[DatasetParamName].Value = geotypicalModelLod.Dataset.Value;
        selectFromGeotypicalModelLodCommand.Parameters[ComponentSelector1ParamName].Value = geotypicalModelLod.ComponentSelector1;
        selectFromGeotypicalModelLodCommand.Parameters[ComponentSelector2ParamName].Value = geotypicalModelLod.ComponentSelector2;
        selectFromGeotypicalModelLodCommand.Parameters[LevelOfDetailParamName].Value = geotypicalModelLod.LevelOfDetail.Value;
        selectFromGeotypicalModelLodCommand.Parameters[FeatureCategoryParamName].Value = geotypicalModelLod.FeatureCode.Category;
        selectFromGeotypicalModelLodCommand.Parameters[FeatureSubcategoryParamName].Value = geotypicalModelLod.FeatureCode.Subcategory;
        selectFromGeotypicalModelLodCommand.Parameters[FeatureTypeParamName].Value = geotypicalModelLod.FeatureCode.Type;
        selectFromGeotypicalModelLodCommand.Parameters[FeatureSubcodeParamName].Value = geotypicalModelLod.FeatureSubcode;
        selectFromGeotypicalModelLodCommand.Parameters[ModelNameParamName].Value = geotypicalModelLod.Name;
        selectFromGeotypicalModelLodCommand.Parameters[FileTypeParamName].Value = geotypicalModelLod.FileType;
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

    #endregion

    #region Moving Model

    #region DIS Code Parameters

    /// <summary>
    /// The name of the SQL parameter for the DIS Code component "kind".
    /// The value must be of type <see cref="DbType.Int32"/>.
    /// </summary>
    protected abstract string KindParamName
    {
        get;
    }

    /// <summary>
    /// The name of the SQL parameter for the DIS Code component "domain".
    /// The value must be of type <see cref="DbType.Int32"/>.
    /// </summary>
    protected abstract string DomainParamName
    {
        get;
    }

    /// <summary>
    /// The name of the SQL parameter for the DIS Code component "country".
    /// The value must be of type <see cref="DbType.Int32"/>.
    /// </summary>
    protected abstract string CountryParamName
    {
        get;
    }

    /// <summary>
    /// The name of the SQL parameter for the DIS Code component "category".
    /// The value must be of type <see cref="DbType.Int32"/>.
    /// </summary>
    protected abstract string CategoryParamName
    {
        get;
    }

    /// <summary>
    /// The name of the SQL parameter for the DIS Code component "subcategory".
    /// The value must be of type <see cref="DbType.Int32"/>.
    /// </summary>
    protected abstract string SubcategoryParamName
    {
        get;
    }

    /// <summary>
    /// The name of the SQL parameter for the DIS Code component "specific".
    /// The value must be of type <see cref="DbType.Int32"/>.
    /// </summary>
    protected abstract string SpecificParamName
    {
        get;
    }

    /// <summary>
    /// The name of the SQL parameter for the DIS Code component "extra".
    /// The value must be of type <see cref="DbType.Int32"/>.
    /// </summary>
    protected abstract string ExtraParamName
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
    /// <see cref="KindParamName"/>,
    /// <see cref="DomainParamName"/>,
    /// <see cref="CountryParamName"/>,
    /// <see cref="CategoryParamName"/>,
    /// <see cref="SubcategoryParamName"/>,
    /// <see cref="SpecificParamName"/>,
    /// <see cref="ExtraParamName"/>,
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
        CreateAndAttachParameter(dbCommand, DatasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, KindParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, DomainParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, CountryParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, CategoryParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, SubcategoryParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, SpecificParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ExtraParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, FileTypeParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, ContentParamName, DbType.String);
        dbCommand.Prepare();
        return dbCommand;
    }

    /// <summary>
    /// Inserts a moving model file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="movingModel">The moving model identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoMovingModel(string cdbName, MovingModel movingModel, Stream content)
    {
        using MemoryStream memoryStream = new();
        content.CopyTo(memoryStream);
        return InsertIntoMovingModel(cdbName, movingModel, memoryStream.ToArray());
    }

    /// <summary>
    /// Inserts a moving model file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="movingModel">The moving model identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoMovingModel(string cdbName, MovingModel movingModel, byte[] content)
    {
        insertIntoMovingModelCommand.Parameters[CdbParamName].Value = cdbName;
        insertIntoMovingModelCommand.Parameters[DatasetParamName].Value = movingModel.Dataset.Value;
        insertIntoMovingModelCommand.Parameters[ComponentSelector1ParamName].Value = movingModel.ComponentSelector1;
        insertIntoMovingModelCommand.Parameters[ComponentSelector2ParamName].Value = movingModel.ComponentSelector2;
        insertIntoMovingModelCommand.Parameters[KindParamName].Value = movingModel.MMDC.Kind;
        insertIntoMovingModelCommand.Parameters[DomainParamName].Value = movingModel.MMDC.Domain;
        insertIntoMovingModelCommand.Parameters[CountryParamName].Value = movingModel.MMDC.Country;
        insertIntoMovingModelCommand.Parameters[CategoryParamName].Value = movingModel.MMDC.Category;
        insertIntoMovingModelCommand.Parameters[SubcategoryParamName].Value = movingModel.MMDC.Subcategory;
        insertIntoMovingModelCommand.Parameters[SpecificParamName].Value = movingModel.MMDC.Specific;
        insertIntoMovingModelCommand.Parameters[ExtraParamName].Value = movingModel.MMDC.Extra;
        insertIntoMovingModelCommand.Parameters[FileTypeParamName].Value = movingModel.FileType;
        insertIntoMovingModelCommand.Parameters[ContentParamName].Value = content;

        return insertIntoMovingModelCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// The SQL statement to select a row from the Moving Model table.
    /// This takes twelve parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="KindParamName"/>,
    /// <see cref="DomainParamName"/>,
    /// <see cref="CountryParamName"/>,
    /// <see cref="CategoryParamName"/>,
    /// <see cref="SubcategoryParamName"/>,
    /// <see cref="SpecificParamName"/>,
    /// <see cref="ExtraParamName"/>,
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
        CreateAndAttachParameter(dbCommand, DatasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, KindParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, DomainParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, CountryParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, CategoryParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, SubcategoryParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, SpecificParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ExtraParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, FileTypeParamName, DbType.String);
        dbCommand.Prepare();
        return dbCommand;
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
        selectFromMovingModelCommand.Parameters[CdbParamName].Value = cdbName;
        selectFromMovingModelCommand.Parameters[DatasetParamName].Value = movingModel.Dataset.Value;
        selectFromMovingModelCommand.Parameters[ComponentSelector1ParamName].Value = movingModel.ComponentSelector1;
        selectFromMovingModelCommand.Parameters[ComponentSelector2ParamName].Value = movingModel.ComponentSelector2;
        selectFromMovingModelCommand.Parameters[KindParamName].Value = movingModel.MMDC.Kind;
        selectFromMovingModelCommand.Parameters[DomainParamName].Value = movingModel.MMDC.Domain;
        selectFromMovingModelCommand.Parameters[CountryParamName].Value = movingModel.MMDC.Country;
        selectFromMovingModelCommand.Parameters[CategoryParamName].Value = movingModel.MMDC.Category;
        selectFromMovingModelCommand.Parameters[SubcategoryParamName].Value = movingModel.MMDC.Subcategory;
        selectFromMovingModelCommand.Parameters[SpecificParamName].Value = movingModel.MMDC.Specific;
        selectFromMovingModelCommand.Parameters[ExtraParamName].Value = movingModel.MMDC.Extra;
        selectFromMovingModelCommand.Parameters[FileTypeParamName].Value = movingModel.FileType;
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
    /// <see cref="KindParamName"/>,
    /// <see cref="DomainParamName"/>,
    /// <see cref="CountryParamName"/>,
    /// <see cref="CategoryParamName"/>,
    /// <see cref="SubcategoryParamName"/>,
    /// <see cref="SpecificParamName"/>,
    /// <see cref="ExtraParamName"/>,
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
        CreateAndAttachParameter(dbCommand, DatasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, LevelOfDetailParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, KindParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, DomainParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, CountryParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, CategoryParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, SubcategoryParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, SpecificParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ExtraParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, FileTypeParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, ContentParamName, DbType.String);
        dbCommand.Prepare();
        return dbCommand;
    }

    /// <summary>
    /// Inserts a moving model level of detail file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="movingModelLod">The moving model level of detail identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoMovingModelLod(string cdbName, MovingModelLod movingModelLod, Stream content)
    {
        using MemoryStream memoryStream = new();
        content.CopyTo(memoryStream);
        return InsertIntoMovingModelLod(cdbName, movingModelLod, memoryStream.ToArray());
    }

    /// <summary>
    /// Inserts a moving model level of detail file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="movingModelLod">The moving model level of detail identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoMovingModelLod(string cdbName, MovingModelLod movingModelLod, byte[] content)
    {
        insertIntoMovingModelLodCommand.Parameters[CdbParamName].Value = cdbName;
        insertIntoMovingModelLodCommand.Parameters[DatasetParamName].Value = movingModelLod.Dataset.Value;
        insertIntoMovingModelLodCommand.Parameters[ComponentSelector1ParamName].Value = movingModelLod.ComponentSelector1;
        insertIntoMovingModelLodCommand.Parameters[ComponentSelector2ParamName].Value = movingModelLod.ComponentSelector2;
        insertIntoMovingModelLodCommand.Parameters[LevelOfDetailParamName].Value = movingModelLod.LevelOfDetail.Value;
        insertIntoMovingModelLodCommand.Parameters[KindParamName].Value = movingModelLod.MMDC.Kind;
        insertIntoMovingModelLodCommand.Parameters[DomainParamName].Value = movingModelLod.MMDC.Domain;
        insertIntoMovingModelLodCommand.Parameters[CountryParamName].Value = movingModelLod.MMDC.Country;
        insertIntoMovingModelLodCommand.Parameters[CategoryParamName].Value = movingModelLod.MMDC.Category;
        insertIntoMovingModelLodCommand.Parameters[SubcategoryParamName].Value = movingModelLod.MMDC.Subcategory;
        insertIntoMovingModelLodCommand.Parameters[SpecificParamName].Value = movingModelLod.MMDC.Specific;
        insertIntoMovingModelLodCommand.Parameters[ExtraParamName].Value = movingModelLod.MMDC.Extra;
        insertIntoMovingModelLodCommand.Parameters[FileTypeParamName].Value = movingModelLod.FileType;
        insertIntoMovingModelLodCommand.Parameters[ContentParamName].Value = content;

        return insertIntoMovingModelLodCommand.ExecuteNonQuery();
    }

    /// <summary>
    /// The SQL statement to select a row from the Moving Model Level of Detail table.
    /// This takes thirteen parameters,
    /// <see cref="CdbParamName"/>,
    /// <see cref="DatasetParamName"/>,
    /// <see cref="ComponentSelector1ParamName"/>,
    /// <see cref="ComponentSelector2ParamName"/>,
    /// <see cref="LevelOfDetailParamName"/>,
    /// <see cref="KindParamName"/>,
    /// <see cref="DomainParamName"/>,
    /// <see cref="CountryParamName"/>,
    /// <see cref="CategoryParamName"/>,
    /// <see cref="SubcategoryParamName"/>,
    /// <see cref="SpecificParamName"/>,
    /// <see cref="ExtraParamName"/>,
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
        CreateAndAttachParameter(dbCommand, DatasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, LevelOfDetailParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, KindParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, DomainParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, CountryParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, CategoryParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, SubcategoryParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, SpecificParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ExtraParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, FileTypeParamName, DbType.String);
        dbCommand.Prepare();
        return dbCommand;
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
        selectFromMovingModelLodCommand.Parameters[CdbParamName].Value = cdbName;
        selectFromMovingModelLodCommand.Parameters[DatasetParamName].Value = movingModelLod.Dataset.Value;
        selectFromMovingModelLodCommand.Parameters[ComponentSelector1ParamName].Value = movingModelLod.ComponentSelector1;
        selectFromMovingModelLodCommand.Parameters[ComponentSelector2ParamName].Value = movingModelLod.ComponentSelector2;
        selectFromMovingModelLodCommand.Parameters[LevelOfDetailParamName].Value = movingModelLod.LevelOfDetail.Value;
        selectFromMovingModelLodCommand.Parameters[KindParamName].Value = movingModelLod.MMDC.Kind;
        selectFromMovingModelLodCommand.Parameters[DomainParamName].Value = movingModelLod.MMDC.Domain;
        selectFromMovingModelLodCommand.Parameters[CountryParamName].Value = movingModelLod.MMDC.Country;
        selectFromMovingModelLodCommand.Parameters[CategoryParamName].Value = movingModelLod.MMDC.Category;
        selectFromMovingModelLodCommand.Parameters[SubcategoryParamName].Value = movingModelLod.MMDC.Subcategory;
        selectFromMovingModelLodCommand.Parameters[SpecificParamName].Value = movingModelLod.MMDC.Specific;
        selectFromMovingModelLodCommand.Parameters[ExtraParamName].Value = movingModelLod.MMDC.Extra;
        selectFromMovingModelLodCommand.Parameters[FileTypeParamName].Value = movingModelLod.FileType;
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
        CreateAndAttachParameter(dbCommand, LatitudeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, LongitudeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, DatasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, LevelOfDetailParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, UpParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, RightParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, FileTypeParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, ContentParamName, DbType.Binary);
        dbCommand.Prepare();
        return dbCommand;
    }

    /// <summary>
    /// Inserts a tiled dataset file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="tile">The tile identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoTile(string cdbName, Tile tile, Stream content)
    {
        using MemoryStream memoryStream = new();
        content.CopyTo(memoryStream);
        return InsertIntoTile(cdbName, tile, memoryStream.ToArray());
    }

    /// <summary>
    /// Inserts a tiled dataset file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="tile">The tile identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoTile(string cdbName, Tile tile, byte[] content)
    {
        insertIntoTileCommand.Parameters[CdbParamName].Value = cdbName;
        insertIntoTileCommand.Parameters[LatitudeParamName].Value = tile.LatitudeValue.Value;
        insertIntoTileCommand.Parameters[LongitudeParamName].Value = tile.LongitudeValue.Value;
        insertIntoTileCommand.Parameters[DatasetParamName].Value = tile.DatasetValue.Value;
        insertIntoTileCommand.Parameters[ComponentSelector1ParamName].Value = tile.ComponentSelector1;
        insertIntoTileCommand.Parameters[ComponentSelector2ParamName].Value = tile.ComponentSelector2;
        insertIntoTileCommand.Parameters[LevelOfDetailParamName].Value = tile.Level.Value;
        insertIntoTileCommand.Parameters[UpParamName].Value = tile.Up;
        insertIntoTileCommand.Parameters[RightParamName].Value = tile.Right;
        insertIntoTileCommand.Parameters[FileTypeParamName].Value = tile.FileType;
        insertIntoTileCommand.Parameters[ContentParamName].Value = content;

        return insertIntoTileCommand.ExecuteNonQuery();
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
        CreateAndAttachParameter(dbCommand, LatitudeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, LongitudeParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, DatasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, LevelOfDetailParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, UpParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, RightParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, FileTypeParamName, DbType.String);
        dbCommand.Prepare();
        return dbCommand;
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
        selectFromTileCommand.Parameters[CdbParamName].Value = cdbName;
        selectFromTileCommand.Parameters[LatitudeParamName].Value = tile.LatitudeValue.Value;
        selectFromTileCommand.Parameters[LongitudeParamName].Value = tile.LongitudeValue.Value;
        selectFromTileCommand.Parameters[DatasetParamName].Value = tile.DatasetValue.Value;
        selectFromTileCommand.Parameters[ComponentSelector1ParamName].Value = tile.ComponentSelector1;
        selectFromTileCommand.Parameters[ComponentSelector2ParamName].Value = tile.ComponentSelector2;
        selectFromTileCommand.Parameters[LevelOfDetailParamName].Value = tile.Level.Value;
        selectFromTileCommand.Parameters[UpParamName].Value = tile.Up;
        selectFromTileCommand.Parameters[RightParamName].Value = tile.Right;
        selectFromTileCommand.Parameters[FileTypeParamName].Value = tile.FileType;
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
        CreateAndAttachParameter(dbCommand, ContentParamName, DbType.Binary);
        dbCommand.Prepare();
        return dbCommand;
    }

    /// <summary>
    /// Inserts an un-archived tiled dataset feature file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="tileArchivedFeature">The un-archived tiled dataset feature identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoTileArchivedFeature(string cdbName, TileArchivedFeature tileArchivedFeature, Stream content)
    {
        using MemoryStream memoryStream = new();
        content.CopyTo(memoryStream);
        return InsertIntoTileArchivedFeature(cdbName, tileArchivedFeature, memoryStream.ToArray());
    }

    /// <summary>
    /// Inserts an un-archived tiled dataset feature file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="tileArchivedFeature">The un-archived tiled dataset feature identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoTileArchivedFeature(string cdbName, TileArchivedFeature tileArchivedFeature, byte[] content)
    {
        insertIntoTileArchivedFeatureCommand.Parameters[CdbParamName].Value = cdbName;
        insertIntoTileArchivedFeatureCommand.Parameters[LatitudeParamName].Value = tileArchivedFeature.LatitudeValue.Value;
        insertIntoTileArchivedFeatureCommand.Parameters[LongitudeParamName].Value = tileArchivedFeature.LongitudeValue.Value;
        insertIntoTileArchivedFeatureCommand.Parameters[DatasetParamName].Value = tileArchivedFeature.DatasetValue.Value;
        insertIntoTileArchivedFeatureCommand.Parameters[ComponentSelector1ParamName].Value = tileArchivedFeature.ComponentSelector1;
        insertIntoTileArchivedFeatureCommand.Parameters[ComponentSelector2ParamName].Value = tileArchivedFeature.ComponentSelector2;
        insertIntoTileArchivedFeatureCommand.Parameters[LevelOfDetailParamName].Value = tileArchivedFeature.Level.Value;
        insertIntoTileArchivedFeatureCommand.Parameters[UpParamName].Value = tileArchivedFeature.Up;
        insertIntoTileArchivedFeatureCommand.Parameters[RightParamName].Value = tileArchivedFeature.Right;
        insertIntoTileArchivedFeatureCommand.Parameters[FeatureCategoryParamName].Value = tileArchivedFeature.FeatureCode.Category;
        insertIntoTileArchivedFeatureCommand.Parameters[FeatureSubcategoryParamName].Value = tileArchivedFeature.FeatureCode.Subcategory;
        insertIntoTileArchivedFeatureCommand.Parameters[FeatureTypeParamName].Value = tileArchivedFeature.FeatureCode.Type;
        insertIntoTileArchivedFeatureCommand.Parameters[FeatureSubcodeParamName].Value = tileArchivedFeature.FeatureSubcode;
        insertIntoTileArchivedFeatureCommand.Parameters[ModelNameParamName].Value = tileArchivedFeature.Name;
        insertIntoTileArchivedFeatureCommand.Parameters[FileTypeParamName].Value = tileArchivedFeature.FileType;
        insertIntoTileArchivedFeatureCommand.Parameters[ContentParamName].Value = content;

        return insertIntoTileArchivedFeatureCommand.ExecuteNonQuery();
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
        dbCommand.Prepare();
        return dbCommand;
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
    public bool TrySelectFromTileArchivedFeature(string cdbName, TileArchivedFeature tileArchivedFeature, Stream output)
    {
        selectFromTileArchivedFeatureCommand.Parameters[CdbParamName].Value = cdbName;
        selectFromTileArchivedFeatureCommand.Parameters[LatitudeParamName].Value = tileArchivedFeature.LatitudeValue.Value;
        selectFromTileArchivedFeatureCommand.Parameters[LongitudeParamName].Value = tileArchivedFeature.LongitudeValue.Value;
        selectFromTileArchivedFeatureCommand.Parameters[DatasetParamName].Value = tileArchivedFeature.DatasetValue.Value;
        selectFromTileArchivedFeatureCommand.Parameters[ComponentSelector1ParamName].Value = tileArchivedFeature.ComponentSelector1;
        selectFromTileArchivedFeatureCommand.Parameters[ComponentSelector2ParamName].Value = tileArchivedFeature.ComponentSelector2;
        selectFromTileArchivedFeatureCommand.Parameters[LevelOfDetailParamName].Value = tileArchivedFeature.Level.Value;
        selectFromTileArchivedFeatureCommand.Parameters[UpParamName].Value = tileArchivedFeature.Up;
        selectFromTileArchivedFeatureCommand.Parameters[RightParamName].Value = tileArchivedFeature.Right;
        selectFromTileArchivedFeatureCommand.Parameters[FeatureCategoryParamName].Value = tileArchivedFeature.FeatureCode.Category;
        selectFromTileArchivedFeatureCommand.Parameters[FeatureSubcategoryParamName].Value = tileArchivedFeature.FeatureCode.Subcategory;
        selectFromTileArchivedFeatureCommand.Parameters[FeatureTypeParamName].Value = tileArchivedFeature.FeatureCode.Type;
        selectFromTileArchivedFeatureCommand.Parameters[FeatureSubcodeParamName].Value = tileArchivedFeature.FeatureSubcode;
        selectFromTileArchivedFeatureCommand.Parameters[ModelNameParamName].Value = tileArchivedFeature.Name;
        selectFromTileArchivedFeatureCommand.Parameters[FileTypeParamName].Value = tileArchivedFeature.FileType;
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
    /// file contents to the <paramref name="content"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="tileArchivedFeature">The tiled dataset feature identifier.</param>
    /// <param name="content">An output variable that will receive the file contents.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public bool TrySelectFromTileArchivedFeature(string cdbName, TileArchivedFeature tileArchivedFeature, [NotNullWhen(true)] out byte[] content)
    {
        using MemoryStream memoryStream = new();
        bool succeeded = TrySelectFromTileArchivedFeature(cdbName, tileArchivedFeature, memoryStream);
        content = memoryStream.ToArray();
        return succeeded;
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
        CreateAndAttachParameter(dbCommand, ContentParamName, DbType.Binary);
        dbCommand.Prepare();
        return dbCommand;
    }

    /// <summary>
    /// Inserts an un-archived tiled dataset texture file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="tileArchivedTexture">The un-archived tiled dataset texture identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoTileArchivedTexture(string cdbName, TileArchivedTexture tileArchivedTexture, Stream content)
    {
        using MemoryStream memoryStream = new();
        content.CopyTo(memoryStream);
        return InsertIntoTileArchivedTexture(cdbName, tileArchivedTexture, memoryStream.ToArray());
    }

    /// <summary>
    /// Inserts an un-archived tiled dataset texture file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="tileArchivedTexture">The un-archived tiled dataset texture identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoTileArchivedTexture(string cdbName, TileArchivedTexture tileArchivedTexture, byte[] content)
    {
        insertIntoTileArchivedTextureCommand.Parameters[CdbParamName].Value = cdbName;
        insertIntoTileArchivedTextureCommand.Parameters[LatitudeParamName].Value = tileArchivedTexture.LatitudeValue.Value;
        insertIntoTileArchivedTextureCommand.Parameters[LongitudeParamName].Value = tileArchivedTexture.LongitudeValue.Value;
        insertIntoTileArchivedTextureCommand.Parameters[DatasetParamName].Value = tileArchivedTexture.DatasetValue.Value;
        insertIntoTileArchivedTextureCommand.Parameters[ComponentSelector1ParamName].Value = tileArchivedTexture.ComponentSelector1;
        insertIntoTileArchivedTextureCommand.Parameters[ComponentSelector2ParamName].Value = tileArchivedTexture.ComponentSelector2;
        insertIntoTileArchivedTextureCommand.Parameters[LevelOfDetailParamName].Value = tileArchivedTexture.Level.Value;
        insertIntoTileArchivedTextureCommand.Parameters[UpParamName].Value = tileArchivedTexture.Up;
        insertIntoTileArchivedTextureCommand.Parameters[RightParamName].Value = tileArchivedTexture.Right;
        insertIntoTileArchivedTextureCommand.Parameters[TextureNameParamName].Value = tileArchivedTexture.Name;
        insertIntoTileArchivedTextureCommand.Parameters[FileTypeParamName].Value = tileArchivedTexture.FileType;
        insertIntoTileArchivedTextureCommand.Parameters[ContentParamName].Value = content;

        return insertIntoTileArchivedTextureCommand.ExecuteNonQuery();
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
        dbCommand.Prepare();
        return dbCommand;
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
    public bool TrySelectFromTileArchivedTexture(string cdbName, TileArchivedTexture tileArchivedTexture, Stream output)
    {
        selectFromTileArchivedTextureCommand.Parameters[CdbParamName].Value = cdbName;
        selectFromTileArchivedTextureCommand.Parameters[LatitudeParamName].Value = tileArchivedTexture.LatitudeValue.Value;
        selectFromTileArchivedTextureCommand.Parameters[LongitudeParamName].Value = tileArchivedTexture.LongitudeValue.Value;
        selectFromTileArchivedTextureCommand.Parameters[DatasetParamName].Value = tileArchivedTexture.DatasetValue.Value;
        selectFromTileArchivedTextureCommand.Parameters[ComponentSelector1ParamName].Value = tileArchivedTexture.ComponentSelector1;
        selectFromTileArchivedTextureCommand.Parameters[ComponentSelector2ParamName].Value = tileArchivedTexture.ComponentSelector2;
        selectFromTileArchivedTextureCommand.Parameters[LevelOfDetailParamName].Value = tileArchivedTexture.Level.Value;
        selectFromTileArchivedTextureCommand.Parameters[UpParamName].Value = tileArchivedTexture.Up;
        selectFromTileArchivedTextureCommand.Parameters[RightParamName].Value = tileArchivedTexture.Right;
        selectFromTileArchivedTextureCommand.Parameters[TextureNameParamName].Value = tileArchivedTexture.Name;
        selectFromTileArchivedTextureCommand.Parameters[FileTypeParamName].Value = tileArchivedTexture.FileType;
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
    /// file contents to the <paramref name="content"/> parameter.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store.</param>
    /// <param name="tileArchivedTexture">The tiled dataset texture identifier.</param>
    /// <param name="content">An output variable that will receive the file contents.</param>
    /// <returns><see langword="true"/> if the file was found and returned.</returns>
    public bool TrySelectFromTileArchivedTexture(string cdbName, TileArchivedTexture tileArchivedTexture, [NotNullWhen(true)] out byte[] content)
    {
        using MemoryStream memoryStream = new();
        bool succeeded = TrySelectFromTileArchivedTexture(cdbName, tileArchivedTexture, memoryStream);
        content = memoryStream.ToArray();
        return succeeded;
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
        CreateAndAttachParameter(dbCommand, DatasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, FileTypeParamName, DbType.String);
        CreateAndAttachParameter(dbCommand, ContentParamName, DbType.Binary);
        dbCommand.Prepare();
        return dbCommand;
    }

    /// <summary>
    /// Inserts a navigation file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="navigation">The navigation identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoNavigation(string cdbName, Navigation navigation, Stream content)
    {
        using MemoryStream memoryStream = new();
        content.CopyTo(memoryStream);
        return InsertIntoNavigation(cdbName, navigation, memoryStream.ToArray());
    }

    /// <summary>
    /// Inserts a navigation file into the CDB data store.
    /// </summary>
    /// <param name="cdbName">The name of the CDB data store to insert the file into.</param>
    /// <param name="navigation">The navigation identifier.</param>
    /// <param name="content">The file contents.</param>
    /// <returns>The number of rows affected.</returns>
    public int InsertIntoNavigation(string cdbName, Navigation navigation, byte[] content)
    {
        insertIntoNavigationCommand.Parameters[CdbParamName].Value = cdbName;
        insertIntoNavigationCommand.Parameters[DatasetParamName].Value = navigation.Dataset.Value;
        insertIntoNavigationCommand.Parameters[ComponentSelector1ParamName].Value = navigation.ComponentSelector1;
        insertIntoNavigationCommand.Parameters[ComponentSelector2ParamName].Value = navigation.ComponentSelector2;
        insertIntoNavigationCommand.Parameters[FileTypeParamName].Value = navigation.FileType;
        insertIntoNavigationCommand.Parameters[ContentParamName].Value = content;

        return insertIntoNavigationCommand.ExecuteNonQuery();
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
        CreateAndAttachParameter(dbCommand, DatasetParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector1ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, ComponentSelector2ParamName, DbType.Int32);
        CreateAndAttachParameter(dbCommand, FileTypeParamName, DbType.String);
        dbCommand.Prepare();
        return dbCommand;
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
        selectFromNavigationCommand.Parameters[CdbParamName].Value = cdbName;
        selectFromNavigationCommand.Parameters[DatasetParamName].Value = navigation.Dataset.Value;
        selectFromNavigationCommand.Parameters[ComponentSelector1ParamName].Value = navigation.ComponentSelector1;
        selectFromNavigationCommand.Parameters[ComponentSelector2ParamName].Value = navigation.ComponentSelector2;
        selectFromNavigationCommand.Parameters[FileTypeParamName].Value = navigation.FileType;
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

    #endregion

    /// <summary>
    /// Imports a filesystem directory into the SQL database as a CDB.
    /// </summary>
    /// <param name="cdbName">The identifying name for the imported CDB.</param>
    /// <param name="cdbRoot">The CDB directory to import.</param>
    /// <param name="serviceProvider">A service provider that provides the necessary directory walkers.</param>
    public void ImportDirectory(string cdbName, DirectoryInfo cdbRoot, IServiceProvider serviceProvider)
    {
        ILogger<SQLCDB> logger = serviceProvider.GetRequiredService<ILogger<SQLCDB>>();

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
