using Microsoft.Data.Sqlite;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Silnith.CDB.SQLite;

/// <summary>
/// A CDB data store that uses SQLite as the backing store.
/// </summary>
public class SQLiteDataStore : IDataStore
{
    private readonly SQLiteCDB sqliteCDB;

    /// <summary>
    /// Creates a new CDB data store that reads from the specified SQLite database file.
    /// </summary>
    /// <param name="cdbName">The name of the CDB.  This must be the value of
    /// the "name" column in one of the rows of the "CDB" table.</param>
    /// <param name="directory">A directory that this data store pretends to
    /// serve files from.  This directory is not actually used.</param>
    /// <param name="sqliteConnectionStringBuilder">The connection string builder for the database.</param>
    /// 
    public SQLiteDataStore(string cdbName, DirectoryInfo directory, SqliteConnectionStringBuilder sqliteConnectionStringBuilder)
    {
        sqliteCDB = new SQLiteCDB(sqliteConnectionStringBuilder.ConnectionString);
        Name = cdbName;
        CdbRoot = directory;
    }

    /// <inheritdoc/>
    public string Name
    {
        get;
    }

    /// <inheritdoc/>
    public DirectoryInfo CdbRoot
    {
        get;
    }

    private static readonly Regex PathPrefixPattern = new(
        "^/?(?<directory>Metadata|GTModel|MModel|Tiles|Navigation)/",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    /// <inheritdoc/>
    public bool TryReadFile(string filePathAndName, [NotNullWhen(true)] out byte[] content)
    {
        using MemoryStream memoryStream = new();
        bool succeeded = TryReadFile(filePathAndName, memoryStream);
        content = memoryStream.ToArray();
        return succeeded;
    }

    /// <inheritdoc/>
    public bool TryReadFile(string filePathAndName, Stream output)
    {
        Match pathPrefixMatch = PathPrefixPattern.Match(filePathAndName);
        if (pathPrefixMatch.Success)
        {
            string directory = pathPrefixMatch.Groups["directory"].Value;
            return directory.ToLowerInvariant() switch
            {
                "metadata" => TryReadMetadata(filePathAndName, output),
                "gtmodel" => TryReadGeotypicalModel(filePathAndName, output),
                "mmodel" => TryReadMovingModel(filePathAndName, output),
                "tiles" => TryReadTile(filePathAndName, output),
                "navigation" => TryReadNavigation(filePathAndName, output),
                _ => false,
            };
        }
        else
        {
            return false;
        }
    }

    private bool TryReadMetadata(string filePathAndName, Stream output)
    {
        Metadata metadata = new(
            Path.GetFileNameWithoutExtension(filePathAndName),
            Path.GetExtension(filePathAndName).Substring(1));
        return sqliteCDB.TrySelectFromMetadata(Name, metadata, output);
    }

    private bool TryReadGeotypicalModel(string filePathAndName, Stream output)
    {
        string filename = Path.GetFileName(filePathAndName);
        Match geotypicalModelLodMatch = GeotypicalModelLod.FilenamePattern.Match(filename);
        if (geotypicalModelLodMatch.Success)
        {
            GeotypicalModelLod geotypicalModelLod = GeotypicalModelLod.FromFilenameMatch(geotypicalModelLodMatch);
            return sqliteCDB.TrySelectFromGeotypicalModelLod(Name, geotypicalModelLod, output);
        }
        Match geotypicalModelMatch = GeotypicalModel.FilenamePattern.Match(filename);
        if (geotypicalModelMatch.Success)
        {
            GeotypicalModel geotypicalModel = GeotypicalModel.FromFilenameMatch(geotypicalModelMatch);
            return sqliteCDB.TrySelectFromGeotypicalModel(Name, geotypicalModel, output);
        }
        Match textureLodMatch = TextureLod.FilenamePattern.Match(filename);
        if (textureLodMatch.Success)
        {
            TextureLod textureLod = TextureLod.FromFilenameMatch(textureLodMatch);
            return sqliteCDB.TrySelectFromTextureLod(Name, textureLod, output);
        }
        Match textureMatch = Texture.FilenamePattern.Match(filename);
        if (textureMatch.Success)
        {
            Texture texture = Texture.FromFilenameMatch(textureMatch);
            return sqliteCDB.TrySelectFromTexture(Name, texture, output);
        }
        return false;
    }

    private bool TryReadMovingModel(string filePathAndName, Stream output)
    {
        string filename = Path.GetFileName(filePathAndName);
        Match movingModelLodMatch = MovingModelLod.FilenamePattern.Match(filename);
        if (movingModelLodMatch.Success)
        {
            MovingModelLod movingModelLod = MovingModelLod.FromFilenameMatch(movingModelLodMatch);
            return sqliteCDB.TrySelectFromMovingModelLod(Name, movingModelLod, output);
        }
        Match movingModelMatch = MovingModel.FilenamePattern.Match(filename);
        if (movingModelMatch.Success)
        {
            MovingModel movingModel = MovingModel.FromFilenameMatch(movingModelMatch);
            return sqliteCDB.TrySelectFromMovingModel(Name, movingModel, output);
        }
        Match textureLodMatch = TextureLod.FilenamePattern.Match(filename);
        if (textureLodMatch.Success)
        {
            TextureLod textureLod = TextureLod.FromFilenameMatch(textureLodMatch);
            return sqliteCDB.TrySelectFromTextureLod(Name, textureLod, output);
        }
        Match textureMatch = Texture.FilenamePattern.Match(filename);
        if (textureMatch.Success)
        {
            Texture texture = Texture.FromFilenameMatch(textureMatch);
            return sqliteCDB.TrySelectFromTexture(Name, texture, output);
        }
        return false;
    }

    private bool TryReadTile(string filePathAndName, Stream output)
    {
        string filename = Path.GetFileName(filePathAndName);
        Match tileMatch = Tile.TiledDatasetFilenamePattern.Match(filename);
        if (tileMatch.Success)
        {
            Tile tile = Tile.FromTiledDatasetFilenameMatch(tileMatch);
            return sqliteCDB.TrySelectFromTile(Name, tile, output);
        }
        else
        {
            return false;
        }
    }

    private bool TryReadNavigation(string filePathAndName, Stream output)
    {
        string filename = Path.GetFileName(filePathAndName);
        Match navigationMatch = Navigation.FilenamePattern.Match(filename);
        if (navigationMatch.Success)
        {
            Navigation navigation = Navigation.FromFilenameMatch(navigationMatch);
            return sqliteCDB.TrySelectFromNavigation(Name, navigation, output);
        }
        else
        {
            return false;
        }
    }

    #region Dispose Pattern

    private bool disposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                sqliteCDB.Dispose();
            }

            disposedValue = true;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion

}
