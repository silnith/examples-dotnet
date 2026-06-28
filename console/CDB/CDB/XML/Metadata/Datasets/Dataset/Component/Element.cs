using System.Xml.Serialization;

namespace Silnith.CDB.XML.Metadata.Datasets.Dataset.Component;

[XmlType("Component", Namespace = "http://www.opengis.net/cdb/1.2/Datasets")]
public class Element
{
    [XmlAttribute("code")]
    public int Code
    {
        get;
        set;
    }

    [XmlAttribute("name")]
    public string Name
    {
        get;
        set;
    }

    [XmlAttribute("deprecated")]
    public bool Deprecated
    {
        get;
        set;
    }
}
