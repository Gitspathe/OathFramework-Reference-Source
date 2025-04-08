using OathFramework.EntitySystem;
using OathFramework.Pooling;
using UnityEngine;

namespace OathFramework.UI
{
    public class DamagePopupManager : MonoBehaviour
    {
        [SerializeField] private PoolParams damagePopupPrefab;
        [SerializeField] private PoolParams damageCritPopupPrefab;
        
        public static DamagePopupManager Instance { get; private set; }
        
        public DamagePopupManager Initialize()
        {
            if(Instance != null) {
                Debug.LogError($"Attempted to initialize multiple {nameof(DamagePopupManager)} singletons.");
                Destroy(gameObject);
                return null;
            }
            Instance = this;
            
            PoolManager.RegisterPool(new PoolManager.GameObjectPool(damagePopupPrefab), true);
            PoolManager.RegisterPool(new PoolManager.GameObjectPool(damageCritPopupPrefab), true);
            return Instance;
        }

        public static void CreatePopup(Vector3 position, in DamageValue damageValue)
        {
            PoolParams prefab = damageValue.IsCritical ? Instance.damageCritPopupPrefab : Instance.damagePopupPrefab;
            GameObject go     = PoolManager.Retrieve(prefab.Prefab, position, Quaternion.identity).gameObject;
            DamagePopup popup = go.GetComponent<DamagePopup>();
            popup.Setup(damageValue.Amount);
        }
    }
}
