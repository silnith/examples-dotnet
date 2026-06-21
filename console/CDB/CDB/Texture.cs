using System.Globalization;
using System.Text.RegularExpressions;

namespace Silnith.CDB;

/// <summary>
/// Identifying characteristics of a texture file.
/// </summary>
/// <param name="Dataset">The dataset.</param>
/// <param name="ComponentSelector1">Component selector 1.  The meaning of this is relative to the dataset.</param>
/// <param name="ComponentSelector2">Component selector 2.  The meaning of this is relative to component selector 1.</param>
/// <param name="TextureName">The texture name.</param>
/// <param name="FileType">The file type.</param>
public record Texture(
        Dataset Dataset,
        int ComponentSelector1,
        int ComponentSelector2,
        string TextureName,
        string FileType)
{
    /// <summary>
    /// The pattern based on 3.4.2.3. GTModelCMT Naming Convention
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Capture Group</term><description>Meaning</description></listheader>
    /// <item><term>dataset</term><description>The dataset code.  Parseable as an integer.</description></item>
    /// <item><term>component_selector_1</term><description>Parseable as an integer.</description></item>
    /// <item><term>component_selector_2</term><description>Parseable as an integer.</description></item>
    /// <item><term>tnam</term><description>The name of the texture.</description></item>
    /// <item><term>file_type</term><description>The file type.</description></item>
    /// </list>
    /// </remarks>
    public static Regex FilenamePattern
    {
        get;
    } = new(@"^D(?<dataset>\d{3})_S(?<component_selector_1>\d{3})_T(?<component_selector_2>\d{3})_(?<tnam>[^.]+)\.(?<file_type>[^.]+)$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    /// <summary>
    /// Extracts the texture from the captured groups of
    /// <see cref="FilenamePattern"/>.
    /// </summary>
    /// <param name="match">A successful match against <see cref="FilenamePattern"/>.</param>
    /// <returns>The texture.</returns>
    public static Texture FromFilenameMatch(Match match)
    {
        return new(
            new Dataset(int.Parse(match.Groups["dataset"].Value, CultureInfo.InvariantCulture)),
            int.Parse(match.Groups["component_selector_1"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["component_selector_2"].Value, CultureInfo.InvariantCulture),
            match.Groups["tnam"].Value,
            match.Groups["file_type"].Value);
    }
}
