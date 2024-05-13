namespace TranscriptionCore
{
    public class DBMerge
    {
        public string DBID { get; private set; }
        public DBType DBtype { get; private set; }

        public DBMerge(string DBID, DBType type)
        {
            this.DBID = DBID;
            this.DBtype = type;
        }
    }
}
