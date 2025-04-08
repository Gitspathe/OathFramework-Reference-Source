using UnityEngine;
using UnityEngine.Advertisements;

namespace OathFramework.Ads
{ 

//     public class BannerAd : MonoBehaviour
//     {
//         [SerializeField] private BannerPosition bannerPosition = BannerPosition.BOTTOM_CENTER;
//         [SerializeField] private string androidAdUnitID = "Banner_Android";
//         [SerializeField] private string iosAdUnitID = "Banner_iOS";
//         private string adUnitID = null;
//
//         public static BannerAd Instance { get; private set; }
//
//         private void Start()
//         {
//             // Get the Ad Unit ID for the current platform:
// #if UNITY_IOS
//             adUnitID = iOSAdUnitID;
// #elif UNITY_ANDROID
//             adUnitID = androidAdUnitID;
// #endif
//
//             if(Instance != null) {
//                 Debug.LogError($"Attempted to initialize duplicate {nameof(BannerAd)} singletons.");
//                 Destroy(gameObject);
//             }
//
//             DontDestroyOnLoad(gameObject);
//
//             Instance = this;
//
//             // Set the banner position:
//             Advertisement.Banner.SetPosition(bannerPosition);
//             LoadBanner();
//             ShowBannerAd();
//         }
//
//         // Implement a method to call when the Load Banner button is clicked:
//         public void LoadBanner()
//         {
//             // Set up options to notify the SDK of load events:
//             BannerLoadOptions options = new BannerLoadOptions {
//                 loadCallback = OnBannerLoaded,
//                 errorCallback = OnBannerError
//             };
//
//             // Load the Ad Unit with banner content:
//             Advertisement.Banner.Load(adUnitID, options);
//         }
//
//         // Implement code to execute when the loadCallback event triggers:
//         private void OnBannerLoaded()
//         {
//             Debug.Log("Banner loaded");
//         }
//
//         // Implement code to execute when the load errorCallback event triggers:
//         private void OnBannerError(string message)
//         {
//             Debug.Log($"Banner Error: {message}");
//             // Optionally execute additional code, such as attempting to load another ad.
//         }
//
//         // Implement a method to call when the Show Banner button is clicked:
//         public void ShowBannerAd()
//         {
//             // Set up options to notify the SDK of show events:
//             BannerOptions options = new BannerOptions {
//                 clickCallback = OnBannerClicked,
//                 hideCallback = OnBannerHidden,
//                 showCallback = OnBannerShown
//             };
//
//             // Show the loaded Banner Ad Unit:
//             Advertisement.Banner.Show(adUnitID, options);
//         }
//
//         // Implement a method to call when the Hide Banner button is clicked:
//         public void HideBannerAd()
//         {
//             // Hide the banner:
//             Advertisement.Banner.Hide();
//         }
//
//         private void OnBannerClicked() { }
//         private void OnBannerShown() { }
//         private void OnBannerHidden() { }
//     }

}
