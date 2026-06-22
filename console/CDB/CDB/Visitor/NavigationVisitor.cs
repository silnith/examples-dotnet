using Microsoft.Extensions.Logging;

namespace Silnith.CDB.Visitor;

/// <summary>
/// Walks the global Navigation datasets.
/// </summary>
public class NavigationVisitor : VisitorBase
{
    private readonly ILogger<NavigationVisitor> logger;

    /// <summary>
    /// Walks the Navigation datasets and visits all recognized files.
    /// </summary>
    /// <remarks>
    /// <para>
    /// See OGC CDB Core Standard: Volume 1,
    /// Section 3.7. Navigation Library Dataset
    /// </para>
    /// </remarks>
    /// <param name="cdbDir">The CDB root directory.</param>
    public void VisitNavigationDatasets(DirectoryInfo cdbDir)
    {
        DirectoryInfo navigationDir = new(Path.Combine(cdbDir.FullName, "Navigation"));
        if (!navigationDir.Exists)
        {
            logger.LogTrace("{Directory} does not exist.  Skipping.",
                navigationDir);
            return;
        }
    }
}
