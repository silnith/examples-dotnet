using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Silnith.CDB.SQL;

/// <summary>
/// A CDB data store that uses SQL as the backing store.
/// </summary>
public class SQLCDB : ICDB
{
    private readonly SQLDataStore sqlDataStore;

    /// <summary>
    /// Creates a new CDB data store that reads from the specified SQL database.
    /// </summary>
    /// <param name="cdbName">The name of the CDB.  This must be the value of
    /// the "name" column in one of the rows of the "CDB" table.</param>
    /// <param name="directory">A directory that this data store pretends to
    /// serve files from.  This directory is not actually used.</param>
    /// <param name="sqlDataStore">An SQL data store implementation for a specific database.</param>
    public SQLCDB(string cdbName, DirectoryInfo directory, SQLDataStore sqlDataStore)
    {
        this.sqlDataStore = sqlDataStore;
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

    /// <inheritdoc/>
    public bool TryReadFile(string filePathAndName, [NotNullWhen(true)] out byte[] content)
    {
        using MemoryStream memoryStream = new();
        bool succeeded = TryReadFile(filePathAndName, memoryStream);
        content = memoryStream.ToArray();
        return succeeded;
    }

    private static readonly Regex PathPrefixPattern = new(
        "^/?(?<directory>Metadata|GTModel|MModel|Tiles|Navigation)/",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

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

    /// <inheritdoc/>
    public Task<bool> TryReadFileAsync(string filePathAndName, Stream output, CancellationToken cancellationToken)
    {
        Match pathPrefixMatch = PathPrefixPattern.Match(filePathAndName);
        if (pathPrefixMatch.Success)
        {
            string directory = pathPrefixMatch.Groups["directory"].Value;
            return directory.ToLowerInvariant() switch
            {
                "metadata" => TryReadMetadataAsync(filePathAndName, output, cancellationToken),
                "gtmodel" => TryReadGeotypicalModelAsync(filePathAndName, output, cancellationToken),
                "mmodel" => TryReadMovingModelAsync(filePathAndName, output, cancellationToken),
                "tiles" => TryReadTileAsync(filePathAndName, output, cancellationToken),
                "navigation" => TryReadNavigationAsync(filePathAndName, output, cancellationToken),
                _ => Task.FromResult(false),
            };
        }
        else
        {
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc/>
    public async Task<byte[]> ReadFileAsync(string filePathAndName, CancellationToken cancellationToken = default)
    {
        await using MemoryStream memoryStream = new();
        if (await TryReadFileAsync(filePathAndName, memoryStream, cancellationToken))
        {
            return memoryStream.ToArray();
        }
        else
        {
            throw new FileNotFoundException("File not found.", filePathAndName);
        }
    }

    private bool TryReadMetadata(string filePathAndName, Stream output)
    {
        Metadata metadata = new(
            Path.GetFileNameWithoutExtension(filePathAndName),
            Path.GetExtension(filePathAndName).Substring(1));
        return sqlDataStore.TrySelectFromMetadata(Name, metadata, output);
    }

    private Task<bool> TryReadMetadataAsync(string filePathAndName, Stream output, CancellationToken cancellationToken = default)
    {
        Metadata metadata = new(
            Path.GetFileNameWithoutExtension(filePathAndName),
            Path.GetExtension(filePathAndName).Substring(1));
        return sqlDataStore.TrySelectFromMetadataAsync(Name, metadata, output, cancellationToken);
    }

    private bool TryReadGeotypicalModel(string filePathAndName, Stream output)
    {
        string filename = Path.GetFileName(filePathAndName);
        Match geotypicalModelLodMatch = GeotypicalModelLod.FilenamePattern.Match(filename);
        if (geotypicalModelLodMatch.Success)
        {
            GeotypicalModelLod geotypicalModelLod = GeotypicalModelLod.FromFilenameMatch(geotypicalModelLodMatch);
            return sqlDataStore.TrySelectFromGeotypicalModelLod(Name, geotypicalModelLod, output);
        }
        Match geotypicalModelMatch = GeotypicalModel.FilenamePattern.Match(filename);
        if (geotypicalModelMatch.Success)
        {
            GeotypicalModel geotypicalModel = GeotypicalModel.FromFilenameMatch(geotypicalModelMatch);
            return sqlDataStore.TrySelectFromGeotypicalModel(Name, geotypicalModel, output);
        }
        Match textureLodMatch = TextureLod.FilenamePattern.Match(filename);
        if (textureLodMatch.Success)
        {
            TextureLod textureLod = TextureLod.FromFilenameMatch(textureLodMatch);
            return sqlDataStore.TrySelectFromTextureLod(Name, textureLod, output);
        }
        Match textureMatch = Texture.FilenamePattern.Match(filename);
        if (textureMatch.Success)
        {
            Texture texture = Texture.FromFilenameMatch(textureMatch);
            return sqlDataStore.TrySelectFromTexture(Name, texture, output);
        }
        return false;
    }

    private Task<bool> TryReadGeotypicalModelAsync(string filePathAndName, Stream output, CancellationToken cancellationToken)
    {
        string filename = Path.GetFileName(filePathAndName);
        Match geotypicalModelLodMatch = GeotypicalModelLod.FilenamePattern.Match(filename);
        if (geotypicalModelLodMatch.Success)
        {
            GeotypicalModelLod geotypicalModelLod = GeotypicalModelLod.FromFilenameMatch(geotypicalModelLodMatch);
            return sqlDataStore.TrySelectFromGeotypicalModelLodAsync(Name, geotypicalModelLod, output, cancellationToken);
        }
        Match geotypicalModelMatch = GeotypicalModel.FilenamePattern.Match(filename);
        if (geotypicalModelMatch.Success)
        {
            GeotypicalModel geotypicalModel = GeotypicalModel.FromFilenameMatch(geotypicalModelMatch);
            return sqlDataStore.TrySelectFromGeotypicalModelAsync(Name, geotypicalModel, output, cancellationToken);
        }
        Match textureLodMatch = TextureLod.FilenamePattern.Match(filename);
        if (textureLodMatch.Success)
        {
            TextureLod textureLod = TextureLod.FromFilenameMatch(textureLodMatch);
            return sqlDataStore.TrySelectFromTextureLodAsync(Name, textureLod, output, cancellationToken);
        }
        Match textureMatch = Texture.FilenamePattern.Match(filename);
        if (textureMatch.Success)
        {
            Texture texture = Texture.FromFilenameMatch(textureMatch);
            return sqlDataStore.TrySelectFromTextureAsync(Name, texture, output, cancellationToken);
        }
        return Task.FromResult(false);
    }

    private bool TryReadMovingModel(string filePathAndName, Stream output)
    {
        string filename = Path.GetFileName(filePathAndName);
        Match movingModelLodMatch = MovingModelLod.FilenamePattern.Match(filename);
        if (movingModelLodMatch.Success)
        {
            MovingModelLod movingModelLod = MovingModelLod.FromFilenameMatch(movingModelLodMatch);
            return sqlDataStore.TrySelectFromMovingModelLod(Name, movingModelLod, output);
        }
        Match movingModelMatch = MovingModel.FilenamePattern.Match(filename);
        if (movingModelMatch.Success)
        {
            MovingModel movingModel = MovingModel.FromFilenameMatch(movingModelMatch);
            return sqlDataStore.TrySelectFromMovingModel(Name, movingModel, output);
        }
        Match textureLodMatch = TextureLod.FilenamePattern.Match(filename);
        if (textureLodMatch.Success)
        {
            TextureLod textureLod = TextureLod.FromFilenameMatch(textureLodMatch);
            return sqlDataStore.TrySelectFromTextureLod(Name, textureLod, output);
        }
        Match textureMatch = Texture.FilenamePattern.Match(filename);
        if (textureMatch.Success)
        {
            Texture texture = Texture.FromFilenameMatch(textureMatch);
            return sqlDataStore.TrySelectFromTexture(Name, texture, output);
        }
        return false;
    }

    private Task<bool> TryReadMovingModelAsync(string filePathAndName, Stream output, CancellationToken cancellationToken)
    {
        string filename = Path.GetFileName(filePathAndName);
        Match movingModelLodMatch = MovingModelLod.FilenamePattern.Match(filename);
        if (movingModelLodMatch.Success)
        {
            MovingModelLod movingModelLod = MovingModelLod.FromFilenameMatch(movingModelLodMatch);
            return sqlDataStore.TrySelectFromMovingModelLodAsync(Name, movingModelLod, output, cancellationToken);
        }
        Match movingModelMatch = MovingModel.FilenamePattern.Match(filename);
        if (movingModelMatch.Success)
        {
            MovingModel movingModel = MovingModel.FromFilenameMatch(movingModelMatch);
            return sqlDataStore.TrySelectFromMovingModelAsync(Name, movingModel, output, cancellationToken);
        }
        Match textureLodMatch = TextureLod.FilenamePattern.Match(filename);
        if (textureLodMatch.Success)
        {
            TextureLod textureLod = TextureLod.FromFilenameMatch(textureLodMatch);
            return sqlDataStore.TrySelectFromTextureLodAsync(Name, textureLod, output, cancellationToken);
        }
        Match textureMatch = Texture.FilenamePattern.Match(filename);
        if (textureMatch.Success)
        {
            Texture texture = Texture.FromFilenameMatch(textureMatch);
            return sqlDataStore.TrySelectFromTextureAsync(Name, texture, output, cancellationToken);
        }
        return Task.FromResult(false);
    }

    private bool TryReadTile(string filePathAndName, Stream output)
    {
        string filename = Path.GetFileName(filePathAndName);
        Match tileMatch = Tile.TiledDatasetFilenamePattern.Match(filename);
        if (tileMatch.Success)
        {
            Tile tile = Tile.FromTiledDatasetFilenameMatch(tileMatch);
            return sqlDataStore.TrySelectFromTile(Name, tile, output);
        }
        else
        {
            return false;
        }
    }

    private Task<bool> TryReadTileAsync(string filePathAndName, Stream output, CancellationToken cancellationToken)
    {
        string filename = Path.GetFileName(filePathAndName);
        Match tileMatch = Tile.TiledDatasetFilenamePattern.Match(filename);
        if (tileMatch.Success)
        {
            Tile tile = Tile.FromTiledDatasetFilenameMatch(tileMatch);
            return sqlDataStore.TrySelectFromTileAsync(Name, tile, output, cancellationToken);
        }
        else
        {
            return Task.FromResult(false);
        }
    }

    private bool TryReadNavigation(string filePathAndName, Stream output)
    {
        string filename = Path.GetFileName(filePathAndName);
        Match navigationMatch = Navigation.FilenamePattern.Match(filename);
        if (navigationMatch.Success)
        {
            Navigation navigation = Navigation.FromFilenameMatch(navigationMatch);
            return sqlDataStore.TrySelectFromNavigation(Name, navigation, output);
        }
        else
        {
            return false;
        }
    }

    private Task<bool> TryReadNavigationAsync(string filePathAndName, Stream output, CancellationToken cancellationToken)
    {
        string filename = Path.GetFileName(filePathAndName);
        Match navigationMatch = Navigation.FilenamePattern.Match(filename);
        if (navigationMatch.Success)
        {
            Navigation navigation = Navigation.FromFilenameMatch(navigationMatch);
            return sqlDataStore.TrySelectFromNavigationAsync(Name, navigation, output, cancellationToken);
        }
        else
        {
            return Task.FromResult(false);
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
                sqlDataStore.Dispose();
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
