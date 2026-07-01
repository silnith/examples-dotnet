using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Silnith.CDB.Visitor;

/// <summary>
/// Walks the global Navigation datasets.
/// </summary>
public class NavigationVisitor : VisitorBase
{
    private static readonly Regex NavigationFilenamePattern = new(
        @"^D(?<dataset>\d{3})_S(?<component_selector_1>\d{3})_T(?<component_selector_2>\d{3})\.(?<file_type>[^.]+)$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);

    private readonly ILogger<NavigationVisitor> logger;

    /// <summary>
    /// A constructor intended for dependency injection.
    /// </summary>
    /// <param name="logger">A logger.</param>
    public NavigationVisitor(ILogger<NavigationVisitor> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        this.logger = logger;
    }

    /// <summary>
    /// Walks the Navigation datasets and visits all recognized files.
    /// </summary>
    /// <remarks>
    /// <para>
    /// See OGC CDB Core Standard: Volume 1,
    /// Section 3.7. Navigation Library Dataset
    /// </para>
    /// </remarks>
    /// <param name="cdbDir">The CDB root directory.</param>
    /// <param name="visitFile">The action to invoke for every file found in the Navigation dataset.</param>
    public void VisitNavigationDatasets(DirectoryInfo cdbDir, Action<Navigation, FileInfo> visitFile)
    {
        DirectoryInfo navigationDir = new(Path.Combine(cdbDir.FullName, "Navigation"));
        if (!navigationDir.Exists)
        {
            logger.LogTrace("{Directory} does not exist.  Skipping.",
                navigationDir);
            return;
        }

        foreach (DirectoryInfo datasetDir in navigationDir.EnumerateDirectories("*", enumerationOptions))
        {
            Match datasetMatch = Dataset.DirectoryPattern.Match(datasetDir.Name);
            if (!datasetMatch.Success)
            {
                logger.LogTrace("{Directory} is not a Dataset directory.  Skipping.",
                    datasetDir);
                continue;
            }
            Dataset datasetFromDirectory = Dataset.FromDirectoryMatch(datasetMatch);
            string datasetName = datasetMatch.Groups["name"].Value;
            if (datasetFromDirectory.Value != 400
                || datasetName != "NavData")
            {
                logger.LogWarning("Directory {DatasetDirectory} is not 400_NavData", datasetDir);
            }

            foreach (FileInfo file in datasetDir.EnumerateFiles("*", enumerationOptions))
            {
                Match match = Navigation.FilenamePattern.Match(file.Name);
                if (!match.Success)
                {
                    logger.LogTrace("{File} is not a Navigation file.",
                        file);
                    continue;
                }
                Navigation navigation = Navigation.FromFilenameMatch(match);

                if (datasetFromDirectory != navigation.Dataset)
                {
                    logger.LogWarning("Directory {DirectoryDataset} does not match file {FileDataset}",
                        datasetFromDirectory, navigation.Dataset);
                }

                visitFile(navigation, file);
            }
        }
    }
}
