namespace Silnith.CDB.Visitor;

/// <summary>
/// Visits all the files in a CDB data store.
/// </summary>
public class CdbVisitor : VisitorBase
{
    private readonly MetadataVisitor metadataVisitor;
    private readonly GTModelVisitor gtModelVisitor;
    private readonly MovingModelVisitor movingModelVisitor;
    private readonly TiledDatasetVisitor tiledDatasetVisitor;
    private readonly NavigationVisitor navigationVisitor;

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
        metadataVisitor.VisitMetadata(cdbDir,
            null);
        gtModelVisitor.VisitGeotypicalModels(cdbDir,
            null, null, null, null);
        movingModelVisitor.VisitMovingModels(cdbDir,
            null, null, null, null);
        tiledDatasetVisitor.VisitTiles(cdbDir,
            null);
        navigationVisitor.VisitNavigationDatasets(cdbDir,
            null);
    }
}
