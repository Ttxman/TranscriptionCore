using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace TranscriptionCore
{
    public static class ToolsAndExtensions
    {
        public static bool CheckRequiredAtributes(this XElement elm, params string[] attributes)
        {
            foreach (var a in attributes)
            {
                if (elm.Attribute(a) is null)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Remove speaker from all paragraphs (Paragraphs will return default Speaker.DefaultSpeaker) and from internal database (if not pinned)
        /// </summary>
        /// <param name="speaker"></param>
        /// <returns></returns>
        public static bool RemoveSpeaker(this Transcription trans, Speaker speaker)
        {
            try
            {
                if (trans.Speakers.Contains(speaker))
                {
                    if (!speaker.PinnedToDocument)
                        trans.Speakers.Remove(speaker);

                    trans.Saved = false;
                    foreach (var p in trans.Paragraphs)
                        if (p.Speaker == speaker)
                            p.Speaker = Speaker.DefaultSpeaker;

                    trans.Saved = false;
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
        public static void ReplaceSpeaker(this Transcription trans, Speaker toReplace, Speaker replacement)
        {
            if (trans.Speakers.Contains(toReplace))
            {
                trans.Speakers.Remove(toReplace);
                if (!trans.Speakers.Contains(replacement))
                    trans.Speakers.Add(replacement);
            }

            foreach (var p in trans.Paragraphs)
                if (p.Speaker == toReplace)
                    p.Speaker = replacement;
        }


        public static bool FindNext(ref TranscriptionParagraph paragraph, ref int TextOffset, out int length, string pattern, bool isregex, bool CaseSensitive, bool searchinspeakers)
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


        /// <summary>
        /// update values from another speaker .. used for merging, probably not doing what user assumes :)
        /// </summary>
        /// <param name="into"></param>
        /// <param name="from"></param>
        public static void MergeSpeakers(Speaker into, Speaker from)
        {
            into.Surname = from.Surname;
            into.FirstName = from.FirstName;
            into.MiddleName = from.MiddleName;
            into.DegreeBefore = from.DegreeBefore;
            into.DegreeAfter = from.DegreeAfter;
            into.DefaultLang = from.DefaultLang;
            into.Sex = from.Sex;
            into.ImgBase64 = from.ImgBase64;
            into.Merges = into.Merges.AddRange(from.Merges);

            if (from.DataBaseID.DBType != DBType.File && into.DataBaseID.DBID != from.DataBaseID.DBID)
                into.Merges = into.Merges.Add(new DBMerge(from.DataBaseID.DBType, from.DataBaseID.DBID));


            into.Attributes = into.Attributes
                 .Concat(from.Attributes).GroupBy(a => a.Name)
                 .SelectMany(g => g.Distinct(new Speaker.AttributeComparer()))
                 .ToImmutableArray();
        }
    }
}
