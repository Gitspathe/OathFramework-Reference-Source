using OathFramework.Audio;
using OathFramework.EntitySystem;
using OathFramework.EquipmentSystem;
using OathFramework.Utility;
using UnityEngine;

namespace OathFramework.Effects
{

    public class CasingsEffect : MonoBehaviour, 
        IWeaponModelInit, IEquipmentUseCallback
    {
        [SerializeField] private Transform spawnTransform;
        [SerializeField] private GameObject instancePrefab;
        [SerializeField] private bool createOnFire = true;

        [Space(10)]
        
        [SerializeField] private AudioParams sound;
        [SerializeField] private float soundDelay  = 0.5f;
        [SerializeField] private float soundChance = 1.0f;
        [SerializeField] private int soundCount    = 8;

        private QList<float> soundBuffer;
        private GameObject instance;
        private ParticleSystem instancePS;
        private IEquipmentUserController owner;

        private void LateUpdate()
        {
            if(ReferenceEquals(instance, null))
                return;

            instance.transform.SetPositionAndRotation(spawnTransform.position, spawnTransform.rotation);
            if(ReferenceEquals(sound, null))
                return;
            
            HandleSounds();
        }

        private void PushSound()
        {
            if(soundBuffer.Count == soundCount) {
                soundBuffer.RemoveAt(soundBuffer.Count - 1);
            }
            soundBuffer.Add(soundDelay);
        }

        private void HandleSounds()
        {
            int count = soundBuffer.Count;
            for(int i = count - 1; i >= 0; i--) {
                soundBuffer.Array[i] -= Time.deltaTime;
                if(soundBuffer.Array[i] > 0.0f)
                    continue;

                AudioPool.Retrieve(spawnTransform.position, sound);
                soundBuffer.RemoveAt(i);
            }
        }
        
        private void OnDisable()
        {
            if(owner != null) {
                owner.Equipment.Callbacks.Unregister((IEquipmentUseCallback)this);
            }
            if(instance == null)
                return;
            
            Destroy(instance, 10.0f);
            instance   = null;
            instancePS = null;
        }

        public void Emit()
        {
            instancePS.Emit(1);
            if(!ReferenceEquals(sound, null) && Random.Range(0.0f, 1.0f) < soundChance) {
                PushSound();
            }
        }

        void IWeaponModelInit.OnInitialized(IEquipmentUserController owner)
        {
            soundBuffer = new QList<float>(soundCount);
            instance    = Instantiate(instancePrefab);
            instancePS  = instance.GetComponent<ParticleSystem>();
            this.owner  = owner;
            owner.Equipment.Callbacks.Register((IEquipmentUseCallback)this);
        }

        void IEquipmentUseCallback.OnEquipmentUse(EntityEquipment equipment, Equippable equippable, int ammo)
        {
            if(!createOnFire)
                return;

            Emit();
        }
    }

}
