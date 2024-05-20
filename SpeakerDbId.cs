namespace TranscriptionCore
{
    /// <summary> Identifies speaker in a database </summary>
    public class SpeakerDbId
    {
        public string DBID { get; set; }

        /// <summary> Scope, in which DBID is valid/used </summary>
        public DBType DBtype { get; set; }
    }
}
