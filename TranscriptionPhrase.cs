using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace TranscriptionCore
{
    /// <summary>
    /// the smallest part of transcription with time tags.
    /// </summary>
    public sealed class TranscriptionPhrase
    {
        public bool Revert(Undo act)
        {
            if (act is PhraseChanged c)
            {
                switch (c)
                {
                    case TextChanged tc:
                        Text = tc.Old;
                        return true;
                    case PhoneticsChanged pc:
                        Phonetics = pc.Old;
                        return true;
                    case BeginChanged bc:
                        Begin = bc.Old;
                        return true;
                    case EndChanged ec:
                        End = ec.Old;
                        return true;
                    case CustomElementsChanged cec:
                        Elements = cec.Old;
                        return true;
                }
            }
            return false;
        }


        public record PhraseChanged : Undo;
        public record TextChanged(string Old) : PhraseChanged;
        public record PhoneticsChanged(string Old) : PhraseChanged;
        public record BeginChanged(TimeSpan Old) : PhraseChanged;
        public record EndChanged(TimeSpan Old) : PhraseChanged;
        public record CustomElementsChanged(ImmutableDictionary<string, string> Old) : PhraseChanged;


        private TimeSpan begin;
        private TimeSpan end;


        public TimeSpan Begin
        {
            get => begin;
            set
            {
                var oldv = begin;
                begin = value;

                Updates.OnContentChanged(new BeginChanged(oldv));
            }
        }
        public TimeSpan End
        {
            get => end;
            set
            {
                var oldv = end;
                end = value;
                Updates.OnContentChanged(new EndChanged(oldv));
            }
        }


        private string text = "";

        public string Text
        {
            get { return text; }
            set
            {
                var oldv = text;
                text = value;
                Updates.OnContentChanged(new TextChanged(oldv));
            }
        }

        private string phonetics = "";

        public string Phonetics
        {
            get
            {
                return phonetics;
            }
            set
            {
                var oldv = phonetics;
                phonetics = value;
                Updates.OnContentChanged(new PhoneticsChanged(oldv));
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
        /// V2 serialization
        /// </summary>
        /// <param name="e"></param>
        /// <param name="isStrict"></param>
        public static TranscriptionPhrase DeserializeV2(XElement e, bool isStrict)
        {
            TranscriptionPhrase phr = new TranscriptionPhrase();
            var allattrs = e.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);
            allattrs.Remove(isStrict ? "begin" : "b");
            allattrs.Remove(isStrict ? "end" : "e");
            allattrs.Remove(isStrict ? "fon" : "f");

            phr.Elements = allattrs.ToImmutableDictionary();

            phr.phonetics = e.Attribute(isStrict ? "fon" : "f")?.Value ?? "";
            phr.text = e.Value.Trim('\r', '\n');
            if (e.Attribute(isStrict ? "begin" : "b")?.Value is { } bval)
            {
                if (int.TryParse(bval, out int ms))
                    phr.Begin = TimeSpan.FromMilliseconds(ms);
                else
                    phr.Begin = XmlConvert.ToTimeSpan(bval);

            }

            if (e.Attribute(isStrict ? "end" : "e")?.Value is { } eval)
            {
                if (int.TryParse(eval, out int ms))
                    phr.End = TimeSpan.FromMilliseconds(ms);
                else
                    phr.End = XmlConvert.ToTimeSpan(eval);
            }

            return phr;
        }

        /// <summary>
        /// v3 serialization
        /// </summary>
        /// <param name="e"></param>
        public TranscriptionPhrase(XElement e) : this()
        {
            var elms = e.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);
            elms.Remove("b");
            elms.Remove("e");
            elms.Remove("f");

            Elements = elms.ToImmutableDictionary();

            this.phonetics = e.Attribute("f")?.Value ?? "";
            this.text = e.Value.Trim('\r', '\n');
            if (e.Attribute("b")?.Value is { } bval)
            {
                if (int.TryParse(bval, out int ms))
                    Begin = TimeSpan.FromMilliseconds(ms);
                else
                    Begin = XmlConvert.ToTimeSpan(bval);

            }

            if (e.Attribute("e")?.Value is { } eval)
            {
                if (int.TryParse(eval, out int ms))
                    End = TimeSpan.FromMilliseconds(ms);
                else
                    End = XmlConvert.ToTimeSpan(eval);
            }
        }


        public XElement Serialize()
        {
            XElement elm = new XElement("p",
                Elements.Select(e =>
                    new XAttribute(e.Key, e.Value))
                    .Union(new[]{
                    new XAttribute("b", Begin),
                    new XAttribute("e", End),
                    new XAttribute("f", phonetics),
                    }),
                    text.Trim('\r', '\n')
            );

            return elm;
        }
        #endregion


        public TranscriptionPhrase(TranscriptionPhrase kopie) : this()
        {
            begin = kopie.begin;
            end = kopie.end;
            text = kopie.text;
            phonetics = kopie.phonetics;
            Elements = kopie.Elements;
        }

        public TranscriptionPhrase()
            : base()
        {
            Updates = new UpdateTracker()
            {
                ContentChanged = OnChange
            };
        }

        public TranscriptionPhrase(TimeSpan begin, TimeSpan end, string aWords)
            : this()
        {
            this.Begin = begin;
            this.End = end;
            this.Text = aWords;
        }

        internal void OnChange(IEnumerable<Undo> undos)
        {
            Parent?.OnChange(undos.Select(u => u with { TranscriptionIndex = u.TranscriptionIndex with { PhraseIndex = ParentIndex } }));
        }

        public UpdateTracker Updates { get; }

        public TranscriptionParagraph? Parent { get; internal set; }
        public int ParentIndex { get; internal set; }

        /// <summary>
        /// return next phrase in transcriptions
        /// </summary>
        /// <param name="parentOnly">search only in parent</param>
        /// <returns></returns>
        public TranscriptionPhrase? Next(bool parentOnly = false)
        {
            if (Parent is null || parentOnly && ParentIndex == Parent.Phrases.Count - 1)
                return null;

            if (ParentIndex < Parent.Phrases.Count - 1)
                return Parent.Phrases[ParentIndex + 1];

            return Parent.EnumerateNext().FirstOrDefault(s => s.Phrases.Count > 1)?.Phrases[0];

        }

        /// <summary>
        /// enumerate next phrase in transcription
        /// </summary>
        /// <param name="parentOnly">search only in parent</param>
        /// <returns></returns>
        public IEnumerable<TranscriptionPhrase> EnumerateNext(bool parentOnly = false)
        {
            int indx = ParentIndex + 1;
            TranscriptionParagraph? par = Parent;

            while (par is { })
            {
                for (int i = indx; i < par.Phrases.Count; i++)
                    yield return par.Phrases[i];

                if (parentOnly)
                    yield break;

                par = par.Next();
                indx = 0;
            }
        }

        /// <summary>
        /// return previous phrase in transcriptions
        /// </summary>
        /// <param name="parentOnly">search only in parent</param>
        /// <returns></returns>
        public TranscriptionPhrase? Previous(bool parentOnly = false)
        {
            if (Parent is null || parentOnly && ParentIndex <= 0)
                return null;

            if (ParentIndex > 0)
                return Parent.Phrases[ParentIndex - 1];

            return Parent.EnumeratePrevious().FirstOrDefault(s => s.Phrases.Count > 1)?.Phrases[^1];

        }

        /// <summary>
        /// enumerate previous phrases in transcription
        /// </summary>
        /// <param name="parentOnly">search only in parent</param>
        /// <returns></returns>
        public IEnumerable<TranscriptionPhrase> EnumeratePrevious(bool parentOnly = false)
        {
            int indx = this.ParentIndex - 1;
            TranscriptionParagraph? par = Parent;

            while (par is { })
            {
                for (int i = indx; i >= 0; i--)
                    yield return par.Phrases[i];

                if (parentOnly)
                    yield break;

                par = par.Previous();
                indx = par.Phrases.Count - 1;
            }
        }
    }
}
