using System.Xml.Serialization;

namespace Silnith.CDB.XML.Metadata.Version.Specification;

/// <summary>
/// Specifies the version of the CDB Specification used to generate the current CDB Version.
/// If &apos;Specification&apos; is present, the version number indicated here has precedence over the one specified in &apos;Version_Specification.xml&apos;, if the file exist.
/// If no explicit version is provided either here or in Specification_Version.xml, the version number is deemed to be 3.0.
/// </summary>
[XmlType("Specification", Namespace = "http://www.opengis.net/cdb/1.2/Version")]
public class Element
{
    /// <summary>
    /// The &apos;version&apos; attribute is a character string representing the version number of the Specification.
    /// </summary>
    [XmlAttribute("version")]
    public Version Version
    {
        get;
        set;
    }

    /// <summary>
    /// The &apos;update&apos; attribute is an integer number starting with 1.
    /// When present, it indicates the update number of this version of the Specification.
    /// Note that an update to the Specification guarantees the compatibility with the original publishing of the said version of the Specification.
    /// The update number should be considered as informative only.
    /// </summary>
    [XmlAttribute("update")]
    public int Update
    {
        get;
        set;
    }
}
