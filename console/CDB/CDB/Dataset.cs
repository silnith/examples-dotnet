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
    /// A pattern for datasets as they are found in filenames.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Capture Group</term><description>Meaning</description></listheader>
    /// <item><term>dataset</term><description>Parseable as an integer.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FromRegexMatch(string)"/>
    public static Regex DatasetInFilenamePattern
    {
        get;
    } = new(@"D(?<dataset>\d{3})",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

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
    public static Regex TiledDatasetDirectoryPattern
    {
        get;
    } = new(@"^(?<dataset>\d{3})_(?<name>.+)$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    /// <summary>
    /// Returns a dataset object by extracting the capture groups from
    /// a match against <see cref="TiledDatasetDirectoryPattern"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This expects capture groups exactly matching
    /// <c>(?&lt;dataset&gt;\d{3})</c> and <c>(?&lt;name&gt;.+)</c>.
    /// </para>
    /// </remarks>
    /// <param name="match">The regular expression match object.</param>
    /// <returns>A latitude object.</returns>
    public static Dataset FromTiledDatasetDirectoryMatch(Match match)
    {
        return new(int.Parse(match.Groups["dataset"].Value, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Returns a dataset object based on the capture groups for a
    /// regular expression defined as <c>(?&lt;dataset&gt;\d{3})_(?&lt;name&gt;.+)</c>
    /// or something equivalent.
    /// </summary>
    /// <param name="dataset">The value captured by <c>(?&lt;dataset&gt;\d{3})</c>.</param>
    /// <param name="name">The value captured by <c>(?&lt;name&gt;.+)</c>.</param>
    /// <returns>A dataset object.</returns>
    /// <seealso cref="TiledDatasetDirectoryPattern"/>
    public static Dataset FromRegexMatch(string dataset, string name)
    {
        return FromRegexMatch(int.Parse(dataset, CultureInfo.InvariantCulture), name);
    }

    /// <summary>
    /// Returns a dataset object based on the capture groups for a
    /// regular expression defined as <c>(?&lt;dataset&gt;\d{3})_(?&lt;name&gt;.+)</c>
    /// or something equivalent.
    /// </summary>
    /// <param name="dataset">The value captured by <c>(?&lt;dataset&gt;\d{3})</c>.</param>
    /// <param name="name">The value captured by <c>(?&lt;name&gt;.+)</c>.</param>
    /// <returns>A dataset object.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "This may be used in the near future.")]
    public static Dataset FromRegexMatch(int dataset, string name)
    {
        return new(dataset);
    }

    /// <summary>
    /// Returns a dataset object based on the capture groups for a
    /// regular expression defined as <c>D(?&lt;dataset&gt;\d{3})</c>
    /// or something equivalent.
    /// </summary>
    /// <param name="dataset">The value captured by <c>(?&lt;dataset&gt;\d{3})</c>.</param>
    /// <returns>A dataset object.</returns>
    /// <seealso cref="DatasetInFilenamePattern"/>
    public static Dataset FromRegexMatch(string dataset)
    {
        return FromRegexMatch(int.Parse(dataset, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Returns a dataset object based on the capture groups for a
    /// regular expression defined as <c>D(?&lt;dataset&gt;\d{3})</c>
    /// or something equivalent.
    /// </summary>
    /// <param name="dataset">The value captured by <c>(?&lt;dataset&gt;\d{3})</c>.</param>
    /// <returns>A dataset object.</returns>
    /// <seealso cref="DatasetInFilenamePattern"/>
    public static Dataset FromRegexMatch(int dataset)
    {
        return new(dataset);
    }
}
