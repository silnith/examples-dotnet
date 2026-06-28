using System.Collections.Immutable;
using System.Xml.Serialization;

namespace Silnith.CDB.XML;

public class DataStoreInformation
{
    public Metadata.Version.Element? Version
    {
        get;
        private set;
    }

    public Metadata.Datasets.Element? Datasets
    {
        get;
        set;
    }

    public IReadOnlyDictionary<int, string> DatasetNames
    {
        get;
        private set;
    } = new SortedDictionary<int, string>().ToImmutableSortedDictionary();

    public void Initialize(IDataStore dataStore)
    {
        XmlSerializerFactory xmlSerializerFactory = new();
        if (dataStore.TryReadFile("Metadata/Version.xml", out byte[] versionContent))
        {
            XmlSerializer xmlSerializer = xmlSerializerFactory.CreateSerializer(typeof(Metadata.Version.Element));
            Version = (Metadata.Version.Element?) xmlSerializer.Deserialize(new MemoryStream(versionContent));
        }
        if (dataStore.TryReadFile("Metadata/Datasets.xml", out byte[] datasetsContent))
        {
            XmlSerializer datasetsDeserializer = xmlSerializerFactory.CreateSerializer(typeof(Metadata.Datasets.Element));
            Datasets = (Metadata.Datasets.Element?) datasetsDeserializer.Deserialize(new MemoryStream(datasetsContent));
            DatasetNames = Datasets!.Datasets
                .Select(d => new KeyValuePair<int, string>(d.Code, d.Name))
                .ToImmutableSortedDictionary();
        }

    }
}
