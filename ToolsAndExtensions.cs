using System.IO;
using System.Xml.Linq;
using TranscriptionCore.Serialization;

namespace TranscriptionCore
{
    public static class ToolsAndExtensions
    {
        public static bool CheckRequiredAttributes(this XElement elm, params string[] attributes)
        {
            foreach (var a in attributes)
            {
                if (elm.Attribute(a) == null)
                    return false;
            }

            return true;
        }

        public static bool Serialize(this Transcription transcription, string filename, bool savecompleteSpeakers = false)
        {
            using (FileStream s = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024))
            {
                bool output = transcription.Serialize(s, savecompleteSpeakers);
                if (!output)
                    return false;

                transcription.FileName = filename;
                transcription.Saved = true;

                return true;
            }
        }

        /// <summary> Serialize to V3 XML stream </summary>
        public static bool Serialize(this Transcription transcription, Stream datastream, bool saveSpeakersDetails = false)
        {
            XDocument xml = transcription.Serialize(saveSpeakersDetails);
            xml.Save(datastream);
            return true;
        }

        /// <summary> Serialize to V3 XML stream </summary>
        public static XDocument Serialize(this Transcription transcription, bool saveSpeakersDetails = false)
        {
            return SerializationV3.Serialize(transcription, saveSpeakersDetails);
        }

        /// <summary>
        /// BEWARE - SpeakerCollection is synchronized manually, It can contain different speakers than transcription
        /// </summary>
        /// <param name="saveAll">save including image and merges, used when saving database</param>
        /// <returns></returns>
        public static void Serialize(this SpeakerCollection speakers, string filename, bool saveAll = true)
        {
            var xelm = SerializationV3.Serialize(speakers, saveAll);
            xelm.Save(filename);
        }

        public static XElement Serialize(this Speaker speaker, bool saveAll = false)
            => SerializationV3.SerializeSpeaker(speaker, saveAll);
    }
}
