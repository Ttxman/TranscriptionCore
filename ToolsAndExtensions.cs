using System;
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
            into.DbId.DBtype = from.DbId.DBtype;
            into.Surname = from.Surname;
            into.FirstName = from.FirstName;
            into.MiddleName = from.MiddleName;
            into.DegreeBefore = from.DegreeBefore;
            into.DegreeAfter = from.DegreeAfter;
            into.DefaultLang = from.DefaultLang;
            into.Sex = from.Sex;
            into.ImgBase64 = from.ImgBase64;
            into.Merges = new List<SpeakerDbId>(from.Merges.Concat(into.Merges));

            if (from.DbId.DBtype != DBType.File && into.GetDbId() != from.DbId.DBID)
            {
                into.Merges.Add(new SpeakerDbId
                {
                    DBID = from.GetDbId(),
                    DBtype = from.DbId.DBtype
                });
            }

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

        /// <summary> Read the db id, it will be generated if not set yet </summary>
        public static string GetDbId(this Speaker speaker)
        {
            if (speaker.DbId.DBID == null && speaker.DbId.DBtype != DBType.File)
            {
                // NOTE: DBID is not used in case of file scope
                speaker.DbId.DBID = Guid.NewGuid().ToString();
            }

            return speaker.DbId.DBID;
        }

        public static void SetDbId(this Speaker speaker, string newId)
        {
            if (string.IsNullOrWhiteSpace(newId))
            {
                // request to reset DBID
                speaker.DbId.DBID = null;
                return;
            }

            if (string.IsNullOrWhiteSpace(speaker.DbId.DBID))
            {
                // first DBID assignment
                speaker.DbId.DBID = newId;
                return;
            }

            if (speaker.DbId.DBtype == DBType.User)
            {
                // modification is disabled for user db type TODO: why?
                throw new ArgumentException("cannot change DBID when Dabase is User");
            }

            speaker.DbId.DBID = newId;
        }
    }
}
