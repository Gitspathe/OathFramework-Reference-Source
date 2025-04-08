using UnityEngine;
using UnityEngine.Advertisements;

namespace OathFramework.Ads
{ 
//
//     public class AdsInitializer : MonoBehaviour, IUnityAdsInitializationListener
//     {
//         [SerializeField] private string androidGameID;
//         [SerializeField] private string iosGameID;
//         [SerializeField] private bool adsEnabled = true;
//         [SerializeField] private bool testMode = true;
//         [SerializeField] private GameObject bannerAdPrefab;
//         private string gameID;
//
//         public static AdsInitializer Instance { get; private set; }
//
//         void Awake()
//         {
//             if(Instance != null) {
//                 Debug.LogError($"Attempted to initialize duplicate {nameof(AdsInitializer)} singleton.");
//                 Destroy(gameObject);
//             }
//
//             DontDestroyOnLoad(gameObject);
//
//             InitializeAds();
//
//             Instance = this;
//         }
//
//         private void OnEnable()
//         {
//             if(adsEnabled && BannerAd.Instance != null) {
//                 BannerAd.Instance.ShowBannerAd();
//             }
//         }
//
//         private void OnDestroy()
//         {
//             if(BannerAd.Instance != null) {
//                 BannerAd.Instance.HideBannerAd();
//             }
//
//             Instance = null;
//         }
//
//         public void InitializeAds()
//         {
// #if UNITY_IOS
//             gameID = iOSGameID;
// #elif UNITY_ANDROID
//             gameID = androidGameID;
// #elif UNITY_EDITOR
//             gameID = androidGameID; //Only for testing the functionality in the Editor
// #endif
//             if(adsEnabled && !Advertisement.isInitialized && Advertisement.isSupported) {
//                 Advertisement.Initialize(gameID, testMode, this);
//             }
//         }
//
//
//         public void OnInitializationComplete()
//         {
//             Debug.Log("Unity Ads initialization complete.");
//             Instantiate(bannerAdPrefab);
//         }
//
//         public void OnInitializationFailed(UnityAdsInitializationError error, string message)
//         {
//             Debug.Log($"Unity Ads Initialization Failed: {error.ToString()} - {message}");
//         }
//
//     }

}
