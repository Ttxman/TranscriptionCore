using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace TranscriptionCore
{
    public class TranscriptionSection: IUpdateTracking
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

        private record NameChanged(string Old) : Undo;
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

        public UndoableCollection<TranscriptionParagraph> Paragraphs;

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

        public static TranscriptionSection DeserializeV2(XElement e, bool isStrict)
        {
            TranscriptionSection tsec = new TranscriptionSection();
            tsec.Paragraphs.Updates.BeginUpdate(false);
            tsec.Name = e.Attribute("name")?.Value ?? "";
            tsec.Elements = tsec.Elements.AddRange
            (
                e.Attributes()
                .Where(a => a.Name.LocalName != "name")
                .Select(a => new KeyValuePair<string, string>(a.Name.LocalName, a.Value))
            );
            tsec.Elements.Remove("name");

            foreach (var p in e.Elements(isStrict ? "paragraph" : "pa").Select(p => TranscriptionParagraph.DeserializeV2(p, isStrict)))
                tsec.Paragraphs.Add(p);

            tsec.Paragraphs.Updates.EndUpdate();
            return tsec;
        }

        public TranscriptionSection(XElement e) : this()
        {
            Name = e.Attribute("name")?.Value ?? "";
            Elements = Elements.AddRange
            (
                e.Attributes()
                .Where(a => a.Name.LocalName != "name")
                .Select(a => new KeyValuePair<string, string>(a.Name.LocalName, a.Value))
            );
            foreach (var p in e.Elements("pa").Select(p => new TranscriptionParagraph(p)))
                Paragraphs.Add(p);

        }

        public XElement Serialize()
        {
            XElement elm = new XElement("se", Elements.Select(e => new XAttribute(e.Key, e.Value)).Union(new[] { new XAttribute("name", Name), }),
                Paragraphs.Select(p => p.Serialize())
            );

            return elm;
        }

        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="toCopy"></param>
        public TranscriptionSection(TranscriptionSection toCopy)
            : this()
        {
            Name = toCopy.Name;
            for (int i = 0; i < toCopy.Paragraphs.Count; i++)
                Paragraphs.Add(new TranscriptionParagraph(toCopy.Paragraphs[i]));
        }

        public TranscriptionSection()
        {
            void childAdded(TranscriptionParagraph child, int index)
            {
                child.Parent = this;
                child.ParentIndex = index;
            }

            void childRemoved(TranscriptionParagraph child)
            {
                child.Parent = null;
                child.ParentIndex = -1;
            }

            Paragraphs = new UndoableCollection<TranscriptionParagraph>()
            {
                OnAdd = childAdded,
                OnRemoved = childRemoved
            };

            Paragraphs.Updates.ContentChanged = OnChange;
            Updates = new UpdateTracker()
            {
                ContentChanged = OnChange
            };
        }

        internal void OnChange(IEnumerable<Undo> undos)
        {
            /*Parent.OnChange(*/
            undos.Select(u => u with { TranscriptionIndex = u.TranscriptionIndex with { Sectionindex = ParentIndex } });
            //);
        }

        public TranscriptionSection(string aName)
            : this()
        {
            name = aName;
        }

        public string InnerText
        {
            get
            {
                return Name + "\r\n" + string.Join("\r\n", Paragraphs.Select(c => c.Text));
            }
        }

        public UpdateTracker Updates { get; }
        public TranscriptionChapter? Parent { get; internal set; }
        public int ParentIndex { get; internal set; }

        /// <summary>
        /// return next sections in transcriptions
        /// </summary>
        /// <param name="parentOnly">search only in parent</param>
        /// <returns></returns>
        public TranscriptionSection? Next(bool parentOnly = false)
        {
            if (Parent is null || parentOnly && ParentIndex == Parent.Sections.Count - 1)
                return null;

            if (ParentIndex < Parent.Sections.Count - 1)
                return Parent.Sections[ParentIndex + 1];

            return Parent.EnumerateNext().FirstOrDefault(s => s.Sections.Count > 1)?.Sections[0];

        }

        /// <summary>
        /// enumerate next sections in transcription
        /// </summary>
        /// <param name="parentOnly">search only in parent</param>
        /// <returns></returns>
        public IEnumerable<TranscriptionSection> EnumerateNext(bool parentOnly = false)
        {
            int indx = this.ParentIndex + 1;
            TranscriptionChapter? chp = Parent;

            while (chp is { })
            {
                for (int i = indx; i < chp.Sections.Count; i++)
                    yield return chp.Sections[i];

                if (parentOnly)
                    yield break;

                chp = chp.Next();
                indx = 0;
            }
        }

        /// <summary>
        /// return previous section in transcriptions
        /// </summary>
        /// <param name="parentOnly">search only in parent</param>
        /// <returns></returns>
        public TranscriptionSection? Previous(bool parentOnly = false)
        {
            if (Parent is null || parentOnly && ParentIndex <= 0)
                return null;

            if (ParentIndex > 0)
                return Parent.Sections[ParentIndex - 1];

            return Parent.EnumeratePrevious().FirstOrDefault(s => s.Sections.Count > 1)?.Sections[^1];

        }

        /// <summary>
        /// enumerate previous sections in transcription
        /// </summary>
        /// <param name="parentOnly">search only in parent</param>
        /// <returns></returns>
        public IEnumerable<TranscriptionSection> EnumeratePrevious(bool parentOnly = false)
        {
            int indx = this.ParentIndex - 1;
            TranscriptionChapter? chp = Parent;

            while (chp is { })
            {
                for (int i = indx; i >= 0; i--)
                    yield return chp.Sections[i];

                if (parentOnly)
                    yield break;

                chp = chp.Previous();
                if (chp is { })
                    indx = chp.Sections.Count - 1;
            }
        }
    }
}
