using System.IO;

namespace Silnith.CDB.Visitor;

/// <summary>
/// A base class to provide a single location for configuring directory traversal
/// default behavior.
/// </summary>
public abstract class VisitorBase
{
    /// <summary>
    /// The options for how to enumerate directory entries.
    /// This specifies case insensitive matching using simple wildcards, no recursion.
    /// </summary>
    protected readonly EnumerationOptions enumerationOptions = new()
    {
        MatchCasing = MatchCasing.CaseInsensitive,
        MatchType = MatchType.Simple,
        RecurseSubdirectories = false,
        ReturnSpecialDirectories = false,
    };

    /*
     * Could potentially put a list of recognized file extensions here.
     * For right now, just concerned with walking the tree, not validating it.
     */
}
