using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using TranscriptionCore.Serialization;

namespace TranscriptionCore
{
    /// <summary>
    /// the smallest part of transcription with time tags.
    /// </summary>
    public sealed class TranscriptionPhrase : TranscriptionElement
    {
        internal string _text = "";

        public override string Text
        {
            get { return _text; }
            set 
            {
                var oldv = _text;
                _text = value;
                OnContentChanged(new TextAction(this, this.TranscriptionIndex, this.AbsoluteIndex, oldv));
            }
        }

        internal string _phonetics = "";

        public override string Phonetics
        {
            get
            {
                return _phonetics;
            }
            set
            {
                var oldv = _phonetics;
                _phonetics = value;
                OnContentChanged(new PhrasePhoneticsAction(this, this.TranscriptionIndex, this.AbsoluteIndex, oldv));
            }
        }



        #region serialization
        public Dictionary<string, string> Elements = new Dictionary<string, string>();
        internal static readonly XAttribute EmptyAttribute = new XAttribute("empty", "");

        /// <summary>
        /// V2 serialization
        /// </summary>
        /// <param name="e"></param>
        /// <param name="isStrict"></param>
        public static TranscriptionPhrase DeserializeV2(XElement e, bool isStrict)
        {
            TranscriptionPhrase phr = new TranscriptionPhrase();
            phr.Elements = e.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);
            phr.Elements.Remove(isStrict ? "begin" : "b");
            phr.Elements.Remove(isStrict ? "end" : "e");
            phr.Elements.Remove(isStrict ? "fon" : "f");


            phr._phonetics = (e.Attribute(isStrict ? "fon" : "f") ?? EmptyAttribute).Value;
            phr._text = e.Value.Trim('\r', '\n');
            if (e.Attribute(isStrict ? "begin" : "b") != null)
            {
                string val = e.Attribute(isStrict ? "begin" : "b").Value;
                int ms;
                if (int.TryParse(val, out ms))
                    phr.Begin = TimeSpan.FromMilliseconds(ms);
                else
                    phr.Begin = XmlConvert.ToTimeSpan(val);

            }

            if (e.Attribute(isStrict ? "end" : "e") != null)
            {
                string val = e.Attribute(isStrict ? "end" : "e").Value;
                int ms;
                if (int.TryParse(val, out ms))
                    phr.End = TimeSpan.FromMilliseconds(ms);
                else
                    phr.End = XmlConvert.ToTimeSpan(val);
            }

            return phr;
        }

        /// <summary>
        /// v3 serialization
        /// </summary>
        /// <param name="e"></param>
        public TranscriptionPhrase(XElement e)
        {
            SerializationV3.DeserializePhrase(e, this);
        }
        
        public XElement Serialize() => SerializationV3.SerializePhrase(this);

        #endregion


        public TranscriptionPhrase(TranscriptionPhrase kopie)
        {
            this._begin = kopie._begin;
            this._end = kopie._end;
            this._text = kopie._text;
            this._phonetics = kopie._phonetics;
            this.height = kopie.height;
        }

        public TranscriptionPhrase()
            : base()
        {

        }

        public TranscriptionPhrase(TimeSpan begin, TimeSpan end, string aWords)
            : this()
        {
            this.Begin = begin;
            this.End = end;
            this.Text = aWords;
        }

        public TranscriptionPhrase(TimeSpan begin, TimeSpan end, string aWords, ElementType aElementType)
            : this(begin, end, aWords)
        {
        }

        public override int GetTotalChildrenCount()
        {
            return 0;
        }

        public override int AbsoluteIndex
        {
            get 
            {
                return -1;
            }
        }

        public override void Add(TranscriptionElement data)
        {
            throw new NotSupportedException();
        }

        public override bool Remove(TranscriptionElement value)
        {
            throw new NotSupportedException();
        }

        public override void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public override bool Replace(TranscriptionElement oldelement, TranscriptionElement newelement)
        {
            throw new NotSupportedException();
        }

        public override void Insert(int index, TranscriptionElement data)
        {
            throw new NotSupportedException();
        }

        public override TranscriptionElement this[int Index]
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override string InnerText
        {
            get
            {
                return Text;
            }
        }

        public override TranscriptionElement this[TranscriptionIndex index]
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override void RemoveAt(TranscriptionIndex index)
        {
            throw new NotImplementedException();
        }

        public override void Insert(TranscriptionIndex index, TranscriptionElement value)
        {
            throw new NotImplementedException();
        }
    }

}
