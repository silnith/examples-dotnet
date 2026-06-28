using System.Xml.Serialization;

namespace Silnith.CDB.XML.Metadata.Datasets.Dataset.Encoding;

[XmlType("Encoding", Namespace = "http://www.opengis.net/cdb/1.2/Datasets")]
public class Element
{
    [XmlElement("Format")]
    public Format.Element Format
    {
        get;
        set;
    }

    [XmlElement("File")]
    public File.Element? File
    {
        get;
        set;
    }
}
