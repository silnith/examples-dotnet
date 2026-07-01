using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Silnith.CDB.Visitor;

/// <summary>
/// Walks a directory hierarchy described in 3.3.8.3 DIS Entity Type,
/// and calls a delegate for every leaf directory that matches the expected
/// structure.
/// </summary>
public class DISEntityDirectoryWalker : VisitorBase
{
    private readonly ILogger<DISEntityDirectoryWalker> logger;

    /// <summary>
    /// The constructor intended for dependency injection.
    /// </summary>
    /// <param name="logger">A logger for pertinent information while walking directories.</param>
    public DISEntityDirectoryWalker(ILogger<DISEntityDirectoryWalker> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        this.logger = logger;
    }

    /// <summary>
    /// Called for every leaf directory found in the directory hierarchy.
    /// </summary>
    /// <param name="disEntityType">The DIS Entity Type represented by the directory hierarchy.</param>
    /// <param name="directoryInfo">The leaf directory of the Moving Model hierarchy.</param>
    public delegate void ProcessDISEntityDirectory(DISEntity disEntityType, DirectoryInfo directoryInfo);

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
    public void WalkDirectories(DirectoryInfo dir, ProcessDISEntityDirectory processMovingModelDirectory)
    {
        foreach (DirectoryInfo kindDir in dir.EnumerateDirectories("*", enumerationOptions))
        {
            Match kindDirectoryMatch = DISEntity.ParentDirectoryPattern.Match(kindDir.Name);
            if (!kindDirectoryMatch.Success)
            {
                logger.LogTrace("{Directory} is not the first level of a DIS Entity.  Skipping.",
                    kindDir);
                continue;
            }
            int kindFromDirectory = int.Parse(kindDirectoryMatch.Groups["code"].Value, CultureInfo.InvariantCulture);
            string kindName = kindDirectoryMatch.Groups["name"].Value;

            foreach (DirectoryInfo domainDir in kindDir.EnumerateDirectories("*", enumerationOptions))
            {
                Match domainDirectoryMatch = DISEntity.ParentDirectoryPattern.Match(domainDir.Name);
                if (!domainDirectoryMatch.Success)
                {
                    logger.LogTrace("{Directory} is not the second level of a DIS Entity.  Skipping.",
                        domainDir);
                    continue;
                }
                int domainFromDirectory = int.Parse(domainDirectoryMatch.Groups["code"].Value, CultureInfo.InvariantCulture);
                string domainName = domainDirectoryMatch.Groups["name"].Value;

                foreach (DirectoryInfo countryDir in domainDir.EnumerateDirectories("*", enumerationOptions))
                {
                    Match countryDirectoryMatch = DISEntity.ParentDirectoryPattern.Match(countryDir.Name);
                    if (!countryDirectoryMatch.Success)
                    {
                        logger.LogTrace("{Directory} is not the third level of a DIS Entity.  Skipping.",
                            countryDir);
                        continue;
                    }
                    int countryFromDirectory = int.Parse(countryDirectoryMatch.Groups["code"].Value, CultureInfo.InvariantCulture);
                    string countryName = countryDirectoryMatch.Groups["name"].Value;

                    foreach (DirectoryInfo categoryDir in countryDir.EnumerateDirectories("*", enumerationOptions))
                    {
                        Match categoryDirectoryMatch = DISEntity.ParentDirectoryPattern.Match(categoryDir.Name);
                        if (!categoryDirectoryMatch.Success)
                        {
                            logger.LogTrace("{Directory} is not the fourth level of a DIS Entity.  Skipping.",
                                categoryDir);
                            continue;
                        }
                        int categoryFromDirectory = int.Parse(categoryDirectoryMatch.Groups["code"].Value, CultureInfo.InvariantCulture);
                        string categoryName = categoryDirectoryMatch.Groups["name"].Value;

                        foreach (DirectoryInfo disDirectory in categoryDir.EnumerateDirectories("*", enumerationOptions))
                        {
                            Match disMatch = DISEntity.DirectoryPattern.Match(disDirectory.Name);
                            if (!disMatch.Success)
                            {
                                logger.LogTrace("{Directory} is not the fifth level of a DIS Entity.  Skipping.",
                                    disDirectory);
                                continue;
                            }
                            DISEntity disEntity = DISEntity.FromDirectoryMatch(disMatch);

                            // We could define error behaviors such as continue, skip, throw.
                            if (kindFromDirectory != disEntity.Kind)
                            {
                                logger.LogError("Directory level 1 {DirectoryKind} does not match directory level 5 {CodeKind}.",
                                    kindFromDirectory, disEntity.Kind);
                            }
                            if (domainFromDirectory != disEntity.Domain)
                            {
                                logger.LogError("Directory level 2 {DirectoryDomain} does not match directory level 5 {CodeDomain}.",
                                    domainFromDirectory, disEntity.Domain);
                            }
                            if (countryFromDirectory != disEntity.Country)
                            {
                                logger.LogError("Directory level 3 {DirectoryCountry} does not match directory level 5 {CodeCountry}.",
                                    countryFromDirectory, disEntity.Country);
                            }
                            if (categoryFromDirectory != disEntity.Category)
                            {
                                logger.LogError("Directory level 4 {DirectoryCategory} does not match directory level 5 {CodeCategory}.",
                                    categoryFromDirectory, disEntity.Category);
                            }

                            logger.LogTrace("Visiting directory for {DISEntity}", disEntity);

                            processMovingModelDirectory(disEntity, disDirectory);
                        }
                    }
                }
            }
        }
    }
}
