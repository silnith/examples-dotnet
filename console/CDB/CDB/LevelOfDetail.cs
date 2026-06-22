using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Silnith.CDB;

/// <summary>
/// A distinct type for representing a level of detail.
/// </summary>
/// <param name="Value">The level of detail.</param>
public record LevelOfDetail([Range(-10, 23)] int Value)
{
    /// <summary>
    /// The pattern for levels of detail as they are used in model geometry directories.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Capture Group</term><description>Meaning</description></listheader>
    /// <item><term>negated</term><description>"C" for negative level of detail, empty otherwise.</description></item>
    /// <item><term>level</term><description>Parseable as an integer.</description></item>
    /// </list>
    /// </remarks>
    public static Regex ModelGeometryDirectoryPattern
    {
        get;
    } = new(@"^L(?<negated>C?)(?<level>\d{2})$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    /// <summary>
    /// Returns a level of detail object by extracting the captured groups from
    /// a match against <see cref="ModelGeometryDirectoryPattern"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This expects capture groups exactly matching
    /// <c>(?&lt;negated&gt;C?)</c> and <c>(?&lt;level&gt;\d{2})</c>.
    /// </para>
    /// </remarks>
    /// <param name="match">The regular expression match object.</param>
    /// <returns>A level of detail object.</returns>
    public static LevelOfDetail FromModelGeometryDirectoryMatch(Match match)
    {
        string negated = match.Groups["negated"].Value;
        int level = int.Parse(match.Groups["level"].Value, CultureInfo.InvariantCulture);
        return new(negated switch
        {
            "C" or "c" => -level,
            _ => level,
        });
    }

    /// <summary>
    /// The pattern for the tiled dataset level of detail directory that stores
    /// coarse levels of detail, the negative levels.
    /// </summary>
    /// <remarks>
    /// <para>
    /// There are no capture groups.
    /// </para>
    /// </remarks>
    public static Regex TiledDatasetCoarsePattern
    {
        get;
    } = new("^LC$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    /// <summary>
    /// The pattern for the tiled dataset level of detail directory that stores
    /// positive levels of detail.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Capture Group</term><description>Meaning</description></listheader>
    /// <item><term>level</term><description>Parseable as an integer.</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="FromTiledDatasetDirectoryMatch(Match)"/>
    public static Regex TiledDatasetDirectoryPattern
    {
        get;
    } = new(@"^L(?<level>\d{2})$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    /// <summary>
    /// Returns a level of detail object by extracting the captured groups from
    /// a match against <see cref="TiledDatasetDirectoryPattern"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This expects a capture group exactly matching
    /// <c>(?&lt;level&gt;\d{2})</c>.
    /// </para>
    /// </remarks>
    /// <param name="match">The regular expression match object.</param>
    /// <returns>A level of detail object.</returns>
    public static LevelOfDetail FromTiledDatasetDirectoryMatch(Match match)
    {
        return new(int.Parse(match.Groups["level"].Value, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Returns a level of detail object based on the capture group for a
    /// regular expression defined as <c>L(?&lt;negated&gt;C?)(?&lt;level&gt;\d{2})</c>
    /// or something equivalent.
    /// </summary>
    /// <param name="negated">The value captured by <c>(?&lt;negated&gt;C?)</c>.</param>
    /// <param name="level">The value captured by <c>(?&lt;level&gt;\d{2})</c>.</param>
    /// <returns>A level of detail object.</returns>
    public static LevelOfDetail FromRegexMatch(string negated, string level)
    {
        return FromRegexMatch(negated, int.Parse(level, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Returns a level of detail object based on the capture group for a
    /// regular expression defined as <c>L(?&lt;negated&gt;C?)(?&lt;level&gt;\d{2})</c>
    /// or something equivalent.
    /// </summary>
    /// <param name="negated">The value captured by <c>(?&lt;negated&gt;C?)</c>.</param>
    /// <param name="level">The value captured by <c>(?&lt;level&gt;\d{2})</c>.</param>
    /// <returns>A level of detail object.</returns>
    public static LevelOfDetail FromRegexMatch(string negated, int level)
    {
        return negated switch
        {
            "C" or "c" => new(-level),
            _ => new(level),
        };
    }
}
