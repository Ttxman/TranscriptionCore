using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        /// <summary> copies all info, and generates new DBI and ID .... (deep copy) </summary>
        public static Speaker CreateCopy(this Speaker original)
        {
            var speaker = new Speaker();
            MergeFrom(speaker, original);
            return speaker;
        }

        /// <summary> update values from another speaker .. used for merging, probably not doing what user assumes :) </summary>
        static void MergeFrom(Speaker into, Speaker from)
        {
            into.DataBaseType = from.DataBaseType;
            into.Surname = from.Surname;
            into.FirstName = from.FirstName;
            into.MiddleName = from.MiddleName;
            into.DegreeBefore = from.DegreeBefore;
            into.DegreeAfter = from.DegreeAfter;
            into.DefaultLang = from.DefaultLang;
            into.Sex = from.Sex;
            into.ImgBase64 = from.ImgBase64;
            into.Merges = new List<DBMerge>(from.Merges.Concat(into.Merges));

            if (from.DBType != DBType.File && into.DBID != from.DBID)
                into.Merges.Add(new DBMerge(from.DBID, from.DataBaseType));

            into.Attributes = into.Attributes
                .Concat(from.Attributes).GroupBy(a => a.Name) // one group for each attribute name
                .SelectMany(g => g.Distinct(SpeakerAttribute.Comparer.Instance)) // unique attributes in each group (TODO: why?)
                .ToList();
        }

        public static SpeakerAttribute CreateCopy(this SpeakerAttribute original)
            => new SpeakerAttribute(
                original.ID,
                original.Name,
                original.Value,
                original.Date);
    }
}
