using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Silnith.CDB;

/// <summary>
/// The distinguishing details of a file in the tiled dataset directories.
/// </summary>
/// <remarks>
/// <para>
/// As a general rule, these files seem to either be zip files, or metadata.
/// </para>
/// </remarks>
/// <param name="LatitudeValue">The latitude.</param>
/// <param name="LongitudeValue">The longitude.</param>
/// <param name="DatasetValue">The dataset.</param>
/// <param name="ComponentSelector1">Component selector 1.</param>
/// <param name="ComponentSelector2">Component selector 2.</param>
/// <param name="Level">The level of detail.</param>
/// <param name="Up">The up reference.</param>
/// <param name="Right">The right reference.</param>
/// <param name="FileType">The file type.</param>
public record Tile(
        Latitude LatitudeValue,
        Longitude LongitudeValue,
        Dataset DatasetValue,
        [property: Range(0, 999)] int ComponentSelector1,
        [property: Range(0, 999)] int ComponentSelector2,
        LevelOfDetail Level,
        int Up,
        int Right,
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
    /// <item><term>ext</term><description>The file extension.</description></item>
    /// </list>
    /// </remarks>
    public static Regex TiledDatasetFilenamePattern
    {
        get;
    } = new(@"^(?<north_south>[NS])(?<latitude>\d{2})(?<east_west>[EW])(?<longitude>\d{3})_D(?<dataset>\d{3})_S(?<selector1>\d{3})_T(?<selector2>\d{3})_L(?<lod_negated>C?)(?<lod>\d{2})_U(?<up>\d+)_R(?<right>\d+)\.(?<ext>[^.]+)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    /// <summary>
    /// Extracts the capture groups from <see cref="TiledDatasetFilenamePattern"/>
    /// and converts them into a <see cref="Tile"/>.
    /// </summary>
    /// <param name="match">A successful match from <see cref="TiledDatasetFilenamePattern"/>.</param>
    /// <returns>The tile representing the filename details.</returns>
    public static Tile FromTiledDatasetFilenameMatch(Match match)
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
            match.Groups["ext"].Value);
    }

    /// <summary>
    /// The tile file name.
    /// </summary>
    public string Filename => $"{LatitudeValue.Code}{LongitudeValue.Code}_D{DatasetValue.Value:D3}_S{ComponentSelector1:D3}_T{ComponentSelector2:D3}_{Level.Code}_U{Up:D}_R{Right:D}.{FileType}";
}
