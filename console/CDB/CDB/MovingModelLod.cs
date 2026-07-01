using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Silnith.CDB;

/// <summary>
/// Identifying characteristics of a Moving Model Geometry file.
/// </summary>
/// <param name="Dataset">The dataset.</param>
/// <param name="ComponentSelector1">Component selector 1.  The meaning of this is relative to the dataset.</param>
/// <param name="ComponentSelector2">Component selector 2.  The meaning of this is relative to component selector 1.</param>
/// <param name="LevelOfDetail">The level of detail.</param>
/// <param name="MMDC">The moving model DIS code.</param>
/// <param name="FileType">The file type.</param>
public record MovingModelLod(
    Dataset Dataset,
    [property: Range(0, 999)] int ComponentSelector1,
    [property: Range(0, 999)] int ComponentSelector2,
    LevelOfDetail LevelOfDetail,
    DISEntity MMDC,
    string FileType)
{
    /// <summary>
    /// The pattern based on 3.5.1.1. MModelGeometry Naming Convention.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Capture Group</term><description>Meaning</description></listheader>
    /// <item><term>dataset</term><description>The dataset code.  Parseable as an integer.</description></item>
    /// <item><term>component_selector_1</term><description>Parseable as an integer.</description></item>
    /// <item><term>component_selector_2</term><description>Parseable as an integer.</description></item>
    /// <item><term>lod_negated</term><description>"C" if the level of detail is negative.</description></item>
    /// <item><term>lod</term><description>Parseable as an integer.</description></item>
    /// <item><term>kind</term><description>Parseable as an integer.</description></item>
    /// <item><term>domain</term><description>Parseable as an integer.</description></item>
    /// <item><term>country</term><description>Parseable as an integer.</description></item>
    /// <item><term>category</term><description>Parseable as an integer.</description></item>
    /// <item><term>subcategory</term><description>Parseable as an integer.</description></item>
    /// <item><term>specific</term><description>Parseable as an integer.</description></item>
    /// <item><term>extra</term><description>Parseable as an integer.</description></item>
    /// <item><term>file_type</term><description>The file type.</description></item>
    /// </list>
    /// </remarks>
    public static Regex FilenamePattern
    {
        get;
    } = new(@"^D(?<dataset>\d{3})_S(?<component_selector_1>\d{3})_T(?<component_selector_2>\d{3})_L(?<lod_negated>C?)(?<lod>\d{2})_(?<kind>\d{1,3})_(?<domain>\d{1,3})_(?<country>\d{1,3})_(?<category>\d{1,3})_(?<subcategory>\d{1,3})_(?<specific>\d{1,3})_(?<extra>\d{1,3})\.(?<file_type>[^.]+)$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    /// <summary>
    /// Extracts the Geotypical Model Geometry from the captured groups of
    /// <see cref="FilenamePattern"/>.
    /// </summary>
    /// <param name="match">A successful match against <see cref="FilenamePattern"/>.</param>
    /// <returns>The Geotypical Model Geometry.</returns>
    public static MovingModelLod FromFilenameMatch(Match match)
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
            new DISEntity(
                int.Parse(match.Groups["kind"].Value, CultureInfo.InvariantCulture),
                int.Parse(match.Groups["domain"].Value, CultureInfo.InvariantCulture),
                int.Parse(match.Groups["country"].Value, CultureInfo.InvariantCulture),
                int.Parse(match.Groups["category"].Value, CultureInfo.InvariantCulture),
                int.Parse(match.Groups["subcategory"].Value, CultureInfo.InvariantCulture),
                int.Parse(match.Groups["specific"].Value, CultureInfo.InvariantCulture),
                int.Parse(match.Groups["extra"].Value, CultureInfo.InvariantCulture)),
            match.Groups["file_type"].Value);
    }

    /// <summary>
    /// The moving model file name.
    /// </summary>
    public string Filename => $"D{Dataset.Value:D3}_S{ComponentSelector1:D3}_T{ComponentSelector2:D3}_{LevelOfDetail.Code}_{MMDC.MovingModelDisCode}.{FileType}";
}
