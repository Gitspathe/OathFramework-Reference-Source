namespace OathFramework.Utility
{
    public sealed class LookupValue
    {
        public string Key       { get; }
        public ushort DefaultID { get; }

        public LookupValue(string key, ushort defaultID)
        {
            Key       = key;
            DefaultID = defaultID;
        }
    }
}
