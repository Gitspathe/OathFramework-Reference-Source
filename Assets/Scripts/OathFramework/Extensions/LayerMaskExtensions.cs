using System.Runtime.CompilerServices;
using UnityEngine;

namespace OathFramework.Extensions
{
    public static class LayerMaskExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsLayer(this LayerMask mask, int layer)
        {
            return (mask.value & (1 << layer)) > 0;
        }
    }
}
