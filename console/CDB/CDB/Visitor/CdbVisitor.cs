using Microsoft.Extensions.Logging;
using System.IO;

namespace Silnith.CDB.Visitor;

/// <summary>
/// Visits all the files in a CDB data store.
/// </summary>
public class CdbVisitor : VisitorBase
{
    private readonly ILogger<CdbVisitor> logger;
    private readonly MetadataVisitor metadataVisitor;
    private readonly GeotypicalModelVisitor geotypicalModelVisitor;
    private readonly MovingModelVisitor movingModelVisitor;
    private readonly TileVisitor tileVisitor;
    private readonly NavigationVisitor navigationVisitor;

    /// <summary>
    /// A constructor intended for dependency injection.
    /// </summary>
    /// <param name="logger">A logger.</param>
    /// <param name="metadataVisitor">A visitor for the Metadata directory.</param>
    /// <param name="geotypicalModelVisitor">A visitor for the GTModel directory.</param>
    /// <param name="movingModelVisitor">A visitor for the MModel directory.</param>
    /// <param name="tileVisitor">A visitor for the Tiles directory.</param>
    /// <param name="navigationVisitor">A visitor for the Navigation directory.</param>
    public CdbVisitor(ILogger<CdbVisitor> logger,
        MetadataVisitor metadataVisitor,
        GeotypicalModelVisitor geotypicalModelVisitor,
        MovingModelVisitor movingModelVisitor,
        TileVisitor tileVisitor,
        NavigationVisitor navigationVisitor)
    {
        this.logger = logger;
        this.metadataVisitor = metadataVisitor;
        this.geotypicalModelVisitor = geotypicalModelVisitor;
        this.movingModelVisitor = movingModelVisitor;
        this.tileVisitor = tileVisitor;
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
        geotypicalModelVisitor.VisitGeotypicalModels(cdbDir,
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
        tileVisitor.VisitTiles(cdbDir,
            (tile, file) => { });
        logger.LogTrace("Walking Navigation for {CDB}", cdbDir);
        navigationVisitor.VisitNavigationDatasets(cdbDir,
            (navigation, file) => { });
        logger.LogTrace("Finished walking CDB data store {CDB}", cdbDir);
    }
}
