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
    public partial class Transcription : IUpdateTracking
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

        private SpeakerCollection _speakers = new SpeakerCollection();

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

            Chapters.Updates.ContentChanged = OnChange;
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
                Chapters.Updates.BeginUpdate(false);
                for (int i = 0; i < toCopy.Chapters.Count; i++)
                    Chapters.Add(new TranscriptionChapter(toCopy.Chapters[i]));
                Chapters.Updates.EndUpdate();
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
