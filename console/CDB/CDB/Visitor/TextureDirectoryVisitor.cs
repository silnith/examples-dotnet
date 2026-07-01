using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Silnith.CDB.Visitor;

/// <summary>
/// Visits a directory hierarchy described in 3.3.8.4. Texture Name,
/// and calls a delegate for every leaf directory that matches the expected
/// structure.
/// </summary>
public class TextureDirectoryVisitor : VisitorBase
{
    /// <summary>
    /// The pattern for the first two directories in the hierarchy.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Capture Group</term><description>Meaning</description></listheader>
    /// <item><term>prefix</term><description>A single alphanumeric character.</description></item>
    /// </list>
    /// </remarks>
    private static Regex PrefixPattern
    {
        get;
    } = new("^(?<prefix>[A-Z0-9])$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    private readonly ILogger<TextureDirectoryVisitor> logger;

    /// <summary>
    /// A constructor for dependency injection.
    /// </summary>
    /// <param name="logger">A logger.</param>
    public TextureDirectoryVisitor(ILogger<TextureDirectoryVisitor> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        this.logger = logger;
    }

    /// <summary>
    /// Called for every leaf directory found in the directory hierarchy.
    /// </summary>
    /// <param name="textureName">The texture name from the directory hierarchy.</param>
    /// <param name="directoryInfo">The leaf directory of the texture directory hierarchy.</param>
    public delegate void ProcessTextureDirectory(string textureName, DirectoryInfo directoryInfo);

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
    /// <param name="processTextureDirectory">The action to take for every leaf directory in the directory hierarchy.</param>
    public void WalkDirectories(DirectoryInfo dir, ProcessTextureDirectory processTextureDirectory)
    {
        foreach (DirectoryInfo level1Dir in dir.EnumerateDirectories("*", enumerationOptions))
        {
            Match level1Match = Texture.PrefixPattern.Match(level1Dir.Name);
            if (!level1Match.Success)
            {
                logger.LogTrace("{Directory} is not the first level of a texture hierarchy.  Skipping.",
                    level1Dir);
                continue;
            }
            string level1Prefix = level1Match.Groups["prefix"].Value;

            foreach (DirectoryInfo level2Dir in level1Dir.EnumerateDirectories("*", enumerationOptions))
            {
                Match level2Match = Texture.PrefixPattern.Match(level2Dir.Name);
                if (!level2Match.Success)
                {
                    logger.LogTrace("{Directory} is not the second level of a texture hierarchy.  Skipping.",
                        level2Dir);
                    continue;
                }
                string level2Prefix = level2Match.Groups["prefix"].Value;

                foreach (DirectoryInfo textureDir in level2Dir.EnumerateDirectories("*", enumerationOptions))
                {
                    string textureName = textureDir.Name;

                    if (!textureName.StartsWith(level1Prefix, true, CultureInfo.InvariantCulture)
                        || !textureName.StartsWith(level1Prefix + level2Prefix, true, CultureInfo.InvariantCulture))
                    {
                        logger.LogWarning("Unexpected directory in texture directory hierarchy.  {TextureName} does not begin with {Prefix}.",
                            textureName, level1Prefix + level2Prefix);
                    }

                    processTextureDirectory(textureName, textureDir);
                }
            }
        }
    }
}
