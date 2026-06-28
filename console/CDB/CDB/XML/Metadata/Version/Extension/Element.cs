using System.Xml.Serialization;

namespace Silnith.CDB.XML.Metadata.Version.Extension;

/// <summary>
/// This element is optional; when present, it tells us that the current &apos;CDB Version&apos; is a &apos;CDB Extension&apos;.
/// As a result, the whole content of this version is not covered by the Specification.
/// A CDB Extension can contain any datasets, including valid CDB Datasets.
/// </summary>
[XmlType("Extension", Namespace = "http://www.opengis.net/cdb/1.2/Version")]
public class Element
{
    [XmlAttribute("name", DataType = "token")]
    public string Name
    {
        get;
        set;
    }

    [XmlAttribute("version", DataType = "token")]
    public string Version
    {
        get;
        set;
    }
}
