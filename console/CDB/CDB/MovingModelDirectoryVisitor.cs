using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Silnith.CDB;

/// <summary>
/// Visits a directory hierarchy described in 3.3.8.3 DIS Entity Type,
/// and calls a delegate for every leaf directory that matches the expected
/// structure.
/// </summary>
public class MovingModelDirectoryVisitor
{
    /// <summary>
    /// The pattern for the first directory defined in 3.3.8.3 DIS Entity Type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// See Volume 2 CDB Core: Model and Physical Structure Annexes.
    /// The field names are defined in Annex M.
    /// </para>
    /// <list type="table">
    /// <listheader><term>Capture Group</term><description>Meaning</description></listheader>
    /// <item><term>kind</term><description>Parseable as an integer.</description></item>
    /// <item><term>name</term><description>The name of the kind.</description></item>
    /// </list>
    /// </remarks>
    private static Regex KindDirectoryPattern
    {
        get;
    } = new("^(?<kind>\\d{1,3})_(?<name>.+)$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    /// <summary>
    /// The pattern for the second directory defined in 3.3.8.3 DIS Entity Type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// See Volume 2 CDB Core: Model and Physical Structure Annexes.
    /// The field names are defined in Annex M.
    /// </para>
    /// <list type="table">
    /// <listheader><term>Capture Group</term><description>Meaning</description></listheader>
    /// <item><term>domain</term><description>Parseable as an integer.</description></item>
    /// <item><term>name</term><description>The name of the domain.</description></item>
    /// </list>
    /// </remarks>
    private static Regex DomainDirectoryPattern
    {
        get;
    } = new("^(?<domain>\\d{1,3})_(?<name>.+)$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    /// <summary>
    /// The pattern for the second directory defined in 3.3.8.3 DIS Entity Type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// See Volume 2 CDB Core: Model and Physical Structure Annexes.
    /// The field names are defined in Annex M.
    /// </para>
    /// <list type="table">
    /// <listheader><term>Capture Group</term><description>Meaning</description></listheader>
    /// <item><term>country</term><description>Parseable as an integer.</description></item>
    /// <item><term>name</term><description>The name of the country.</description></item>
    /// </list>
    /// </remarks>
    private static Regex CountryDirectoryPattern
    {
        get;
    } = new("^(?<country>\\d{1,3})_(?<name>.+)$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    /// <summary>
    /// The pattern for the second directory defined in 3.3.8.3 DIS Entity Type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// See Volume 2 CDB Core: Model and Physical Structure Annexes.
    /// The field names are defined in Annex M.
    /// </para>
    /// <list type="table">
    /// <listheader><term>Capture Group</term><description>Meaning</description></listheader>
    /// <item><term>category</term><description>Parseable as an integer.</description></item>
    /// <item><term>name</term><description>The name of the category.</description></item>
    /// </list>
    /// </remarks>
    private static Regex CategoryDirectoryPattern
    {
        get;
    } = new("^(?<category>\\d{1,3})_(?<name>.+)$",
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
    private static Regex MovingModelDISCodePattern
    {
        get;
    } = new("^(?<kind>\\d{1,3})_(?<domain>\\d{1,3})_(?<country>\\d{1,3})_(?<category>\\d{1,3})_(?<subcategory>\\d{1,3})_(?<specific>\\d{1,3})_(?<extra>\\d{1,3})$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    private readonly ILogger<MovingModelDirectoryVisitor> logger;

    /// <summary>
    /// The constructor intended for dependency injection.
    /// </summary>
    /// <param name="logger">A logger for pertinent information while walking directories.</param>
    public MovingModelDirectoryVisitor(ILogger<MovingModelDirectoryVisitor> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        this.logger = logger;
    }

    /// <summary>
    /// The options for how to enumerate directory entries.
    /// This specifies case insensitive matching using simple wildcards, no recursion.
    /// </summary>
    private EnumerationOptions EnumerationOptions
    {
        get;
    } = new()
    {
        MatchCasing = MatchCasing.CaseInsensitive,
        MatchType = MatchType.Simple,
        RecurseSubdirectories = false,
        ReturnSpecialDirectories = false,
    };

    /// <summary>
    /// Called for every leaf directory found in the directory hierarchy.
    /// </summary>
    /// <param name="disEntityType">The DIS Entity Type represented by the directory hierarchy.</param>
    /// <param name="directoryInfo">The leaf directory of the Moving Model hierarchy.</param>
    public delegate void ProcessMovingModelDirectory(DisEntityType disEntityType, DirectoryInfo directoryInfo);

    /// <summary>
    /// Walks a directory tree matching that described in the CDB specification
    /// volume 1, Section 3.3.8.3. DIS Entity Type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This walks a directory hierarchy conforming to the pattern <c>/1_Kind/2_Domain/3_Country/4_Category/1_2_3_4_5_6_7/</c>.
    /// </para>
    /// </remarks>
    /// <param name="dir">The root directory containing child directories of the form <c>/1_Kind/2_Domain/3_Country/4_Category/1_2_3_4_5_6_7/</c>.</param>
    /// <param name="processMovingModelDirectory">The action to take for every leaf directory in the directory hierarchy.</param>
    public void WalkDirectories(DirectoryInfo dir, ProcessMovingModelDirectory processMovingModelDirectory)
    {
        foreach (var kindDir in dir.EnumerateDirectories("*", EnumerationOptions))
        {
            Match kindDirectoryMatch = KindDirectoryPattern.Match(kindDir.Name);
            if (!kindDirectoryMatch.Success)
            {
                continue;
            }
            int kindFromDirectory = int.Parse(kindDirectoryMatch.Groups["kind"].Value, CultureInfo.InvariantCulture);
            string kindName = kindDirectoryMatch.Groups["name"].Value;

            foreach (var domainDir in kindDir.EnumerateDirectories("*", EnumerationOptions))
            {
                Match domainDirectoryMatch = DomainDirectoryPattern.Match(domainDir.Name);
                if (!domainDirectoryMatch.Success)
                {
                    continue;
                }
                int domainFromDirectory = int.Parse(domainDirectoryMatch.Groups["domain"].Value, CultureInfo.InvariantCulture);
                string domainName = domainDirectoryMatch.Groups["name"].Value;

                foreach (var countryDir in domainDir.EnumerateDirectories("*", EnumerationOptions))
                {
                    Match countryDirectoryMatch = CountryDirectoryPattern.Match(countryDir.Name);
                    if (!countryDirectoryMatch.Success)
                    {
                        continue;
                    }
                    int countryFromDirectory = int.Parse(countryDirectoryMatch.Groups["country"].Value, CultureInfo.InvariantCulture);
                    string countryName = countryDirectoryMatch.Groups["name"].Value;

                    foreach (var categoryDir in countryDir.EnumerateDirectories("*", EnumerationOptions))
                    {
                        Match categoryDirectoryMatch = CategoryDirectoryPattern.Match(categoryDir.Name);
                        if (!categoryDirectoryMatch.Success)
                        {
                            continue;
                        }
                        int categoryFromDirectory = int.Parse(categoryDirectoryMatch.Groups["category"].Value, CultureInfo.InvariantCulture);
                        string categoryName = categoryDirectoryMatch.Groups["name"].Value;

                        foreach (var disDirectory in categoryDir.EnumerateDirectories("*", EnumerationOptions))
                        {
                            Match disMatch = MovingModelDISCodePattern.Match(disDirectory.Name);
                            if (!disMatch.Success)
                            {
                                continue;
                            }
                            int kind = int.Parse(disMatch.Groups["category"].Value, CultureInfo.InvariantCulture);
                            int domain = int.Parse(disMatch.Groups["domain"].Value, CultureInfo.InvariantCulture);
                            int country = int.Parse(disMatch.Groups["country"].Value, CultureInfo.InvariantCulture);
                            int category = int.Parse(disMatch.Groups["category"].Value, CultureInfo.InvariantCulture);
                            int subcategory = int.Parse(disMatch.Groups["subcategory"].Value, CultureInfo.InvariantCulture);
                            int specific = int.Parse(disMatch.Groups["specific"].Value, CultureInfo.InvariantCulture);
                            int extra = int.Parse(disMatch.Groups["extra"].Value, CultureInfo.InvariantCulture);

                            // We could define error behaviors such as continue, skip, throw.
                            if (kindFromDirectory != kind)
                            {
                                logger.LogError("Directory level 1 {DirectoryKind} does not match directory level 5 {CodeKind}.", kindFromDirectory, kind);
                            }
                            if (domainFromDirectory != domain)
                            {
                                logger.LogError("Directory level 2 {DirectoryDomain} does not match directory level 5 {CodeDomain}.", domain, domainFromDirectory);
                            }
                            if (countryFromDirectory != country)
                            {
                                logger.LogError("Directory level 3 {DirectoryCountry} does not match directory level 5 {CodeCountry}.", countryFromDirectory, country);
                            }
                            if (categoryFromDirectory != category)
                            {
                                logger.LogError("Directory level 4 {DirectoryCategory} does not match directory level 5 {CodeCategory}.", categoryFromDirectory, category);
                            }

                            logger.LogTrace("Visiting ");

                            logger.LogTrace("Visiting directory for DIS Entity Type {Kind} {Domain} {Country} {Category} {Subcategory} {Specific} {Extra}",
                                kindName, domainName, countryName, categoryName, subcategory, specific, extra);

                            DisEntityType disEntityType = new(kind, domain, country, category, subcategory, specific, extra);

                            processMovingModelDirectory(disEntityType, disDirectory);
                        }
                    }
                }
            }
        }
    }
}
