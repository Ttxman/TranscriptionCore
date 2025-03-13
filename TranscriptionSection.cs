using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using TranscriptionCore.Serialization;

namespace TranscriptionCore
{
    //sekce textu nadrazena odstavci
    public class TranscriptionSection : TranscriptionElement
    {
        string _text = "";
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

        public override string Phonetics
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

        public override bool IsSection => true;

        public string Name
        {
            get
            { 
                return Text; 
            }
            set 
            { 
                Text = value; 
            }
        }


        private VirtualTypeList<TranscriptionParagraph> _Paragraphs;

        public VirtualTypeList<TranscriptionParagraph> Paragraphs
        {
            get { return _Paragraphs; }
            private set { _Paragraphs = value; }
        }



        public int Speaker;


        #region serializace nova

        public Dictionary<string, string> Elements = new();

        public static TranscriptionSection DeserializeV2(XElement e, bool isStrict)
            => SerializationV2.DeserializeSection(e, isStrict);

        public TranscriptionSection(XElement e) : this()
            => SerializationV3.DeserializeSection(e, this);

        public XElement Serialize()
            => SerializationV3.SerializeSection(this);

        #endregion


        /// <summary>
        /// copy constructor
        /// </summary>
        /// <param name="toCopy"></param>
        public TranscriptionSection(TranscriptionSection toCopy)
            : this()
        {
            this.Begin = toCopy.Begin;
            this.End = toCopy.End;
            this.Name = toCopy.Name;
            if (toCopy.Paragraphs != null)
            {
                this.Paragraphs = new VirtualTypeList<TranscriptionParagraph>(this, this._children);
                for (int i = 0; i < toCopy.Paragraphs.Count; i++)
                {
                    this.Paragraphs.Add(new TranscriptionParagraph(toCopy.Paragraphs[i]));
                }
            }
        }

        public TranscriptionSection()
        {
            Paragraphs = new VirtualTypeList<TranscriptionParagraph>(this,this._children);
            Begin = new TimeSpan(-1);
            End = new TimeSpan(-1);
        }

        public TranscriptionSection(String aName)
            : this(aName, new TimeSpan(-1), new TimeSpan(-1))
        {
        }
        public TranscriptionSection(String aName, TimeSpan aBegin, TimeSpan aEnd)
            : this()
        {
            this.Name = aName;
            this.Begin = aBegin;
            this.End = aEnd;
        }

        public override int GetTotalChildrenCount()
        {
            return _children.Count;
        }

        public override int AbsoluteIndex
        {
            get
            {

                if (_Parent != null)
                {

                    int sum = _Parent.AbsoluteIndex;//parent absolute index index
                    sum += _Parent.Children.Take(this.ParentIndex) //take previous siblings
                        .Sum(s => s.GetTotalChildrenCount()); //+ all pre siblings counts (index on sublayers)

                    sum += ParentIndex; //+ parent index (index on sibling layer)
                    //... sum = all previous

                    sum++;//+1 - this
                    // this.Text = sum.ToString();
                    return sum;

                }

                return 0;
            }
        }

        public override string InnerText
        {
            get
            {
                return Name + "\r\n" + string.Join("\r\n", Children.Select(c => c.Text));
            }
        }


        public override TranscriptionElement this[TranscriptionIndex index]
        {
            get
            {
                ValidateIndexOrThrow(index);

                if (index.IsParagraphIndex)
                {
                    if (index.IsPhraseIndex)
                        return Paragraphs[index.ParagraphIndex][index];

                    return Paragraphs[index.ParagraphIndex];
                }

                throw new IndexOutOfRangeException("index");
            }
            set
            {
                ValidateIndexOrThrow(index);

                if (index.IsParagraphIndex)
                {
                    if (index.IsPhraseIndex)
                        Paragraphs[index.ParagraphIndex][index] = value;
                    else
                        Paragraphs[index.ParagraphIndex] = (TranscriptionParagraph)value;
                }
                else
                {
                    throw new IndexOutOfRangeException("index");
                }

            }
        }

        public override void RemoveAt(TranscriptionIndex index)
        {
            ValidateIndexOrThrow(index);
            if (index.IsParagraphIndex)
            {
                if (index.IsPhraseIndex)
                    Paragraphs[index.ParagraphIndex].RemoveAt(index);
                else
                    Paragraphs.RemoveAt(index.ParagraphIndex);
            }
            else
            {
                throw new IndexOutOfRangeException("index");
            }
        }

        public override void Insert(TranscriptionIndex index, TranscriptionElement value)
        {
            ValidateIndexOrThrow(index);
            if (index.IsParagraphIndex)
            {
                if (index.IsPhraseIndex)
                    Paragraphs[index.ParagraphIndex].Insert(index,value);
                else
                    Paragraphs.Insert(index.ParagraphIndex,(TranscriptionParagraph)value);
            }
            else
            {
                throw new IndexOutOfRangeException("index");
            }
        }
    }
}
