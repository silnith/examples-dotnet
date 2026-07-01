using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Silnith.CDB.Visitor;

/// <summary>
/// Visits a directory hierarchy described in 3.6.2. Tiled Dataset Directory Structure,
/// and calls a delegate for every file that matches the expected
/// structure and name.
/// </summary>
public class TileVisitor : VisitorBase
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

    private readonly ILogger<TileVisitor> logger;

    private readonly LevelOfDetailDirectoryWalker levelOfDetailDirectoryWalker;

    /// <summary>
    /// A constructor for dependency injection.
    /// </summary>
    /// <param name="logger">A logger.</param>
    /// <param name="levelOfDetailDirectoryWalker">A visitor for levels of detail directories.</param>
    public TileVisitor(ILogger<TileVisitor> logger,
        LevelOfDetailDirectoryWalker levelOfDetailDirectoryWalker)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(levelOfDetailDirectoryWalker);

        this.logger = logger;
        this.levelOfDetailDirectoryWalker = levelOfDetailDirectoryWalker;
    }

    /// <summary>
    /// Called for every file in a tiled dataset directory hierarchy.
    /// </summary>
    /// <param name="tile">The details of the tile extracted from the filename.</param>
    /// <param name="fileInfo">The file.</param>
    public delegate void VisitTiledDatasetFile(Tile tile, FileInfo fileInfo);

    /// <summary>
    /// Walks the Tiles directory and visits all recognized files.
    /// </summary>
    /// <remarks>
    /// <para>
    /// See OGC CDB Core Standard: Volume 1,
    /// Section 3.6. CDB Tiled Datasets
    /// </para>
    /// </remarks>
    /// <param name="cdbDir">The CDB root directory.</param>
    /// <param name="visitTile">The action to call for every file found.</param>
    public void VisitTiles(DirectoryInfo cdbDir, VisitTiledDatasetFile visitTile)
    {
        DirectoryInfo tilesDir = new(Path.Combine(cdbDir.FullName, "Tiles"));
        if (!tilesDir.Exists)
        {
            logger.LogTrace("{Directory} does not exist.  Skipping.",
                tilesDir);
            return;
        }

        foreach (DirectoryInfo latitudeDir in tilesDir.EnumerateDirectories("*", enumerationOptions))
        {
            Match latitudeMatch = Latitude.TiledDatasetDirectoryPattern.Match(latitudeDir.Name);
            if (!latitudeMatch.Success)
            {
                logger.LogTrace("{Directory} is not a Latitude directory.  Skipping.",
                    latitudeDir);
                continue;
            }
            Latitude latitudeFromDirectory = Latitude.FromTiledDatasetDirectoryMatch(latitudeMatch);

            foreach (DirectoryInfo longitudeDir in latitudeDir.EnumerateDirectories("*", enumerationOptions))
            {
                Match longitudeMatch = Longitude.TiledDatasetDirectoryPattern.Match(longitudeDir.Name);
                if (!longitudeMatch.Success)
                {
                    logger.LogTrace("{Directory} is not a Longitude directory.  Skipping.",
                        longitudeDir);
                    continue;
                }
                Longitude longitudeFromDirectory = Longitude.FromTiledDatasetDirectoryMatch(longitudeMatch);

                foreach (DirectoryInfo datasetDir in longitudeDir.EnumerateDirectories("*", enumerationOptions))
                {
                    Match datasetMatch = Dataset.DirectoryPattern.Match(datasetDir.Name);
                    if (!datasetMatch.Success)
                    {
                        logger.LogTrace("{Directory} is not a Dataset directory.  Skipping.",
                            datasetDir);
                        continue;
                    }
                    Dataset datasetFromDirectory = Dataset.FromDirectoryMatch(datasetMatch);

                    levelOfDetailDirectoryWalker.WalkTiledDatasetDirectories(datasetDir, (levelOfDetailFromDirectory, lodDir) =>
                    {
                        foreach (DirectoryInfo upDir in lodDir.EnumerateDirectories("*", enumerationOptions))
                        {
                            Match upMatch = UpDirPattern.Match(upDir.Name);
                            if (!upMatch.Success)
                            {
                                logger.LogTrace("{Directory} is not an UREF directory.  Skipping.",
                                    upDir);
                                continue;
                            }
                            int upFromDirectory = int.Parse(upMatch.Groups["up"].Value, CultureInfo.InvariantCulture);

                            foreach (FileInfo file in upDir.EnumerateFiles("*", enumerationOptions))
                            {
                                Match tileMatch = Tile.TiledDatasetFilenamePattern.Match(file.Name);
                                if (!tileMatch.Success)
                                {
                                    continue;
                                }
                                Tile tile = Tile.FromTiledDatasetFilenameMatch(tileMatch);

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
                                if (levelOfDetailFromDirectory is null && tile.Level.Value < 0)
                                {
                                    logger.LogError("File {Tile} should be in level 4 directory LC.", tile);
                                }
                                if (upFromDirectory != tile.Up)
                                {
                                    logger.LogError("Directory level 5 {DirectoryUref} does not match file {FileUref}", upFromDirectory, tile.Up);
                                }

                                visitTile(tile, file);
                            }
                        }
                    });
                }
            }
        }
    }
}
