using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;


namespace TranscriptionCore
{
    public partial class Transcription
    {
        public bool Serialize(string filename, bool savecompleteSpeakers = false)
        {
            using FileStream s = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024);

            bool output = Serialize(s, savecompleteSpeakers);

            if (output)
            {
                this.FileName = filename;
                this.Saved = true;

                return true;
            }
            else
            {
                return false;
            }
        }

        public XDocument Serialize(bool SaveSpeakersDetailed = false)
        {
            ReindexSpeakers();
            XElement pars = new XElement("transcription", Elements.Select(e => new XAttribute(e.Key, e.Value)).Union(new[] {
                        new XAttribute("version", "3.0"),
                        new XAttribute("mediauri", MediaURI ?? ""),
                        new XAttribute("created",Created)
                        }),
                        this.Meta,
                        Chapters.Select(c => c.Serialize()),
                        SerializeSpeakers(SaveSpeakersDetailed)
                    );

            if (!string.IsNullOrWhiteSpace(this.DocumentID))
                pars.Add(new XAttribute("documentid", this.DocumentID));

            XDocument xdoc =
                new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
                    pars
                );

            return xdoc;
        }


        public bool Serialize(Stream datastream, bool SaveSpeakersDetailed = false)
        {
            XDocument xdoc = Serialize(SaveSpeakersDetailed);
            xdoc.Save(datastream);
            return true;
        }

        private void ReindexSpeakers()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var speakers = Paragraphs.Select(p => p.Speaker).Where(s => s != Speaker.DefaultSpeaker && s.SerializationID != Speaker.DefaultID).Distinct().ToList();
            for (int i = 0; i < speakers.Count; i++)
                speakers[i].SerializationID = i;
#pragma warning restore CS0618 // Type or member is obsolete
        }

        private XElement SerializeSpeakers(bool SaveSpeakersDetailed)
        {
            var speakers = Paragraphs.Select(p => p.Speaker)
#pragma warning disable CS0618 // Type or member is obsolete
                .Where(s => s != Speaker.DefaultSpeaker && s.SerializationID != Speaker.DefaultID)
#pragma warning restore CS0618 // Type or member is obsolete
                .Concat(_speakers.Where(s => s.PinnedToDocument))
                .Distinct()
                .ToList();

            return new SpeakerCollection(speakers).Serialize(SaveSpeakersDetailed);
        }


        public static Transcription Deserialize(string filename)
        {
            Transcription tr = new Transcription();
            Deserialize(filename, tr);
            return tr;
        }


        public static void Deserialize(string filename, Transcription storage)
        {
            using Stream s = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            Deserialize(s, storage);
            if (storage is { })
            {
                storage.FileName = filename;
                storage.Saved = true;
            }
        }


        public static Transcription Deserialize(Stream datastream)
        {
            Transcription tr = new Transcription();
            Deserialize(datastream, tr);
            return tr;
        }


        public static void Deserialize(Stream datastream, Transcription storage)
        {
            storage.Updates.BeginUpdate(false);
            try
            {
                XmlTextReader reader = new XmlTextReader(datastream);
                if (reader.Read())
                {
                    reader.Read();
                    reader.Read();
                    string version = reader.GetAttribute("version") ?? "";

                    if (version == "3.0")
                        DeserializeV3(reader, storage);
                    else if (version == "2.0")
                        DeserializeV2_0(reader, storage);
                    else
                        throw new NotImplementedException("Support for internal formats older than V2 was removed");
                }
            }
            finally
            {
                storage.Updates.EndUpdate();
            }
        }

        /// <summary>
        /// deserialize transcription in v2 format
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="storage">data will be deserialized into this object, useful for overrides </param>
        private static void DeserializeV2_0(XmlTextReader reader, Transcription storage)
        {

            Transcription data = storage;
            data.Updates.BeginUpdate(false);
            var document = XDocument.Load(reader);
            var transcription = document.Elements().First();

            string? style = transcription.Attribute("style")?.Value;

            bool isStrict = style == "strict";
            string? version = transcription.Attribute("version")?.Value;
            string? mediaURI = transcription.Attribute("mediaURI")?.Value;
            data.MediaURI = mediaURI ?? "";
            data.Meta = transcription.Element("meta") ?? EmptyMeta();

            var chapters = transcription.Elements(isStrict ? "chapter" : "ch");

            var elms = transcription.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);
            elms.Remove("style");
            elms.Remove("mediaURI");
            elms.Remove("version");

            data.Elements = data.Elements.AddRange(elms);

            chapters.Select(c => TranscriptionChapter.DeserializeV2(c, isStrict)).ToList().ForEach(c => data.Add(c));

            var speakers = transcription.Element(isStrict ? "speakers" : "sp")!;
            data.Speakers.Clear();
            foreach (var spkr in speakers.Elements(isStrict ? "speaker" : "s").Select(s => Speaker.DeserializeV2(s, isStrict)))
                data.Speakers.Add(spkr);

            storage.AssingSpeakersByID();
            data.Updates.EndUpdate();
        }

        /// <summary>
        /// deserialize transcription in v3 format
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="storage">data will be deserialized into this object, useful for overrides </param>
        private static void DeserializeV3(XmlTextReader reader, Transcription storage)
        {

            Transcription data = storage;
            data.Updates.BeginUpdate(false);
            var document = XDocument.Load(reader);
            var transcription = document.Elements().First();

            string version = transcription.Attribute("version")?.Value ?? "";
            string mediaURI = transcription.Attribute("mediauri")?.Value ?? "";
            data.MediaURI = mediaURI;
            data.Meta = transcription.Element("meta") ?? EmptyMeta();
            var chapters = transcription.Elements("ch");


            var elms = transcription.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);
            if (elms.TryGetValue("documentid", out string? did))
                data.DocumentID = did;
            if (elms.TryGetValue("created", out did))
                data.Created = XmlConvert.ToDateTime(did, XmlDateTimeSerializationMode.Unspecified);


            elms.Remove("style");
            elms.Remove("mediauri");
            elms.Remove("version");
            elms.Remove("documentid");
            elms.Remove("created");

            data.Elements = data.elements.AddRange(elms);

            foreach (var c in chapters.Select(c => new TranscriptionChapter(c)))
                data.Add(c);

            data.Speakers = new SpeakerCollection(transcription.Element("sp")!);
            storage.AssingSpeakersByID();
            data.Updates.EndUpdate();
        }

        /// <summary>
        /// Assigns Speaker (from internal Speaker pool Transcription.Speakers) to all paragraphs in Transcription by ID. Default speaker (Speaker.DefaultSpeaker) is assgned when no speaker si found
        /// </summary>
        private void AssingSpeakersByID()
        {
            foreach (var par in Paragraphs)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                var sp = _speakers.FirstOrDefault(s => s.SerializationID == par.InternalID);
#pragma warning restore CS0618 // Type or member is obsolete
                if (sp is { })
                {
                    par.Speaker = sp;
                }
                else
                {
                    par.Speaker = Speaker.DefaultSpeaker;
                }
            }
        }
    }
}
