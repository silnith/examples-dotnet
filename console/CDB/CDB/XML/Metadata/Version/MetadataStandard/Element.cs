using System.Xml.Serialization;

namespace Silnith.CDB.XML.Metadata.Version.MetadataStandard;

/// <summary>
/// The optional Metadata-standard element specifies the metadata standard
/// used in a CDB data store.The metadata standard specifically refers to traditional resource metadata,
/// such as title, author and geographic bounding box.
/// </summary>
[XmlType("Metadata-standard", Namespace = "http://www.opengis.net/cdb/1.2/Version")]
public class Element
{
    /// <summary>
    /// The &apos;metadata-standard-name&apos; attribute is a character string representing the
    /// metadata standard used in a CDB data store.
    /// </summary>
    [XmlAttribute("metadata-standard-name")]
    public MetadataStandardName MetadataStandardName
    {
        get;
        set;
    }
}
