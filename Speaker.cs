using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Linq;


namespace TranscriptionCore
{
    public class Speaker : IUpdateTracking
    {
        public bool Revert(Undo act)
        {
            if (act is SpeakerUndo)
            {
                switch (act)
                {
                    case PinnedChanged c:
                        PinnedToDocument = c.Old;
                        return true;
                    case FirstNameChanged c:
                        FirstName = c.Old;
                        return true;
                    case MiddleNameChanged c:
                        MiddleName = c.Old;
                        return true;
                    case SurnameChanged c:
                        Surname = c.Old;
                        return true;
                    case SexChanged c:
                        Sex = c.Old;
                        return true;
                    case CustomElementsChanged c:
                        Elements = c.Old;
                        return true;
                    case ImgBase64Changed c:
                        ImgBase64 = c.Old;
                        return true;
                    case DefaultLangChanged c:
                        DefaultLang = c.Old;
                        return true;
                    case DegreeBeforeChanged c:
                        DegreeBefore = c.Old;
                        return true;
                    case DegreeAfterChanged c:
                        DegreeAfter = c.Old;
                        return true;
                    case DatabaseIdChanged c:
                        DataBaseID = c.Old;
                        return true;
                    case SynchronizedChanged c:
                        Synchronized = c.Old;
                        return true;
                    case MergesChanged c:
                        Merges = c.Old;
                        return true;

                }
            }
            return false;
        }
        public record SpeakerUndo : Undo;
        public record CustomElementsChanged(ImmutableDictionary<string, string> Old) : SpeakerUndo;
        public record PinnedChanged(bool Old) : SpeakerUndo;
        public record FirstNameChanged(string Old) : SpeakerUndo;
        public record MiddleNameChanged(string? Old) : SpeakerUndo;
        public record SurnameChanged(string Old) : SpeakerUndo;
        public record SexChanged(Sexes Old) : SpeakerUndo;

        public record ImgBase64Changed(string? Old) : SpeakerUndo;
        public record DefaultLangChanged(string Old) : SpeakerUndo;
        public record DegreeBeforeChanged(string? Old) : SpeakerUndo;
        public record DegreeAfterChanged(string? Old) : SpeakerUndo;
        public record DatabaseIdChanged(DBMerge Old) : SpeakerUndo;
        public record SynchronizedChanged(DateTime Old) : SpeakerUndo;
        public record MergesChanged(ImmutableArray<DBMerge> Old) : SpeakerUndo;

        public UpdateTracker Updates { get; }


        public ImmutableArray<SpeakerAttribute> Attributes = ImmutableArray<SpeakerAttribute>.Empty;

        private bool _PinnedToDocument = false;
        /// <summary>
        ///  == Do not delete from document, when not used in any paragraph
        /// </summary>
        public bool PinnedToDocument
        {
            get => _PinnedToDocument;
            set
            {
                var oldv = _PinnedToDocument;
                Updates.OnContentChanged(new PinnedChanged(oldv));
                _PinnedToDocument = value;
            }
        }

        [Obsolete("Serialization ID, Changed when Transcription is serialized.For user ID use DBID property")]
        internal int SerializationID { get; set; } = DefaultID;


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
        [AllowNull]
        public string FirstName
        {
            get => _firstName;

            set
            {
                var oldv = _firstName;
                _firstName = value ?? "";
                Updates.OnContentChanged(new FirstNameChanged(oldv));
            }
        }
        private string _surName = "";
        public string Surname
        {
            get => _surName;

            set
            {
                var oldv = _surName;
                _surName = value ?? "";
                Updates.OnContentChanged(new SurnameChanged(oldv));
            }
        }

        private Sexes sex;
        public Sexes Sex
        {
            get => sex;
            set
            {
                var oldv = sex;
                sex = value;
                Updates.OnContentChanged(new SexChanged(oldv));
            }
        }


        private string? imgBase64;
        public string? ImgBase64
        {
            get => imgBase64;
            set
            {
                var oldv = imgBase64;
                imgBase64 = value;
                Updates.OnContentChanged(new ImgBase64Changed(oldv));
            }
        }

        string _defaultLang = "";
        public string DefaultLang
        {
            get => _defaultLang;

            set
            {
                var oldv = _defaultLang;
                _defaultLang = value;
                Updates.OnContentChanged(new DefaultLangChanged(oldv));
            }
        }

        private string? middleName;
        public string? MiddleName
        {
            get => middleName;
            set
            {
                var oldv = middleName;
                middleName = value;
                Updates.OnContentChanged(new MiddleNameChanged(oldv));
            }
        }

        private string? degreeBefore;

        public string? DegreeBefore
        {
            get => degreeBefore;
            set
            {
                var oldv = degreeBefore;
                degreeBefore = value;
                Updates.OnContentChanged(new DegreeBeforeChanged(oldv));
            }
        }
        private string? degreeAfter;
        public string? DegreeAfter
        {
            get => degreeAfter;
            set
            {
                var oldv = degreeAfter;
                degreeAfter = value;
                Updates.OnContentChanged(new DegreeAfterChanged(oldv));
            }
        }

        private DBMerge dataBaseID = Constants.DefaultSpeakerID;
        public DBMerge DataBaseID
        {
            get => dataBaseID;
            set
            {
                var oldv = dataBaseID;
                dataBaseID = value;
                Updates.OnContentChanged(new DatabaseIdChanged(oldv));
            }
        }
        private DateTime synchronized;
        public DateTime Synchronized
        {
            get => synchronized;
            set
            {
                var oldv = synchronized;
                synchronized = value;
                Updates.OnContentChanged(new SynchronizedChanged(oldv));
            }
        }


        private ImmutableArray<DBMerge> merges = ImmutableArray<DBMerge>.Empty;
        public ImmutableArray<DBMerge> Merges
        {
            get => merges;
            set
            {
                var oldv = merges;
                merges = value;
                Updates.OnContentChanged(new MergesChanged(oldv));
            }
        }

        public Speaker()
        {
            FirstName = "";
            Surname = "";
            Sex = Sexes.X;
            ImgBase64 = null;

            Updates = new UpdateTracker()
            {
                ContentChanged = OnChange
            };
        }
        internal void OnChange(IEnumerable<Undo> undos)
        {
            /*Parent.OnChange(*/
            // undos.Select(u => u with { TranscriptionIndex = u.TranscriptionIndex with { Sectionindex = ParentIndex } });
            //);
        }


        private ImmutableDictionary<string, string> elements = ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase);


        public ImmutableDictionary<string, string> Elements
        {

            get => elements;
            set
            {
                var oldv = elements;
                elements = value;
                Updates.OnContentChanged(new CustomElementsChanged(oldv));
            }
        }

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

            var elms = s.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);

            if (elms.TryGetValue("comment", out string? rem))
            {
                SpeakerAttribute sa = new SpeakerAttribute("comment", rem, default);
                sp.Attributes.Add(sa);
            }

            if (elms.TryGetValue("lang", out rem))
            {
                sp.DefaultLang = rem;
            }

            elms.Remove("id");
            elms.Remove("firstname");
            elms.Remove("surname");
            elms.Remove("sex");
            elms.Remove("comment");
            elms.Remove("lang");

            sp.Elements = sp.Elements.AddRange(elms);

            return sp;
        }
        public Speaker(XElement s) : this()//V3 format
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

            var elms = s.Attributes().ToDictionary(a => a.Name.ToString(), a => a.Value);

            this.DataBaseID = DBMerge.Deserialize(s);

            if (elms.TryGetValue("synchronized", out var rem))
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
                        date = DateTime.Now;
                    }
                }
                else
                    date = DateTime.Now;
                this.Synchronized = date;
            }


            if (elms.TryGetValue("middlename", out rem))
                this.MiddleName = rem;

            if (elms.TryGetValue("degreebefore", out rem))
                this.DegreeBefore = rem;

            if (elms.TryGetValue("pinned", out rem))
                this.PinnedToDocument = XmlConvert.ToBoolean(rem);

            if (elms.TryGetValue("degreeafter", out rem))
                this.DegreeAfter = rem;


            elms.Remove("id");
            elms.Remove("surname");
            elms.Remove("firstname");
            elms.Remove("sex");
            elms.Remove("lang");

            elms.Remove("dbid");
            elms.Remove("dbtype");
            elms.Remove("middlename");
            elms.Remove("degreebefore");
            elms.Remove("degreeafter");
            elms.Remove("synchronized");

            elms.Remove("pinned");

            Elements.AddRange(elms);
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
        private Speaker(Speaker s) : this()
        {
            ToolsAndExtensions.MergeSpeakers(this, s);
        }

        public Speaker(string aSpeakerFirstname, string aSpeakerSurname, Sexes aPohlavi, string aSpeakerFotoBase64) : this() //constructor ktery vytvori speakera
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
    }
}