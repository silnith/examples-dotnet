using System.Xml.Serialization;

namespace Silnith.CDB.XML.Metadata.Version.MetadataStandard;

/// <summary>
/// The &apos;metadata-standard-name&apos; attribute is a character string representing the
/// metadata standard used in a CDB data store.
/// </summary>
//[XmlType(AnonymousType = true, Namespace = "http://www.opengis.net/cdb/1.2/Version")]
public enum MetadataStandardName
{
    [XmlEnum("ISO-19115:2014")]
    ISO191152014,

    [XmlEnum("ISO-19115:2003")]
    ISO191152003,

    [XmlEnum("DDMS-5.0")]
    DDMS50,

    [XmlEnum("DDMS-5.0-MS-Profile")]
    DDMS50MSProfile,

    DCAT,

    [XmlEnum("DCAT-AP")]
    DCATAP,

    [XmlEnum("GeoDCAT-AP")]
    GeoDCATAP,

    NGCMP,

    NoMetadata,
}
