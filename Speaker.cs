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

        /// <summary> Id of the speaker unique only in the parent transcription document </summary>
        internal int SerializationID { get; set; }

        public string FirstName { get; set; } = "";
        public string MiddleName { get; set; }
        public string Surname { get; set; } = "";

        public string DegreeBefore { get; set; }
        public string DegreeAfter { get; set; }

        public Sexes Sex { get; set; }

        public string ImgBase64 { get; set; } // nullable

        public string DefaultLang { get; set; }

        public DateTime Synchronized { get; set; }

        public SpeakerDbId DbId { get; set; } = new SpeakerDbId();

        /// <summary> List of other identifications for this speaker </summary>
        public List<SpeakerDbId> Merges { get; set; } = new List<SpeakerDbId>();

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
            => $"{FullName} ({DefaultLang})";

        public static readonly int DefaultID = int.MinValue;

        public static readonly Speaker DefaultSpeaker = new Speaker()
        {
            SerializationID = DefaultID,
            DbId = new SpeakerDbId { DBID = new Guid().ToString() }
        };


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
