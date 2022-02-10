using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranscriptionCore
{
    internal class Constants
    {
        public static readonly TimeSpan UnknownTime = new TimeSpan(-1);

        public static readonly ImmutableHashSet<string> IgnoreCaseHashset = ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase);

        public static readonly int DefaultSpeakerId = int.MinValue;
        internal static readonly Speaker DefailtSpeaker = new Speaker()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            SerializationID = DefaultSpeakerId,
#pragma warning restore CS0618 // Type or member is obsolete
            DBID = Guid.Empty.ToString() //0000-0000 ....
        };
    }
}
