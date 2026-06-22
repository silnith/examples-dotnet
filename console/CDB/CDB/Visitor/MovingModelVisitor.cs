using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Silnith.CDB.Visitor;

/// <summary>
/// Visits all the files in the Moving Models Datasets.
/// </summary>
/// <remarks>
/// <para>
/// See OGC CDB Core Standard: Volume 1,
/// Section 3.5. MModel Library Datasets.
/// </para>
/// </remarks>
public class MovingModelVisitor : VisitorBase
{
    private readonly ILogger<MovingModelVisitor> logger;

    private readonly DISEntityDirectoryWalker disEntityDirectoryWalker;

    private readonly TextureDirectoryVisitor textureDirectoryVisitor;

    private readonly LevelOfDetailDirectoryWalker levelOfDetailDirectoryWalker;

    public MovingModelVisitor(ILogger<MovingModelVisitor> logger,
        DISEntityDirectoryWalker disEntityDirectoryWalker,
        TextureDirectoryVisitor textureDirectoryVisitor,
        LevelOfDetailDirectoryWalker levelOfDetailDirectoryWalker)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(disEntityDirectoryWalker);
        ArgumentNullException.ThrowIfNull(textureDirectoryVisitor);
        ArgumentNullException.ThrowIfNull(levelOfDetailDirectoryWalker);

        this.logger = logger;
        this.disEntityDirectoryWalker = disEntityDirectoryWalker;
        this.textureDirectoryVisitor = textureDirectoryVisitor;
        this.levelOfDetailDirectoryWalker = levelOfDetailDirectoryWalker;
    }

    public delegate void MovingModelAction(MovingModelGeometry movingModel, FileInfo file);
    public delegate void MovingModelActionLod(MovingModelGeometryLod movingModelLod, FileInfo file);
    public delegate void ModelTextureAction(ModelTexture modelTexture, FileInfo file);
    public delegate void TextureAction(Texture texture, FileInfo file);

    /// <summary>
    /// Walks the MModel directory and visits all recognized files.
    /// </summary>
    /// <remarks>
    /// <para>
    /// See OGC CDB Core Standard: Volume 1,
    /// Section 3.5. MModel Library Datasets.
    /// </para>
    /// </remarks>
    /// <param name="cdbDir">The CDB root directory.</param>
    public void VisitMovingModels(DirectoryInfo cdbDir,
        MovingModelAction visitMovingModel,
        MovingModelActionLod visitMovingModelLod,
        ModelTextureAction modelTextureAction,
        TextureAction textureAction)
    {
        DirectoryInfo mModelDir = new(Path.Combine(cdbDir.FullName, "MModel"));
        if (!mModelDir.Exists)
        {
            logger.LogTrace("{Directory} does not exist.  Skipping.",
                mModelDir);
            return;
        }

        foreach (DirectoryInfo datasetDir in mModelDir.EnumerateDirectories("*", enumerationOptions))
        {
            Match datasetMatch = Dataset.DirectoryPattern.Match(datasetDir.Name);
            if (!datasetMatch.Success)
            {
                logger.LogTrace("{Directory} is not a Dataset directory.  Skipping.",
                    datasetDir);
                continue;
            }
            Dataset datasetFromDirectory = Dataset.FromDirectoryMatch(datasetMatch);

            // See 3.5.1. MModel Directory Structure 1: Geometry and Descriptor
            disEntityDirectoryWalker.WalkDirectories(datasetDir, (disEntityType, entityDir) =>
            {
                // See 3.5.1.1. MModelGeometry Naming Convention
                // See 3.5.1.2. MModelDescriptor Naming Convention
                foreach (FileInfo file in entityDir.EnumerateFiles("*", enumerationOptions))
                {
                    Match movingModelMatch = MovingModelGeometry.FilenamePattern.Match(file.Name);
                    if (!movingModelMatch.Success)
                    {
                        logger.LogWarning("{File} is not a Moving Model.  Skipping.",
                            file);
                        continue;
                    }
                    MovingModelGeometry movingModelGeometry = MovingModelGeometry.FromFilenameMatch(movingModelMatch);

                    if (datasetFromDirectory != movingModelGeometry.Dataset)
                    {
                        logger.LogWarning("Directory {DirectoryDataset} does not match file {FileDataset}",
                            datasetFromDirectory, movingModelGeometry.Dataset);
                    }
                    if (disEntityType != movingModelGeometry.MMDC)
                    {
                        logger.LogWarning("Directory {DirectoryDISCode} does not match file {FileDISCode}",
                            disEntityType, movingModelGeometry.MMDC);
                    }

                    visitMovingModel(movingModelGeometry, file);
                }

                // See 3.5.3. MModel Directory Structure 3: Signature
                levelOfDetailDirectoryWalker.WalkModelGeometryDirectories(entityDir, (lod, lodDir) =>
                {
                    foreach (var file in lodDir.EnumerateFiles("*", enumerationOptions))
                    {
                        // See 3.5.3.1. Naming Convention
                        Match movingModelGeometryLodMatch = MovingModelGeometryLod.FilenamePattern.Match(file.Name);
                        if (!movingModelGeometryLodMatch.Success)
                        {
                            logger.LogTrace("{File} is not a Moving Model Level of Detail.  Skipping.",
                                file);
                            continue;
                        }
                        MovingModelGeometryLod movingModelGeometryLod = MovingModelGeometryLod.FromFilenameMatch(movingModelGeometryLodMatch);

                        if (datasetFromDirectory != movingModelGeometryLod.Dataset)
                        {
                            logger.LogWarning("Directory {DirectoryDataset} does not match file {FileDataset}",
                                datasetFromDirectory, movingModelGeometryLod.Dataset);
                        }
                        if (disEntityType != movingModelGeometryLod.MMDC)
                        {
                            logger.LogWarning("Directory {DirectoryDISCode} does not match file {FileDISCode}",
                                disEntityType, movingModelGeometryLod.MMDC);
                        }
                        if (lod != movingModelGeometryLod.LevelOfDetail)
                        {
                            logger.LogWarning("Directory {DirectoryLod} does not match file {FileLod}",
                                lod, movingModelGeometryLod.LevelOfDetail);
                        }

                        visitMovingModelLod(movingModelGeometryLod, file);
                    }
                });
            });
            // See 3.5.2. MModel Directory Structure 2: Texture, Material, and CMT
            textureDirectoryVisitor.WalkDirectories(datasetDir, (textureName, textureDir) =>
            {
                foreach (FileInfo file in textureDir.EnumerateFiles("*", enumerationOptions))
                {
                    // See 3.5.2.1. MModelTexture Naming Convention
                    // See 3.5.2.2. MModelMaterial Naming Convention
                    Match modelTextureMatch = ModelTexture.FilenamePattern.Match(file.Name);
                    if (modelTextureMatch.Success)
                    {
                        ModelTexture modelTexture = ModelTexture.FromFilenameMatch(modelTextureMatch);

                        if (datasetFromDirectory != modelTexture.Dataset)
                        {
                            logger.LogWarning("Directory {DirectoryDataset} does not match file {FileDataset}",
                                datasetFromDirectory, modelTexture.Dataset);
                        }
                        if (textureName != modelTexture.TextureName)
                        {
                            logger.LogWarning("Directory {DirectoryTexture} does not match file {FileTexture}",
                                textureName, modelTexture.TextureName);
                        }

                        modelTextureAction(modelTexture, file);
                    }
                    else
                    {
                        // See 3.5.2.3. MModelCMT Naming Convention
                        Match textureMatch = Texture.FilenamePattern.Match(file.Name);
                        if (textureMatch.Success)
                        {
                            Texture texture = Texture.FromFilenameMatch(textureMatch);

                            if (datasetFromDirectory != texture.Dataset)
                            {
                                logger.LogWarning("Directory {DirectoryDataset} does not match file {FileDataset}",
                                    datasetFromDirectory, texture.Dataset);
                            }
                            if (textureName != texture.Name)
                            {
                                logger.LogWarning("Directory {DirectoryTexture} does not match file {FileTexture}",
                                    textureName, texture.Name);
                            }

                            textureAction(texture, file);
                        }
                        else
                        {
                            logger.LogWarning("{File} is not a texture.  Skipping.",
                                file);
                            continue;
                        }
                    }
                }
            });
        }
    }
}
