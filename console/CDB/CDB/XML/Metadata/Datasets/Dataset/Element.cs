using System.Collections.Generic;
using System.Xml.Serialization;

namespace Silnith.CDB.XML.Metadata.Datasets.Dataset;

[XmlType("Dataset", Namespace = "http://www.opengis.net/cdb/1.2/Datasets")]
public class Element
{
    [XmlElement("Encoding")]
    public Encoding.Element? Encoding
    {
        get;
        set;
    }

    [XmlElement("Component")]
    public List<Component.Element> Components
    {
        get;
        set;
    }

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
