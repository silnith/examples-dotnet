using System.Xml.Serialization;

namespace Silnith.CDB.XML.Metadata.Datasets.Dataset.Encoding.Format;

public enum FormatName
{

    /// <remarks/>
    GeoPackage,

    /// <remarks/>
    [XmlEnum("JPEG 2000")]
    JPEG2000,

    /// <remarks/>
    GeoTIFF,

    /// <remarks/>
    TIFF,

    /// <remarks/>
    Shapefile,

    /// <remarks/>
    OpenFlight,

    /// <remarks/>
    XML,

    /// <remarks/>
    SGI,
}
