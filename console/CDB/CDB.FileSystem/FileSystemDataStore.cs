using Microsoft.Extensions.Logging;
using Silnith.CDB;
using System.Diagnostics.CodeAnalysis;

namespace CDB.FileSystem;

/// <summary>
/// A CDB data store that reads directly from the filesystem.
/// </summary>
/// <remarks>
/// <para>
/// This is a classic CDB implementation as described in the OGC CDB standard.
/// </para>
/// </remarks>
public class FileSystemDataStore : IDataStore
{
    private readonly ILogger<FileSystemDataStore> logger;

    /// <summary>
    /// Creates a new data store that reads from the specified directory.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum legal directory structure underneath <paramref name="cdbRoot"/>
    /// is <c>Metadata/Version.xml</c>.
    /// </para>
    /// </remarks>
    /// <param name="logger">A logger.</param>
    /// <param name="name">A name for the data store.  This can be any string,
    /// but short, simple values are typical.</param>
    /// <param name="cdbRoot">The CDB root directory.  In the CDB standard, this
    /// is usually referred to as a directory named "CDB".  In practice, it can
    /// have any name, but it should contain subdirectories like "Metadata" as
    /// described in the standard.</param>
    public FileSystemDataStore(ILogger<FileSystemDataStore> logger, string name, DirectoryInfo cdbRoot)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(cdbRoot);

        this.logger = logger;
        Name = name;
        CdbRoot = cdbRoot;
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public DirectoryInfo CdbRoot { get; }

    /// <inheritdoc/>
    public bool TryReadFile(string filePathAndName, [NotNullWhen(true)] out byte[] content)
    {
        FileInfo file = new(Path.Combine(CdbRoot.FullName, filePathAndName));
        if (file.Exists)
        {
            logger.LogTrace("Found: {File}", file);
            content = File.ReadAllBytes(file.FullName);
            return true;
        }
        else
        {
            logger.LogTrace("Not found: {File}", file);
            content = Array.Empty<byte>();
            return false;
        }
    }

    /// <inheritdoc/>
    public bool TryReadFile(string filePathAndName, Stream output)
    {
        FileInfo file = new(Path.Combine(CdbRoot.FullName, filePathAndName));
        if (file.Exists)
        {
            logger.LogTrace("Found: {File}", file);
            using FileStream fileStream = file.OpenRead();
            fileStream.CopyTo(output);
            return true;
        }
        else
        {
            logger.LogTrace("Not found: {File}", file);
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
                // TODO: dispose managed state (managed objects)
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
