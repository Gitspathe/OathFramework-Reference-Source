using System.Text;
using System.Threading;

namespace OathFramework.Utility
{
    public static class StringBuilderCache
    {
        private const int InitialCapacity = 100;

        private static readonly ThreadLocal<StringBuilder> Cache = new(() => new StringBuilder(InitialCapacity));

        public static StringBuilder Retrieve => Cache.Value.Clear();
    }
}
