using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace OathFramework.Utility
{
    public class CallbackAccessor
    {
#if DEBUG
        private AccessToken token;
        private bool init;
#endif

        public AccessToken GenerateAccessToken()
        {
#if DEBUG
            if(init)
                throw new InvalidOperationException("Callback accessor is already initialized.");

            token = new AccessToken();
            return token;
#else
            return default;
#endif
        }
        
        [Conditional("DEBUG"), MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void EnsureAccess(AccessToken token)
        {
#if DEBUG
            if(this.token != token)
                throw new InvalidOperationException("Invalid callback trigger. Token does not match!");
#endif
        }
    }
    
#if DEBUG
    public class AccessToken { /* Empty class - the token is used for access only. */ }
#else 
    public struct AccessToken { }
#endif
}
