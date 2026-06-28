using System.Xml.Serialization;

namespace Silnith.CDB.XML.Metadata.Datasets.Dataset.Encoding.File;

[XmlType("File", Namespace = "http://www.opengis.net/cdb/1.2/Datasets")]
public class Element
{
    [XmlAttribute("extension")]
    public string Extension
    {
        get;
        set;
    }
}
