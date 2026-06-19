using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace CDBPopulator
{
    public record TextureName(string Name)
    {
        public static Regex PrefixPattern
        {
            get;
        } = new("^(?<prefix>[A-Z0-9])$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

        /// <summary>
        /// Walks a directory tree matching that described in the CDB specification
        /// volume 1, Section 3.3.8.4. Texture Name.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This walks a directory hierarchy conforming to the pattern <c>/H/O/house/</c>.
        /// </para>
        /// </remarks>
        /// <param name="dir">The root directory containing child directories of the form <c>/H/O/house/</c>.</param>
        /// <param name="action">The action to take for every leaf directory in the directory hierarchy.</param>
        public static void WalkDirectories(DirectoryInfo dir, Action<string, DirectoryInfo> action)
        {
            foreach (var level1Dir in dir.EnumerateDirectories())
            {
                Match level1Match = PrefixPattern.Match(level1Dir.Name);
                if (level1Match.Success)
                {
                    var level1Prefix = level1Match.Groups["prefix"].Value;

                    foreach (var level2Dir in level1Dir.EnumerateDirectories())
                    {
                        Match level2Match = PrefixPattern.Match(level2Dir.Name);
                        if (level2Match.Success)
                        {
                            var level2Prefix = level2Match.Groups["prefix"].Value;

                            foreach (var level3Dir in level2Dir.EnumerateDirectories())
                            {
                                string textureName = level3Dir.Name;
                                if (textureName.StartsWith(level1Prefix, true, CultureInfo.InvariantCulture)
                                    && textureName.StartsWith(level1Prefix + level2Prefix, true, CultureInfo.InvariantCulture))
                                {
                                    // Success, proceed.

                                    action(textureName, level3Dir);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
