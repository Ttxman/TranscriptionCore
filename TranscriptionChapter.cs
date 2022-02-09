using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace TranscriptionCore
{
    public class TranscriptionChapter
    {
        public bool Revert(Undo act)
        {
            switch (act)
            {
                case NameChanged nc:
                    Name = nc.Old;
                    return true;
                case CustomElementsChanged cec:
                    Elements = cec.Old;
                    return true;
            }
            return false;
        }

        private record NameChanged(string Old) : Undo { }
        public record CustomElementsChanged(ImmutableDictionary<string, string> Old) : Undo;

        string name = "";
        public string Name
        {
            get => name;
            set
            {
                var old = name;
                name = value;
                Updates.OnContentChanged(new NameChanged(old));
            }
        }


        public UndoableCollection<TranscriptionSection> Sections { get; }
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

        #region serializtion

        public static TranscriptionChapter DeserializeV2(XElement c, bool isStrict)
        {
            TranscriptionChapter chap = new TranscriptionChapter();
            chap.Updates.BeginUpdate();
            chap.Name = c.Attribute("name")?.Value ?? "";
            chap.Elements = chap.Elements.AddRange
            (
                c.Attributes()
                .Where(a => a.Name.LocalName != "name")
                .Select(a => new KeyValuePair<string, string>(a.Name.LocalName, a.Value))
            );
            foreach (var s in c.Elements(isStrict ? "section" : "se").Select(s => TranscriptionSection.DeserializeV2(s, isStrict)))
                chap.Sections.Add(s);

            chap.Updates.EndUpdate();
            return chap;
        }

        public TranscriptionChapter(XElement c) : this()
        {
            Updates.BeginUpdate(false);
            Name = c.Attribute("name")?.Value ?? "";
            Elements = Elements.AddRange
            (
                c.Attributes()
                .Where(a => a.Name.LocalName != "name")
                .Select(a => new KeyValuePair<string, string>(a.Name.LocalName, a.Value))
            );

            foreach (var s in c.Elements("se").Select(s => new TranscriptionSection(s)))
                Sections.Add(s);

            Updates.EndUpdate();
        }

        public XElement Serialize()
        {

            XElement elm = new XElement("ch",
                Elements.Select(e => new XAttribute(e.Key, e.Value)).Union(new[] { new XAttribute("name", Name), }),
                Sections.Select(s => s.Serialize())
            );

            return elm;
        }
        #endregion


        public TranscriptionChapter(TranscriptionChapter toCopy) : this()
        {
            Name = toCopy.Name;
            for (int i = 0; i < toCopy.Sections.Count; i++)
                Sections.Add(new TranscriptionSection(toCopy.Sections[i]));
        }

        public TranscriptionChapter()
        {
            void childAdded(TranscriptionSection child, int index)
            {
                child.Parent = this;
                child.ParentIndex = index;
            }

            void childRemoved(TranscriptionSection child)
            {
                child.Parent = null;
                child.ParentIndex = -1;
            }

            Sections = new UndoableCollection<TranscriptionSection>()
            {
                OnAdd = childAdded,
                OnRemoved = childRemoved
            };

            Sections.Update.ContentChanged = OnChange;
            Updates = new UpdateTracker()
            {
                ContentChanged = OnChange
            };
        }

        internal void OnChange(IEnumerable<Undo> undos)
        {
            /*Parent.OnChange(*/
            undos.Select(u => u with { TranscriptionIndex = u.TranscriptionIndex with { Chapterindex = ParentIndex } });
            //);
        }

        public TranscriptionChapter(string aName) : this()
        {
            Name = aName;
        }



        public string InnerText
        {
            get { return Name + "\r\n" + string.Join("\r\n", Sections.Select(c => c.InnerText)); }
        }

        public UpdateTracker Updates { get; }
        public Transcription? Parent { get; internal set; }
        public int ParentIndex { get; internal set; }

        /// <summary>
        /// return next chapter in transcriptions
        /// </summary>
        /// <returns></returns>
        public TranscriptionChapter? Next()
        {
            if (Parent is null || ParentIndex == Parent.Chapters.Count - 1)
                return null;

            if (ParentIndex < Parent.Chapters.Count - 1)
                return Parent.Chapters[ParentIndex + 1];

            return null;
        }

        /// <summary>
        /// enumerate next chapters in transcription
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TranscriptionChapter> EnumerateNext()
        {
            int indx = this.ParentIndex + 1;

            if (Parent is null)
                yield break;

            for (int i = indx; i < Parent.Chapters.Count; i++)
                yield return Parent.Chapters[i];
        }

        /// <summary>
        /// return previous section in transcriptions
        /// </summary>
        /// <returns></returns>
        public TranscriptionChapter? Previous()
        {
            if (Parent is null || ParentIndex <= 0)
                return null;

            return Parent.Chapters[ParentIndex - 1];
        }

        /// <summary>
        /// enumerate previous sections in transcription
        /// </summary>
        /// <param name="parentOnly">search only in parent</param>
        /// <returns></returns>
        public IEnumerable<TranscriptionChapter> EnumeratePrevious(bool parentOnly = false)
        {
            int indx = this.ParentIndex - 1;
            if (Parent is null)
                yield break;

            for (int i = indx; i >= 0; i--)
                yield return Parent.Chapters[i];

        }
    }
}
