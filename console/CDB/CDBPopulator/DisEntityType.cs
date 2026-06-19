using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace CDBPopulator
{
    public record DisEntityType(int Kind, int Domain, int Country, int Category, int Subcategory, int Specific, int Extra)
    {
        public static Regex KindDirectoryPattern
        {
            get;
        } = new("^(?<kind>\\d{1,3})_(?<name>.+)$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);
        public static Regex DomainDirectoryPattern
        {
            get;
        } = new("^(?<domain>\\d{1,3})_(?<name>.+)$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);
        public static Regex CountryDirectoryPattern
        {
            get;
        } = new("^(?<country>\\d{1,3})_(?<name>.+)$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);
        public static Regex CategoryDirectoryPattern
        {
            get;
        } = new("^(?<category>\\d{1,3})_(?<name>.+)$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);
        public static Regex MovingModelDISCodePattern
        {
            get;
        } = new("^(?<kind>\\d{1,3})_(?<domain>\\d{1,3})_(?<country>\\d{1,3})_(?<category>\\d{1,3})_(?<subcategory>\\d{1,3})_(?<specific>\\d{1,3})_(?<extra>\\d{1,3})$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

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
        /// <param name="action">The action to take for every leaf directory in the directory hierarchy.</param>
        public static void WalkDirectories(DirectoryInfo dir, Action<DisEntityType, DirectoryInfo> action)
        {
            foreach (var kindDir in dir.EnumerateDirectories())
            {
                Match kindDirectoryMatch = KindDirectoryPattern.Match(kindDir.Name);
                if (kindDirectoryMatch.Success)
                {
                    int kindFromDirectory = int.Parse(kindDirectoryMatch.Groups["kind"].Value, CultureInfo.InvariantCulture);
                    _ = kindDirectoryMatch.Groups["name"].Value;

                    foreach (var domainDir in kindDir.EnumerateDirectories())
                    {
                        Match domainDirectoryMatch = DomainDirectoryPattern.Match(domainDir.Name);
                        if (domainDirectoryMatch.Success)
                        {
                            int domainFromDirectory = int.Parse(domainDirectoryMatch.Groups["domain"].Value, CultureInfo.InvariantCulture);
                            _ = domainDirectoryMatch.Groups["name"].Value;

                            foreach (var countryDir in domainDir.EnumerateDirectories())
                            {
                                Match countryDirectoryMatch = CountryDirectoryPattern.Match(countryDir.Name);
                                if (countryDirectoryMatch.Success)
                                {
                                    int countryFromDirectory = int.Parse(countryDirectoryMatch.Groups["country"].Value, CultureInfo.InvariantCulture);
                                    _ = countryDirectoryMatch.Groups["name"].Value;

                                    foreach (var level4Dir in countryDir.EnumerateDirectories())
                                    {
                                        Match categoryDirectoryMatch = CategoryDirectoryPattern.Match(level4Dir.Name);
                                        if (categoryDirectoryMatch.Success)
                                        {
                                            int categoryFromDirectory = int.Parse(categoryDirectoryMatch.Groups["category"].Value, CultureInfo.InvariantCulture);
                                            _ = categoryDirectoryMatch.Groups["name"].Value;

                                            foreach (var disDirectory in level4Dir.EnumerateDirectories())
                                            {
                                                Match disMatch = MovingModelDISCodePattern.Match(disDirectory.Name);
                                                if (disMatch.Success)
                                                {
                                                    int kind = int.Parse(disMatch.Groups["category"].Value, CultureInfo.InvariantCulture);
                                                    int domain = int.Parse(disMatch.Groups["domain"].Value, CultureInfo.InvariantCulture);
                                                    int country = int.Parse(disMatch.Groups["country"].Value, CultureInfo.InvariantCulture);
                                                    int category = int.Parse(disMatch.Groups["category"].Value, CultureInfo.InvariantCulture);
                                                    int subcategory = int.Parse(disMatch.Groups["subcategory"].Value, CultureInfo.InvariantCulture);
                                                    int specific = int.Parse(disMatch.Groups["specific"].Value, CultureInfo.InvariantCulture);
                                                    int extra = int.Parse(disMatch.Groups["extra"].Value, CultureInfo.InvariantCulture);

                                                    if (kindFromDirectory != kind
                                                        || domainFromDirectory != domain
                                                        || countryFromDirectory != country
                                                        || categoryFromDirectory != category)
                                                    {
                                                        // TODO: Log error.
                                                    }

                                                    DisEntityType disEntityType = new(kind, domain, country, category, subcategory, specific, extra);

                                                    action(disEntityType, disDirectory);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public string MovingModelDisCode => $"{Kind:D}_{Domain:D}_{Country:D}_{Category:D}_{Subcategory:D}_{Specific:D}_{Extra:D}";

        public string GetHierarchy()
        {
            return Path.Combine($"{Kind}_Name", $"{Domain}_Name", $"{Country}_Name", $"{Category}_Name",
                $"{Kind}_{Domain}_{Country}_{Category}_{Subcategory}_{Specific}_{Extra}");
        }
    }
}
