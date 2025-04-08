using OathFramework.Audio;
using OathFramework.Utility;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameCode.MagitechRequiem.Misc
{
    [RequireComponent(typeof(AudioSource), typeof(BasicAudioFadeIn))]
    public class MainMenuMeme : MonoBehaviour
    {
        [SerializeField] private AudioClip clip;

        private void Awake()
        {
            if(FRandom.Cache.Range(0, 256) == 1) {
                GetComponent<BasicAudioFadeIn>().enabled = false;
                GetComponent<AudioSource>().clip         = clip;
                GetComponent<AudioSource>().pitch        = 1.0f;
                GetComponent<AudioSource>().Play();
            }
        }
    }
}
