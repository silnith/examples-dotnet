using System.Xml.Serialization;

namespace Silnith.CDB.XML.Metadata.Version;

/// <summary>
/// The Version Metadata document as described in 5.1.7. Version Metadata.
/// </summary>
[XmlRoot("Version", Namespace = "http://www.opengis.net/cdb/1.2/Version")]
public class Element
{
    /// <summary>
    /// This element is optional; when present it provides the location of another CDB Version that is linked with the current one.
    /// </summary>
    [XmlElement("PreviousIncrementalRootDirectory")]
    public PreviousIncrementalRootDirectory.Element? PreviousIncrementalRootDirectory
    {
        get;
        set;
    }

    /// <summary>
    /// This choice preserves backward compatibility with version 3.0 of the Specification
    /// where a &apos;comment&apos; element was used to describe the content of a CDB Version.
    /// As of CDB 3.1, the prefered syntax for commenting a version is to use the &apos;Comment&apos; element.
    /// </summary>
    [XmlElement("Comment")]
    [XmlElement("comment")]
    [XmlChoiceIdentifier(nameof(Foo))]
    public string? Comment
    {
        get;
        set;
    }

    public enum Bar
    {
        Comment,
        comment,
    }

    [XmlIgnore]
    public Bar Foo
    {
        get;
        set;
    }

    /// <summary>
    /// Specifies the version of the CDB Specification used to generate the current CDB Version.
    /// If &apos;Specification&apos; is present, the version number indicated here has precedence over the one specified in &apos;Version_Specification.xml&apos;, if the file exist.
    /// If no explicit version is provided either here or in Specification_Version.xml, the version number is deemed to be 3.0.
    /// </summary>
    [XmlElement("Specification")]
    public Specification.Element? Specification
    {
        get;
        set;
    }

    /// <summary>
    /// The optional Metadata-standard element specifies the metadata standard
    /// used in a CDB data store.The metadata standard specifically refers to traditional resource metadata,
    /// such as title, author and geographic bounding box.
    /// </summary>
    [XmlElement("Metadata-standard")]
    public MetadataStandard.Element? MetadataStandard
    {
        get;
        set;
    }

    /// <summary>
    /// This element is optional; when present, it tells us that the current &apos;CDB Version&apos; is a &apos;CDB Extension&apos;.
    /// As a result, the whole content of this version is not covered by the Specification.
    /// A CDB Extension can contain any datasets, including valid CDB Datasets.
    /// </summary>
    [XmlElement("Extension")]
    public Extension.Element? Extension
    {
        get;
        set;
    }
}
