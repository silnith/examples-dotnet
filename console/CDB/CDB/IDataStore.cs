using System.Diagnostics.CodeAnalysis;

namespace Silnith.CDB;

/// <summary>
/// A shared interface for individual instances of a CDB data store.
/// </summary>
/// <remarks>
/// <para>
/// This represents one single storage location as described in the OGC CDB standard.
/// Typically this would be a filesystem hierarchy rooted in a single directory.
/// Alternate implementations are possible, however, that can translate the standard
/// file paths and names into keys for other storage mechanisms.
/// </para>
/// <para>
/// A list of CDB versions would consists of multiple instances of this interface.
/// The file replacement mechanism would involve querying multiple instances of
/// this interface.
/// </para>
/// </remarks>
public interface IDataStore
{
    /// <summary>
    /// A simple name for the CDB data store.
    /// </summary>
    public string Name
    {
        get;
    }

    /// <summary>
    /// The root directory of the CDB data store.
    /// </summary>
    public DirectoryInfo CdbRoot
    {
        get;
    }

    /// <summary>
    /// Reads a file out of the CDB and returns its contents.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <paramref name="filePathAndName"/> should always begin with one of
    /// the known root directories.  These are:
    /// </para>
    /// <list type="bullet">
    /// <item><term><c>/Metadata/</c></term></item>
    /// <item><term><c>/GTModel/</c></term></item>
    /// <item><term><c>/MModel/</c></term></item>
    /// <item><term><c>/Tiles/</c></term></item>
    /// <item><term><c>/Navigation/</c></term></item>
    /// </list>
    /// </remarks>
    /// <param name="filePathAndName">The relative path and filename of the file to read.
    /// The path should be relative to the CDB root.</param>
    /// <returns>The file contents, or an empty array.</returns>
    public byte[] ReadFile(string filePathAndName);

    /// <summary>
    /// Tries to read a file out of the CDB and return its contents.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <paramref name="filePathAndName"/> should always begin with one of
    /// the known root directories.  These are:
    /// </para>
    /// <list type="bullet">
    /// <item><term><c>/Metadata/</c></term></item>
    /// <item><term><c>/GTModel/</c></term></item>
    /// <item><term><c>/MModel/</c></term></item>
    /// <item><term><c>/Tiles/</c></term></item>
    /// <item><term><c>/Navigation/</c></term></item>
    /// </list>
    /// </remarks>
    /// <param name="filePathAndName">The relative path and filename of the file to read.
    /// The path should be relative to the CDB root.</param>
    /// <param name="content">The file contents.</param>
    /// <returns><see langword="true"/> if the file was found and its contents returned.</returns>
    public bool TryReadFile(string filePathAndName, [NotNullWhen(true)] out byte[] content);
}
