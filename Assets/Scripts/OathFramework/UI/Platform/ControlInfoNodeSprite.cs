using UnityEngine;
using UnityEngine.UI;

namespace OathFramework.UI.Platform
{
    public class ControlInfoNodeSprite : MonoBehaviour
    {
        [SerializeField] private Image image;

        public GameObject Setup(Sprite sprite)
        {
            image.sprite = sprite;
            return gameObject;
        }
    }
}
