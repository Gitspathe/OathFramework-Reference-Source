using OathFramework.UI;
using OathFramework.Utility;
using UnityEngine;

namespace OathFramework.EntitySystem
{
    public class EntityDamagePopupHandler : MonoBehaviour, 
        IEntityInitCallback, IEntityTakeDamageCallback
    {
        uint ILockableOrderedListElement.Order => 9_000;
        
        void IEntityInitCallback.OnEntityInitialize(Entity entity)
        {
            entity.Callbacks.Register((IEntityTakeDamageCallback)this);
        }

        void IEntityTakeDamageCallback.OnDamage(Entity entity, bool fromRpc, in DamageValue val)
        {
            if(fromRpc && val.GetInstigator(out Entity instigator) && instigator.IsOwner)
                return;

            DamagePopupManager.CreatePopup(val.HitPosition, in val);
        }
    }
}
