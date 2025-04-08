namespace OathFramework.Editor
{
    public static class PropertySerializerHelper
    {
        public static string ToBackingField(string property) => $"<{property}>k__BackingField";
    }
}
