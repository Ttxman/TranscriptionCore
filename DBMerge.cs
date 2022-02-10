using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace TranscriptionCore
{
    public record DBMerge(DBType DBType, string DBID)
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="elm"></param>
        /// <returns>if elm is not null returns elm with new attributes, otherwise returns new "m" element</returns>
        internal XElement Serialize(XElement? elm = null)
        {
            elm ??= new XElement("m");
            string val = this.DBType switch
            {
                DBType.Api => "api",
                DBType.User => "user",
                _ => "file",
            };

            elm.Add(new XAttribute("dbid", DBID), new XAttribute("dbtype", val));
            return elm;
        }

        public static DBMerge Deserialize(XElement e)
        {
            var dbid = e.Attribute("dbid")?.Value;
            if (dbid is null)
                return Constants.DefaultSpeakerID;

            var t = e.Attribute("dbtype")?.Value switch
            {
                "user" => DBType.User,
                "api" => DBType.Api,
                _ => DBType.File,
            };

            return new DBMerge(t, dbid);
        }
    }
}
