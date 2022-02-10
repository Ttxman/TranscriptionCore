using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace TranscriptionCore
{
    /// <summary>
    /// BEWARE - SpeakerCollection is synchronized manually, It can contain different speakers than transcription
    /// </summary>
    /// <returns></returns>
    public class SpeakerCollection : UndoableCollection<Speaker>
    {
        protected string _fileName;
        public string FileName
        {
            get { return _fileName; }
            set { _fileName = value; }
        }


        protected Dictionary<string, string> elements = new Dictionary<string, string>();
        public SpeakerCollection(XElement e)
        {
            elements = e.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);

            Update.BeginUpdate();
            try
            {
                foreach (var spkr in e.Elements("s").Select(x => new Speaker(x)))
                    this.Add(spkr);
            }
            finally
            {
                Update.EndUpdate();
            }
        }

        public SpeakerCollection(IEnumerable<Speaker> speakers)
        {
            Update.BeginUpdate();
            try
            {
                foreach (var spkr in speakers)
                    this.Add(spkr);
            }
            finally
            {
                Update.EndUpdate();
            }
        }

        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="aSpeakers"></param>
        public SpeakerCollection(SpeakerCollection aSpeakers)
        {
            if (aSpeakers is null)
                throw new ArgumentNullException(nameof(aSpeakers));

            _fileName = aSpeakers._fileName;
            foreach (var spkr in aSpeakers.Select(s => s.Copy()))
                this.Add(spkr);
        }

        public SpeakerCollection()
        {

        }


        public Speaker? GetSpeakerByDBID(string dbid)
        {
            return this.FirstOrDefault(s => s.DBID == dbid || s.Merges.Any(m => m.DBID == dbid));
        }

        public Speaker? GetSpeakerByName(string fullname)
        {
            return this.FirstOrDefault(s => s.FullName == fullname);
        }

        /// <summary>
        /// BEWARE - SpeakerCollection is synchronized manually, It can contain different speakers than transcription
        /// </summary>
        /// <param name="saveAll">save including image and merges, used when saving database</param>
        /// <returns></returns>
        public virtual XElement Serialize(bool saveAll = true)
        {
            XElement elm = new XElement("sp",
                elements.Select(e => new XAttribute(e.Key, e.Value)),
                this.Select(s => s.Serialize(saveAll))
            );

            return elm;
        }

        /// <summary>
        /// BEWARE - SpeakerCollection is synchronized manually, It can contain different speakers than transcription
        /// </summary>
        /// <param name="saveAll">save including image and merges, used when saving database</param>
        /// <returns></returns>
        public void Serialize(string filename, bool saveAll = true)
        {
            var xelm = Serialize(saveAll);
            xelm.Save(filename);
        }

        /// <summary>
        /// called after deserialization
        /// </summary>
        protected virtual void Initialize(XDocument doc)
        {

        }


        /// <summary>
        /// //deserialize speaker database file. 
        /// Old file format support should not concern anyone outside ite.tul.cz, public release never containded old format
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="store"></param>
        public static void Deserialize(string filename, SpeakerCollection store)
        {
            //if file do not exists, do not modify store
            if (!File.Exists(filename))
            {
                return;
            }
            store._fileName = filename;
            XDocument doc = XDocument.Load(filename);

            if (doc.Root is null)
                return;

            store.Update.BeginUpdate(false);
            try
            {

                if (doc.Root.Name == "MySpeakers") //old format from XmlSerializer
                {
                    #region old format
                    var root = doc.Root;
                    var speakers = root.Elements("Speakers").Elements("Speaker");
                    foreach (var sp in speakers)
                    {
                        Speaker speaker = new Speaker();

                        var id = sp.Element("ID");
                        var fname = sp.Element("FirstName");
                        var sname = sp.Element("Surname");
                        var sex = sp.Element("Sex");
                        var comment = sp.Element("Comment");
                        var lang = sp.Element("DefaultLang");

                        if (id is { })
                        {
#pragma warning disable CS0618 // Type or member is obsolete
                            speaker.SerializationID = XmlConvert.ToInt32(id.Value);
#pragma warning restore CS0618 // Type or member is obsolete
                        }
                        else
                            continue;

                        speaker.DBID = Guid.NewGuid().ToString();
                        speaker.FirstName = fname?.Value ?? "";
                        speaker.Surname = sname?.Value ?? "";

                        speaker.Sex = sex?.Value?.ToLower() switch
                        {
                            "m" or "muž" or "male" => Speaker.Sexes.Male,
                            "f" or "žena" or "female" => Speaker.Sexes.Female,
                            _ => Speaker.Sexes.X,
                        };
                        if (comment is { } && !string.IsNullOrWhiteSpace(comment.Value))
                            speaker.Attributes.Add(new SpeakerAttribute("comment", comment.Value, default));


                        if (int.TryParse(lang?.Value, out int vvvv) && vvvv < Speaker.Langs.Count)
                        {
                            speaker.DefaultLang = Speaker.Langs[vvvv];
                        }
                        else
                        {
                            speaker.DefaultLang = lang?.Value ?? Speaker.Langs[0];
                        }
                        store.Add(speaker);
                    }
                    #endregion
                }
                else
                {
                    foreach (var spkr in doc.Root.Elements("s").Select(x => new Speaker(x)))
                        store.Add(spkr);

                    store.Initialize(doc);
                }
            }
            finally
            {
                store.Update.EndUpdate();
            }
        }

        //deserialize speaker database file...          
        public static SpeakerCollection Deserialize(string filename)
        {
            var mysp = new SpeakerCollection();
            Deserialize(filename, mysp);

            return mysp;
        }

        public SpeakerCollection(string filename)
        {
            SpeakerCollection.Deserialize(filename, this);
        }
    }
}
