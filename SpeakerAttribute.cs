using System;
using System.Collections.Generic;

namespace TranscriptionCore
{
    /// <summary>
    /// custom text based value for speaker V3+
    /// </summary>
    public class SpeakerAttribute
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public DateTime Date { get; set; }
        public SpeakerAttributeScope For { get; set; }

        public SpeakerAttribute(string id, string name, string value, DateTime date = default, SpeakerAttributeScope @for = SpeakerAttributeScope.All)
        {
            ID = id;
            Name = name;
            Value = value;
            Date = date;
            For = @for;
        }

        public class Comparer : IEqualityComparer<SpeakerAttribute>
        {
            public static Comparer Instance { get; } = new Comparer();

            public bool Equals(SpeakerAttribute x, SpeakerAttribute y)
                => x?.Name == y?.Name && x?.Value == y?.Value;

            public int GetHashCode(SpeakerAttribute obj)
                => obj.Name.GetHashCode() ^ obj.Value.GetHashCode();
        }
    }

    /// <summary>Defines scope in which this attribute is valid and which the attribute should not leave</summary>
    public enum SpeakerAttributeScope
    {
        All,
        Trsx,
        Db
    }
}
