using System.IO;
using System.Xml;

namespace TranscriptionCore.Serialization
{

    /// <summary> Automatically uses proper serializer version for deserialization </summary>
    public static class SerializationSelector
    {
        public static void Deserialize(Stream dataStream, Transcription storage)
        {
            storage.BeginUpdate(false);
            XmlTextReader reader = new XmlTextReader(dataStream);
            if (reader.Read())
            {
                reader.Read();
                reader.Read();
                string version = reader.GetAttribute("version");

                switch (version)
                {
                    // V3
                    case SerializationV3.VersionNumber:
                        SerializationV3.Deserialize(reader, storage);
                        break;

                    // V2
                    case SerializationV2.VersionNumber:
                        SerializationV2.Deserialize(reader, storage);
                        break;

                    // V1
                    default:
                        dataStream.Position = 0;
                        SerializationV1.Deserialize(new XmlTextReader(dataStream), storage);
                        break;
                }
            }
            storage.EndUpdate();
        }

        public static void Deserialize(string filename, Transcription storage)
        {
            using (Stream s = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Deserialize(s, storage);
                if (storage != null)
                {
                    storage.FileName = filename;
                    storage.Saved = true;
                }
            }
        }
    }
}
