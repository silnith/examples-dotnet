using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Silnith.CDB;

/// <summary>
/// Visits all the files in the Geotypical Models Datasets.
/// </summary>
/// <remarks>
/// <para>
/// See OGC CDB Core Standard: Volume 1,
/// Section 3.4. GTModel Library Datasets
/// </para>
/// </remarks>
public class GTModelVisitor : Visitor
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

    private readonly ILogger<GTModelVisitor> logger;

    private readonly FeatureCodeDirectoryVisitor featureCodeDirectoryVisitor;

    private readonly LevelOfDetailDirectoryWalker levelOfDetailDirectoryWalker;

    private readonly TextureDirectoryVisitor textureDirectoryVisitor;

    public delegate void VisitGTModel(GTModelGeometry modelGeometry, FileInfo file);
    public delegate void VisitGTModelLod(GTModelGeometryLod modelGeometryLod, FileInfo file);
    public delegate void VisitTexture(Texture texture, FileInfo file);
    public delegate void VisitTextureLod(TextureLod textureLod, FileInfo file);

    /// <summary>
    ///  Walks the GTModel directory and visits all recognized files.
    /// </summary>
    /// <param name="gtModelDir">The GTModel directory.</param>
    public void WalkGeotypicalModels(DirectoryInfo gtModelDir,
        VisitGTModel modelAction,
        VisitGTModelLod modelLodAction,
        VisitTexture textureAction,
        VisitTextureLod textureLodAction)
    {
        if (!gtModelDir.Exists)
        {
            return;
        }

        foreach (DirectoryInfo datasetDir in gtModelDir.EnumerateDirectories("*", enumerationOptions))
        {
            Match datasetMatch = Dataset.TiledDatasetDirectoryPattern.Match(datasetDir.Name);
            if (!datasetMatch.Success)
            {
                logger.LogWarning("Unrecognized dataset directory {Directory} in GTModel.", datasetDir);
                continue;
            }
            Dataset datasetFromDirectory = Dataset.FromTiledDatasetDirectoryMatch(datasetMatch);

            // See 3.4.1. GTModel Directory Structure 1: Geometry and Descriptor
            // See 3.4.3. GTModel Directory Structure 3: Interior Geometry and Descriptor
            // See 3.4.5. GTModel Directory Structure 5: Signature
            featureCodeDirectoryVisitor.WalkDirectories(datasetDir, (featureCode, featureTypeDir) =>
            {
                // See 3.4.1.1. GTModelGeometry Entry File Naming Convention
                // See 3.4.1.3. GTModelDescriptor Naming Convention
                // The only difference between the two is the file type.
                // See 3.4.3.2. GTModelInteriorDescriptor Naming Convention
                foreach (FileInfo file in featureTypeDir.EnumerateFiles("*", enumerationOptions))
                {
                    Match gtModelGeometryMatch = GTModelGeometry.FilenamePattern.Match(file.Name);
                    if (gtModelGeometryMatch.Success)
                    {
                        GTModelGeometry gtModelGeometry = GTModelGeometry.FromFilenameMatch(gtModelGeometryMatch);

                        if (datasetFromDirectory != gtModelGeometry.Dataset)
                        {
                            logger.LogWarning("Directory {DirectoryDataset} does not match file {FileDataset}",
                                datasetFromDirectory, gtModelGeometry.Dataset);
                        }
                        if (featureCode != gtModelGeometry.FeatureCode)
                        {
                            logger.LogWarning("Directory {DirectoryFeatureCode} does not match file {FileFeatureCode}",
                                featureCode, gtModelGeometry.FeatureCode);
                        }

                        modelAction(gtModelGeometry, file);
                    }
                }

                levelOfDetailDirectoryWalker.WalkModelGeometryDirectories(featureTypeDir, (lod, lodDir) =>
                {
                    // See 3.4.1.2. GTModelGeometry Level of Detail Naming Convention
                    // See 3.4.3.1. GTModelInteriorGeometry Naming Convention
                    // See 3.4.5.1. GTModelSignature Naming Convention
                    foreach (FileInfo file in lodDir.EnumerateFiles("*", enumerationOptions))
                    {
                        Match gtModelGeometryLodMatch = GTModelGeometryLod.FilenamePattern.Match(file.Name);
                        if (gtModelGeometryLodMatch.Success)
                        {
                            GTModelGeometryLod gtModelGeometryLod = GTModelGeometryLod.FromFilenameMatch(gtModelGeometryLodMatch);

                            if (datasetFromDirectory != gtModelGeometryLod.Dataset)
                            {
                                logger.LogWarning("Directory {DirectoryDataset} does not match file {FileDataset}",
                                    datasetFromDirectory, gtModelGeometryLod.Dataset);
                            }
                            if (featureCode != gtModelGeometryLod.FeatureCode)
                            {
                                logger.LogWarning("Directory {DirectoryFeatureCode} does not match file {FileFeatureCode}",
                                    featureCode, gtModelGeometryLod.FeatureCode);
                            }
                            if (lod != gtModelGeometryLod.LevelOfDetail)
                            {
                                logger.LogWarning("Directory {DirectoryLod} does not match file {FileLod}",
                                    lod, gtModelGeometryLod.LevelOfDetail);
                            }

                            modelLodAction(gtModelGeometryLod, file);
                        }
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
                        if (CultureInfo.InvariantCulture.CompareInfo.Compare(textureName, textureLod.TextureName, CompareOptions.IgnoreCase) != 0)
                        {
                            logger.LogWarning("Texture directory {DirectoryName} does not match file {FileName}",
                                textureName, textureLod.TextureName);
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
                            if (CultureInfo.InvariantCulture.CompareInfo.Compare(textureName, texture.TextureName, CompareOptions.IgnoreCase) != 0)
                            {
                                logger.LogWarning("Texture directory {DirectoryName} does not match file {FileName}",
                                    textureName, texture.TextureName);
                            }

                            textureAction(texture, file);
                        }
                    }
                }
            });
        }
    }
}
