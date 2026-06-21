using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Silnith.CDB;

/// <summary>
/// A distinct type for longitude values.
/// </summary>
/// <param name="Value">The longitude value.</param>
public record Longitude([Range(-180, 179)] int Value)
{
    /// <summary>
    /// A pattern for longitude as it is used in CDB tiled dataset directories.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Capture Group</term><description>Meaning</description></listheader>
    /// <item><term>east_west</term><description>"E" or "W"</description></item>
    /// <item><term>longitude</term><description>Parseable as an integer.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FromRegexMatch(string, string)"/>
    public static Regex TiledDatasetDirectoryPattern
    {
        get;
    } = new(@"^(?<east_west>[EW])(?<longitude>\d{3})$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    /// <summary>
    /// Returns a longitude object by extracting the capture groups from
    /// a match against <see cref="TiledDatasetDirectoryPattern"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This expects capture groups exactly matching
    /// <c>(?&lt;east_west&gt;[EW])</c> and <c>(?&lt;longitude&gt;\d{3})</c>.
    /// </para>
    /// </remarks>
    /// <param name="match">The regular expression match object.</param>
    /// <returns>A longitude object.</returns>
    public static Longitude FromTiledDatasetDirectoryMatch(Match match)
    {
        int longitude = int.Parse(match.Groups["longitude"].Value, CultureInfo.InvariantCulture);
        return new(match.Groups["east_west"].Value switch
        {
            "E" or "e" => longitude,
            "W" or "w" => -longitude,
            _ => throw new ArgumentException($"Does not match the capture groups of {nameof(TiledDatasetDirectoryPattern)}", nameof(match)),
        });
    }

    /// <summary>
    /// Returns a longitude object based on the capture groups for a
    /// regular expression defined as <c>(?&lt;east_west&gt;[EW])(?&lt;longitude&gt;\d{3})</c>
    /// or something equivalent.
    /// </summary>
    /// <param name="eastWest">The value captured by <c>(?&lt;east_west&gt;[EW])</c>.</param>
    /// <param name="longitude">The value captured by <c>(?&lt;longitude&gt;\d{3})</c>.</param>
    /// <returns>A longitude object.</returns>
    public static Longitude FromRegexMatch(string eastWest, string longitude)
    {
        return FromRegexMatch(eastWest, int.Parse(longitude, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Returns a longitude object based on the capture groups for a
    /// regular expression defined as <c>(?&lt;east_west&gt;[EW])(?&lt;longitude&gt;\d{3})</c>
    /// or something equivalent.
    /// </summary>
    /// <param name="eastWest">The value captured by <c>(?&lt;east_west&gt;[EW])</c>.</param>
    /// <param name="longitude">The value captured by <c>(?&lt;longitude&gt;\d{3})</c>.</param>
    /// <returns>A longitude object.</returns>
    public static Longitude FromRegexMatch(string eastWest, int longitude)
    {
        return new(eastWest switch
        {
            "E" or "e" => longitude,
            "W" or "w" => -longitude,
            _ => throw new ArgumentException("Must be E or W.", nameof(eastWest)),
        });
    }

    /// <summary>
    /// The longitude in the <c>E119</c> format, where the first character is
    /// the hemisphere represented as <c>E</c> or <c>W</c>, followed by the
    /// three-digit absolute value of the longitude.
    /// </summary>
    public string Code => Value < 0 ? $"W{-Value:D3}" : $"E{Value:D3}";
}
