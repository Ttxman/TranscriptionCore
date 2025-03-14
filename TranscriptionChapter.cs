﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using TranscriptionCore.Serialization;

namespace TranscriptionCore
{
    //kapitola
    public class TranscriptionChapter : TranscriptionElement
    {
        public override bool IsChapter
        {
            get
            {
                return true;
            }
        }

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


        private VirtualTypeList<TranscriptionSection> _Sections;

        public VirtualTypeList<TranscriptionSection> Sections
        {
            get { return _Sections; }
            private set { _Sections = value; }
        }


        #region serializtion
        public Dictionary<string, string> Elements = new();

        public TranscriptionChapter(XElement xml) : this()
        {
            SerializationV3.DeserializeChapter(xml, this);
        }

        #endregion


        public TranscriptionChapter(TranscriptionChapter toCopy)
            : this()
        {
            this.Begin = toCopy.Begin;
            this.End = toCopy.End;
            this.Name = toCopy.Name;
            if (toCopy.Sections != null)
            {
                this.Sections = new VirtualTypeList<TranscriptionSection>(this, this._children);
                for (int i = 0; i < toCopy.Sections.Count; i++)
                {
                    this.Sections.Add(new TranscriptionSection(toCopy.Sections[i]));
                }
            }
        }

        public TranscriptionChapter()
            : base()
        {
            Sections = new VirtualTypeList<TranscriptionSection>(this,this._children);
            Begin = new TimeSpan(-1);
            End = new TimeSpan(-1);
        }

        public TranscriptionChapter(String aName)
            : this(aName, new TimeSpan(-1), new TimeSpan(-1))
        {

        }
        public TranscriptionChapter(String aName, TimeSpan aBegin, TimeSpan aEnd)
        {
            Sections = new VirtualTypeList<TranscriptionSection>(this, this._children);
            this.Name = aName;
            this.Begin = aBegin;
            this.End = aEnd;
        }

        public override int AbsoluteIndex
        {
            get
            {

                if (_Parent != null)
                {
                    int sum = 0; //transcription (parent) is root element
                    sum += _Parent.Children.Take(this.ParentIndex) //take previous siblings
                        .Sum(s => s.GetTotalChildrenCount()); //+ all pre siblings counts (index on sublayers)

                    sum += ParentIndex; //+ parent index (index on sibling layer)
                    //this.Text = sum.ToString();
                    return sum;//+1 self .... first children is +1 in absolute indexing

                }

                return 0;
            }
        }

        public override string InnerText
        {
            get { return Name + "\r\n" + string.Join("\r\n", Children.Select(c => c.Text)); }
        }


        public override TranscriptionElement this[TranscriptionIndex index]
        {
            get
            {
                ValidateIndexOrThrow(index);

                if (index.IsSectionIndex)
                {
                    if (index.IsParagraphIndex)
                        return Sections[index.Sectionindex][index];

                    return Sections[index.Sectionindex];
                }
                
                throw new IndexOutOfRangeException("index");
            }
            set
            {
                ValidateIndexOrThrow(index);

                if (index.IsSectionIndex)
                {
                    if (index.IsParagraphIndex)
                        Sections[index.Sectionindex][index] = value;
                    else
                        Sections[index.Sectionindex] = (TranscriptionSection)value;
                }
                else
                    throw new IndexOutOfRangeException("index");

            }
        }

        public override void RemoveAt(TranscriptionIndex index)
        {
            ValidateIndexOrThrow(index);
            if (index.IsSectionIndex)
            {
                if (index.IsParagraphIndex)
                    Sections[index.Sectionindex].RemoveAt(index);
                else
                    Sections.RemoveAt(index.ParagraphIndex);
            }
            else
            {
                throw new IndexOutOfRangeException("index");
            }
        }

        public override void Insert(TranscriptionIndex index, TranscriptionElement value)
        {
            ValidateIndexOrThrow(index);
            if (index.IsSectionIndex)
            {
                if (index.IsParagraphIndex)
                    Sections[index.Sectionindex].Insert(index, value);
                else
                    Sections.Insert(index.Sectionindex,(TranscriptionSection)value);
            }
            else
            {
                throw new IndexOutOfRangeException("index");
            }
        }
    }


}
