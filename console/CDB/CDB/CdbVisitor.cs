namespace Silnith.CDB;

/// <summary>
/// Visits all the files in a CDB data store.
/// </summary>
public class CdbVisitor
{
    private readonly MetadataVisitor metadataVisitor;
    private readonly GTModelVisitor gTModelVisitor;
    private readonly MovingModelVisitor movingModelVisitor;
    private readonly TiledDatasetVisitor2 tiledDatasetVisitor2;
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
    /// <param name="dir">The root directory of the CDB data store.</param>
    public void WalkDataStore(DirectoryInfo dir)
    {
        DirectoryInfo metadata = new(Path.Combine(dir.FullName, "Metadata"));
        metadataVisitor.WalkMetadata(metadata, null);
        DirectoryInfo gtModel = new(Path.Combine(dir.FullName, "GTModel"));
        gTModelVisitor.WalkGeotypicalModels(gtModel,
            null, null, null, null);
        DirectoryInfo mModel = new(Path.Combine(dir.FullName, "MModel"));
        movingModelVisitor.VisitMovingModels(mModel);
        DirectoryInfo tiles = new(Path.Combine(dir.FullName, "Tiles"));
        tiledDatasetVisitor2.VisitTiles(tiles);
        DirectoryInfo navigation = new(Path.Combine(dir.FullName, "Navigation"));
        navigationVisitor.WalkNavigationDatasets(navigation);
    }
}
