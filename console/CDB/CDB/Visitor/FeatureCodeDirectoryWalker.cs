using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Silnith.CDB.Visitor;

/// <summary>
/// Visits a directory hierarchy described in 3.3.8.1. Feature Classification,
/// and calls a delegate for every leaf directory that matches the expected
/// structure.
/// </summary>
public class FeatureCodeDirectoryWalker : VisitorBase
{
    private readonly ILogger<FeatureCodeDirectoryWalker> logger;

    /// <summary>
    /// A constructor for dependency injection.
    /// </summary>
    /// <param name="logger">A logger.</param>
    public FeatureCodeDirectoryWalker(ILogger<FeatureCodeDirectoryWalker> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        this.logger = logger;
    }

    /// <summary>
    /// Called for every leaf directory found in the directory hierarchy.
    /// </summary>
    /// <param name="featureCode">The feature code represented by the directory hierarchy.</param>
    /// <param name="directory">The leaf directory of the hierarchy corresponding to the feature code.</param>
    public delegate void VisitFeatureCodeDirectory(FeatureCode featureCode, DirectoryInfo directory);

    /// <summary>
    /// Walks a directory tree matching that described in the CDB specification
    /// volume 1, Section 3.3.8.1. Feature Classification.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This walks a directory hierarchy conforming to the pattern <c>/A_Category/B_Subcategory/999_Type/</c>.
    /// </para>
    /// </remarks>
    /// <param name="dir">The directory containing child directories of the form <c>/A_Category/B_Subcategory/999_Type/</c>.</param>
    /// <param name="visitFeatureCodeDirectory">The action to take for every leaf directory in the directory hierarchy.</param>
    public void WalkDirectories(DirectoryInfo dir, VisitFeatureCodeDirectory visitFeatureCodeDirectory)
    {
        foreach (DirectoryInfo categoryDirectory in dir.EnumerateDirectories("*", enumerationOptions))
        {
            Match categoryDirectoryMatch = FeatureCode.CategoryDirectoryPattern.Match(categoryDirectory.Name);
            if (!categoryDirectoryMatch.Success)
            {
                logger.LogTrace("{Directory} is not the first level of a FeatureCode.  Skipping.",
                    categoryDirectory);
                continue;
            }

            foreach (DirectoryInfo subcategoryDirectory in categoryDirectory.EnumerateDirectories("*", enumerationOptions))
            {
                Match subcategoryDirectoryMatch = FeatureCode.SubcategoryDirectoryPattern.Match(subcategoryDirectory.Name);
                if (!subcategoryDirectoryMatch.Success)
                {
                    logger.LogTrace("{Directory} is not the second level of a FeatureCode.  Skipping.",
                        subcategoryDirectory);
                    continue;
                }

                foreach (DirectoryInfo typeDirectory in subcategoryDirectory.EnumerateDirectories("*", enumerationOptions))
                {
                    Match typeDirectoryMatch = FeatureCode.TypeDirectoryPattern.Match(typeDirectory.Name);
                    if (!typeDirectoryMatch.Success)
                    {
                        logger.LogTrace("{Directory} is not the third level of a FeatureCode.  Skipping.",
                            typeDirectory);
                        continue;
                    }

                    FeatureCode featureCode = FeatureCode.FromDirectoryPatternMatches(
                        categoryDirectoryMatch,
                        subcategoryDirectoryMatch,
                        typeDirectoryMatch);

                    logger.LogTrace("Visiting directory for feature {Category} {Subcategory} {Type}",
                        categoryDirectoryMatch.Groups["name"].Value,
                        subcategoryDirectoryMatch.Groups["name"].Value,
                        typeDirectoryMatch.Groups["name"].Value);

                    visitFeatureCodeDirectory(featureCode, typeDirectory);
                }
            }
        }
    }
}
