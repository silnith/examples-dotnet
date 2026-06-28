using System.Xml.Serialization;

namespace Silnith.CDB.XML.Metadata.Version.PreviousIncrementalRootDirectory;

/// <summary>
/// This element is optional; when present it provides the location of another CDB Version that is linked with the current one.
/// </summary>
[XmlType("PreviousIncrementalRootDirectory", Namespace = "http://www.opengis.net/cdb/1.2/Version")]
public class Element
{
    /// <summary>
    /// Provides the relative or absolute path to another CDB Version.
    /// </summary>
    [XmlAttribute("name")]
    public string Name
    {
        get;
        set;
    }
}
