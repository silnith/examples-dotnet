using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Silnith.CDB;

/// <summary>
/// A feature code (FC) used to classify features in the CDB standard.
/// </summary>
/// <remarks>
/// <para>
/// The feature code is a five-character code where the first character
/// represents a category of features, the second represents a subcategory
/// of the current category, and the last three characters represent a
/// specific type in the subcategory.
/// </para>
/// <para>
/// The full list of feature codes is available in the <c>Feature_Data_Dictionary.xml</c>
/// file in a CDB Metadata directory.
/// </para>
/// <para>
/// This code implements Section 3.3.8.1 Feature Classification from the CDB standard,
/// volume 1.
/// </para>
/// </remarks>
/// <param name="Category">The feature category.</param>
/// <param name="Subcategory">The feature subcategory of the category.</param>
/// <param name="Type">The feature type in the subcategory.</param>
/// <seealso href="https://github.com/opengeospatial/cdb-volume-1"/>
public record FeatureCode(
    [MaxLength(1)] string Category,
    [MaxLength(1)] string Subcategory,
    [Range(0, 999)] int Type)
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
    public static Regex CategoryDirectoryPattern
    {
        get;
    } = new(@"^(?<category>[A-Z])_(?<name>.+)$",
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
    public static Regex SubcategoryDirectoryPattern
    {
        get;
    } = new(@"^(?<subcategory>[A-Z])_(?<name>.+)$",
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
    public static Regex TypeDirectoryPattern
    {
        get;
    } = new(@"^(?<type>\d{3})_(?<name>.+)$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    /// <summary>
    /// Returns a feature code by extracting the capture groups from
    /// matches against the directory patterns for feature code.
    /// </summary>
    /// <param name="categoryMatch">A successful match against <see cref="CategoryDirectoryPattern"/>.</param>
    /// <param name="subcategoryMatch">A successful match against <see cref="SubcategoryDirectoryPattern"/>.</param>
    /// <param name="typeMatch">A successful match against <see cref="TypeDirectoryPattern"/>.</param>
    /// <returns>A feature code.</returns>
    public static FeatureCode FromDirectoryPatternMatches(Match categoryMatch, Match subcategoryMatch, Match typeMatch)
    {
        return new(
            categoryMatch.Groups["category"].Value,
            subcategoryMatch.Groups["subcategory"].Value,
            int.Parse(typeMatch.Groups["type"].Value, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// The five-character code.
    /// </summary>
    public string Code => $"{Category}{Subcategory}{Type:D3}";
}
