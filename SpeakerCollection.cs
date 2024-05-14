using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace TranscriptionCore
{
    /// <summary>
    /// BEWARE - SpeakerCollection is synchronized manually, It can contain different speakers than transcription
    /// </summary>
    /// <returns></returns>
    public class SpeakerCollection : IList<Speaker>
    {
        protected string _fileName;
        public string FileName
        {
            get { return _fileName; }
            set { _fileName  = value; }
        }

        internal List<Speaker> _Speakers = new List<Speaker>(); 
        
        internal Dictionary<string, string> elements = new Dictionary<string, string>();

        public SpeakerCollection()
        {
        }

        public SpeakerCollection(IEnumerable<Speaker> speakers)
        {
            _Speakers = speakers.ToList();
        }

        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="aSpeakers"></param>
        public SpeakerCollection(SpeakerCollection aSpeakers)
        {
            if (aSpeakers != null)
            {
                this._fileName = aSpeakers._fileName;
                if (aSpeakers._Speakers != null)
                {
                    this._Speakers = new List<Speaker>();
                    for (int i = 0; i < aSpeakers._Speakers.Count; i++)
                    {
                        this._Speakers.Add(aSpeakers._Speakers[i].CreateCopy());
                    }
                }
            }
        }

        /// <summary>
        /// remove speaker from list - NOT FROM TRANSCRIPTION !!!!
        /// </summary>
        /// <param name="aSpeaker"></param>
        /// <returns></returns>
        public bool RemoveSpeaker(Speaker aSpeaker)
        {
            return _Speakers.Remove(aSpeaker);
        }


        public Speaker GetSpeakerByDBID(string dbid)
        {
            return _Speakers.FirstOrDefault(s => s.DbId.DBID == dbid || s.Merges.Any(m=>m.DBID ==dbid));
        }

        public Speaker GetSpeakerByName(string fullname)
        {
            return _Speakers.FirstOrDefault(s => s.FullName == fullname);
        }

        /// <summary>
        /// called after deserialization
        /// </summary>
        protected virtual void Initialize(XDocument doc)
        {

        }

        public int IndexOf(Speaker item)
        {
            return _Speakers.IndexOf(item);
        }

        public virtual void Insert(int index, Speaker item)
        {
            _Speakers.Insert(index, item);
        }

        public virtual void RemoveAt(int index)
        {
            _Speakers.RemoveAt(index);
        }

        public Speaker this[int index]
        {
            get
            {
                return _Speakers[index];
            }
            set
            {
                _Speakers[index] = value;
            }
        }

        public virtual void Add(Speaker item)
        {
            _Speakers.Add(item);
        }

        public virtual void Clear()
        {
            _Speakers.Clear();
        }

        public bool Contains(Speaker item)
        {
            return _Speakers.Contains(item);
        }

        public void CopyTo(Speaker[] array, int arrayIndex)
        {
            _Speakers.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _Speakers.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public virtual bool Remove(Speaker item)
        {
            return _Speakers.Remove(item);
        }

        public IEnumerator<Speaker> GetEnumerator()
        {
            return _Speakers.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _Speakers.GetEnumerator();
        }

        public virtual void AddRange(IEnumerable<Speaker> enumerable)
        {
            _Speakers.AddRange(enumerable);
        }
    }
}
