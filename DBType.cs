namespace TranscriptionCore
{
    /// <summary> Declares in which scope the object is identified </summary>
    public enum DBType
    {
        /// <summary> Default, but otherwise probably not used... what does it mean? Maybe user-defined id? </summary>
        User,

        /// <summary> The object is defined only in scope of the file, in this case DBID is not used</summary>
        File,

        /// <summary> The object is identified and persisted by a backend API (usually database) </summary>
        Api
    }
}
