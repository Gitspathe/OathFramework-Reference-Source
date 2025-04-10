# Disclaimer
This repo is for public access to the code of OathFramework and Magitech Requiem. It is not a complete project, and you WILL get compile errors without making your own changes. With that being said, the code is fairly modular, so you are free to learn and/or take pieces of the code for your own projects.
If you want to compile your own version, the following assets and changes are needed:
- MicroSplat and MicroSplat for URP 2022.3
- FinalIK
- Basic replacement for "Scripts/OathFramework/Core/SupporterDLCUtil.cs", which is excluded due to containing secrets. (see below)
- Various misc. assets which are not important. Code referencing these can be deleted without breaking major functionality.

Getting an actual project working is another issue, since a ton of work needs to be spent on configuring objects within Unity. Eventually I'll try to write some docs on how to get that working, probably.

## Example SupporterDLCUtil Replacement

```
namespace OathFramework.Core 
{
    public static class SupporterDLCUtil
    {
        public static bool HasSupporterDLC { get; private set; }
        public static ulong Secret         { get; private set; }
    
        public static async UniTask Init()
        {
            Secret          = await GetSecret();
            HasSupporterDLC = ExecHasSupporterDLCCheck(Secret);
        }
    
        private static bool ExecHasSupporterDLCCheck(ulong secret)
        {
            return false;
        }
    
        public static async UniTask<ulong> GetSecret()
        {
            return 0;
        }
    
        public static bool VerifySecret(ulong secret)
        {
            return false;
        }
    }
}
```
