namespace Silnith.CDB;

/// <summary>
/// Visits all the files in the Moving Models Datasets.
/// </summary>
public class MovingModelVisitor
{
    /// <summary>
    /// Walks the MModel directory and visits all recognized files.
    /// </summary>
    /// <remarks>
    /// <para>
    /// See OGC CDB Core Standard: Volume 1,
    /// Section 3.5. MModel Library Datasets.
    /// </para>
    /// </remarks>
    /// <param name="mModelDir">The MModel directory.</param>
    public void VisitMovingModels(DirectoryInfo mModelDir)
    {
        if (!mModelDir.Exists)
        {
            return;
        }
    }
}
