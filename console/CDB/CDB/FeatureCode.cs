using System.ComponentModel.DataAnnotations;

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
public record FeatureCode([MaxLength(1)] string Category, [MaxLength(1)] string Subcategory, [Range(0, 999)] int Type)
{
    /// <summary>
    /// The five-character code.
    /// </summary>
    public string Code => $"{Category}{Subcategory}{Type:D3}";
}
