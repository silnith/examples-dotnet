using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Silnith.CDB.Visitor;

/// <summary>
/// Visits all the files in the Geotypical Models Datasets.
/// </summary>
/// <remarks>
/// <para>
/// See OGC CDB Core Standard: Volume 1,
/// Section 3.4. GTModel Library Datasets
/// </para>
/// </remarks>
public class GeotypicalModelVisitor : VisitorBase
{
    /*
     * 1. GTModelGeometry
     * 2. GTModelTexture
     * 3. GTModelDescriptor
     * 4. GTModelMaterial
     * 5. GTModelCMT
     * 6. GTModelInteriorGeometry
     * 7. GTModelInteriorTexture
     * 8. GTModelInteriorDescriptor
     * 9. GTModelInteriorMaterial
     * 10. GTModelInteriorCMT
     * 11. GTModelSignature
     */
    private static readonly SortedDictionary<int, string> recognizedDatasets = new()
    {
        { 500, "GTModelGeometry Entry File" },
        { 510, "GTModelGeometry Level of Detail" },
        { 503, "GTModelDescriptor" },
    };

    private readonly ILogger<GeotypicalModelVisitor> logger;

    private readonly FeatureCodeDirectoryWalker featureCodeDirectoryWalker;

    private readonly LevelOfDetailDirectoryWalker levelOfDetailDirectoryWalker;

    private readonly TextureDirectoryVisitor textureDirectoryVisitor;

    /// <summary>
    /// A constructor intended for dependency injection.
    /// </summary>
    /// <param name="logger">A logger.</param>
    /// <param name="featureCodeDirectoryWalker">A feature code directory walker.</param>
    /// <param name="levelOfDetailDirectoryWalker">A level of detail directory walker.</param>
    /// <param name="textureDirectoryVisitor">A texture directory walker.</param>
    public GeotypicalModelVisitor(ILogger<GeotypicalModelVisitor> logger,
        FeatureCodeDirectoryWalker featureCodeDirectoryWalker,
        LevelOfDetailDirectoryWalker levelOfDetailDirectoryWalker,
        TextureDirectoryVisitor textureDirectoryVisitor)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(featureCodeDirectoryWalker);
        ArgumentNullException.ThrowIfNull(levelOfDetailDirectoryWalker);
        ArgumentNullException.ThrowIfNull(textureDirectoryVisitor);

        this.logger = logger;
        this.featureCodeDirectoryWalker = featureCodeDirectoryWalker;
        this.levelOfDetailDirectoryWalker = levelOfDetailDirectoryWalker;
        this.textureDirectoryVisitor = textureDirectoryVisitor;
    }

    public delegate void VisitGeotypicalModel(GeotypicalModel geotypicalModel, FileInfo file);
    public delegate void VisitGeotypicalModelLod(GeotypicalModelLod geotypicalModelLod, FileInfo file);
    public delegate void VisitTexture(Texture texture, FileInfo file);
    public delegate void VisitTextureLod(TextureLod textureLod, FileInfo file);

    /// <summary>
    ///  Walks the GTModel directory and visits all recognized files.
    /// </summary>
    /// <param name="cdbDir">The CDB root directory.</param>
    public void VisitGeotypicalModels(DirectoryInfo cdbDir,
        VisitGeotypicalModel geotypicalModelAction,
        VisitGeotypicalModelLod geotypicalModelLodAction,
        VisitTexture textureAction,
        VisitTextureLod textureLodAction)
    {
        DirectoryInfo gtModelDir = new(Path.Combine(cdbDir.FullName, "GTModel"));
        if (!gtModelDir.Exists)
        {
            logger.LogTrace("{Directory} does not exist.  Skipping.", gtModelDir);
            return;
        }

        foreach (DirectoryInfo datasetDir in gtModelDir.EnumerateDirectories("*", enumerationOptions))
        {
            Match datasetMatch = Dataset.DirectoryPattern.Match(datasetDir.Name);
            if (!datasetMatch.Success)
            {
                logger.LogTrace("{Directory} is not a Dataset directory.  Skipping.",
                    datasetDir);
                continue;
            }
            Dataset datasetFromDirectory = Dataset.FromDirectoryMatch(datasetMatch);

            // See 3.4.1. GTModel Directory Structure 1: Geometry and Descriptor
            // See 3.4.3. GTModel Directory Structure 3: Interior Geometry and Descriptor
            // See 3.4.5. GTModel Directory Structure 5: Signature
            featureCodeDirectoryWalker.WalkDirectories(datasetDir, (featureCode, featureDir) =>
            {
                // See 3.4.1.1. GTModelGeometry Entry File Naming Convention
                // See 3.4.1.3. GTModelDescriptor Naming Convention
                // The only difference between the two is the file type.
                // See 3.4.3.2. GTModelInteriorDescriptor Naming Convention
                foreach (FileInfo file in featureDir.EnumerateFiles("*", enumerationOptions))
                {
                    Match geotypicalModelMatch = GeotypicalModel.FilenamePattern.Match(file.Name);
                    if (!geotypicalModelMatch.Success)
                    {
                        logger.LogTrace("{File} is not a Geotypical Model.  Skipping.",
                            file);
                        continue;
                    }
                    GeotypicalModel geotypicalModel = GeotypicalModel.FromFilenameMatch(geotypicalModelMatch);

                    if (datasetFromDirectory != geotypicalModel.Dataset)
                    {
                        logger.LogWarning("Directory {DirectoryDataset} does not match file {FileDataset}",
                            datasetFromDirectory, geotypicalModel.Dataset);
                    }
                    if (featureCode != geotypicalModel.FeatureCode)
                    {
                        logger.LogWarning("Directory {DirectoryFeatureCode} does not match file {FileFeatureCode}",
                            featureCode, geotypicalModel.FeatureCode);
                    }

                    geotypicalModelAction(geotypicalModel, file);
                }

                levelOfDetailDirectoryWalker.WalkModelGeometryDirectories(featureDir, (lod, lodDir) =>
                {
                    // See 3.4.1.2. GTModelGeometry Level of Detail Naming Convention
                    // See 3.4.3.1. GTModelInteriorGeometry Naming Convention
                    // See 3.4.5.1. GTModelSignature Naming Convention
                    foreach (FileInfo file in lodDir.EnumerateFiles("*", enumerationOptions))
                    {
                        Match geotypicalModelLodMatch = GeotypicalModelLod.FilenamePattern.Match(file.Name);
                        if (!geotypicalModelLodMatch.Success)
                        {
                            logger.LogTrace("{File} is not a Geotypical Model Level of Detail.  Skipping.",
                                file);
                            continue;
                        }
                        GeotypicalModelLod geotypicalModelLod = GeotypicalModelLod.FromFilenameMatch(geotypicalModelLodMatch);

                        if (datasetFromDirectory != geotypicalModelLod.Dataset)
                        {
                            logger.LogWarning("Directory {DirectoryDataset} does not match file {FileDataset}",
                                datasetFromDirectory, geotypicalModelLod.Dataset);
                        }
                        if (featureCode != geotypicalModelLod.FeatureCode)
                        {
                            logger.LogWarning("Directory {DirectoryFeatureCode} does not match file {FileFeatureCode}",
                                featureCode, geotypicalModelLod.FeatureCode);
                        }
                        if (lod != geotypicalModelLod.LevelOfDetail)
                        {
                            logger.LogWarning("Directory {DirectoryLod} does not match file {FileLod}",
                                lod, geotypicalModelLod.LevelOfDetail);
                        }

                        geotypicalModelLodAction(geotypicalModelLod, file);
                    }
                });
            });
            // See 3.4.2. GTModel Directory Structure 2: Texture, Material, and CMT
            // See 3.4.4. GTModel Directory Structure 4: Interior Texture, Material, and CMT
            textureDirectoryVisitor.WalkDirectories(datasetDir, (textureName, textureDir) =>
            {
                foreach (FileInfo file in textureDir.EnumerateFiles("*", enumerationOptions))
                {
                    // See 3.4.2.1. GTModelTexture Naming Convention
                    // See 3.4.2.2. GTModelMaterial Naming Convention
                    // The only disambiguation between textures and materials seems to be the dataset they are contained in.
                    // See 3.4.4.1. GTModelInteriorTexture Naming Convention
                    // See 3.4.4.2. GTModelInteriorMaterial Naming Convention
                    Match textureLodMatch = TextureLod.FilenamePattern.Match(file.Name);
                    if (textureLodMatch.Success)
                    {
                        TextureLod textureLod = TextureLod.FromFilenameMatch(textureLodMatch);

                        if (datasetFromDirectory != textureLod.Dataset)
                        {
                            logger.LogWarning("Directory {DirectoryDataset} does not match file {FileDataset}",
                                datasetFromDirectory, textureLod.Dataset);
                        }
                        if (CultureInfo.InvariantCulture.CompareInfo.Compare(textureName, textureLod.Name, CompareOptions.IgnoreCase) != 0)
                        {
                            logger.LogWarning("Texture directory {DirectoryName} does not match file {FileName}",
                                textureName, textureLod.Name);
                        }

                        textureLodAction(textureLod, file);
                    }
                    else
                    {
                        /*
                         * Since the regular expression matching non-LOD files also matches
                         * LOD files, we put this into an else block to avoid duplication.
                         */
                        // See 3.4.2.3. GTModelCMT Naming Convention
                        Match textureMatch = Texture.FilenamePattern.Match(file.Name);
                        if (textureMatch.Success)
                        {
                            Texture texture = Texture.FromFilenameMatch(textureMatch);

                            if (datasetFromDirectory != texture.Dataset)
                            {
                                logger.LogWarning("Directory {DirectoryDataset} does not match file {FileDataset}",
                                    datasetFromDirectory, texture.Dataset);
                            }
                            if (CultureInfo.InvariantCulture.CompareInfo.Compare(textureName, texture.Name, CompareOptions.IgnoreCase) != 0)
                            {
                                logger.LogWarning("Texture directory {DirectoryName} does not match file {FileName}",
                                    textureName, texture.Name);
                            }

                            textureAction(texture, file);
                        }
                        else
                        {
                            logger.LogTrace("{File} is not a texture file.  Skipping.",
                                file);
                            continue;
                        }
                    }
                }
            });
        }
    }
}
