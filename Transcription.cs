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
    public class Transcription
    {
        public bool Revert(Undo act)
        {
            switch (act)
            {
                case CustomElementsChanged cec:
                    Elements = cec.Old;
                    return true;
            }
            return false;
        }

        private record NameChanged(string Old) : Undo { }
        public record CustomElementsChanged(ImmutableDictionary<string, string> Old) : Undo;

        public bool FindNext(ref TranscriptionParagraph paragraph, ref int TextOffset, out int length, string pattern, bool isregex, bool CaseSensitive, bool searchinspeakers)
        {
            length = 0;
            if (paragraph is null)
                return false;

            if (searchinspeakers)
            {
                foreach (TranscriptionParagraph pr in paragraph.EnumerateNext())
                {
                    if (pr.Speaker.FullName.ToLower().Contains(pattern.ToLower()))
                    {
                        paragraph = pr;
                        TextOffset = 0;
                        return true;
                    }
                }
                return false;
            }

            Regex r;
            if (isregex)
            {
                r = new Regex(pattern);
            }
            else
            {
                if (!CaseSensitive)
                    pattern = pattern.ToLower();
                r = new Regex(Regex.Escape(pattern));
            }

            var par = paragraph;
            while (par is { })
            {
                string s = par.Text;
                if (!CaseSensitive && !isregex)
                    s = s.ToLower();
                if (TextOffset >= s.Length)
                    TextOffset = 0;
                Match m = r.Match(s, TextOffset);

                if (m.Success)
                {
                    TextOffset = m.Index;
                    length = m.Length;
                    paragraph = par;
                    return true;
                }

                par = par.Next();
                if (par is null)
                    return false;
                TextOffset = 0;
            }

            return false;
        }

        public string? FileName { get; set; }

        private bool _saved;
        public bool Saved
        {
            get
            {
                return _saved;
            }
            set
            {
                _saved = value;
            }
        }

        public string DocumentID { get; set; }
        public DateTime Created { get; set; }

        /// <summary>
        /// file containing audio data for transcription
        /// </summary>
        public string? MediaURI { get; set; }


        /// <summary>
        /// all chapters in transcription
        /// </summary>
        public UndoableCollection<TranscriptionChapter> Chapters { get; }

        [XmlElement("SpeakersDatabase")]
        private SpeakerCollection _speakers = new SpeakerCollection();

        [XmlIgnore]
        public SpeakerCollection Speakers
        {
            get { return _speakers; }
            set { _speakers = value; }
        }



        public Transcription()
        {
            FileName = null;
            Saved = false;
            DocumentID = Guid.NewGuid().ToString();
            Created = DateTime.UtcNow;

            void childAdded(TranscriptionChapter child, int index)
            {
                child.Parent = this;
                child.ParentIndex = index;
            }

            void childRemoved(TranscriptionChapter child)
            {
                child.Parent = null;
                child.ParentIndex = -1;
            }

            Chapters = new UndoableCollection<TranscriptionChapter>()
            {
                OnAdd = childAdded,
                OnRemoved = childRemoved
            };

            Chapters.Update.ContentChanged = OnChange;
            Updates = new UpdateTracker()
            {
                ContentChanged = OnChange
            };
        }
        public UpdateTracker Updates { get; }

        internal void OnChange(IEnumerable<Undo> undos)
        {

        }



        /// <summary>
        /// copy contructor
        /// </summary>
        /// <param name="toCopy"></param>
        public Transcription(Transcription toCopy) : this()
        {
            this.MediaURI = toCopy.MediaURI;
            this.Created = toCopy.Created;
            if (toCopy.Chapters is { })
            {
                Chapters.Update.BeginUpdate(false);
                for (int i = 0; i < toCopy.Chapters.Count; i++)
                    Chapters.Add(new TranscriptionChapter(toCopy.Chapters[i]));
                Chapters.Update.EndUpdate();
            }
            this.FileName = toCopy.FileName;
            this._speakers = new SpeakerCollection(toCopy._speakers);
            this.Saved = toCopy.Saved;
        }

        /// <summary>
        /// automaticly deserialize from file
        /// </summary>
        /// <param name="path"></param>
        public Transcription(string path) : this()
        {
            Deserialize(path, this);
        }
        public Transcription(FileInfo f) : this(f.FullName)
        {

        }

        public IEnumerable<TranscriptionSection> Sections
            => Chapters
                .SelectMany(c => c.Sections);
        public IEnumerable<TranscriptionParagraph> Paragraphs
            => Sections
                .SelectMany(s => s.Paragraphs);

        public IEnumerable<TranscriptionParagraph> ParagraphsAt(TimeSpan time)
            => Paragraphs
                .Where(p => p.Begin <= time && p.End > time);

        public TranscriptionParagraph? LastParagraphBefore(TimeSpan time)
            => Paragraphs
                .TakeWhile(p => p.End < time)
                .LastOrDefault();


        public TranscriptionParagraph? LastParagraphStartingBefore(TimeSpan time)
            => Paragraphs
                .TakeWhile(p => p.Begin < time)
                .LastOrDefault();

        /// <summary>
        /// Remove speaker from all paragraphs (Paragraphs will return default Speaker.DefaultSpeaker) and from internal database (if not pinned)
        /// </summary>
        /// <param name="speaker"></param>
        /// <returns></returns>
        public bool RemoveSpeaker(Speaker speaker)
        {
            try
            {
                if (this._speakers.Contains(speaker))
                {
                    if (!speaker.PinnedToDocument)
                        _speakers.Remove(speaker);

                    Saved = false;
                    foreach (var p in Paragraphs)
                        if (p.Speaker == speaker)
                            p.Speaker = Speaker.DefaultSpeaker;

                    Saved = false;
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Replace speaker in all paragraphs and in databse
        /// !!!also can modify Transcription.Speakers order
        /// </summary>
        /// <param name="aSpeaker"></param>
        /// <returns></returns>
        public void ReplaceSpeaker(Speaker toReplace, Speaker replacement)
        {
            if (_speakers.Contains(toReplace))
            {
                _speakers.Remove(toReplace);
                if (!_speakers.Contains(replacement))
                    _speakers.Add(replacement);
            }

            foreach (var p in Paragraphs)
                if (p.Speaker == toReplace)
                    p.Speaker = replacement;

        }

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
            var speakers = Paragraphs.Select(p => p.Speaker).Where(s => s != Speaker.DefaultSpeaker && s.SerializationID != Speaker.DefaultID).Distinct().ToList();
            for (int i = 0; i < speakers.Count; i++)
                speakers[i].SerializationID = i;
        }

        private XElement SerializeSpeakers(bool SaveSpeakersDetailed)
        {
            var speakers = Paragraphs.Select(p => p.Speaker)
                .Where(s => s != Speaker.DefaultSpeaker && s.SerializationID != Speaker.DefaultID)
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
            XmlTextReader reader = new XmlTextReader(datastream);
            if (reader.Read())
            {
                reader.Read();
                reader.Read();
                string version = reader.GetAttribute("version") ?? "";

                if (version == "3.0")
                {
                    DeserializeV3(reader, storage);
                }
                else if (version == "2.0")
                {
                    DeserializeV2_0(reader, storage);
                }
                else
                {
                    datastream.Position = 0;
                    DeserializeV1(new XmlTextReader(datastream), storage);
                }
            }
            storage.Updates.EndUpdate();
        }

        public XElement Meta = EmptyMeta();
        private static XElement EmptyMeta()
        {
            return new XElement("meta");
        }

        private ImmutableDictionary<string, string> elements = ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase);
        public ImmutableDictionary<string, string> Elements
        {

            get => elements;
            set
            {
                var oldv = elements;
                elements = value;
                Updates.OnContentChanged(new CustomElementsChanged(oldv));
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
            data.Speakers.AddRange(speakers.Elements(isStrict ? "speaker" : "s").Select(s => Speaker.DeserializeV2(s, isStrict)));
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
        /// read old transcription format (v1)
        /// v1 format is mess, it was direct serialization of internal data structures
        /// documents in this format most probably do not exists anymore
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="storage">deserializes into this transcription (useful for overrides)</param>
        /// <returns>deserialized transcription</returns>
        /// <exception cref="NanotransSerializationException"></exception>
        public static void DeserializeV1(XmlTextReader reader, Transcription storage)
        {
            try
            {
                Transcription data = storage;
                reader.WhitespaceHandling = WhitespaceHandling.Significant;

                reader.Read(); //<?xml version ...
                reader.Read();
                data.MediaURI = reader.GetAttribute("audioFileName") ?? "";
                string? val = reader.GetAttribute("dateTime");
                if (val is { })
                    data.Created = XmlConvert.ToDateTime(val, XmlDateTimeSerializationMode.Local);

                reader.ReadStartElement("Transcription");


                //reader.Read();
                reader.ReadStartElement("Chapters");
                //reader.ReadStartElement("Chapter");
                while (reader.Name == "Chapter")
                {
                    TranscriptionChapter c = new TranscriptionChapter();
                    c.Name = reader.GetAttribute("name") ?? "";

                    val = reader.GetAttribute("begin");
                    val = reader.GetAttribute("end");

                    reader.Read();
                    reader.ReadStartElement("Sections");


                    while (reader.Name == "Section")
                    {

                        TranscriptionSection s = new TranscriptionSection();
                        s.Name = reader.GetAttribute("name") ?? "";

                        val = reader.GetAttribute("begin");
                        val = reader.GetAttribute("end");

                        reader.Read();
                        reader.ReadStartElement("Paragraphs");
                        int result = -1;
                        while (reader.Name == "Paragraph")
                        {
                            TranscriptionParagraph p = new TranscriptionParagraph();
                            val = reader.GetAttribute("begin");
                            if (int.TryParse(val, out result))
                                if (result < 0)
                                    if (result == -1 && s.Paragraphs.Count > 0)
                                        p.Begin = s.Paragraphs[^1].End;
                                    else
                                        p.Begin = new TimeSpan(result);
                                else
                                    p.Begin = TimeSpan.FromMilliseconds(result);
                            else if (val is { })
                                p.Begin = XmlConvert.ToTimeSpan(val);

                            val = reader.GetAttribute("end");
                            if (int.TryParse(val, out result))
                                if (result < 0)
                                    p.End = new TimeSpan(result);
                                else
                                    p.End = TimeSpan.FromMilliseconds(result);
                            else if (val is { })
                                p.End = XmlConvert.ToTimeSpan(val);

                            reader.GetAttribute("trainingElement");
                            if (reader.GetAttribute("Attributes") is { } atts)
                                p.Attributes = p.Attributes.Union(atts.Split('|'));

                            reader.Read();
                            reader.ReadStartElement("Phrases");

                            while (reader.Name == "Phrase")
                            {
                                TranscriptionPhrase ph = new TranscriptionPhrase();
                                val = reader.GetAttribute("begin");
                                if (int.TryParse(val, out result))
                                    if (result < 0)
                                        ph.Begin = new TimeSpan(result);
                                    else
                                        ph.Begin = TimeSpan.FromMilliseconds(result);
                                else if (val is { })
                                    ph.Begin = XmlConvert.ToTimeSpan(val);

                                val = reader.GetAttribute("end");
                                if (int.TryParse(val, out result))
                                    if (result < 0)
                                        ph.End = new TimeSpan(result);
                                    else
                                        ph.End = TimeSpan.FromMilliseconds(result);
                                else if (val is { })
                                    ph.End = XmlConvert.ToTimeSpan(val);

                                reader.Read();//Text;

                                if (reader.IsEmptyElement)
                                    reader.Read();

                                while (reader.Name == "Text")
                                {
                                    reader.WhitespaceHandling = WhitespaceHandling.All;
                                    if (!reader.IsEmptyElement)
                                    {
                                        reader.Read();
                                        while (reader.NodeType != XmlNodeType.EndElement && reader.NodeType != XmlNodeType.Element)
                                        {
                                            ph.Text = reader.Value.Trim('\r', '\n');
                                            reader.Read();
                                        }
                                    }
                                    reader.WhitespaceHandling = WhitespaceHandling.Significant;
                                    reader.ReadEndElement();//text
                                }
                                p.Phrases.Add(ph);
                                if (reader.Name != "Phrase") //text nebyl prazdny
                                {
                                    reader.Read();//text;
                                    reader.ReadEndElement();//Text;
                                }
                                reader.ReadEndElement();//Phrase;

                            }

                            if (reader.Name != "speakerID")
                                reader.ReadEndElement();//Phrases - muze byt emptyelement a ten nema end..

                            p.InternalID = XmlConvert.ToInt32(reader.ReadElementString());
                            if (p.InternalID == -1)
                                p.InternalID = Speaker.DefaultID;

                            reader.ReadEndElement();//paragraph
                            s.Paragraphs.Add(p);

                        }

                        if (reader.Name == "Paragraphs") //teoreticky mohl byt prazdny
                            reader.ReadEndElement();

                        if (reader.Name == "PhoneticParagraphs")
                            reader.ReadStartElement();

                        while (reader.Name == "Paragraph")
                        {
                            TranscriptionParagraph p = new TranscriptionParagraph();
                            val = reader.GetAttribute("begin");
                            if (int.TryParse(val, out result))
                                if (result < 0)
                                    p.Begin = TimeSpan.Zero;
                                else
                                    p.Begin = TimeSpan.FromMilliseconds(result);
                            else if (val is { })
                                p.Begin = XmlConvert.ToTimeSpan(val);

                            val = reader.GetAttribute("end");
                            if (int.TryParse(val, out result))
                                if (result < 0)
                                    if (result == -1)
                                    {
                                        p.Begin = s.Paragraphs[^1].End;
                                    }
                                    else
                                        p.End = TimeSpan.FromMilliseconds(result);
                                else
                                    p.End = TimeSpan.FromMilliseconds(result);
                            else if (val is { })
                                p.End = XmlConvert.ToTimeSpan(val);

                            reader.GetAttribute("trainingElement");
                            if (reader.GetAttribute("Attributes") is { } atts)
                                p.Attributes = p.Attributes.Union(atts.Split('|'));

                            reader.Read();
                            reader.ReadStartElement("Phrases");

                            while (reader.Name == "Phrase")
                            {
                                TranscriptionPhrase ph = new TranscriptionPhrase();
                                val = reader.GetAttribute("begin");
                                if (int.TryParse(val, out result))
                                    if (result < 0)
                                        ph.Begin = p.Begin;
                                    else
                                        ph.Begin = TimeSpan.FromMilliseconds(result);
                                else if (val is { })
                                    ph.Begin = XmlConvert.ToTimeSpan(val);

                                val = reader.GetAttribute("end");
                                if (int.TryParse(val, out result))
                                    if (result < 0)
                                        ph.End = new TimeSpan(result);
                                    else
                                        ph.End = TimeSpan.FromMilliseconds(result);
                                else if (val is { })
                                    ph.End = XmlConvert.ToTimeSpan(val);

                                reader.Read();//Text;
                                reader.ReadStartElement("Text");//posun na content
                                ph.Text = reader.Value.Trim('\r', '\n');

                                if (reader.Name != "Phrase") //text nebyl prazdny
                                {
                                    reader.Read();//text;
                                    reader.ReadEndElement();//Text;
                                }

                                if (reader.Name == "TextPrepisovany")
                                {
                                    reader.ReadElementString();
                                }

                                p.Phrases.Add(ph);
                                reader.ReadEndElement();//Phrase;
                            }

                            if (reader.Name != "speakerID")
                                reader.ReadEndElement();//Phrases - muze byt emptyelement a ten nema end..

                            p.InternalID = XmlConvert.ToInt32(reader.ReadElementString());
                            if (p.InternalID == -1)
                                p.InternalID = Speaker.DefaultID;

                            reader.ReadEndElement();//paragraph

                            //zarovnani fonetiky k textu


                            TranscriptionParagraph? bestpar = null;
                            TimeSpan timeinboth = TimeSpan.Zero;

                            if (p.Phrases.Count == 0)
                                continue;


                            TimeSpan minusone = new TimeSpan(-1);

                            foreach (TranscriptionParagraph v in s.Paragraphs)
                            {
                                if (v.End < p.Begin && v.End != minusone && p.Begin != minusone)
                                    continue;

                                if (v.Begin > p.End && v.End != minusone && v.Begin != minusone)
                                    continue;

                                TimeSpan beg = v.Begin > p.Begin ? v.Begin : p.Begin;
                                TimeSpan end;

                                if (v.End < p.End)
                                {
                                    end = v.End;
                                    if (v.End == minusone)
                                        end = p.End;
                                }
                                else
                                {
                                    end = p.End;
                                    if (p.End == minusone)
                                        end = v.End;
                                }

                                TimeSpan duration = end - beg;





                                if (bestpar is null)
                                {
                                    bestpar = v;
                                    timeinboth = duration;
                                }
                                else
                                {
                                    if (duration > timeinboth)
                                    {
                                        timeinboth = duration;
                                        bestpar = v;
                                    }
                                }
                            }

                            if (bestpar is { })
                            {
                                if (p.Phrases.Count == bestpar.Phrases.Count)
                                {
                                    for (int i = 0; i < p.Phrases.Count; i++)
                                    {
                                        bestpar.Phrases[i].Phonetics = p.Phrases[i].Text;
                                    }
                                }
                                else
                                {
                                    int i = 0;
                                    int j = 0;

                                    TimeSpan actual = p.Phrases[i].Begin;
                                    while (i < p.Phrases.Count && j < bestpar.Phrases.Count)
                                    {
                                        TranscriptionPhrase to = p.Phrases[i];
                                        TranscriptionPhrase from = bestpar.Phrases[j];
                                        if (true)
                                        {

                                        }
                                        i++;
                                    }
                                }

                            }
                        }
                        if (reader.Name == "PhoneticParagraphs" && reader.NodeType == XmlNodeType.EndElement)
                            reader.ReadEndElement();


                        if (!(reader.Name == "Section" && reader.NodeType == XmlNodeType.EndElement))
                        {

                            if (reader.Name != "speaker")
                                reader.Read();
                            reader.ReadElementString("speaker");
                        }
                        c.Sections.Add(s);
                        reader.ReadEndElement();//section
                    }

                    if (reader.Name == "Sections")
                        reader.ReadEndElement();//sections
                    reader.ReadEndElement();//chapter
                    data.Chapters.Add(c);
                }

                reader.ReadEndElement();//chapters
                reader.ReadStartElement("SpeakersDatabase");
                reader.ReadStartElement("Speakers");


                while (reader.Name == "Speaker")
                {
                    bool end = false;

                    Speaker sp = new Speaker();
                    sp.DBType = DBType.File;
                    sp.DBID = null;
                    reader.ReadStartElement("Speaker");
                    while (!end)
                    {
                        switch (reader.Name)
                        {
                            case "ID":

                                sp.SerializationID = XmlConvert.ToInt32(reader.ReadElementString("ID"));
                                break;
                            case "Surname":
                                sp.Surname = reader.ReadElementString("Surname");
                                break;
                            case "Firstname":
                                sp.FirstName = reader.ReadElementString("Firstname");
                                break;
                            case "FirstName":
                                sp.FirstName = reader.ReadElementString("FirstName");
                                break;
                            case "Sex":
                                {
                                    string ss = reader.ReadElementString("Sex");

                                    if (new[] { "male", "m", "muž" }.Contains(ss.ToLower()))
                                    {
                                        sp.Sex = Speaker.Sexes.Male;
                                    }
                                    else if (new[] { "female", "f", "žena" }.Contains(ss.ToLower()))
                                    {
                                        sp.Sex = Speaker.Sexes.Female;
                                    }
                                    else
                                        sp.Sex = Speaker.Sexes.X;

                                }
                                break;
                            case "Comment":
                                var str = reader.ReadElementString("Comment");
                                if (string.IsNullOrWhiteSpace(str))
                                    break;
                                sp.Attributes.Add(new SpeakerAttribute("comment", "comment", str));
                                break;
                            case "Speaker":
                                if (reader.NodeType == XmlNodeType.EndElement)
                                {
                                    reader.ReadEndElement();
                                    end = true;
                                }
                                else
                                    goto default;
                                break;

                            default:
                                if (reader.IsEmptyElement)
                                    reader.Read();
                                else
                                    reader.ReadElementString();
                                break;
                        }
                    }
                    data._speakers.Add(sp);
                }

                storage.AssingSpeakersByID();
            }
            catch (Exception ex)
            {
                if (reader is { })
                    throw new TranscriptionSerializationException(string.Format("Deserialization error:(line:{0}, offset:{1}) {2}", reader.LineNumber, reader.LinePosition, ex.Message), ex);
                else
                    throw new TranscriptionSerializationException("Deserialization error: " + ex.Message, ex);
            }

        }

        /// <summary>
        /// Assigns Speaker (from internal Speaker pool Transcription.Speakers) to all paragraphs in Transcription by ID. Default speaker (Speaker.DefaultSpeaker) is assgned when no speaker si found
        /// </summary>
        public void AssingSpeakersByID()
        {
            foreach (var par in Paragraphs)
            {
                var sp = _speakers.FirstOrDefault(s => s.SerializationID == par.InternalID);
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


        public TranscriptionChapter LastChapter
            => Chapters[^1];

        public TranscriptionSection LastSection
            => Chapters[^1].Sections[^1];

        public TranscriptionParagraph LastParagraph
            => Chapters[^1].Sections[^1].Paragraphs[^1];

        public void Add(TranscriptionChapter item)
            => Chapters.Add(item);
        public void Add(TranscriptionSection item)
            => Chapters[^1].Sections.Add(item);
        public void Add(TranscriptionParagraph item)
            => Chapters[^1].Sections[^1].Paragraphs.Add(item);
        public void Add(TranscriptionPhrase item)
            => Chapters[^1].Sections[^1].Paragraphs[^1].Phrases.Add(item);

        public void Remove(TranscriptionChapter item)
            => Chapters.Remove(item);
        public void Remove(TranscriptionSection item)
            => item.Parent?.Sections?.Remove(item);
        public void Remove(TranscriptionParagraph item)
            => item.Parent?.Paragraphs?.Remove(item);
        public void Remove(TranscriptionPhrase item)
            => item.Parent?.Phrases?.Remove(item);


        public string InnerText
        {
            get { return string.Join("\r\n", Chapters.Select(c => c.InnerText)); }
        }
    }
}
