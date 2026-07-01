namespace Silnith.CDB;

/// <summary>
/// Metadata as described in 3.1.1. Metadata Directory.
/// </summary>
/// <param name="Name">The metadata name.</param>
/// <param name="FileType">The file type.</param>
public record Metadata(string Name, string FileType)
{
    /// <summary>
    /// The metadata file name.
    /// </summary>
    public string Filename => $"{Name}.{FileType}";
}
