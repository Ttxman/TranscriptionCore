using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace TranscriptionCore.Serialization
{
    public static class SerializationV2
    {
        public const string VersionNumber = "2.0";

        /// <summary>
        /// deserialize transcription in v2 format
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="storage">data will be deserialized into this object, useful for overrides </param>
        internal static void Deserialize(XmlTextReader reader, Transcription storage)
        {
            Transcription data = storage;
            data.BeginUpdate();
            var document = XDocument.Load(reader);
            var transcription = document.Elements().First();

            string style = transcription.Attribute("style").Value;

            bool isStrict = style == "strict";
            string version = transcription.Attribute("version").Value;
            string mediaURI = transcription.Attribute("mediaURI").Value;
            data.MediaURI = mediaURI;
            data.Meta = transcription.Element("meta");
            if (data.Meta == null)
                data.Meta = Transcription.EmptyMeta();

            data.Elements = transcription.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);
            data.Elements.Remove("style");
            data.Elements.Remove("mediaURI");
            data.Elements.Remove("version");

            var chapters = transcription.Elements(isStrict ? "chapter" : "ch")
                .Select(c => (TranscriptionElement)DeserializeChapter(c, isStrict))
                .ToList();
            chapters.ForEach(c => data.Add(c));

            var speakers = transcription.Element(isStrict ? "speakers" : "sp");
            data.Speakers.Clear();
            data.Speakers.AddRange(speakers.Elements(isStrict ? "speaker" : "s").Select(s => DeserializeSpeaker(s, isStrict)));
            storage.AssingSpeakersByID();
            data.EndUpdate();
        }

        internal static Speaker DeserializeSpeaker(XElement s, bool isStrict)
        {
            if (!s.CheckRequiredAttributes("id", "surname"))
                throw new ArgumentException("required attribute missing on v2format speaker  (id, surname)");

            var sp = new Speaker();

            foreach (var attr in s.Attributes())
            {
                switch (attr.Name.ToString())
                {
                    case "id":
                        sp.SerializationID = int.Parse(attr.Value);
                        break;

                    case "firstname":
                        sp.FirstName = attr.Value;
                        break;

                    case "surname":
                        sp.Surname = attr.Value;
                        break;

                    case "lang":
                        sp.DefaultLang = attr.Value;
                        break;

                    case "sex":
                        sp.Sex = SexFromXmlValue(attr.Value);
                        break;

                    case "comment":
                        sp.Attributes.Add(new SpeakerAttribute("comment", "comment", attr.Value));
                        break;

                    default:
                        sp.Elements.Add(attr.Name.ToString(), attr.Value);
                        break;
                }
            }

            return sp;
        }

        public static TranscriptionChapter DeserializeChapter(XElement c, bool isStrict)
        {
            var chap = new TranscriptionChapter();
            chap.Name = c.Attribute("name").Value;
            chap.Elements = c.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);
            chap.Elements.Remove("name");
            foreach (var s in c.Elements(isStrict ? "section" : "se"))
            {
                var sec = DeserializeSection(s, isStrict);
                chap.Add(sec);
            }

            return chap;
        }

        public static TranscriptionSection DeserializeSection(XElement e, bool isStrict)
        {
            var tsec = new TranscriptionSection();
            tsec.Name = e.Attribute("name").Value;
            tsec.Elements = e.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);
            tsec.Elements.Remove("name");
            foreach (var p in e.Elements(isStrict ? "paragraph" : "pa"))
            {
                var para = DeserializeParagraph(p, isStrict);
                tsec.Add(para);
            }

            return tsec;
        }

        public static TranscriptionParagraph DeserializeParagraph(XElement e, bool isStrict)
        {
            var par = new TranscriptionParagraph();
            par._internalID = int.Parse(e.Attribute(isStrict ? "speakerid" : "s").Value);
            par.AttributeString = (e.Attribute(isStrict ? "attributes" : "a") ?? TranscriptionParagraph.EmptyAttribute).Value;

            par.Elements = e.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);
            par.Elements.Remove(isStrict ? "begin" : "b");
            par.Elements.Remove(isStrict ? "end" : "e");
            par.Elements.Remove(isStrict ? "attributes" : "a");
            par.Elements.Remove(isStrict ? "speakerid" : "s");

            foreach (var p in e.Elements(isStrict ? "phrase" : "p"))
            {
                var phr = DeserializePhrase(p, isStrict);
                par.Add(phr);
            }

            if (e.Attribute(isStrict ? "attributes" : "a") != null)
                par.AttributeString = e.Attribute(isStrict ? "attributes" : "a").Value;

            if (e.Attribute(isStrict ? "begin" : "b") != null)
            {
                string val = e.Attribute(isStrict ? "begin" : "b").Value;
                par.Begin = int.TryParse(val, out int ms)
                    ? TimeSpan.FromMilliseconds(ms)
                    : XmlConvert.ToTimeSpan(val);
            }
            else
            {
                var ch = par._children.FirstOrDefault();
                par.Begin = ch?.Begin ?? TimeSpan.Zero;
            }

            if (e.Attribute(isStrict ? "end" : "e") != null)
            {
                string val = e.Attribute(isStrict ? "end" : "e").Value;
                par.End = int.TryParse(val, out int ms)
                    ? TimeSpan.FromMilliseconds(ms)
                    : XmlConvert.ToTimeSpan(val);
            }
            else
            {
                var ch = par._children.LastOrDefault();
                par.End = ch?.Begin ?? TimeSpan.Zero;
            }

            return par;
        }

        public static TranscriptionPhrase DeserializePhrase(XElement e, bool isStrict)
        {
            var phr = new TranscriptionPhrase();
            phr.Elements = e.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);
            phr.Elements.Remove(isStrict ? "begin" : "b");
            phr.Elements.Remove(isStrict ? "end" : "e");
            phr.Elements.Remove(isStrict ? "fon" : "f");

            phr._phonetics = (e.Attribute(isStrict ? "fon" : "f") ?? TranscriptionPhrase.EmptyAttribute).Value;
            phr._text = e.Value.Trim('\r', '\n');
            if (e.Attribute(isStrict ? "begin" : "b") != null)
            {
                string val = e.Attribute(isStrict ? "begin" : "b").Value;
                phr.Begin = int.TryParse(val, out int ms)
                    ? TimeSpan.FromMilliseconds(ms)
                    : XmlConvert.ToTimeSpan(val);
            }

            if (e.Attribute(isStrict ? "end" : "e") != null)
            {
                string val = e.Attribute(isStrict ? "end" : "e").Value;
                phr.End = int.TryParse(val, out int ms)
                    ? TimeSpan.FromMilliseconds(ms)
                    : XmlConvert.ToTimeSpan(val);
            }

            return phr;
        }

        static Speaker.Sexes SexFromXmlValue(string value)
        {
            switch (value)
            {
                case "M": return Speaker.Sexes.Male;
                case "F": return Speaker.Sexes.Female;
                default: return Speaker.Sexes.X;
            }
        }
    }
}
