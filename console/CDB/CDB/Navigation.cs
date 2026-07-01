using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Silnith.CDB;

/// <summary>
/// Navigation data as described in 3.7.2. Navigation Data Naming Convention.
/// </summary>
/// <param name="Dataset">The dataset.</param>
/// <param name="ComponentSelector1">Component selector 1.  I still have no idea what these things mean.</param>
/// <param name="ComponentSelector2">Component selector 2.  Whatever that is.</param>
/// <param name="FileType">The file type.</param>
public record Navigation(
    Dataset Dataset,
    [property: Range(0, 999)] int ComponentSelector1,
    [property: Range(0, 999)] int ComponentSelector2,
    string FileType)
{
    /// <summary>
    /// The pattern based on 3.7.2. Navigation Data Naming Convention
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Capture Group</term><description>Meaning</description></listheader>
    /// <item><term>dataset</term><description>The dataset code.  Parseable as an integer.</description></item>
    /// <item><term>component_selector_1</term><description>Parseable as an integer.</description></item>
    /// <item><term>component_selector_2</term><description>Parseable as an integer.</description></item>
    /// <item><term>file_type</term><description>The file type.</description></item>
    /// </list>
    /// </remarks>
    public static Regex FilenamePattern
    {
        get;
    } = new(@"^D(?<dataset>\d{3})_S(?<component_selector_1>\d{3})_T(?<component_selector_2>\d{3})\.(?<file_type>[^.]+)$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    /// <summary>
    /// Extracts the navigation data from the captured groups of
    /// <see cref="FilenamePattern"/>.
    /// </summary>
    /// <param name="match">A successful match against <see cref="FilenamePattern"/>.</param>
    /// <returns>The navigation data.</returns>
    public static Navigation FromFilenameMatch(Match match)
    {
        Dataset dataset = new(int.Parse(match.Groups["dataset"].Value, CultureInfo.InvariantCulture));
        int componentSelector1 = int.Parse(match.Groups["component_selector_1"].Value, CultureInfo.InvariantCulture);
        int componentSelector2 = int.Parse(match.Groups["component_selector_2"].Value, CultureInfo.InvariantCulture);
        string fileType = match.Groups["file_type"].Value;
        return new(dataset, componentSelector1, componentSelector2, fileType);
    }

    /// <summary>
    /// The navigation file name.
    /// </summary>
    public string Filename => $"D{Dataset.Value:D3}_S{ComponentSelector1:D3}_T{ComponentSelector2:D3}.{FileType}";
}
