using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
public interface ICDB : IDisposable
{
    /// <summary>
    /// A simple identifier for the CDB data store.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Some implementations of the interface may use this value programatically,
    /// so it should remain stable and consistent for a data store once set.
    /// Each distinct data store should have a unique value.
    /// </para>
    /// </remarks>
    public string Name
    {
        get;
    }

    /// <summary>
    /// The root directory of the CDB data store.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Consumers of the data store cannot assume that the files in the CDB
    /// are directly accessible inside of this directory.  Clients must use
    /// the public API to access files.
    /// </para>
    /// </remarks>
    /// <seealso cref="TryReadFile(string, out byte[])"/>
    public DirectoryInfo CdbRoot
    {
        get;
    }

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
    /// <param name="content">An output variable that will receive the file contents.</param>
    /// <returns><see langword="true"/> if the file was found and its contents returned.</returns>
    public bool TryReadFile(string filePathAndName, [NotNullWhen(true)] out byte[] content);

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
    /// <param name="output">A stream that will receive the file contents.</param>
    /// <returns><see langword="true"/> if the file was found and its contents returned.</returns>
    public bool TryReadFile(string filePathAndName, Stream output);

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
    /// <param name="output">A stream that will receive the file contents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the file was found and its contents returned.</returns>
    public Task<bool> TryReadFileAsync(string filePathAndName, Stream output, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a file out of the CDB and return its contents.
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
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The file contents.</returns>
    /// <exception cref="FileNotFoundException">If the file was not found.</exception>
    public Task<byte[]> ReadFileAsync(string filePathAndName, CancellationToken cancellationToken = default);
}
