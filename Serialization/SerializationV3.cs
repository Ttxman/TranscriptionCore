using System;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace TranscriptionCore.Serialization
{
    public static class SerializationV3
    {
        public const string VersionNumber = "3.0";

        static readonly CultureInfo csCulture = CultureInfo.CreateSpecificCulture("cs");

        public static XDocument Serialize(Transcription transcription, bool saveSpeakerDetails = false)
        {
            transcription.ReindexSpeakers();

            var xmlAttributes = transcription.Elements.Select(e => new XAttribute(e.Key, e.Value)).Union(new[]
            {
                new XAttribute("version", VersionNumber),
                new XAttribute("mediauri", transcription.MediaURI ?? ""),
                new XAttribute("created", transcription.Created)
            });

            var pars = new XElement("transcription", xmlAttributes,
                transcription.Meta,
                transcription.Chapters.Select(c => SerializeChapter(c)),
                SerializeSpeakers(transcription, saveSpeakerDetails)
            );

            if (!string.IsNullOrWhiteSpace(transcription.DocumentID))
                pars.Add(new XAttribute("documentid", transcription.DocumentID));

            var xdoc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), pars);
            return xdoc;
        }

        internal static XElement SerializeSpeakers(Transcription transcription, bool saveSpeakerDetails)
        {
            var speakers = transcription.EnumerateParagraphs()
                .Select(p => p.Speaker)
                .Where(s => s != Speaker.DefaultSpeaker && s.SerializationID != Speaker.DefaultID)
                .Concat(transcription._speakers.Where(s => s.PinnedToDocument))
                .Distinct()
                .ToList();
            var collection = new SpeakerCollection(speakers);
            return Serialize(collection, saveSpeakerDetails);
        }

        /// <summary>
        /// BEWARE - SpeakerCollection is synchronized manually, It can contain different speakers than transcription
        /// </summary>
        /// <param name="saveAll">save including image and merges, used when saving database</param>
        /// <returns></returns>
        public static XElement Serialize(SpeakerCollection collection, bool saveAll = true)
        {
            XElement elm = new XElement("sp",
                collection.elements.Select(e => new XAttribute(e.Key, e.Value)),
                collection._Speakers.Select(s => SerializeSpeaker(s, saveAll))
            );

            return elm;
        }

        /// <summary> serialize speaker </summary>
        /// <param name="saveAll">save including image and merges, used when saving database</param>
        public static XElement SerializeSpeaker(Speaker speaker, bool saveAll = false)
        {
            XElement elm = new XElement("s",
                speaker.Elements.Select(e =>
                        new XAttribute(e.Key, e.Value))
                    .Union(new[]{
                        new XAttribute("id", speaker.SerializationID.ToString()),
                        new XAttribute("surname", speaker.Surname),
                        new XAttribute("firstname", speaker.FirstName),
                        new XAttribute("sex", SexToXmlValue(speaker.Sex)),
                        new XAttribute("lang", speaker.DefaultLang.ToLower())
                    })
            );

            string val = "file";
            if (speaker.DataBaseType != DBType.File)
            {
                elm.Add(new XAttribute("dbid", speaker.GetDbId()));
                elm.Add(new XAttribute("dbtype", DBTypeToXmlValue(speaker.DataBaseType)));
            }

            if (!string.IsNullOrWhiteSpace(speaker.MiddleName))
                elm.Add(new XAttribute("middlename", speaker.MiddleName));

            if (!string.IsNullOrWhiteSpace(speaker.DegreeBefore))
                elm.Add(new XAttribute("degreebefore", speaker.DegreeBefore));

            if (!string.IsNullOrWhiteSpace(speaker.DegreeAfter))
                elm.Add(new XAttribute("degreeafter", speaker.DegreeAfter));

            if (speaker.DataBaseType != DBType.File)
                elm.Add(new XAttribute("synchronized", DateTimeToXmlValue(DateTime.UtcNow)));

            if (speaker.PinnedToDocument)
                elm.Add(new XAttribute("pinned", true));

            if (saveAll)
                foreach (var m in speaker.Merges)
                    elm.Add(Serialize(m));

            foreach (var a in speaker.Attributes)
                elm.Add(Serialize(a));

            return elm;
        }



        /// <summary>
        /// deserialize transcription in v3 format
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="storage">data will be deserialized into this object, useful for overrides </param>
        public static void Deserialize(XmlTextReader reader, Transcription storage)
        {
            storage.BeginUpdate();
            var document = XDocument.Load(reader);
            var transcription = document.Elements().First();

            //string version = transcription.Attribute("version").Value;
            string mediaURI = transcription.Attribute("mediauri").Value;
            storage.MediaURI = mediaURI;
            storage.Meta = transcription.Element("meta");
            if (storage.Meta == null)
                storage.Meta = Transcription.EmptyMeta();
            var chapters = transcription.Elements("ch");


            storage.Elements = transcription.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);
            string did;
            if (storage.Elements.TryGetValue("documentid", out did))
                storage.DocumentID = did;
            if (storage.Elements.TryGetValue("created", out did))
                storage.Created = XmlConvert.ToDateTime(did, XmlDateTimeSerializationMode.Unspecified);


            storage.Elements.Remove("style");
            storage.Elements.Remove("mediauri");
            storage.Elements.Remove("version");
            storage.Elements.Remove("documentid");
            storage.Elements.Remove("created");

            foreach (var c in chapters.Select(c => new TranscriptionChapter(c)))
                storage.Add(c);

            storage.Speakers = DeserializeSpeakerCollection(transcription.Element("sp"));
            storage.AssingSpeakersByID();
            storage.EndUpdate();
        }

        public static SpeakerCollection DeserializeSpeakerCollection(XElement xml)
        {
            var result = new SpeakerCollection
            {
                elements = xml.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value),
                _Speakers = xml.Elements("s").Select(DeserializeSpeaker).ToList()
            };
            return result;
        }

        public static Speaker DeserializeSpeaker(XElement xml)
        {
            var sp = new Speaker();
            Deserialize(xml, sp);
            return sp;
        }

        public static void Deserialize(XElement xml, Speaker sp)
        {
            if (!xml.CheckRequiredAttributes("id", "surname", "firstname", "sex", "lang"))
                throw new ArgumentException("required attribute missing on speaker (id, surname, firstname, sex, lang)");

            sp.SerializationID = int.Parse(xml.Attribute("id").Value);
            sp.Surname = xml.Attribute("surname")?.Value ?? "";
            sp.FirstName = xml.Attribute("firstname")?.Value ?? "";
            sp.Sex = XmlValueToSex(xml.Attribute("sex")?.Value);
            sp.DefaultLang = xml.Attribute("lang").Value.ToUpper();

            //merges
            sp.Merges.AddRange(xml.Elements("m").Select(m => new DBMerge(m.Attribute("dbid").Value, XmlValueToDBType(m.Attribute("dbtype").Value))));
            sp.Attributes.AddRange(xml.Elements("a").Select(e => DeserializeAttribute(e)));
            sp.Elements = xml.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);

            if (sp.Elements.TryGetValue("dbid", out string rem))
            {
                sp.SetDbId(rem);
                if (sp.Elements.TryGetValue("dbtype", out rem))
                    sp.DataBaseType = XmlValueToDBType(rem);

            }

            if (sp.Elements.TryGetValue("synchronized", out rem))
            {
                sp.Synchronized = string.IsNullOrWhiteSpace(rem)
                    ? DateTime.Now
                    : XmlValueToDateTime(rem);
            }

            if (sp.Elements.TryGetValue("middlename", out rem))
                sp.MiddleName = rem;

            if (sp.Elements.TryGetValue("degreebefore", out rem))
                sp.DegreeBefore = rem;

            if (sp.Elements.TryGetValue("pinned", out rem))
                sp.PinnedToDocument = XmlConvert.ToBoolean(rem);

            if (sp.Elements.TryGetValue("degreeafter", out rem))
                sp.DegreeAfter = rem;

            sp.Elements.Remove("id");
            sp.Elements.Remove("surname");
            sp.Elements.Remove("firstname");
            sp.Elements.Remove("sex");
            sp.Elements.Remove("lang");

            sp.Elements.Remove("dbid");
            sp.Elements.Remove("dbtype");
            sp.Elements.Remove("middlename");
            sp.Elements.Remove("degreebefore");
            sp.Elements.Remove("degreeafter");
            sp.Elements.Remove("synchronized");

            sp.Elements.Remove("pinned");
        }

        public static XElement SerializeChapter(TranscriptionChapter chapter)
        {
            XElement elm = new XElement("ch",
                chapter.Elements.Select(e => new XAttribute(e.Key, e.Value)).Union(new[] { new XAttribute("name", chapter.Name) }),
                chapter.Sections.Select(s => s.Serialize())
            );

            return elm;
        }

        static SpeakerAttribute DeserializeAttribute(XElement elm)
        {
            var name = elm.Attribute("name").Value;

            var dateValue = elm.Attribute("date");
            var date = dateValue != null
                ? XmlValueToDateTime(dateValue.Value)
                : default;

            return new SpeakerAttribute(
                default,
                name,
                elm.Value,
                date);
        }

        public static XElement Serialize(DBMerge merge)
            => new XElement("m",
                new XAttribute("dbid", merge.DBID),
                new XAttribute("dbtype", DBTypeToXmlValue(merge.DBtype)));

        public static XElement Serialize(SpeakerAttribute attribute)
        {
            return new XElement("a",
                new XAttribute("name", attribute.Name),
                new XAttribute("date", DateTimeToXmlValue(attribute.Date)),
                attribute.Value
            );
        }

        static string SexToXmlValue(Speaker.Sexes sex)
        {
            switch (sex)
            {
                case Speaker.Sexes.Male: return "m";
                case Speaker.Sexes.Female: return "f";
                default: return "x";
            }
        }

        static Speaker.Sexes XmlValueToSex(string sex)
        {
            switch (sex)
            {
                case "m": return Speaker.Sexes.Male;
                case "f": return Speaker.Sexes.Female;
                default: return Speaker. Sexes.X;
            }
        }

        static string DBTypeToXmlValue(DBType dbType)
        {
            switch (dbType)
            {
                case DBType.Api: return "api";
                case DBType.User: return "user";
                default: return "file";
            }
        }

        static DBType XmlValueToDBType(string rem)
        {
            switch (rem)
            {
                case "api": return DBType.Api;
                case "user": return DBType.User;
                default: return DBType.File;
            }
        }

        static string DateTimeToXmlValue(DateTime dateTime)
            => XmlConvert.ToString(dateTime, XmlDateTimeSerializationMode.Utc); //stored in UTC convert from local

        static DateTime XmlValueToDateTime(string xmlValue)
        {
            //problem with saving datetimes in local format
            try
            {
                return XmlConvert.ToDateTime(xmlValue, XmlDateTimeSerializationMode.Local); //stored in UTC convert to local
            }
            catch
            {
                return DateTime.TryParse(xmlValue, csCulture, DateTimeStyles.None, out var date)
                    ? TimeZoneInfo.ConvertTimeFromUtc(date, TimeZoneInfo.Local)
                    : DateTime.Now;
            }

        }
    }
}
