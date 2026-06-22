using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Silnith.CDB;

/// <summary>
/// A distinct type for datasets.
/// </summary>
/// <remarks>
/// <para>
/// Dataset codes are listed in Annex Q of OGC CDB Core: Model and Physical Structure: Informative Annexes.
/// </para>
/// </remarks>
/// <param name="Value">The dataset code.</param>
public record Dataset([Range(0, 999)] int Value)
{
    /// <summary>
    /// A pattern for datasets as they are used in CDB tiled dataset directories.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Capture Group</term><description>Meaning</description></listheader>
    /// <item><term>dataset</term><description>Parseable as an integer.</description></item>
    /// <item><term>name</term><description>The name of the dataset.</description></item>
    /// </list>
    /// </remarks>
    public static Regex DirectoryPattern
    {
        get;
    } = new(@"^(?<dataset>\d{3})_(?<name>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    /// <summary>
    /// Returns a dataset object by extracting the capture groups from
    /// a match against <see cref="DirectoryPattern"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This expects capture groups exactly matching
    /// <c>(?&lt;dataset&gt;\d{3})</c> and <c>(?&lt;name&gt;.+)</c>.
    /// </para>
    /// </remarks>
    /// <param name="match">The successful match against <see cref="DirectoryPattern"/>.</param>
    /// <returns>A dataset object.</returns>
    public static Dataset FromDirectoryMatch(Match match)
    {
        return new(int.Parse(match.Groups["dataset"].Value, CultureInfo.InvariantCulture));
    }
}
