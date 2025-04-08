using System.Text;
using UnityEngine;

namespace OathFramework.Utility
{
    public static class TransformUtil
    {
        /// <summary>
        /// Returns a path relative to the root, excluding the root. I.e: arm/elbow/hand.
        /// </summary>
        /// <param name="transform">Transform relative to the root.</param>
        /// <param name="root">Root transform.</param>
        /// <returns>Path relative to and excluding the root transform.</returns>
        public static string GetRelativePath(Transform transform, Transform root)
        {
            StringBuilder builder = StringBuilderCache.Retrieve;
            BuildPath(transform, root, builder);
            return builder.ToString();
        }

        private static void BuildPath(Transform transform, Transform root, StringBuilder builder)
        {
            if(transform == root)
                return;

            if(transform.parent != root) {
                BuildPath(transform.parent, root, builder);
                builder.Append("/");
            }
            builder.Append(transform.name);
        }

        public static Transform GetSiblingTransform(Transform reference, string siblingName)
        {
            if(reference == null || reference.parent == null) 
                return null;
            
            foreach(Transform sibling in reference) {
                if(sibling.name == siblingName) 
                    return sibling;
            }
            return null;
        }
    }
}
