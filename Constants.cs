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

        public static readonly int DefailtSpeakerId = int.MinValue;
    }
}
