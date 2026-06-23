using Microsoft.Extensions.Logging;

namespace Silnith.CDB.Visitor;

/// <summary>
/// Visits all the files in a CDB data store.
/// </summary>
public class CdbVisitor : VisitorBase
{
    private readonly ILogger<CdbVisitor> logger;
    private readonly MetadataVisitor metadataVisitor;
    private readonly GTModelVisitor gtModelVisitor;
    private readonly MovingModelVisitor movingModelVisitor;
    private readonly TiledDatasetVisitor tiledDatasetVisitor;
    private readonly NavigationVisitor navigationVisitor;

    /// <summary>
    /// A constructor intended for dependency injection.
    /// </summary>
    /// <param name="logger">A logger.</param>
    /// <param name="metadataVisitor">A visitor for the Metadata directory.</param>
    /// <param name="gtModelVisitor">A visitor for the GTModel directory.</param>
    /// <param name="movingModelVisitor">A visitor for the MModel directory.</param>
    /// <param name="tiledDatasetVisitor">A visitor for the Tiles directory.</param>
    /// <param name="navigationVisitor">A visitor for the Navigation directory.</param>
    public CdbVisitor(ILogger<CdbVisitor> logger,
        MetadataVisitor metadataVisitor,
        GTModelVisitor gtModelVisitor,
        MovingModelVisitor movingModelVisitor,
        TiledDatasetVisitor tiledDatasetVisitor,
        NavigationVisitor navigationVisitor)
    {
        this.logger = logger;
        this.metadataVisitor = metadataVisitor;
        this.gtModelVisitor = gtModelVisitor;
        this.movingModelVisitor = movingModelVisitor;
        this.tiledDatasetVisitor = tiledDatasetVisitor;
        this.navigationVisitor = navigationVisitor;
    }

    /// <summary>
    /// Walks the CDB and visits all recognized files.
    /// </summary>
    /// <remarks>
    /// <para>
    /// See OGC CDB Core Standard: Volume 1,
    /// Section 3.1. Top Level CDB Model/Structure Description
    /// </para>
    /// </remarks>
    /// <param name="cdbDir">The root directory of the CDB data store.</param>
    public void WalkDataStore(DirectoryInfo cdbDir)
    {
        logger.LogTrace("Walking Metadata for {CDB}", cdbDir);
        metadataVisitor.VisitMetadata(cdbDir,
            (metadata, file) => { });
        logger.LogTrace("Walking GTModel for {CDB}", cdbDir);
        gtModelVisitor.VisitGeotypicalModels(cdbDir,
            (gtModel, file) => { },
            (gtModelLod, file) => { },
            (texture, file) => { },
            (textureLod, file) => { });
        logger.LogTrace("Walking MModel for {CDB}", cdbDir);
        movingModelVisitor.VisitMovingModels(cdbDir,
            (movingModel, file) => { },
            (movingModelLod, file) => { },
            (texture, file) => { },
            (textureLod, file) => { });
        logger.LogTrace("Walking Tiles for {CDB}", cdbDir);
        tiledDatasetVisitor.VisitTiles(cdbDir,
            (tile, file) => { });
        logger.LogTrace("Walking Navigation for {CDB}", cdbDir);
        navigationVisitor.VisitNavigationDatasets(cdbDir,
            (navigation, file) => { });
        logger.LogTrace("Finished walking CDB data store {CDB}", cdbDir);
    }
}
