using System.Xml.Serialization;

namespace Silnith.CDB.XML.Metadata.Version.Specification;

/// <summary>
/// The &apos;version&apos; attribute is a character string representing the version number of the Specification.
/// </summary>
//[XmlType(AnonymousType = true, Namespace = "http://www.opengis.net/cdb/1.2/Version")]
public enum Version
{

    [XmlEnum("3.0")]
    CDB30,

    [XmlEnum("3.1")]
    CDB31,

    [XmlEnum("3.2")]
    CDB32,

    [XmlEnum("1.0")]
    OGC_CDB10,

    [XmlEnum("1.1")]
    OGC_CDB11,

    [XmlEnum("1.2")]
    OGC_CDB12,
}
