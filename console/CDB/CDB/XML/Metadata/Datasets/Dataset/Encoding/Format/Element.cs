using System.Xml.Serialization;

namespace Silnith.CDB.XML.Metadata.Datasets.Dataset.Encoding.Format;

[XmlType("Format", Namespace = "http://www.opengis.net/cdb/1.2/Datasets")]
public class Element
{
    [XmlAttribute("name")]
    public FormatName Name
    {
        get;
        set;
    }

    [XmlAttribute("version")]
    public string? Version
    {
        get;
        set;
    }
}
