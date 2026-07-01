using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Silnith.CDB;

/// <summary>
/// Identifying characteristics of a Geotypical Model Geometry Level of Detail file.
/// </summary>
/// <param name="Dataset">The dataset.</param>
/// <param name="ComponentSelector1">Component selector 1.  The meaning of this is relative to the dataset.</param>
/// <param name="ComponentSelector2">Component selector 2.  The meaning of this is relative to component selector 1.</param>
/// <param name="LevelOfDetail">The level of detail.</param>
/// <param name="FeatureCode">The feature code.</param>
/// <param name="FeatureSubcode">The feature subcode.</param>
/// <param name="Name">The name of the model.</param>
/// <param name="FileType">The file type.</param>
public record GeotypicalModelLod(
        Dataset Dataset,
        [property: Range(0, 999)] int ComponentSelector1,
        [property: Range(0, 999)] int ComponentSelector2,
        LevelOfDetail LevelOfDetail,
        FeatureCode FeatureCode,
        [property: Range(0, 999)] int FeatureSubcode,
        string Name,
        string FileType)
{
    /// <summary>
    /// The pattern based on 3.4.1.2. GTModelGeometry Level of Detail Naming Convention.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Capture Group</term><description>Meaning</description></listheader>
    /// <item><term>dataset</term><description>The dataset code.  Parseable as an integer.</description></item>
    /// <item><term>component_selector_1</term><description>Parseable as an integer.</description></item>
    /// <item><term>component_selector_2</term><description>Parseable as an integer.</description></item>
    /// <item><term>lod_negated</term><description>"C" if the level of detail is negative.</description></item>
    /// <item><term>lod</term><description>Parseable as an integer.</description></item>
    /// <item><term>fc_category</term><description>The feature category.</description></item>
    /// <item><term>fc_subcategory</term><description>The feature subcategory.</description></item>
    /// <item><term>fc_type</term><description>The feature type code.  Parseable as an integer.</description></item>
    /// <item><term>feature_subcode</term><description>Parseable as an integer.</description></item>
    /// <item><term>modl</term><description>The name of the model.</description></item>
    /// <item><term>file_type</term><description>The file type.</description></item>
    /// </list>
    /// </remarks>
    public static Regex FilenamePattern
    {
        get;
    } = new(@"^D(?<dataset>\d{3})_S(?<component_selector_1>\d{3})_T(?<component_selector_2>\d{3})_L(?<lod_negated>C?)(?<lod>\d{2})_(?<fc_category>[A-Z])(?<fc_subcategory>[A-Z])(?<fc_type>\d{3})_(?<feature_subcode>\d{3})_(?<modl>[^.]+)\.(?<file_type>[^.]+)$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    /// <summary>
    /// Extracts the Geotypical Model Geometry Level of Detail from the captured groups of
    /// <see cref="FilenamePattern"/>.
    /// </summary>
    /// <param name="match">A successful match against <see cref="FilenamePattern"/>.</param>
    /// <returns>The Geotypical Model Geometry Level of Detail.</returns>
    public static GeotypicalModelLod FromFilenameMatch(Match match)
    {
        return new(
            new Dataset(int.Parse(match.Groups["dataset"].Value, CultureInfo.InvariantCulture)),
            int.Parse(match.Groups["component_selector_1"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["component_selector_2"].Value, CultureInfo.InvariantCulture),
            new LevelOfDetail(match.Groups["lod_negated"].Value switch
            {
                "C" or "c" => -int.Parse(match.Groups["lod"].Value, CultureInfo.InvariantCulture),
                _ => int.Parse(match.Groups["lod"].Value, CultureInfo.InvariantCulture),
            }),
            new FeatureCode(
                match.Groups["fc_category"].Value,
                match.Groups["fc_subcategory"].Value,
                int.Parse(match.Groups["fc_type"].Value, CultureInfo.InvariantCulture)),
            int.Parse(match.Groups["feature_subcode"].Value, CultureInfo.InvariantCulture),
            match.Groups["modl"].Value,
            match.Groups["file_type"].Value);
    }

    /// <summary>
    /// The geotypical model level of detail file name.
    /// </summary>
    public string Filename => $"D{Dataset.Value:D3}_S{ComponentSelector1:D3}_T{ComponentSelector2:D3}_{LevelOfDetail.Code}_{FeatureCode.Code}_{FeatureSubcode:D3}_{Name}.{FileType}";
}
