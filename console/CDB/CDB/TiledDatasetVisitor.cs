using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Silnith.CDB;

/// <summary>
/// Visits a directory hierarchy described in 3.6.2. Tiled Dataset Directory Structure,
/// and calls a delegate for every file that matches the expected
/// structure and name.
/// </summary>
public class TiledDatasetVisitor
{
    /// <summary>
    /// A pattern that matches level 5 directories.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Capture Group</term><description>Meaning</description></listheader>
    /// <item><term>up</term><description>Parseable as an integer.</description></item>
    /// </list>
    /// </remarks>
    private static Regex UpDirPattern
    {
        get;
    } = new(@"^U(?<up>\d+)$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    private readonly ILogger<TiledDatasetVisitor> logger;

    private readonly LevelOfDetailDirectoryWalker levelOfDetailDirectoryWalker;

    /// <summary>
    /// A constructor for dependency injection.
    /// </summary>
    /// <param name="logger">A logger.</param>
    /// <param name="levelOfDetailDirectoryWalker">A visitor for levels of detail directories.</param>
    public TiledDatasetVisitor(ILogger<TiledDatasetVisitor> logger,
        LevelOfDetailDirectoryWalker levelOfDetailDirectoryWalker)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(levelOfDetailDirectoryWalker);

        this.logger = logger;
        this.levelOfDetailDirectoryWalker = levelOfDetailDirectoryWalker;
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
    /// Called for every file in a tiled dataset directory hierarchy.
    /// </summary>
    /// <param name="tile">The details of the tile extracted from the filename.</param>
    /// <param name="fileInfo">The file.</param>
    public delegate void VisitTiledDatasetFile(Tile tile, FileInfo fileInfo);

    public void VisitFiles(DirectoryInfo dir, VisitTiledDatasetFile visitFile)
    {
        foreach (DirectoryInfo latitudeDir in dir.EnumerateDirectories("*", EnumerationOptions))
        {
            Match latitudeMatch = Latitude.TiledDatasetDirectoryPattern.Match(latitudeDir.Name);
            if (!latitudeMatch.Success)
            {
                continue;
            }
            Latitude latitudeFromDirectory = Latitude.FromTiledDatasetDirectoryMatch(latitudeMatch);

            foreach (DirectoryInfo longitudeDir in latitudeDir.EnumerateDirectories("*", EnumerationOptions))
            {
                Match longitudeMatch = Longitude.TiledDatasetDirectoryPattern.Match(longitudeDir.Name);
                if (!longitudeMatch.Success)
                {
                    continue;
                }
                Longitude longitudeFromDirectory = Longitude.FromTiledDatasetDirectoryMatch(longitudeMatch);

                foreach (DirectoryInfo datasetDir in longitudeDir.EnumerateDirectories("*", EnumerationOptions))
                {
                    Match datasetMatch = Dataset.TiledDatasetDirectoryPattern.Match(datasetDir.Name);
                    if (!datasetMatch.Success)
                    {
                        continue;
                    }
                    Dataset datasetFromDirectory = Dataset.FromTiledDatasetDirectoryMatch(datasetMatch);

                    levelOfDetailDirectoryWalker.WalkTiledDatasetDirectories(datasetDir, (levelOfDetailFromDirectory, lodDir) =>
                    {
                        foreach (DirectoryInfo upDir in lodDir.EnumerateDirectories("*", EnumerationOptions))
                        {
                            Match upMatch = UpDirPattern.Match(upDir.Name);
                            if (!upMatch.Success)
                            {
                                continue;
                            }
                            int upFromDirectory = int.Parse(upMatch.Groups[""].Value, CultureInfo.InvariantCulture);

                            foreach (FileInfo file in upDir.EnumerateFiles("*", EnumerationOptions))
                            {
                                Match fileMatch = Tile.TiledDatasetFilenamePattern.Match(file.Name);
                                if (!fileMatch.Success)
                                {
                                    continue;
                                }
                                Tile tile = Tile.FromTiledDatasetFilenameMatch(fileMatch);

                                if (latitudeFromDirectory != tile.LatitudeValue)
                                {
                                    logger.LogError("Directory level 1 {DirectoryLatitude} does not match file {FileLatitude}.", latitudeFromDirectory, tile.LatitudeValue);
                                }
                                if (longitudeFromDirectory != tile.LongitudeValue)
                                {
                                    logger.LogError("Directory level 2 {DirectoryLongitude} does not match file {FileLongitude}.", longitudeFromDirectory, tile.LongitudeValue);
                                }
                                if (datasetFromDirectory != tile.DatasetValue)
                                {
                                    logger.LogError("Directory level 3 {DirectoryDataset} does not match file {FileDataset}.", datasetFromDirectory, tile.DatasetValue);
                                }
                                if (levelOfDetailFromDirectory is not null && levelOfDetailFromDirectory != tile.Level)
                                {
                                    logger.LogError("Directory level 4 {DirectoryLod} does not match file {FileLod}", levelOfDetailFromDirectory, tile.Level);
                                }
                                if (levelOfDetailFromDirectory is null && tile.Level.Level < 0)
                                {
                                    logger.LogError("File {Tile} should be in level 4 directory LC.", tile);
                                }
                                if (upFromDirectory != tile.Up)
                                {
                                    logger.LogError("Directory level 5 {DirectoryUref} does not match file {FileUref}", upFromDirectory, tile.Up);
                                }

                                visitFile(tile, file);
                            }
                        }
                    });
                }
            }
        }
    }
}
