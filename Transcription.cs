using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.Serialization;
using TranscriptionCore.Serialization;


namespace TranscriptionCore
{
    public class Transcription : TranscriptionElement, IList<TranscriptionElement>
    {
        public double TotalHeigth;
        public bool FindNext(ref TranscriptionElement paragraph, ref int TextOffset, out int length, string pattern, bool isregex, bool CaseSensitive, bool searchinspeakers)
        {
            TranscriptionElement par = paragraph;
            length = 0;
            if (par == null)
                return false;

            if (searchinspeakers)
            {
                TranscriptionElement prs = paragraph.Next();

                while (prs != null)
                {
                    TranscriptionParagraph pr = prs as TranscriptionParagraph;
                    if (pr != null && pr.Speaker.FullName.ToLower().Contains(pattern.ToLower()))
                    {
                        paragraph = pr;
                        TextOffset = 0;
                        return true;
                    }
                    prs = prs.Next();
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

            TranscriptionElement tag = paragraph;
            while (par != null)
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
                    paragraph = tag;
                    return true;
                }

                tag = tag.Next();
                if (tag == null)
                    return false;
                par = tag;
                TextOffset = 0;
            }

            return false;
        }

        public string FileName { get; set; }

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
        /// transcription source, not mandatory
        /// </summary>
        public string Source { get; set; }
        /// <summary>
        /// transcription type, not mandatory
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// file containing audio data for transcription
        /// </summary>
        public string MediaURI { get; set; }
        /// <summary>
        /// file containing video data - can be same as audio 
        /// </summary>
        public string VideoFileName { get; set; }

        


        private VirtualTypeList<TranscriptionChapter> _Chapters;

        /// <summary>
        /// all chapters in transcription
        /// </summary>
        public VirtualTypeList<TranscriptionChapter> Chapters
        {
            get { return _Chapters; }
            private set { _Chapters = value; }
        }

        [XmlElement("SpeakersDatabase")]
        internal SpeakerCollection _speakers = new SpeakerCollection();

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
            Chapters = new VirtualTypeList<TranscriptionChapter>(this,this._children);
            Created = DateTime.UtcNow;
            //constructor  
        }



        /// <summary>
        /// copy contructor
        /// </summary>
        /// <param name="toCopy"></param>
        public Transcription(Transcription toCopy)
            : this()
        {
            this.Source = toCopy.Source;
            this.MediaURI = toCopy.MediaURI;
            this.VideoFileName = toCopy.VideoFileName;
            this.Type = toCopy.Type;
            this.Created = toCopy.Created;
            if (toCopy.Chapters != null)
            {
                this.Chapters = new VirtualTypeList<TranscriptionChapter>(this, this._children);
                for (int i = 0; i < toCopy.Chapters.Count; i++)
                {
                    this.Chapters.Add(new TranscriptionChapter(toCopy.Chapters[i]));
                }
            }
            this.FileName = toCopy.FileName;
            this._speakers = new SpeakerCollection(toCopy._speakers);
            this.Saved = toCopy.Saved;
        }

        /// <summary>
        /// automaticly deserialize from file
        /// </summary>
        /// <param name="path"></param>
        public Transcription(string path)
            : this()
        {
            SerializationSelector.Deserialize(path, this);
        }
        public Transcription(FileInfo f)
            : this(f.FullName)
        {

        }

        /// <summary>
        /// vrati vsechny vyhovujici elementy casu
        /// </summary>
        /// <param name="aPoziceKurzoru"></param>
        /// <returns></returns>
        public List<TranscriptionParagraph> ReturnElementsAtTime(TimeSpan time)
        {
            List<TranscriptionParagraph> toret = new List<TranscriptionParagraph>();
            foreach (var el in this)
            {
                if (el.IsParagraph && el.Begin <= time && el.End > time)
                {
                    toret.Add((TranscriptionParagraph)el);
                }
            }
            return toret;
        }

        public TranscriptionParagraph ReturnLastElemenWithEndBeforeTime(TimeSpan cas)
        {
            List<TranscriptionParagraph> toret = new List<TranscriptionParagraph>();
            TranscriptionParagraph par = null;
            foreach (var el in this)
            {
                if (el.End < cas)
                {
                    if (el.IsParagraph)
                    {
                        par = (TranscriptionParagraph)el;
                    }
                }
                else
                    break;
            }
            return par;
        }


        public TranscriptionParagraph ReturnLastElemenWithBeginBeforeTime(TimeSpan cas)
        {
            List<TranscriptionParagraph> toret = new List<TranscriptionParagraph>();
            TranscriptionParagraph par = null;
            foreach (var el in this)
            {
                if (el.Begin < cas)
                {
                    if (el.IsParagraph)
                    {
                        par = (TranscriptionParagraph)el;
                    }
                }
                else
                    break;
            }
            return par;

        }

        /// <summary>
        /// Remove speaker from all paragraphs (Paragraphs will return default Speaker.DefaultSpeaker) and from internal database (if not pinned)
        /// </summary>
        /// <param name="speaker"></param>
        /// <returns></returns>
        public bool RemoveSpeaker(Speaker speaker)
        {
            try
            {
                if (speaker.FullName != null && speaker.FullName != "")
                {
                    if (this._speakers.Contains(speaker))
                    {
                        if (!speaker.PinnedToDocument)
                            _speakers.Remove(speaker);

                        Saved = false;
                        foreach (var p in EnumerateParagraphs())
                            if (p.Speaker == speaker)
                                p.Speaker = Speaker.DefaultSpeaker;

                        Saved = false;
                        return true;
                    }
                    return false;

                }
                else return false;
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

            foreach (var p in EnumerateParagraphs())
                if (p.Speaker == toReplace)
                    p.Speaker = replacement;

        }

        internal void ReindexSpeakers()
        {
            var speakers = this.EnumerateParagraphs().Select(p => p.Speaker).Where(s => s != Speaker.DefaultSpeaker && s.SerializationID != Speaker.DefaultID).Distinct().ToList();
            for (int i = 0; i < speakers.Count; i++)
            {
                speakers[i].SerializationID = i;
            }
        }

        public static Transcription Deserialize(string filename)
        {
            Transcription tr = new Transcription();
            SerializationSelector.Deserialize(filename, tr);
            return tr;
        }

        public static Transcription Deserialize(Stream datastream)
        {
            Transcription tr = new Transcription();
            SerializationSelector.Deserialize(datastream, tr);
            return tr;
        }
          

        public XElement Meta = EmptyMeta();
        internal static XElement EmptyMeta()
        {
            return new XElement("meta");
        }

        public Dictionary<string, string> Elements = new Dictionary<string, string>();

        /// <summary>
        /// Assigns Speaker (from internal Speaker pool Transcription.Speakers) to all paragraphs in Transcription by ID. Default speaker (Speaker.DefaultSpeaker) is assgned when no speaker si found
        /// </summary>
        public void AssingSpeakersByID()
        {
            foreach (var par in this.Where(e => e.IsParagraph).Cast<TranscriptionParagraph>())
            {
                var sp = _speakers.FirstOrDefault(s => s.SerializationID == par.InternalID);
                if (sp != null)
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
        {
            get { return Chapters.Last(); }
        }

        public TranscriptionSection LastSection
        {
            get { return Chapters.Last().Sections.Last(); }
        }

        public TranscriptionParagraph LastParagraph
        {
            get { return Chapters.Last().Sections.Last().Paragraphs.Last(); }
        }


        #region IList<TranscriptionElement> Members

        public int IndexOf(TranscriptionElement item)
        {
            int i = 0;
            if (Chapters.Count == 0)
                return -1;

            TranscriptionElement cur = Chapters[0];
            while (cur != null && cur != item)
            {
                i++;
                cur = cur.NextSibling();
            }


            return i;
        }

        public override void Insert(int index, TranscriptionElement item)
        {
            throw new NotSupportedException();
        }


        public override TranscriptionElement this[TranscriptionIndex index]
        {
            get
            {
                ValidateIndexOrThrow(index);

                if (index.IsChapterIndex)
                {
                    if (index.IsSectionIndex)
                        return Chapters[index.Chapterindex][index];

                    return Chapters[index.Chapterindex];
                }

                throw new IndexOutOfRangeException("index");
            }
            set
            {
                ValidateIndexOrThrow(index);

                if (index.IsChapterIndex)
                {
                    if (index.IsSectionIndex)
                        Chapters[index.Chapterindex][index] = value;
                    else
                        Chapters[index.Chapterindex] = (TranscriptionChapter)value;
                }
                else
                    throw new IndexOutOfRangeException("index");

            }

        }


        public override void RemoveAt(TranscriptionIndex index)
        {
            ValidateIndexOrThrow(index);
            if (index.IsChapterIndex)
            {
                if (index.IsSectionIndex)
                    Chapters[index.Chapterindex].RemoveAt(index);
                else
                    Chapters.RemoveAt(index.Chapterindex);
            }
            else
            {
                throw new IndexOutOfRangeException("index");
            }
        }

        public override void Insert(TranscriptionIndex index, TranscriptionElement value)
        {
            ValidateIndexOrThrow(index);
            if (index.IsChapterIndex)
            {
                if (index.IsSectionIndex)
                    Chapters[index.Chapterindex].Insert(index, value);
                else
                    Chapters.Insert(index.Chapterindex,(TranscriptionChapter)value);
            }
            else
            {
                throw new IndexOutOfRangeException("index");
            }
        }


        public override TranscriptionElement this[int index]
        {
            get
            {
                int i = 0;
                foreach (TranscriptionChapter c in Chapters)
                {
                    if (i == index)
                        return c;
                    i++;
                    if (index < i + c.GetTotalChildrenCount())
                    {
                        foreach (TranscriptionSection s in c.Sections)
                        {
                            if (i == index)
                                return s;
                            i++;
                            if (index < i + s.GetTotalChildrenCount())
                            {
                                return s.Paragraphs[index - i];

                            }
                            i += s.GetTotalChildrenCount();
                        }

                    }
                    i += c.GetTotalChildrenCount();
                }

                throw new IndexOutOfRangeException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        #endregion

        #region ICollection<TranscriptionElement> Members

        public override void Add(TranscriptionElement item)
        {
           
            if (item is TranscriptionChapter)
            {
                base.Add(item);
            }
            else if (item is TranscriptionSection)
            {
                if (_children.Count == 0)
                    Add(new TranscriptionChapter());

                _children[_children.Count - 1].Add(item);
            }
            else if (item is TranscriptionParagraph)
            {
                if (_children.Count == 0)
                    Add(new TranscriptionChapter());

                if (_children[_children.Count - 1].Children.Count == 0)
                    Add(new TranscriptionSection());

                _children[_children.Count - 1].Children[_children[_children.Count - 1].Children.Count - 1].Add(item);
            }
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(TranscriptionElement item)
        {
            throw new NotSupportedException();
        }

        public void CopyTo(TranscriptionElement[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        public int Count
        {
            get { return Chapters.Sum(x => x.GetTotalChildrenCount()) + Chapters.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public override bool Remove(TranscriptionElement item)
        {
            if (item is TranscriptionChapter)
            {
                return base.Remove(item);
            }

            foreach (TranscriptionElement el in this)
            {
                if (el == item)
                {
                    return item.Parent.Remove(item);
                }
            }

            return false;
        }

        #endregion

        #region IEnumerable<TranscriptionElement> Members

        public IEnumerator<TranscriptionElement> GetEnumerator()
        {
            foreach (TranscriptionChapter c in this.Chapters)
            {
                yield return c;

                foreach (TranscriptionSection s in c.Sections)
                {
                    yield return s;

                    foreach (TranscriptionParagraph p in s.Paragraphs)
                    {
                        yield return p;
                    }
                }
            }
            yield break;
        }


        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        public override string Text
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override string Phonetics
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }


        public override int AbsoluteIndex
        {
            get { throw new NotSupportedException(); }
        }


        public IEnumerable<TranscriptionParagraph> EnumerateParagraphs()
        {
            return this.Where(p => p.IsParagraph).Cast<TranscriptionParagraph>();
        }

        public override string InnerText
        {
            get { return string.Join("\r\n", Children.Select(c => c.Text)); }
        }
    }
}
