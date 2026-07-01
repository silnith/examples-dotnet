using System.Collections.Generic;
using System.Xml.Serialization;

namespace Silnith.CDB.XML.Metadata.Datasets;

[XmlRoot("Datasets", Namespace = "http://www.opengis.net/cdb/1.2/Datasets")]
public class Element
{
    [XmlElement("Dataset")]
    public List<Dataset.Element> Datasets
    {
        get;
        set;
    }
}
