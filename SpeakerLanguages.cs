using System.Collections.Generic;

namespace TranscriptionCore
{
    public static class SpeakerLanguages
    {
        public static IReadOnlyList<string> All { get; } = new[]
        {
            "CZ",
            "SK",
            "RU",
            "HR",
            "PL",
            "EN",
            "DE",
            "ES",
            "IT",
            "CU",
            "--",
            "😃" // ??
        };

        public static string Default => All[0];
    }
}
