using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using TranscriptionCore.Serialization;

namespace TranscriptionCore
{
    /// <summary> Information about a speaker inside a transcription document </summary>
    public class Speaker
    {
        /// <summary> Do not delete from document, when not used in any paragraph </summary>
        public bool PinnedToDocument { get; set; } = false;

        /// <summary> Serialization ID, Changed when Transcription is serialized. For user ID use DBID property </summary>
        internal int SerializationID { get; set; }

        public string FirstName { get; set; } = "";
        public string MiddleName { get; set; }
        public string Surname { get; set; } = "";

        public string DegreeBefore { get; set; }
        public string DegreeAfter { get; set; }

        public Sexes Sex { get; set; }

        public string ImgBase64 { get; set; } // nullable

        public string DefaultLang { get; set; }

        public DBType DataBaseType { get; set; }

        public DateTime Synchronized { get; set; }


        public List<DBMerge> Merges { get; set; } = new List<DBMerge>();

        public Dictionary<string, string> Elements { get; set; } = new Dictionary<string, string>();

        public List<SpeakerAttribute> Attributes { get; set; } = new List<SpeakerAttribute>();


        public enum Sexes : byte
        {
            X = 0,
            Male = 1,
            Female = 2
        }

        public Speaker()
        {
            Sex = Sexes.X;
            ImgBase64 = null;
            DefaultLang = SpeakerLanguages.Default;
        }

        public Speaker(XElement s)
            : this()
        {
            SerializationV3.Deserialize(s, this);
        }

        public override string ToString()
        {
            return FullName + " (" + DefaultLang + ")";
        }

        public static readonly int DefaultID = int.MinValue;
        public static readonly Speaker DefaultSpeaker = new Speaker() { SerializationID = DefaultID, DBID = new Guid().ToString() };

        string _dbid = null;

        /// <summary>
        /// if not set, GUID is automatically generated on first read
        /// if DataBaseType is DBType.User - modification is disabled
        /// if DataBaseType is DBType.File - processing of value is disabled
        /// SHOULD be always UNIQUE GUID-like string (NanoTrans expects that ids from DBType.API and DBType.User can't conflict)
        /// empty string is automatically converted to null, dbid will be generated on next read
        /// </summary>
        public string DBID
        {
            get
            {
                if (_dbid == null && DBType != DBType.File)
                {
                    _dbid = Guid.NewGuid().ToString();
                }

                return _dbid;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    _dbid = null;
                else if (string.IsNullOrWhiteSpace(_dbid))
                    _dbid = value;
                else if (DataBaseType == DBType.User)
                    throw new ArgumentException("cannot change DBID when Dabase is User");
                else
                    _dbid = value;

            }
        }

        /// <summary> alias for DataBaseType </summary>
        public DBType DBType
        {
            get => this.DataBaseType;
            set => this.DataBaseType = value;
        }

        public string FullName
        {
            get
            {
                var buff = new StringBuilder();
                Append(this.FirstName);
                Append(this.MiddleName);
                Append(this.Surname);

                return buff.Length > 0
                    ? buff.ToString()
                    : "---";

                void Append(string part)
                {
                    if (string.IsNullOrWhiteSpace(part))
                        return;

                    if (buff.Length > 0)
                        buff.Append(" "); // add separator

                    buff.Append(part.Trim());
                }
            }
        }
    }
}
