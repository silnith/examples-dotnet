using Microsoft.Extensions.Logging;

namespace Silnith.CDB;

/// <summary>
/// Visits all the files in the CDB Metadata directory.
/// </summary>
/// <remarks>
/// <para>
/// See OGC CDB Core Standard: Volume 1,
/// Section 3.1.1. Metadata Directory
/// </para>
/// </remarks>
public class MetadataVisitor : Visitor
{
    /// <summary>
    /// The metadata files defined in the standard.
    /// Also recognized are files whose names begin with the prefix "Lights_".
    /// </summary>
    private static readonly SortedSet<string> recognizedMetadata = new()
    {
        "Global_Spatial",
        "Datasets",
        "Lights",
        "Model_Components",
        "Materials",
        "Defaults",
        "Version",
        "CDB_Attributes",
        "Geomatics_Attributes",
        "Vendor_Attributes",
        "Configuration",
    };

    private readonly ILogger<MetadataVisitor> logger;

    /// <summary>
    /// A constructor intended for dependency injection.
    /// </summary>
    /// <param name="logger">A logger.</param>
    public MetadataVisitor(ILogger<MetadataVisitor> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        this.logger = logger;
    }

    public delegate void MetadataFileVisitor(string name, string extension, FileInfo file);

    /// <summary>
    /// Walks the Metadata directory and visits all files.
    /// </summary>
    /// <param name="metadataDir">The metadata directory.</param>
    /// <param name="visitMetadataFile">The action to take for each metadata file.</param>
    public void WalkMetadata(DirectoryInfo metadataDir, MetadataFileVisitor visitMetadataFile)
    {
        // No reason to enumerate child directories, just files.
        foreach (var file in metadataDir.EnumerateFiles("*", enumerationOptions))
        {
            string name = file.Name.Remove(file.Name.Length - file.Extension.Length);
            string extension = file.Extension.Substring(1);

            if (!recognizedMetadata.Contains(name))
            {
                logger.LogWarning("Unrecognized Metadata {File}", name);
            }

            visitMetadataFile(name, extension, file);
        }
    }
}
