using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TranscriptionCore
{

    /// <summary>
    /// Base class for structure change events and Undo
    /// </summary>
    public abstract class ChangeAction
    {
        public ChangeType ChangeType { get; }

        public TranscriptionElement ChangedElement { get; }

        public TranscriptionIndex ChangeTranscriptionIndex { get; }

        public ChangeAction(ChangeType changeType, TranscriptionElement changedElement, TranscriptionIndex changeIndex, int changeAbsoluteIndex)
        {
            ChangeType = changeType;
            ChangedElement = changedElement;
            ChangeTranscriptionIndex = changeIndex;
            ChangeAbsoluteIndex = changeAbsoluteIndex;
        }

        public abstract void Revert(Transcription trans);
        public int ChangeAbsoluteIndex { get; }
    }


    public class InsertAction : ChangeAction
    {

        public InsertAction(TranscriptionElement changedElement, TranscriptionIndex changeIndex, int changeAbsoluteIndex)
            : base(ChangeType.Add, changedElement, changeIndex, changeAbsoluteIndex)
        {

        }

        public override void Revert(Transcription trans)
        {
            trans.RemoveAt(ChangeTranscriptionIndex);
        }
    }


    public class RemoveAction : ChangeAction
    {

        public RemoveAction(TranscriptionElement changedElement, TranscriptionIndex changeIndex, int changeAbsoluteIndex)
            : base(ChangeType.Remove, changedElement, changeIndex, changeAbsoluteIndex)
        {

        }

        public override void Revert(Transcription trans)
        {
            trans.Insert(ChangeTranscriptionIndex, ChangedElement);
        }
    }


    public class ReplaceAction : ChangeAction
    {

        public ReplaceAction(TranscriptionElement changedElement, TranscriptionIndex changeIndex, int changeAbsoluteIndex)
            : base(ChangeType.Replace, changedElement, changeIndex, changeAbsoluteIndex)
        {

        }

        public override void Revert(Transcription trans)
        {
            trans[ChangeTranscriptionIndex] = ChangedElement;
        }
    }


    public class ParagraphSpeakerAction : ChangeAction
    {
        public Speaker OldSpeaker { get; }

        public ParagraphSpeakerAction(TranscriptionParagraph changedParagraph, TranscriptionIndex changeIndex, int changeAbsoluteIndex, Speaker oldSpeaker)
            : base(ChangeType.Modify, changedParagraph, changeIndex, changeAbsoluteIndex)
        {
            OldSpeaker = oldSpeaker;
        }

        public override void Revert(Transcription trans)
        {
            ((TranscriptionParagraph)trans[ChangeTranscriptionIndex]).Speaker = OldSpeaker;
        }
    }


    public class ParagraphAttibutesAction : ChangeAction
    {
        public ParagraphAttibutesAction(TranscriptionParagraph changedParagraph, TranscriptionIndex changeIndex, int changeAbsoluteIndex, ParagraphAttributes oldAttributes)
            : base(ChangeType.Modify, changedParagraph, changeIndex, changeAbsoluteIndex)
        {
            OldAttributes = oldAttributes;
        }

        public override void Revert(Transcription trans)
        {
            ((TranscriptionParagraph)trans[ChangeTranscriptionIndex]).DataAttributes = OldAttributes;
        }

        public ParagraphAttributes OldAttributes { get; }
    }

    public class ParagraphLanguageAction : ChangeAction
    {
        public ParagraphLanguageAction(TranscriptionParagraph changedParagraph, TranscriptionIndex changeIndex, int changeAbsoluteIndex, string oldLanguage)
            : base(ChangeType.Modify, changedParagraph, changeIndex, changeAbsoluteIndex)
        {
            OldLanguage = oldLanguage;
        }

        public override void Revert(Transcription trans)
        {
            ((TranscriptionParagraph)trans[ChangeTranscriptionIndex]).Language = OldLanguage;
        }
        public string OldLanguage { get; }
    }


    public class BeginAction : ChangeAction
    {
        public BeginAction(TranscriptionElement changedElement, TranscriptionIndex changeIndex, int changeAbsoluteIndex, TimeSpan oldtime)
            : base(ChangeType.Modify, changedElement, changeIndex, changeAbsoluteIndex)
        {
            Oldtime = oldtime;
        }

        public override void Revert(Transcription trans)
        {
            trans[ChangeTranscriptionIndex].Begin = Oldtime;
        }

        public TimeSpan Oldtime { get; }
    }


    public class EndAction : ChangeAction
    {
        public EndAction(TranscriptionElement changedelement, TranscriptionIndex changeIndex, int changeAbsoluteIndex, TimeSpan oldtime)
            : base(ChangeType.Modify, changedelement, changeIndex, changeAbsoluteIndex)
        {
            Oldtime = oldtime;
        }

        public override void Revert(Transcription trans)
        {
            trans[ChangeTranscriptionIndex].End = Oldtime;
        }
        public TimeSpan Oldtime { get; }
    }


    public class TextAction : ChangeAction
    {
        public TextAction(TranscriptionElement changedelement, TranscriptionIndex changeIndex, int changeAbsoluteIndex, string oldtstring)
            : base(ChangeType.Modify, changedelement, changeIndex, changeAbsoluteIndex)
        {
            Oldtstring = oldtstring;
        }

        public override void Revert(Transcription trans)
        {
            trans[ChangeTranscriptionIndex].Text = Oldtstring;
        }

        public string Oldtstring { get; }
    }


    public class PhrasePhoneticsAction : ChangeAction
    {
        public PhrasePhoneticsAction(TranscriptionPhrase changedelement, TranscriptionIndex changeIndex, int changeAbsoluteIndex, string oldphonetics)
            : base(ChangeType.Modify, changedelement, changeIndex, changeAbsoluteIndex)
        {
            Oldtstring = oldphonetics;
        }

        public override void Revert(Transcription trans)
        {
            trans[ChangeTranscriptionIndex].Phonetics = Oldtstring;
        }


        public string Oldtstring { get; }
    }



    /// <summary>
    /// Used for compatibility with collection changes in wpf
    /// </summary>
    public enum ChangeType : uint
    {
        Add,
        Remove,
        Replace,
        Modify,
    }
}
