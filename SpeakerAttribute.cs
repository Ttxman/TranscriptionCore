using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace TranscriptionCore
{
    /// <summary>
    /// custom text based value for speaker V3+
    /// </summary>
    public record SpeakerAttribute(string Name, string Value, DateTime Date)
    {
        public static SpeakerAttribute Deserialize(XElement elm)
        {
            var name = elm.Attribute("name")!.Value;

            DateTime date = default;

            if (elm.Attribute("date")?.Value is { } sdate)
            {
                try
                {
                    date = XmlConvert.ToDateTime(sdate, XmlDateTimeSerializationMode.Local); //stored in UTC convert to local
                }
                catch
                {
                    if (DateTime.TryParse(sdate, CultureInfo.CreateSpecificCulture("cs"), DateTimeStyles.None, out date))
                        date = TimeZoneInfo.ConvertTimeFromUtc(date, TimeZoneInfo.Local);
                    else
                        date = DateTime.Now;
                }
            }

            return new SpeakerAttribute(name, elm.Value, date);
        }

        public XElement Serialize()
        {
            return new XElement("a",
                new XAttribute("name", Name),
                new XAttribute("date", XmlConvert.ToString(Date, XmlDateTimeSerializationMode.Utc)),
                Value
                );
        }
    }
}
