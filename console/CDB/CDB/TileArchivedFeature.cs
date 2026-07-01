using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Silnith.CDB;

/// <summary>
/// The name of a feature code entry in an archived tiled dataset file.
/// </summary>
/// <param name="LatitudeValue">The latitude.</param>
/// <param name="LongitudeValue">The longitude.</param>
/// <param name="DatasetValue">The dataset.
/// This should be one of: (300, 302, 303, 305, 307)
/// GSModelGeometry
/// GSModelInteriorGeometry
/// GSModelGeometry
/// GSModelInteriorGeometry</param>
/// <param name="ComponentSelector1">Component selector 1.</param>
/// <param name="ComponentSelector2">Component selector 2.</param>
/// <param name="Level">The level of detail.</param>
/// <param name="Up">The up reference.</param>
/// <param name="Right">The right reference.</param>
/// <param name="FeatureCode">The feature code.</param>
/// <param name="FeatureSubcode">The feature subcode.</param>
/// <param name="Name">The name of the model.</param>
/// <param name="FileType">The file type.</param>
public record TileArchivedFeature(Latitude LatitudeValue,
    Longitude LongitudeValue,
    Dataset DatasetValue,
    [property: Range(0, 999)] int ComponentSelector1,
    [property: Range(0, 999)] int ComponentSelector2,
    LevelOfDetail Level,
    int Up,
    int Right,
    FeatureCode FeatureCode,
    [property: Range(0, 999)] int FeatureSubcode,
    string Name,
    string FileType)
{
    /// <summary>
    /// The pattern for filenames in the tiled dataset directory hierarchy.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Capture Group</term><description>Meaning</description></listheader>
    /// <item><term>north_south</term><description>"N" or "S"</description></item>
    /// <item><term>latitude</term><description>Parseable as an integer.</description></item>
    /// <item><term>east_west</term><description>"E" or "W"</description></item>
    /// <item><term>longitude</term><description>Parseable as an integer.</description></item>
    /// <item><term>dataset</term><description>Parseable as an integer.</description></item>
    /// <item><term>selector1</term><description>Parseable as an integer.</description></item>
    /// <item><term>selector2</term><description>Parseable as an integer.</description></item>
    /// <item><term>lod_negated</term><description>"C" if the level of detail is negative, otherwise an empty string.</description></item>
    /// <item><term>lod</term><description>Parseable as an integer.</description></item>
    /// <item><term>up</term><description>Parseable as an integer.</description></item>
    /// <item><term>right</term><description>Parseable as an integer.</description></item>
    /// <item><term>feature_category</term><description>The feature category.</description></item>
    /// <item><term>feature_subcategory</term><description>The feature subcategory.</description></item>
    /// <item><term>feature_type</term><description>The feature type code.  Parseable as an integer.</description></item>
    /// <item><term>feature_subcode</term><description>Parseable as an integer.</description></item>
    /// <item><term>model_name</term><description>The name of the model.</description></item>
    /// <item><term>ext</term><description>The file extension.</description></item>
    /// </list>
    /// </remarks>
    public static Regex ArchivedFilenamePattern
    {
        get;
    } = new(@"^(?<north_south>[NS])(?<latitude>\d{2})(?<east_west>[EW])(?<longitude>\d{3})_D(?<dataset>\d{3})_S(?<selector1>\d{3})_T(?<selector2>\d{3})_L(?<lod_negated>C?)(?<lod>\d{2})_U(?<up>\d+)_R(?<right>\d+)_(?<feature_category>[A-Z])(?<feature_subcategory>[A-Z])(?<feature_type>\d{3})_(?<feature_subcode>\d{3})_(?<model_name>[^.]+)\.(?<ext>[^.]+)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    /// <summary>
    /// Extracts the capture groups from <see cref="ArchivedFilenamePattern"/>
    /// and converts them into a <see cref="TileArchivedFeature"/>.
    /// </summary>
    /// <param name="match">A successful match from <see cref="ArchivedFilenamePattern"/>.</param>
    /// <returns>The tile representing the filename details.</returns>
    public static TileArchivedFeature FromArchivedFilenameMatch(Match match)
    {
        return new(
            Latitude.FromRegexMatch(match.Groups["north_south"].Value, match.Groups["latitude"].Value),
            Longitude.FromRegexMatch(match.Groups["east_west"].Value, match.Groups["longitude"].Value),
            new Dataset(int.Parse(match.Groups["dataset"].Value, CultureInfo.InvariantCulture)),
            int.Parse(match.Groups["selector1"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["selector2"].Value, CultureInfo.InvariantCulture),
            LevelOfDetail.FromRegexMatch(match.Groups["lod_negated"].Value, match.Groups["lod"].Value),
            int.Parse(match.Groups["up"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["right"].Value, CultureInfo.InvariantCulture),
            new FeatureCode(
                match.Groups["feature_category"].Value,
                match.Groups["feature_subcategory"].Value,
                int.Parse(match.Groups["feature_type"].Value, CultureInfo.InvariantCulture)),
            int.Parse(match.Groups["feature_subcode"].Value, CultureInfo.InvariantCulture),
            match.Groups["model_name"].Value,
            match.Groups["ext"].Value);
    }

    /// <summary>
    /// The tile unarchived feature file name.
    /// </summary>
    public string Filename => $"{LatitudeValue.Code}{LongitudeValue.Code}_D{DatasetValue.Value:D3}_S{ComponentSelector1:D3}_T{ComponentSelector2:D3}_{Level.Code}_U{Up:D}_R{Right:D}_{FeatureCode.Code}_{FeatureSubcode:D3}_{Name}.{FileType}";
}
