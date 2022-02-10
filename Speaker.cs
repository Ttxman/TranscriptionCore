using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Linq;


namespace TranscriptionCore
{
    public class Speaker
    {
        public List<SpeakerAttribute> Attributes = new List<SpeakerAttribute>();

        private bool _PinnedToDocument = false;
        /// <summary>
        ///  == Do not delete from document, when not used in any paragraph
        /// </summary>
        public bool PinnedToDocument
        {
            get { return _PinnedToDocument; }
            set
            {
                _PinnedToDocument = value;
            }
        }

        [Obsolete("Serialization ID, Changed when Transcription is serialized.For user ID use DBID property")]
        internal int SerializationID
        {
            get;
            set;
        } = DefaultID;


        public static string GetFullName(string FirstName, string? MiddleName, string Surname)
        {
            string pJmeno = "";
            if (FirstName is { } && FirstName.Length > 0)
            {
                pJmeno += FirstName.Trim();
            }
            if (MiddleName is { } && MiddleName.Length > 0)
            {
                pJmeno += " " + MiddleName.Trim();
            }

            if (Surname is { } && Surname.Length > 0)
            {
                if (pJmeno.Length > 0) pJmeno += " ";
                pJmeno += Surname.Trim();
            }

            if (string.IsNullOrEmpty(pJmeno))
                pJmeno = "---";


            return pJmeno;
        }

        public string FullName
        {
            get
            {
                return GetFullName(this.FirstName, this.MiddleName, this.Surname);
            }
        }

        public enum Sexes : byte
        {
            X = 0,
            Male = 1,
            Female = 2
        }

        private string _firstName = "";
        public string FirstName
        {
            get
            {
                return _firstName;
            }

            set
            {
                _firstName = value ?? "";
            }
        }
        private string _surName = "";
        public string Surname
        {
            get
            {
                return _surName;
            }

            set
            {
                _surName = value ?? "";
            }
        }
        public Sexes Sex;

        public string? ImgBase64;

        string? _defaultLang = null;
        public string DefaultLang
        {
            get
            {
                return _defaultLang ?? Langs[0];
            }

            set
            {
                _defaultLang = value;
            }
        }


        public string? DegreeBefore;
        public string? MiddleName;
        public string? DegreeAfter;


        public Speaker()
        {
            FirstName = "";
            Surname = "";
            Sex = Sexes.X;
            ImgBase64 = null;
            DefaultLang = Langs[0];
        }
        #region serializace nova


        public static readonly List<string> Langs = new List<string> { "CZ", "SK", "RU", "HR", "PL", "EN", "DE", "ES", "IT", "CU", "--", "😃" };
        public Dictionary<string, string> Elements = new Dictionary<string, string>();

        internal static Speaker DeserializeV2(XElement s, bool isStrict)
        {
            Speaker sp = new Speaker();

            if (!s.CheckRequiredAtributes("id", "surname"))
                throw new ArgumentException("required attribute missing on v2format speaker  (id, surname)");

#pragma warning disable CS0618 // Type or member is obsolete
            sp.SerializationID = int.Parse(s.Attribute("id")!.Value);
#pragma warning restore CS0618 // Type or member is obsolete
            sp.Surname = s.Attribute("surname")!.Value;
            sp.FirstName = s.Attribute("firstname")?.Value ?? "";

            sp.Sex = s.Attribute("sex")?.Value switch
            {
                "M" => Sexes.Male,
                "F" => Sexes.Female,
                _ => Sexes.X,
            };

            sp.Elements = s.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);

            if (sp.Elements.TryGetValue("comment", out string? rem))
            {
                SpeakerAttribute sa = new SpeakerAttribute("comment", rem, default);
                sp.Attributes.Add(sa);
            }

            if (sp.Elements.TryGetValue("lang", out rem))
            {
                int idx = Langs.IndexOf(rem);
                sp.DefaultLang = rem;
            }

            sp.Elements.Remove("id");
            sp.Elements.Remove("firstname");
            sp.Elements.Remove("surname");
            sp.Elements.Remove("sex");
            sp.Elements.Remove("comment");
            sp.Elements.Remove("lang");

            return sp;
        }
        internal static CultureInfo csCulture = CultureInfo.CreateSpecificCulture("cs");
        public Speaker(XElement s)//V3 format
        {
            if (!s.CheckRequiredAtributes("id", "surname", "firstname", "sex", "lang"))
                throw new ArgumentException("required attribute missing on speaker (id, surname, firstname, sex, lang)");

#pragma warning disable CS0618 // Type or member is obsolete
            SerializationID = int.Parse(s.Attribute("id")!.Value);
#pragma warning restore CS0618 // Type or member is obsolete
            Surname = s.Attribute("surname")!.Value;
            FirstName = s.Attribute("firstname")?.Value ?? "";

            Sex = s.Attribute("sex")?.Value switch
            {
                "m" => Sexes.Male,
                "f" => Sexes.Female,
                _ => Sexes.X,
            };

            DefaultLang = s.Attribute("lang")!.Value.ToUpper();

            //merges
            this.Merges.AddRange(s.Elements("m").Select(m => DBMerge.Deserialize(m)));

            Attributes.AddRange(s.Elements("a").Select(e => SpeakerAttribute.Deserialize(e)));

            Elements = s.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);

            this.DataBaseID = DBMerge.Deserialize(s);

            if (Elements.TryGetValue("synchronized", out var rem))
            {
                DateTime date;
                if (!string.IsNullOrWhiteSpace(rem)) //i had to load big archive with empty synchronized attribute .. this is significant speedup
                {
                    //problem with saving datetimes in local format
                    try
                    {
                        date = XmlConvert.ToDateTime(rem, XmlDateTimeSerializationMode.Local); //stored in UTC convert to local
                    }
                    catch
                    {
                        if (DateTime.TryParse(rem, csCulture, DateTimeStyles.None, out date))
                            date = TimeZoneInfo.ConvertTimeFromUtc(date, TimeZoneInfo.Local);
                        else
                            date = DateTime.Now;
                    }
                }
                else
                    date = DateTime.Now;
                this.Synchronized = date;
            }


            if (Elements.TryGetValue("middlename", out rem))
                this.MiddleName = rem;

            if (Elements.TryGetValue("degreebefore", out rem))
                this.DegreeBefore = rem;

            if (Elements.TryGetValue("pinned", out rem))
                this.PinnedToDocument = XmlConvert.ToBoolean(rem);

            if (Elements.TryGetValue("degreeafter", out rem))
                this.DegreeAfter = rem;


            Elements.Remove("id");
            Elements.Remove("surname");
            Elements.Remove("firstname");
            Elements.Remove("sex");
            Elements.Remove("lang");

            Elements.Remove("dbid");
            Elements.Remove("dbtype");
            Elements.Remove("middlename");
            Elements.Remove("degreebefore");
            Elements.Remove("degreeafter");
            Elements.Remove("synchronized");

            Elements.Remove("pinned");
        }

        /// <summary>
        /// serialize speaker
        /// </summary>
        /// <param name="saveAll">save including image and merges, used when saving database</param>
        /// <returns></returns>
        public XElement Serialize(bool saveAll = false) //v3
        {
            XElement elm = new XElement("s",
                Elements.Select(e =>
                    new XAttribute(e.Key, e.Value))
                    .Union(new[]{
#pragma warning disable CS0618 // Type or member is obsolete
                    new XAttribute("id", SerializationID.ToString()),
#pragma warning restore CS0618 // Type or member is obsolete
                    new XAttribute("surname",Surname),
                    new XAttribute("firstname",FirstName),
                    new XAttribute("sex",(Sex==Sexes.Male)?"m":(Sex==Sexes.Female)?"f":"x"),
                    new XAttribute("lang",DefaultLang.ToLower())

                    })
            );

            if (!string.IsNullOrWhiteSpace(MiddleName))
                elm.Add(new XAttribute("middlename", MiddleName));

            if (!string.IsNullOrWhiteSpace(DegreeBefore))
                elm.Add(new XAttribute("degreebefore", DegreeBefore));

            if (!string.IsNullOrWhiteSpace(DegreeAfter))
                elm.Add(new XAttribute("degreeafter", DegreeAfter));

            if (DataBaseID.DBType != DBType.File)
            {
                DataBaseID.Serialize(elm);
                elm.Add(new XAttribute("synchronized", XmlConvert.ToString(DateTime.UtcNow, XmlDateTimeSerializationMode.Utc)));//stored in UTC convert from local
            }

            if (PinnedToDocument)
                elm.Add(new XAttribute("pinned", true));

            if (saveAll)
                foreach (var m in Merges)
                    elm.Add(m.Serialize());

            foreach (var a in Attributes)
                elm.Add(a.Serialize());

            return elm;
        }
        #endregion

        internal class AttributeComparer : IEqualityComparer<SpeakerAttribute>
        {
            public bool Equals(SpeakerAttribute? x, SpeakerAttribute? y)
            {
                if (x is null && y is null)
                    return true;

                if (x is null || y is null)
                    return false;

                return x.Name == y.Name && x.Value == y.Value;
            }

            public int GetHashCode(SpeakerAttribute obj)
                => HashCode.Combine(obj.Name, obj.Value);
        }

        /// <summary>
        /// copy constructor - copies all info, but with new DBID, ID ....
        /// </summary>
        /// <param name="s"></param>
        private Speaker(Speaker s)
        {
            ToolsAndExtensions.MergeSpeakers(this, s);
        }

        public Speaker(string aSpeakerFirstname, string aSpeakerSurname, Sexes aPohlavi, string aSpeakerFotoBase64) //constructor ktery vytvori speakera
        {
            FirstName = aSpeakerFirstname;
            Surname = aSpeakerSurname;
            Sex = aPohlavi;
            ImgBase64 = aSpeakerFotoBase64;
        }


        public override string ToString()
        {
            return FullName + " (" + DefaultLang + ")";
        }

        public static readonly int DefaultID = Constants.DefaultSpeakerId;
        public static readonly Speaker DefaultSpeaker = Constants.DefaultSpeaker;

        /// <summary>
        /// copies all info, and generates new DBI and ID .... (deep copy)
        /// </summary>
        /// <param name="s"></param>
        public Speaker Copy()
        {
            return new Speaker(this);
        }

        public DBMerge DataBaseID { get; set; } = Constants.DefaultSpeakerID;

        public DateTime Synchronized { get; set; }

        public List<DBMerge> Merges = new List<DBMerge>();
    }
}