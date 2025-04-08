using System;
using UnityEngine.Rendering.Universal;

namespace OathFramework.Persistence
{
    public class LoadAbortedException : Exception
    {
        public LoadAbortedException() {}
        public LoadAbortedException(string message) : base(message) {}
        public LoadAbortedException(string message, Exception innerException) : base(message, innerException) {}
    }
}
