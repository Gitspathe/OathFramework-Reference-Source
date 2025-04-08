using UnityEngine;
using OathFramework.Core;
using OathFramework.Data;
using OathFramework.Effects;
using OathFramework.EntitySystem;
using OathFramework.Utility;
using System.Collections.Generic;

namespace OathFramework.EquipmentSystem
{ 

    public abstract class EquippableModel : LoopComponent, ILoopUpdate
    {
        [field: SerializeField] public GameObject ModelGO        { get; private set; }

        [field: Space(10)]

        [field: SerializeField] public Vector3 PositionOffset    { get; private set; }
        [field: SerializeField] public Vector3 RotationOffset    { get; private set; }
        
        [field: Space(10)]
        
        [field: SerializeField] public List<MeshRenderer> Meshes { get; private set; }

        private HashSet<IWeaponModelInit> initCallbacks = new();
        private bool isClone;

        public IEntityController Controller  { get; private set; }
        public ModelSocketHandler Sockets    { get; private set; }
        public Equippable EquippableTemplate { get; private set; }
        public ExtBool.Handler IsVisible     { get; } = ExtBool.Handler.CreateBasic(true);

        protected abstract ModelPerspective Perspective { get; }
        
        private void Awake()
        {
            ModelGO.SetActive(false);
            Sockets = GetComponent<ModelSocketHandler>();
        }

        public void LoopUpdate()
        {
            if(!isClone)
                return;
            
            // Model is active, so update the offsets.
            transform.localPosition = PositionOffset;
            transform.localRotation = Quaternion.Euler(RotationOffset);
            
            // Handling when the model is invisible such as when using abilities etc.
            ModelGO.SetActive(IsVisible.Value);
        }

        public EquippableModel Clone(IEquipmentUserController entity, Equippable template)
        {
            if(isClone) {
                Debug.LogError("Attempted to clone a EquippableModel which has already been cloned.");
                return null;
            }

            GameObject clone           = Instantiate(gameObject);
            EquippableModel cloneModel = clone.GetComponent<EquippableModel>();
            cloneModel.InitializeClone(entity, template);
            return cloneModel;
        }

        protected virtual void InitializeClone(IEquipmentUserController entity, Equippable template)
        {
            EquippableTemplate = template;
            isClone            = true;
            Controller         = entity;
            foreach(IWeaponModelInit init in GetComponentsInChildren<IWeaponModelInit>(true)) {
                init.OnInitialized(entity);
                initCallbacks.Add(init);
            }
            
            switch(Perspective) {
                case ModelPerspective.ThirdPerson: {
                    EquippableThirdPersonModel model = (EquippableThirdPersonModel)this;
                    EntityModel eModel               = entity.Model;
                    ModelSocketHandler sockets       = eModel.Sockets;
                    transform.SetParent(model.Location == ThirdPersonModelLocation.LeftHand
                        ? sockets.GetModelSpot(ModelSpotLookup.Human.LHand, false).Transform 
                        : sockets.GetModelSpot(ModelSpotLookup.Human.RHand, false).Transform
                    );
                } break;
                case ModelPerspective.Holstered: {
                    EquippableHolsteredModel model = (EquippableHolsteredModel)this;
                    EntityModel eModel             = entity.Model;
                    ModelSocketHandler sockets     = eModel.Sockets;
                    Transform holsterParent;
                    switch (model.Location) {
                        case HolsteredModelLocation.RightHip: {
                            holsterParent = sockets.GetModelSpot(ModelSpotLookup.Human.RHip, false).Transform;
                        } break;
                        case HolsteredModelLocation.LeftHip: {
                            holsterParent = sockets.GetModelSpot(ModelSpotLookup.Human.LHip, false).Transform;
                        } break;
                        case HolsteredModelLocation.Back: {
                            holsterParent = sockets.GetModelSpot(ModelSpotLookup.Human.LowerTorso, false).Transform;
                        } break;
                        default: {
                            Debug.LogError("Invalid holstered equippable parent location.");
                            return;
                        }
                    }
                    transform.SetParent(holsterParent);
                } break;

                default: {
                    Debug.LogError("Invalid equippable perspective.");
                    return;
                }
            }

            ModelGO.SetActive(true);
        }

        public void ClearVFX(ushort effectID, ModelPlugRemoveBehavior removeBehavior = ModelPlugRemoveBehavior.None)
        {
            if(!TryGetComponent(out ModelSocketHandler sockets))
                return;
            
            sockets.RemovePlug(effectID, removeBehavior);
        }
    }

    public interface IWeaponModelInit
    {
        void OnInitialized(IEquipmentUserController owner);
    }

    public enum ModelPerspective
    {
        ThirdPerson,
        Holstered
    }

    public enum ThirdPersonModelLocation
    {
        RightHand,
        LeftHand
    }

    public enum HolsteredModelLocation
    {
        RightHip,
        LeftHip,
        Back
    }

}
