namespace Silnith.CDB;

/// <summary>
/// The DIS Entity Type as described in OGC CDB 1.2: Section 3.3.8.3 DIS Entity Type.
/// CDB Moving Models use the DIS standard to identify themselves.
/// The semantics of the fields are described in Annex M of
/// Volume 2 CDB Core: Model and Physical Structure Annexes.
/// </summary>
/// <remarks>
/// <para>
/// These are used to identify CDB Moving Models.
/// </para>
/// </remarks>
/// <param name="Kind">The entity kind.  TBD</param>
/// <param name="Domain">The entity domain.  TBD</param>
/// <param name="Country">The entity country.  TBD</param>
/// <param name="Category">The entity category.  TBD</param>
/// <param name="Subcategory">The entity subcategory.  TBD</param>
/// <param name="Specific">The specific entity.  TBD</param>
/// <param name="Extra">Extra classification for the entity.  TBD</param>
public record DisEntityType(int Kind, int Domain, int Country, int Category, int Subcategory, int Specific, int Extra)
{
    /// <summary>
    /// The Moving Model DIS Code (MMDC) is defined in 5.7.1.3.40.
    /// </summary>
    // TODO: Explain this!
    public string MovingModelDisCode => $"{Kind:D}_{Domain:D}_{Country:D}_{Category:D}_{Subcategory:D}_{Specific:D}_{Extra:D}";
}
