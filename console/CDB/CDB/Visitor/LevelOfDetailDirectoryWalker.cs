using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Silnith.CDB.Visitor;

/// <summary>
/// Visits a directory hierarchy for a tiled dataset described in 3.6.2.4. LOD Directory,
/// and calls a delegate for every leaf directory that matches the expected
/// structure.
/// </summary>
public class LevelOfDetailDirectoryWalker : VisitorBase
{
    private readonly ILogger<LevelOfDetailDirectoryWalker> logger;

    /// <summary>
    /// A constructor for dependency injection.
    /// </summary>
    /// <param name="logger">A logger.</param>
    public LevelOfDetailDirectoryWalker(ILogger<LevelOfDetailDirectoryWalker> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        this.logger = logger;
    }

    /// <summary>
    /// Called for every level of detail directory found in the directory hierarchy.
    /// </summary>
    /// <param name="levelOfDetail">The level of detail from the directory hierarchy.
    /// Can be <see langword="null"/> if the level of detail is negative and the hierarchy is of a tiled dataset.</param>
    /// <param name="directory">The leaf directory of the hierarchy corresponding to the level of detail.</param>
    public delegate void VisitLevelOfDetailDirectory(LevelOfDetail? levelOfDetail, DirectoryInfo directory);

    /// <summary>
    /// Walks a directory tree matching that described in the CDB specification
    /// volume 1, Section 3.6.2.4. Directory Level 4 (LOD Directory).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This walks a directory hierarchy conforming to the pattern <c>/LC/</c> or <c>/L00/</c>.
    /// </para>
    /// </remarks>
    /// <param name="dir">The directory containing child directories of the form <c>/LC/</c> or <c>/L00/</c>.</param>
    /// <param name="visitDirectory">The action to take for every leaf directory in the directory hierarchy.</param>
    public void WalkTiledDatasetDirectories(DirectoryInfo dir, VisitLevelOfDetailDirectory visitDirectory)
    {
        foreach (DirectoryInfo lodDir in dir.EnumerateDirectories("*", enumerationOptions))
        {
            if (LevelOfDetail.TiledDatasetCoarsePattern.Match(lodDir.Name).Success)
            {
                visitDirectory(null, lodDir);
            }
            else
            {
                Match lodMatch = LevelOfDetail.TiledDatasetDirectoryPattern.Match(lodDir.Name);
                if (lodMatch.Success)
                {
                    LevelOfDetail levelOfDetail = LevelOfDetail.FromTiledDatasetDirectoryMatch(lodMatch);
                    visitDirectory(levelOfDetail, lodDir);
                }
                else
                {
                    logger.LogTrace("{Directory} is not a Level of Detail directory.  Skipping.",
                        lodDir);
                    continue;
                }
            }
        }
    }

    /// <summary>
    /// Walks a directory tree matching that described in the CDB specification
    /// volume 1, Section 3.4.1.2.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This walks a directory hierarchy conforming to the pattern <c>/LC00/</c> or <c>/L00/</c>.
    /// </para>
    /// </remarks>
    /// <param name="dir">The directory containing child directories of the form <c>/LC00/</c> or <c>/L00/</c>.</param>
    /// <param name="visitDirectory">The action to take for every leaf directory in the directory hierarchy.</param>
    public void WalkModelGeometryDirectories(DirectoryInfo dir, VisitLevelOfDetailDirectory visitDirectory)
    {
        foreach (DirectoryInfo lodDir in dir.EnumerateDirectories("*", enumerationOptions))
        {
            Match lodMatch = LevelOfDetail.ModelGeometryDirectoryPattern.Match(lodDir.Name);
            if (!lodMatch.Success)
            {
                logger.LogTrace("{Directory} is not a Level of Detail directory.  Skipping.",
                    lodDir);
                continue;
            }
            LevelOfDetail levelOfDetail = LevelOfDetail.FromModelGeometryDirectoryMatch(lodMatch);

            visitDirectory(levelOfDetail, lodDir);
        }
    }
}
