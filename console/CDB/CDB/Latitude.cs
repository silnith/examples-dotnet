using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Silnith.CDB;

/// <summary>
/// A distinct type for latitude values.
/// </summary>
/// <param name="Value">The latitude value.</param>
public record Latitude([property: Range(-90, 89)] int Value)
{
    /// <summary>
    /// A pattern for latitude as it is used in CDB tiled dataset directories.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Capture Group</term><description>Meaning</description></listheader>
    /// <item><term>north_south</term><description>"N" or "S"</description></item>
    /// <item><term>latitude</term><description>Parseable as an integer.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FromTiledDatasetDirectoryMatch(Match)"/>
    public static Regex TiledDatasetDirectoryPattern
    {
        get;
    } = new(@"^(?<north_south>[NS])(?<latitude>\d{2})$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    /// <summary>
    /// Returns a latitude object by extracting the capture groups from
    /// a match against <see cref="TiledDatasetDirectoryPattern"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This expects capture groups exactly matching
    /// <c>(?&lt;north_south&gt;[NS])</c> and <c>(?&lt;latitude&gt;\d{2})</c>.
    /// </para>
    /// </remarks>
    /// <param name="match">The regular expression match object.</param>
    /// <returns>A latitude object.</returns>
    public static Latitude FromTiledDatasetDirectoryMatch(Match match)
    {
        int latitude = int.Parse(match.Groups["latitude"].Value, CultureInfo.InvariantCulture);
        return new(match.Groups["north_south"].Value switch
        {
            "N" or "n" => latitude,
            "S" or "s" => -latitude,
            _ => throw new ArgumentException($"Does not match the capture groups of {nameof(TiledDatasetDirectoryPattern)}", nameof(match)),
        });
    }

    /// <summary>
    /// Returns a latitude object based on the capture groups for a
    /// regular expression defined as <c>(?&lt;north_south&gt;[NS])(?&lt;latitude&gt;\d{2})</c>
    /// or something equivalent.
    /// </summary>
    /// <param name="northSouth">The value captured by <c>(?&lt;north_south&gt;[NS])</c>.</param>
    /// <param name="latitude">The value captured by <c>(?&lt;latitude&gt;\d{2})</c>.</param>
    /// <returns>A latitude object.</returns>
    /// <seealso cref="TiledDatasetDirectoryPattern"/>
    public static Latitude FromRegexMatch(string northSouth, string latitude)
    {
        return FromRegexMatch(northSouth, int.Parse(latitude, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Returns a latitude object based on the capture groups for a
    /// regular expression defined as <c>(?&lt;north_south&gt;[NS])(?&lt;latitude&gt;\d{2})</c>
    /// or something equivalent.
    /// </summary>
    /// <param name="northSouth">The value captured by <c>(?&lt;north_south&gt;[NS])</c>.</param>
    /// <param name="latitude">The value captured by <c>(?&lt;latitude&gt;\d{2})</c>.</param>
    /// <returns>A latitude object.</returns>
    public static Latitude FromRegexMatch(string northSouth, int latitude)
    {
        return new(northSouth switch
        {
            "N" or "n" => latitude,
            "S" or "s" => -latitude,
            _ => throw new ArgumentException("Must be N or S.", nameof(northSouth)),
        });
    }

    /// <summary>
    /// The latitude in the <c>S34</c> format, where the first character is the
    /// hemisphere represented by <c>N</c> or <c>S</c>, followed by the two-digit
    /// absolute value of the latitude.
    /// </summary>
    public string Code => Value < 0 ? $"S{-Value:D2}" : $"N{Value:D2}";
}
