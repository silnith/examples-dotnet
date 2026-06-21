using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Silnith.CDB;

/// <summary>
/// Visits a directory hierarchy described in 3.3.8.1. Feature Classification,
/// and calls a delegate for every leaf directory that matches the expected
/// structure.
/// </summary>
public class FeatureCodeDirectoryVisitor
{
    /// <summary>
    /// Matches directory names of the form <c>A_Category</c>,
    /// where the <c>A</c> is the first character of a feature code,
    /// and <c>Category</c> is the name of the category identified
    /// by the code.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The capture groups of this pattern are <c>category</c> and <c>name</c>.
    /// </para>
    /// </remarks>
    private static Regex CategoryDirectoryPattern
    {
        get;
    } = new("^(?<category>[A-Z])_(?<name>.+)$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    /// <summary>
    /// Matches directory names of the form <c>B_Subcategory</c>,
    /// where the <c>B</c> is the second character of the feature code,
    /// and <c>Subcategory</c> is the name of the subcategory identified
    /// by the code.  The subcategory is relative to the category.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The capture groups of this pattern are <c>subcategory</c> and <c>name</c>.
    /// </para>
    /// </remarks>
    private static Regex SubcategoryDirectoryPattern
    {
        get;
    } = new("^(?<subcategory>[A-Z])_(?<name>.+)$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    /// <summary>
    /// Matches directory names of the form <c>999_Type</c>,
    /// where the <c>999</c> is the last three characters of the feature code,
    /// and <c>Type</c> is the name of the feature type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The capture groups of this pattern are <c>type</c> and <c>name</c>.
    /// Capture group <c>type</c> can be parsed as a non-negative integer.
    /// </para>
    /// </remarks>
    private static Regex TypeDirectoryPattern
    {
        get;
    } = new("^(?<type>\\d{3})_(?<name>.+)$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    private readonly ILogger<FeatureCodeDirectoryVisitor> logger;

    /// <summary>
    /// A constructor for dependency injection.
    /// </summary>
    /// <param name="logger">A logger.</param>
    public FeatureCodeDirectoryVisitor(ILogger<FeatureCodeDirectoryVisitor> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        this.logger = logger;
    }

    /// <summary>
    /// The options for how to enumerate directory entries.
    /// This specifies case insensitive matching using simple wildcards, no recursion.
    /// </summary>
    private EnumerationOptions EnumerationOptions
    {
        get;
    } = new()
    {
        MatchCasing = MatchCasing.CaseInsensitive,
        MatchType = MatchType.Simple,
        RecurseSubdirectories = false,
        ReturnSpecialDirectories = false,
    };

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
        foreach (DirectoryInfo categoryDirectory in dir.EnumerateDirectories("*", EnumerationOptions))
        {
            Match categoryDirectoryMatch = CategoryDirectoryPattern.Match(categoryDirectory.Name);
            if (!categoryDirectoryMatch.Success)
            {
                continue;
            }
            string categoryCode = categoryDirectoryMatch.Groups["category"].Value;
            string categoryName = categoryDirectoryMatch.Groups["name"].Value;

            foreach (DirectoryInfo subcategoryDirectory in categoryDirectory.EnumerateDirectories("*", EnumerationOptions))
            {
                Match subcategoryDirectoryMatch = SubcategoryDirectoryPattern.Match(subcategoryDirectory.Name);
                if (!subcategoryDirectoryMatch.Success)
                {
                    continue;
                }
                string subcategoryCode = subcategoryDirectoryMatch.Groups["subcategory"].Value;
                string subcategoryName = subcategoryDirectoryMatch.Groups["name"].Value;

                foreach (DirectoryInfo typeDirectory in subcategoryDirectory.EnumerateDirectories("*", EnumerationOptions))
                {
                    Match typeDirectoryMatch = TypeDirectoryPattern.Match(typeDirectory.Name);
                    if (!typeDirectoryMatch.Success)
                    {
                        continue;
                    }
                    int typeCode = int.Parse(typeDirectoryMatch.Groups["type"].Value, CultureInfo.InvariantCulture);
                    string typeName = typeDirectoryMatch.Groups["name"].Value;

                    logger.LogTrace("Visiting directory for feature {Category} {Subcategory} {Type}",
                        categoryName, subcategoryName, typeName);

                    FeatureCode featureCode = new(categoryCode, subcategoryCode, typeCode);

                    visitFeatureCodeDirectory(featureCode, typeDirectory);
                }
            }
        }
    }
}
