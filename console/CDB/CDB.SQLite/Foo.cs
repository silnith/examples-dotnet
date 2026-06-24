using Microsoft.Data.Sqlite;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Silnith.CDB.SQLite;

public class Foo : IDataStore, IDisposable
{
    private readonly SQLiteCDB sqliteCDB;

    public Foo(string cdbName, DirectoryInfo directory, FileInfo databaseFile)
    {
        SqliteConnectionStringBuilder sqliteConnectionStringBuilder = new()
        {
            DataSource = databaseFile.FullName,
            ForeignKeys = true,
        };
        SqliteConnection sqliteConnection = new(sqliteConnectionStringBuilder.ConnectionString);
        sqliteCDB = new SQLiteCDB(sqliteConnection);
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
    private bool disposedValue;

    /// <inheritdoc/>
    public byte[] ReadFile(string filePathAndName)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public bool TryReadFile(string filePathAndName, [NotNullWhen(true)] out byte[] content)
    {
        Match pathPrefixMatch = PathPrefixPattern.Match(filePathAndName);
        if (pathPrefixMatch.Success)
        {
            string directory = pathPrefixMatch.Groups["directory"].Value;
            switch (directory.ToLowerInvariant())
            {
                case "metadata":
                    return TryReadMetadata(filePathAndName, out content);
                case "gtmodel":
                    return TryReadGeotypicalModel(filePathAndName, out content);
                case "mmodel":
                    return TryReadMovingModel(filePathAndName, out content);
                case "tiles":
                    return TryReadTile(filePathAndName, out content);
                case "navigation":
                    return TryReadNavigation(filePathAndName, out content);
                default:
                    content = Array.Empty<byte>();
                    return false;
            }
        }
        else
        {
            content = Array.Empty<byte>();
            return false;
        }
    }

    public bool TryReadMetadata(string filePathAndName, [NotNullWhen(true)] out byte[] content)
    {
        Metadata metadata = new(
            Path.GetFileNameWithoutExtension(filePathAndName),
            Path.GetExtension(filePathAndName));
        return sqliteCDB.TrySelectFromMetadata(Name, metadata, out content);
    }

    public bool TryReadGeotypicalModel(string filePathAndName, [NotNullWhen(true)] out byte[] content)
    {
        string filename = Path.GetFileName(filePathAndName);
        Match geotypicalModelLodMatch = GeotypicalModelLod.FilenamePattern.Match(filename);
        if (geotypicalModelLodMatch.Success)
        {
            GeotypicalModelLod geotypicalModelLod = GeotypicalModelLod.FromFilenameMatch(geotypicalModelLodMatch);
            return sqliteCDB.TrySelectFromGeotypicalModelLod(Name, geotypicalModelLod, out content);
        }
        Match geotypicalModelMatch = GeotypicalModel.FilenamePattern.Match(filename);
        if (geotypicalModelMatch.Success)
        {
            GeotypicalModel geotypicalModel = GeotypicalModel.FromFilenameMatch(geotypicalModelMatch);
            return sqliteCDB.TrySelectFromGeotypicalModel(Name, geotypicalModel, out content);
        }
        Match textureLodMatch = TextureLod.FilenamePattern.Match(filename);
        if (textureLodMatch.Success)
        {
            TextureLod textureLod = TextureLod.FromFilenameMatch(textureLodMatch);
            return sqliteCDB.TrySelectFromTextureLod(Name, textureLod, out content);
        }
        Match textureMatch = Texture.FilenamePattern.Match(filename);
        if (textureMatch.Success)
        {
            Texture texture = Texture.FromFilenameMatch(textureMatch);
            return sqliteCDB.TrySelectFromTexture(Name, texture, out content);
        }
        content = Array.Empty<byte>();
        return false;
    }

    public bool TryReadMovingModel(string filePathAndName, [NotNullWhen(true)] out byte[] content)
    {
        string filename = Path.GetFileName(filePathAndName);
        Match movingModelLodMatch = MovingModelLod.FilenamePattern.Match(filename);
        if (movingModelLodMatch.Success)
        {
            MovingModelLod movingModelLod = MovingModelLod.FromFilenameMatch(movingModelLodMatch);
            return sqliteCDB.TrySelectFromMovingModelLod(Name, movingModelLod, out content);
        }
        Match movingModelMatch = MovingModel.FilenamePattern.Match(filename);
        if (movingModelMatch.Success)
        {
            MovingModel movingModel = MovingModel.FromFilenameMatch(movingModelMatch);
            return sqliteCDB.TrySelectFromMovingModel(Name, movingModel, out content);
        }
        Match textureLodMatch = TextureLod.FilenamePattern.Match(filename);
        if (textureLodMatch.Success)
        {
            TextureLod textureLod = TextureLod.FromFilenameMatch(textureLodMatch);
            return sqliteCDB.TrySelectFromTextureLod(Name, textureLod, out content);
        }
        Match textureMatch = Texture.FilenamePattern.Match(filename);
        if (textureMatch.Success)
        {
            Texture texture = Texture.FromFilenameMatch(textureMatch);
            return sqliteCDB.TrySelectFromTexture(Name, texture, out content);
        }
        content = Array.Empty<byte>();
        return false;
    }

    public bool TryReadTile(string filePathAndName, [NotNullWhen(true)] out byte[] content)
    {
        string filename = Path.GetFileName(filePathAndName);
        Match tileMatch = Tile.TiledDatasetFilenamePattern.Match(filename);
        if (tileMatch.Success)
        {
            Tile tile = Tile.FromTiledDatasetFilenameMatch(tileMatch);
            return sqliteCDB.TrySelectFromTile(Name, tile, out content);
        }
        else
        {
            content = Array.Empty<byte>();
            return false;
        }
    }

    public bool TryReadNavigation(string filePathAndName, [NotNullWhen(true)] out byte[] content)
    {
        string filename = Path.GetFileName(filePathAndName);
        Match navigationMatch = Navigation.FilenamePattern.Match(filename);
        if (navigationMatch.Success)
        {
            Navigation navigation = Navigation.FromFilenameMatch(navigationMatch);
            return sqliteCDB.TrySelectFromNavigation(Name, navigation, out content);
        }
        else
        {
            content = Array.Empty<byte>();
            return false;
        }
    }

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
}
