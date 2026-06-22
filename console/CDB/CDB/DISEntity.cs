using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Silnith.CDB;

/// <summary>
/// The DIS Entity Type as described in OGC CDB 1.2: Section 3.3.8.3 DIS Entity Type.
/// CDB Moving Models use the DIS standard to identify themselves.
/// The semantics of the fields are described in Annex M of
/// Volume 2 CDB Core: Model and Physical Structure Annexes.
/// </summary>
/// <remarks>
/// <para>
/// This is also called the Moving Model DIS Code (MMDC).
/// </para>
/// </remarks>
/// <param name="Kind">The entity kind.  TBD</param>
/// <param name="Domain">The entity domain.  TBD</param>
/// <param name="Country">The entity country.  TBD</param>
/// <param name="Category">The entity category.  TBD</param>
/// <param name="Subcategory">The entity subcategory.  TBD</param>
/// <param name="Specific">The specific entity.  TBD</param>
/// <param name="Extra">Extra classification for the entity.  TBD</param>
public record DISEntity(
    [property: Range(0, 999)] int Kind,
    [property: Range(0, 999)] int Domain,
    [property: Range(0, 999)] int Country,
    [property: Range(0, 999)] int Category,
    [property: Range(0, 999)] int Subcategory,
    [property: Range(0, 999)] int Specific,
    [property: Range(0, 999)] int Extra)
{
    /// <summary>
    /// The pattern for the first four directories defined in 3.3.8.3 DIS Entity Type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// See Volume 2 CDB Core: Model and Physical Structure Annexes.
    /// The field names are defined in Annex M.
    /// </para>
    /// <list type="table">
    /// <listheader><term>Capture Group</term><description>Meaning</description></listheader>
    /// <item><term>code</term><description>Parseable as an integer.</description></item>
    /// <item><term>name</term><description>The name of the category.</description></item>
    /// </list>
    /// </remarks>
    public static Regex ParentDirectoryPattern
    {
        get;
    } = new("^(?<code>\\d{1,3})_(?<name>.+)$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    /// <summary>
    /// The pattern for the fifth directory of the DIS Entity Type
    /// directory hierarchy.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Capture Group</term><description>Meaning</description></listheader>
    /// <item><term>kind</term><description>Parseable as an integer.</description></item>
    /// <item><term>domain</term><description>Parseable as an integer.</description></item>
    /// <item><term>country</term><description>Parseable as an integer.</description></item>
    /// <item><term>category</term><description>Parseable as an integer.</description></item>
    /// <item><term>subcategory</term><description>Parseable as an integer.</description></item>
    /// <item><term>specific</term><description>Parseable as an integer.</description></item>
    /// <item><term>extra</term><description>Parseable as an integer.</description></item>
    /// </list>
    /// </remarks>
    public static Regex DirectoryPattern
    {
        get;
    } = new(@"^(?<kind>\d{1,3})_(?<domain>\d{1,3})_(?<country>\d{1,3})_(?<category>\d{1,3})_(?<subcategory>\d{1,3})_(?<specific>\d{1,3})_(?<extra>\d{1,3})$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    /// <summary>
    /// Returns a DIS Entity by extracting the capture groups from
    /// a match against <see cref="DirectoryPattern"/>.
    /// </summary>
    /// <param name="match">The successful match against <see cref="DirectoryPattern"/>.</param>
    /// <returns>A dataset object.</returns>
    public static DISEntity FromDirectoryMatch(Match match)
    {
        return new(
            int.Parse(match.Groups["category"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["domain"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["country"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["category"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["subcategory"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["specific"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["extra"].Value, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// The Moving Model DIS Code (MMDC) is defined in 5.7.1.3.40.
    /// </summary>
    // TODO: Explain this!
    public string MovingModelDisCode => $"{Kind:D}_{Domain:D}_{Country:D}_{Category:D}_{Subcategory:D}_{Specific:D}_{Extra:D}";
}
