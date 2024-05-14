using System;
using System.Linq;
using System.Xml;

namespace TranscriptionCore.Serialization
{
    public static class SerializationV1
    {
        /// <summary> read old transcription format (v1) </summary>
        public static void Deserialize(XmlTextReader reader, Transcription storage)
        {
            try
            {
                Transcription data = storage;
                reader.WhitespaceHandling = WhitespaceHandling.Significant;

                reader.Read(); //<?xml version ...
                reader.Read();
                data.MediaURI = reader.GetAttribute("audioFileName");
                string val = reader.GetAttribute("dateTime");
                if (val != null)
                    data.Created = XmlConvert.ToDateTime(val, XmlDateTimeSerializationMode.Local);

                reader.ReadStartElement("Transcription");

                int result;

                //reader.Read();
                reader.ReadStartElement("Chapters");
                //reader.ReadStartElement("Chapter");
                while (reader.Name == "Chapter")
                {
                    TranscriptionChapter c = new TranscriptionChapter();
                    c.Name = reader.GetAttribute("name");

                    val = reader.GetAttribute("begin");
                    if (int.TryParse(val, out result))
                        if (result < 0)
                            c.Begin = new TimeSpan(result);
                        else
                            c.Begin = TimeSpan.FromMilliseconds(result);
                    else
                        c.Begin = XmlConvert.ToTimeSpan(val);

                    val = reader.GetAttribute("end");
                    if (int.TryParse(val, out result))
                        if (result < 0)
                            c.End = new TimeSpan(result);
                        else
                            c.End = TimeSpan.FromMilliseconds(result);
                    else
                        c.End = XmlConvert.ToTimeSpan(val);

                    reader.Read();

                    reader.ReadStartElement("Sections");


                    while (reader.Name == "Section")
                    {

                        TranscriptionSection s = new TranscriptionSection();
                        s.Name = reader.GetAttribute("name");

                        val = reader.GetAttribute("begin");
                        if (int.TryParse(val, out result))
                            if (result < 0)
                                s.Begin = new TimeSpan(result);
                            else
                                s.Begin = TimeSpan.FromMilliseconds(result);
                        else
                            s.Begin = XmlConvert.ToTimeSpan(val);

                        val = reader.GetAttribute("end");
                        if (int.TryParse(val, out result))
                            if (result < 0)
                                s.End = new TimeSpan(result);
                            else
                                s.End = TimeSpan.FromMilliseconds(result);
                        else
                            s.End = XmlConvert.ToTimeSpan(val);

                        reader.Read();
                        reader.ReadStartElement("Paragraphs");

                        while (reader.Name == "Paragraph")
                        {
                            TranscriptionParagraph p = new TranscriptionParagraph();
                            val = reader.GetAttribute("begin");
                            if (int.TryParse(val, out result))
                                if (result < 0)
                                    if (result == -1 && s.Paragraphs.Count > 0)
                                        p.Begin = s.Paragraphs[s.Paragraphs.Count - 1].End;
                                    else
                                        p.Begin = new TimeSpan(result);
                                else
                                    p.Begin = TimeSpan.FromMilliseconds(result);
                            else
                                p.Begin = XmlConvert.ToTimeSpan(val);

                            val = reader.GetAttribute("end");
                            if (int.TryParse(val, out result))
                                if (result < 0)
                                    p.End = new TimeSpan(result);
                                else
                                    p.End = TimeSpan.FromMilliseconds(result);
                            else
                                p.End = XmlConvert.ToTimeSpan(val);

                            val = reader.GetAttribute("trainingElement");
                            p.trainingElement = val == null ? false : XmlConvert.ToBoolean(val);
                            p.AttributeString = reader.GetAttribute("Attributes");

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
                                else
                                    ph.Begin = XmlConvert.ToTimeSpan(val);

                                val = reader.GetAttribute("end");
                                if (int.TryParse(val, out result))
                                    if (result < 0)
                                        ph.End = new TimeSpan(result);
                                    else
                                        ph.End = TimeSpan.FromMilliseconds(result);
                                else
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
                            else
                                p.Begin = XmlConvert.ToTimeSpan(val);

                            val = reader.GetAttribute("end");
                            if (int.TryParse(val, out result))
                                if (result < 0)
                                    if (result == -1)
                                    {
                                        p.Begin = s.Paragraphs[s.Paragraphs.Count - 1].End;
                                    }
                                    else
                                        p.End = TimeSpan.FromMilliseconds(result);
                                else
                                    p.End = TimeSpan.FromMilliseconds(result);
                            else
                                p.End = XmlConvert.ToTimeSpan(val);

                            p.trainingElement = XmlConvert.ToBoolean(reader.GetAttribute("trainingElement"));
                            p.AttributeString = reader.GetAttribute("Attributes");

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
                                else
                                    ph.Begin = XmlConvert.ToTimeSpan(val);

                                val = reader.GetAttribute("end");
                                if (int.TryParse(val, out result))
                                    if (result < 0)
                                        ph.End = new TimeSpan(result);
                                    else
                                        ph.End = TimeSpan.FromMilliseconds(result);
                                else
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


                            TranscriptionParagraph bestpar = null;
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





                                if (bestpar == null)
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

                            if (bestpar != null)
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

                            int spkr = XmlConvert.ToInt32(reader.ReadElementString("speaker"));
                            s.Speaker = (spkr < 0) ? Speaker.DefaultID : spkr;

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
                    sp.DbId.DBtype = DBType.File;
                    sp.SetDbId(null);
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
                if (reader != null)
                    throw new TranscriptionSerializationException(string.Format("Deserialization error:(line:{0}, offset:{1}) {2}", reader.LineNumber, reader.LinePosition, ex.Message), ex);
                else
                    throw new TranscriptionSerializationException("Deserialization error: " + ex.Message, ex);
            }

        }
    }
}
