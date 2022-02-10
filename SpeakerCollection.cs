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

            foreach (var spkr in aSpeakers.Select(s => s.Copy()))
                this.Add(spkr);
        }

        public SpeakerCollection()
        {

        }


        public Speaker? GetSpeakerByDBID(string dbid)
            => this.FirstOrDefault(s => s.DataBaseID.DBID == dbid || s.Merges.Any(m => m.DBID == dbid));

        public Speaker? GetSpeakerByName(string fullname)
            => this.FirstOrDefault(s => s.FullName == fullname);

        /// <summary>
        /// BEWARE - SpeakerCollection is synchronized manually, It can contain different speakers than transcription
        /// </summary>
        /// <param name="saveAll">save including image and merges, used when saving database</param>
        /// <returns></returns>
        public XElement Serialize(bool saveAll = true)
        {
            XElement elm = new XElement("sp",
                elements.Select(e => new XAttribute(e.Key, e.Value)),
                this.Select(s => s.Serialize(saveAll))
            );

            return elm;
        }
    }
}
