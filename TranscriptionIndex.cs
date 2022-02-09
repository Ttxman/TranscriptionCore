using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TranscriptionCore
{
    public struct TranscriptionIndex
    {
        public int Chapterindex { get; init; }
        public readonly int Sectionindex { get; init; }
        public readonly int ParagraphIndex { get; init; }
        public readonly int PhraseIndex { get; init; }

        public TranscriptionIndex(int chapterindex = -1, int sectionindex = -1, int paragraphIndex = -1, int phraseIndex = -1)
        {
            Chapterindex = chapterindex;
            Sectionindex = sectionindex;
            ParagraphIndex = paragraphIndex;
            PhraseIndex = phraseIndex;
        }

        public TranscriptionIndex(int[] indexa)
        {
            Chapterindex = indexa[0];
            Sectionindex = indexa[1];
            ParagraphIndex = indexa[2];
            PhraseIndex = indexa[3];
        }

        public static readonly TranscriptionIndex FirstChapter = new TranscriptionIndex(0, -1, -1, -1);
        public static readonly TranscriptionIndex FirstSection = new TranscriptionIndex(0, 0, -1, -1);
        public static readonly TranscriptionIndex FirstParagraph = new TranscriptionIndex(0, 0, 0, -1);
        public static readonly TranscriptionIndex FirstPhrase = new TranscriptionIndex(0, 0, 0, -1);
        public static readonly TranscriptionIndex Invalid = new TranscriptionIndex(-1, -1, -1, -1);

        public int[] ToArray()
        {
            return new int[] { Chapterindex, Sectionindex, ParagraphIndex, PhraseIndex };
        }


        /// <summary>
        /// Is index valid ( starts with positives integers, negative integers from end and no mixing between positive and negative
        ///  for example you cannot index first paragraph of -1 section
        /// </summary>
        public bool IsValid
        {
            get
            {
                var ind = this.ToArray();

                var fromstart = ind.TakeWhile(i => i >= 0).Count();
                var fromend = ind.Reverse().TakeWhile(i => i < 0).Count();

                return fromstart != 0 && fromstart + fromend == 4;
            }
        }


        public bool IsPhraseIndex
        {
            get
            {
                return IsValid && PhraseIndex >= 0;
            }
        }

        public bool IsParagraphIndex
        {
            get
            {
                return IsValid && ParagraphIndex >= 0;
            }
        }

        public bool IsSectionIndex
        {
            get
            {
                return IsValid && Sectionindex >= 0;
            }
        }


        public bool IsChapterIndex
        {
            get
            {
                return IsValid && Chapterindex >= 0;
            }
        }

        public override string ToString()
        {
            return string.Format("{4}: {0};{1};{2};{3}", Chapterindex, Sectionindex, ParagraphIndex, PhraseIndex, IsValid ? "TIndex" : "TInvalidIndex");
        }
    }
}
