using Microsoft.Extensions.Logging;
using Silnith.CDB;
using System.Diagnostics.CodeAnalysis;

namespace CDB.FileSystem;

public class FileSystemCDB : IDataStore
{
    private readonly ILogger<FileSystemCDB> logger;

    public FileSystemCDB(ILogger<FileSystemCDB> logger, DirectoryInfo cdbRoot)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(cdbRoot);

        this.logger = logger;
        Name = cdbRoot.Name;
        CdbRoot = cdbRoot;
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public DirectoryInfo CdbRoot { get; }

    /// <inheritdoc/>
    public byte[] ReadFile(string filePathAndName)
    {
        return Array.Empty<byte>();
    }

    /// <inheritdoc/>
    public bool TryReadFile(string filePathAndName, [NotNullWhen(true)] out byte[] content)
    {
        Path.Join(CdbRoot.FullName, filePathAndName);
        FileInfo file = new(Path.Combine(CdbRoot.FullName, filePathAndName));
        if (file.Exists)
        {
            // read it and return
            file.OpenRead().CopyTo(new MemoryStream());
            content = new byte[1];
            return true;
        }
        else
        {
            content = Array.Empty<byte>();
            return false;
        }
    }
}
