using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace TranscriptionCore
{
    public class TranscriptionParagraph
    {
        public bool Revert(Undo act)
        {
            if (act is ParagraphChanged c)
            {
                switch (c)
                {
                    case BeginChanged bc:
                        Begin = bc.Old;
                        return true;
                    case EndChanged ec:
                        End = ec.Old;
                        return true;
                    case LanguageChanged lc:
                        Language = lc.Old;
                        return true;
                    case SpeakerChanged sc:
                        Speaker = sc.Old;
                        return true;
                    case CustomElementsChanged cec:
                        Elements = cec.Old;
                        return true;
                }
            }

            return false;
        }


        public record ParagraphChanged : Undo;
        public record BeginChanged(TimeSpan Old) : ParagraphChanged;
        public record EndChanged(TimeSpan Old) : ParagraphChanged;
        public record SpeakerChanged(Speaker Old) : ParagraphChanged;
        public record LanguageChanged(string? Old) : ParagraphChanged;
        public record AttributesChanged(ImmutableHashSet<string> old) : ParagraphChanged;
        public record CustomElementsChanged(ImmutableDictionary<string, string> Old) : ParagraphChanged;

        readonly private UndoableCollection<TranscriptionPhrase> phrases;
        public IList<TranscriptionPhrase> Phrases => phrases;

        protected TimeSpan _begin = Constants.UnknownTime;

        public TimeSpan Begin
        {
            get
            {
                if (_begin == Constants.UnknownTime)
                {
                    if (phrases.Count > 0 && phrases[0] is { } ph && ph.Begin != Constants.UnknownTime)
                        return ph.Begin;

                    foreach (var prev in EnumeratePrevious())
                    {
                        if (prev._end != Constants.UnknownTime)
                            return prev._end;
                        if (prev._begin != Constants.UnknownTime)
                            return prev._begin;
                    }
                }

                return _begin;
            }
            set
            {
                var ov = Begin;
                _begin = value;
                Updates.OnContentChanged(new BeginChanged(ov));
            }
        }
        protected TimeSpan _end = Constants.UnknownTime;
        public TimeSpan End
        {
            get
            {
                if (_end == Constants.UnknownTime)
                {
                    if (phrases.Count > 0 && phrases[^1] is { } ph && ph.End != Constants.UnknownTime)
                        return ph.End;

                    foreach (var prev in EnumerateNext())
                    {
                        if (prev._begin != Constants.UnknownTime)
                            return prev._begin;

                        if (prev._end != Constants.UnknownTime)
                            return prev._end;
                    }
                }

                return _end;
            }
            set
            {
                var ov = _end;
                _end = value;
                Updates.OnContentChanged(new EndChanged(ov));
            }
        }

        /// <summary>
        /// concat all text from all Phrases
        /// </summary>
        public string Text
        {
            get
            {
                if (this.Phrases is { })
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < this.Phrases.Count; i++)
                        sb.Append(this.Phrases[i].Text);

                    return sb.ToString();
                }

                return "";
            }
        }


        /// <summary>
        /// concat all phonetics from all Phrases
        /// </summary>
        public string Phonetics
        {
            get
            {
                if (this.Phrases is { })
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < this.Phrases.Count; i++)
                        sb.Append(Phrases[i].Phonetics);

                    return sb.ToString();
                }

                return "";
            }
        }

        private ImmutableHashSet<string> attributes = Constants.IgnoreCaseHashset;

        /// <summary>
        /// attributes of paragraph to annotate "noise", "narrowband" etc.
        /// attributes ignore case, intended use is Paragraph.Attributes = Paragraph.Attributes.Add("None");
        /// </summary>
        public ImmutableHashSet<string> Attributes
        {
            get => attributes;
            set
            {
                var old = attributes;
                Updates.OnContentChanged(new AttributesChanged(old));
                attributes = value;
            }
        }

        private int _internalID = Speaker.DefaultID;


        [Obsolete("Used only for speaker identification when serializing or deserializing")]
        internal int InternalID
        {
            get
            {
                if (_speaker == Speaker.DefaultSpeaker)
                    return _internalID;
                else
                    return _speaker.SerializationID;
            }
            set
            {
                if (_speaker is { } && _internalID != Speaker.DefaultID)
                    throw new ArgumentException("cannot set speaker ID while Speaker is set");
                _internalID = value;
            }
        }

        Speaker _speaker = Speaker.DefaultSpeaker;
        public Speaker Speaker
        {
            get
            {
                return _speaker ?? Speaker.DefaultSpeaker;
            }
            set
            {
                var old = _speaker;
                _speaker = value ?? throw new ArgumentException("speaker on paragraph cannot be null, use TranscriptionCore.Speaker.DefaultSepeaker");
#pragma warning disable CS0618 // Type or member is obsolete
                _internalID = value?.SerializationID ?? Speaker.DefaultID;
#pragma warning restore CS0618 // Type or member is obsolete

                Updates.OnContentChanged(new SpeakerChanged(old));
            }
        }

        /// <summary>
        /// Length of Paragraph
        /// </summary>
        public TimeSpan Length
        {
            get
            {
                if (Begin == new TimeSpan(-1) || End == new TimeSpan(-1))
                    return TimeSpan.Zero;

                return End - Begin;
            }
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

        #region serialization

        /// <summary>
        /// V2 deserialization beware of local variable names
        /// </summary>
        /// <param name="e"></param>
        /// <param name="isStrict"></param>
        /// <returns></returns>
        public static TranscriptionParagraph DeserializeV2(XElement e, bool isStrict)
        {
            TranscriptionParagraph par = new TranscriptionParagraph();
            par._internalID = int.Parse(e.Attribute(isStrict ? "speakerid" : "s")!.Value);

            if (e.Attribute(isStrict ? "attributes" : "a")?.Value is { } astring)
                par.attributes = par.attributes.Union(astring.Split('|'));

            var rems = isStrict ? new[] { "begin", "end", "attributes", "speakerid" } : new[] { "b", "e", "a", "s" };
            par.Elements = par.Elements.AddRange
                (e.Attributes()
                    .Where(a => !rems.Contains(a.Name.LocalName))
                    .Select(a => new KeyValuePair<string, string>(a.Name.LocalName, a.Value))
                );



            e.Elements(isStrict ? "phrase" : "p").Select(p => TranscriptionPhrase.DeserializeV2(p, isStrict)).ToList().ForEach(p => par.Phrases.Add(p));


            if (e.Attribute(isStrict ? "begin" : "b")?.Value is string bval)
            {
                if (int.TryParse(bval, out int ms))
                    par.Begin = TimeSpan.FromMilliseconds(ms);
                else
                    par.Begin = XmlConvert.ToTimeSpan(bval);
            }

            if (e.Attribute(isStrict ? "end" : "e")?.Value is string eval)
            {
                if (int.TryParse(eval, out int ms))
                    par.End = TimeSpan.FromMilliseconds(ms);
                else
                    par.End = XmlConvert.ToTimeSpan(eval);
            }

            return par;
        }

        public TranscriptionParagraph(XElement e) : this()
        {
            if (!e.CheckRequiredAtributes("b", "e", "s"))
                throw new ArgumentException("required attribute missing on paragraph (b,e,s)");

            _internalID = int.Parse(e.Attribute("s")!.Value);
            if (e.Attribute("a")?.Value is { } astring)
                attributes = attributes.Union(astring.Split('|'));

            var allattrs = e.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);

            foreach (var p in e.Elements("p").Select(p => new TranscriptionPhrase(p)))
                Phrases.Add(p);

            if (allattrs.TryGetValue("b", out var bfr))
            {
                if (int.TryParse(bfr, out int ms))
                    Begin = TimeSpan.FromMilliseconds(ms);
                else
                    Begin = XmlConvert.ToTimeSpan(bfr);
            }

            if (allattrs.TryGetValue("e", out bfr))
            {
                if (int.TryParse(bfr, out int ms))
                    End = TimeSpan.FromMilliseconds(ms);
                else
                    End = XmlConvert.ToTimeSpan(bfr);
            }

            if (allattrs.TryGetValue("l", out bfr))
            {
                if (!string.IsNullOrWhiteSpace(bfr))
                    Language = bfr.ToUpper();
            }

            allattrs.Remove("b");
            allattrs.Remove("e");
            allattrs.Remove("s");
            allattrs.Remove("a");
            allattrs.Remove("l");

            Elements = elements.AddRange(allattrs);
        }

        public XElement Serialize()
        {
            XElement elm = new XElement("pa",
                Elements.Select(e => new XAttribute(e.Key, e.Value)).Union(new[] {
                    new XAttribute("b", Begin),
                    new XAttribute("e", End),
                    new XAttribute("a", string.Join('|',Attributes)),
#pragma warning disable CS0618 // Type or member is obsolete
                    new XAttribute("s", InternalID), //DO NOT use _speakerID,  it is not equivalent
#pragma warning restore CS0618 // Type or member is obsolete
                }),
                Phrases.Select(p => p.Serialize())
            );

            if (_lang is { })
                elm.Add(new XAttribute("l", Language.ToLower()));

            return elm;
        }
        #endregion

        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="aKopie"></param>
        public TranscriptionParagraph(TranscriptionParagraph aKopie)
            : this()
        {
            this.Begin = aKopie.Begin;
            this.End = aKopie.End;
            this.Attributes = aKopie.Attributes;
            if (aKopie.Phrases is { })
            {
                for (int i = 0; i < aKopie.Phrases.Count; i++)
                    this.Phrases.Add(new TranscriptionPhrase(aKopie.Phrases[i]));
            }
            this.Speaker = aKopie.Speaker;
        }

        public TranscriptionParagraph(IEnumerable<TranscriptionPhrase> phrases)
            : this()
        {
            foreach (var p in phrases)
                Phrases.Add(p);

            if (Phrases.Count > 0)
            {
                this.Begin = Phrases[0].Begin;
                this.End = Phrases[^1].End;
            }
        }
        public TranscriptionParagraph(params TranscriptionPhrase[] phrases)
            : this(phrases.AsEnumerable())
        {

        }


        public TranscriptionParagraph() : base()
        {
            void childAdded(TranscriptionPhrase child, int index)
            {
                child.Parent = this;
                child.ParentIndex = index;
            }

            void childRemoved(TranscriptionPhrase child)
            {
                child.Parent = null;
                child.ParentIndex = -1;
            }

            phrases = new UndoableCollection<TranscriptionPhrase>()
            {
                OnAdd = childAdded,
                OnRemoved = childRemoved
            };
            phrases.Update.ContentChanged = OnChange;
            Updates = new UpdateTracker()
            {
                ContentChanged = OnChange
            };
        }

        string? _lang = null;


        [NotNull]
        [AllowNull]
        public string Language
        {
            get
            {
                return _lang ?? Speaker.DefaultLang;
            }
            set
            {
                var oldlang = _lang;
                _lang = value?.ToUpper();
                Updates.OnContentChanged(new LanguageChanged(oldlang));
            }
        }


        internal void OnChange(IEnumerable<Undo> undos)
        {
            /*Parent.OnChange(*/
            undos.Select(u => u with { TranscriptionIndex = u.TranscriptionIndex with { ParagraphIndex = ParentIndex } });
            //);
        }

        public UpdateTracker Updates { get; }
        public TranscriptionSection? Parent { get; internal set; }
        public int ParentIndex { get; internal set; }

        /// <summary>
        /// return next paragraphs in transcriptions
        /// </summary>
        /// <param name="parentOnly">search only in parent</param>
        /// <returns></returns>
        public TranscriptionParagraph? Next(bool parentOnly = false)
        {
            if (Parent is null || parentOnly && ParentIndex == Parent.Paragraphs.Count - 1)
                return null;

            if (ParentIndex < Parent.Paragraphs.Count - 1)
                return Parent.Paragraphs[ParentIndex + 1];

            return Parent.EnumerateNext().FirstOrDefault(s => s.Paragraphs.Count > 1)?.Paragraphs[0];

        }

        /// <summary>
        /// enumerate next paragraph in transcription
        /// </summary>
        /// <param name="parentOnly">search only in parent</param>
        /// <returns></returns>
        public IEnumerable<TranscriptionParagraph> EnumerateNext(bool parentOnly = false)
        {
            int indx = this.ParentIndex + 1;
            TranscriptionSection? par = Parent;

            while (par is { })
            {
                for (int i = indx; i < par.Paragraphs.Count; i++)
                    yield return par.Paragraphs[i];

                if (parentOnly)
                    yield break;

                par = par.Next();
                indx = 0;
            }
        }

        /// <summary>
        /// return previous paragraph in transcriptions
        /// </summary>
        /// <param name="parentOnly">search only in parent</param>
        /// <returns></returns>
        public TranscriptionParagraph? Previous(bool parentOnly = false)
        {
            if (Parent is null || parentOnly && ParentIndex <= 0)
                return null;

            if (ParentIndex > 0)
                return Parent.Paragraphs[ParentIndex - 1];

            return Parent.EnumeratePrevious().FirstOrDefault(s => s.Paragraphs.Count > 1)?.Paragraphs[^1];

        }

        /// <summary>
        /// enumerate previous paragraphs in transcription
        /// </summary>
        /// <param name="parentOnly">search only in parent</param>
        /// <returns></returns>
        public IEnumerable<TranscriptionParagraph> EnumeratePrevious(bool parentOnly = false)
        {
            int indx = this.ParentIndex - 1;
            TranscriptionSection? sec = Parent;

            while (sec is { })
            {
                for (int i = indx; i >= 0; i--)
                    yield return sec.Paragraphs[i];

                if (parentOnly)
                    yield break;

                sec = sec.Previous();
                if (sec is { })
                    indx = sec.Paragraphs.Count - 1;
            }
        }
    }
}
