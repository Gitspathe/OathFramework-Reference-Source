using UnityEngine;

namespace OathFramework.EntitySystem.Actions
{
    [CreateAssetMenu(fileName = "Action Anim Params", menuName = "ScriptableObjects/Action Anim Params", order = 1)]
    public class ActionAnimParams : ScriptableObject
    {
        [field: SerializeField] public ushort DefaultID          { get; private set; }
        [field: SerializeField] public string LookupKey          { get; private set; }
        
        [field: Header("Mechanical")]

        [field: SerializeField] public float BaseDuration        { get; private set; } = 1.5f;
        [field: SerializeField] public float UncontrollableRatio { get; private set; } = 0.9f;
        [field: SerializeField] public AnimType AnimType         { get; private set; } = AnimType.None;
        [field: SerializeField] public bool ApplyModifiers       { get; private set; } = true;
        
        [field: Space(10)]
        
        [field: SerializeField] public bool DoMovementSpeedDampen    { get; private set; }
        [field: SerializeField] public AnimationCurve MovementDampen { get; private set; }
        
        [field: Header("Animation")]
        
        [field: SerializeField] public bool HideWeapon           { get; private set; } = true;
        [field: SerializeField] public int AnimIndex             { get; private set; }
        [field: SerializeField] public float AnimDuration        { get; private set; } = 1.0f;
        [field: SerializeField] public float AimRatio            { get; private set; } = 0.9f;
        
        [field: SerializeField] public AnimationCurve AimIKCurve { get; private set; }
        
        [field: Space(5)]
        [field: SerializeField] public bool DoAimSpeedDampen         { get; private set; }
        [field: SerializeField] public AnimationCurve AimSpeedDampen { get; private set; }
        [field: SerializeField] public float ExtraAimSmoothenTime    { get; private set; }
        
        [field: Space(5)]
        
        [field: SerializeField] public bool LerpUpperAnimLayer            { get; private set; }
        [field: SerializeField] public float AnimUpperLayerRatio          { get; private set; } = 0.9f;
        [field: SerializeField] public AnimationCurve AnimUpperLayerCurve { get; private set; }
        
        [field: Space(5)]
        
        [field: SerializeField] public bool LerpLowerAnimLayer            { get; private set; }
        [field: SerializeField] public float AnimLowerLayerRatio          { get; private set; } = 0.9f;
        [field: SerializeField] public AnimationCurve AnimLowerLayerCurve { get; private set; }
        
        public ushort ID { get; set; }
    }
    
    public enum AnimType { None, Item, Ability }
}
