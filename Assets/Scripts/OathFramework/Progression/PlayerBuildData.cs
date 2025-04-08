using OathFramework.AbilitySystem;
using UnityEngine;
using Unity.Netcode;
using OathFramework.EntitySystem.Attributes;
using OathFramework.EquipmentSystem;
using OathFramework.PerkSystem;
using System;
using Unity.Serialization.Json;

namespace OathFramework.Progression
{

    [Serializable]
    public struct PlayerBuildData : INetworkSerializable, IEquatable<PlayerBuildData>
    {
        public byte constitution;
        public byte endurance;
        public byte agility;
        public byte strength;
        public byte expertise;
        public byte intelligence;

        public string equippable1;
        public string equippable2;

        public string ability1;
        public string ability2;

        public string perk1;
        public string perk2;
        public string perk3;
        public string perk4;

        // Network IDs.
        private ushort equippable1NetID;
        private ushort equippable2NetID;
        private ushort ability1NetID;
        private ushort ability2NetID;
        private ushort perk1NetID;
        private ushort perk2NetID;
        private ushort perk3NetID;
        private ushort perk4NetID;

        // TODO: Weapon data.

        public static byte BaseAttributeLevel => 3;
        public static byte MaxAttributeLevel  => 25;
        public static byte MinAttributeLevel  => 3;
        public static byte BaseLevel          => (byte)(BaseAttributeLevel * 6);
        public static byte MaxLevel           => 80;
        public readonly byte Level            => (byte)(constitution + endurance + agility + strength + expertise + intelligence);

        public static PlayerBuildData Default => new() { 
            constitution = BaseAttributeLevel,
            endurance    = BaseAttributeLevel,
            agility      = BaseAttributeLevel, 
            strength     = BaseAttributeLevel,
            expertise    = BaseAttributeLevel, 
            intelligence = BaseAttributeLevel,
            equippable1  = "core:soviet_rifle",
            equippable2  = "core:silenced_pistol",
            ability1     = "core:heal_pool",
            ability2     = "",
            perk1        = "",
            perk2        = "",
            perk3        = "",
            perk4        = ""
        };

        public string GetEquippable(EquipmentSlot slot)
        {
            switch(slot) {
                case EquipmentSlot.Primary:
                    return equippable1;
                case EquipmentSlot.Secondary:
                    return equippable2;
                
                case EquipmentSlot.None:
                default:
                    Debug.LogError($"Invalid equippable slot '{slot}'");
                    return string.Empty;
            }
        }

        public void SetEquippable(EquipmentSlot slot, string equippable)
        {
            switch(slot) {
                case EquipmentSlot.Primary:
                    equippable1 = equippable;
                    break;
                case EquipmentSlot.Secondary:
                    equippable2 = equippable;
                    break;
                
                case EquipmentSlot.None:
                default:
                    return;
            }
        }

        public string GetAbility(byte slot)
        {
            switch(slot) {
                case 0:
                    return ability1;
                case 1:
                    return ability2;
                default:
                    return string.Empty;
            }
        }

        public bool IsAbilityEquipped(string ability)
        {
            return ability == ability1 || ability == ability2;
        }

        public bool IsAbilityEquipped(string ability, out byte index)
        {
            index = default;
            if(ability == ability1) {
                index = 0;
                return true;
            }
            if(ability == ability2) {
                index = 1;
                return true;
            }
            return false;
        }

        public void SetAbility(byte slot, string ability)
        {
            switch(slot) {
                case 0:
                    ability1 = ability;
                    break;
                case 1:
                    ability2 = ability;
                    break;
                default:
                    throw new IndexOutOfRangeException(nameof(slot));
            }
        }

        public string GetPerk(byte slot)
        {
            switch(slot) {
                case 0:
                    return perk1;
                case 1:
                    return perk2;
                case 2:
                    return perk3;
                case 3:
                    return perk4;
                default:
                    throw new IndexOutOfRangeException(nameof(slot));
            }
        }

        public void SetPerk(byte slot, string perk)
        {
            switch(slot) {
                case 0:
                    perk1 = perk;
                    break;
                case 1:
                    perk2 = perk;
                    break;
                case 2:
                    perk3 = perk;
                    break;
                case 3:
                    perk4 = perk;
                    break;
                default:
                    throw new IndexOutOfRangeException(nameof(slot));
            }
        }
        
        public bool IsPerkEquipped(string perk)
        {
            return perk == perk1 || perk == perk2 || perk == perk3 || perk == perk4;
        }

        public bool IsPerkEquipped(string perk, out byte index)
        {
            index = default;
            if(perk == perk1) {
                index = 0;
                return true;
            }
            if(perk == perk2) {
                index = 1;
                return true;
            }
            if(perk == perk3) {
                index = 2;
                return true;
            }
            if(perk == perk4) {
                index = 3;
                return true;
            }
            return false;
        }
        
        public string GetEquipment(EquipSlotType type, byte index)
        {
            switch(type) {
                case EquipSlotType.Equippable:
                    return GetEquippable((EquipmentSlot)index);
                case EquipSlotType.Ability:
                    return GetAbility(index);
                case EquipSlotType.Perk:
                    return GetPerk(index);
                
                case EquipSlotType.None:
                default:
                    return string.Empty;
            }
        }

        public void SetEquipment(EquipSlotType type, byte index, string val)
        {
            switch(type) {
                case EquipSlotType.Equippable:
                    SetEquippable((EquipmentSlot)index, val);
                    break;
                case EquipSlotType.Ability:
                    SetAbility(index, val);
                    break;
                case EquipSlotType.Perk:
                    SetPerk(index, val);
                    break;
                
                case EquipSlotType.None:
                default:
                    throw new IndexOutOfRangeException(nameof(type));
            }
        }

        public byte GetAttributeValue(AttributeTypes attribute)
        {
            switch(attribute) {
                case AttributeTypes.Constitution:
                    return constitution;
                case AttributeTypes.Endurance:
                    return endurance;
                case AttributeTypes.Agility:
                    return agility;
                case AttributeTypes.Strength:
                    return strength;
                case AttributeTypes.Expertise:
                    return expertise;
                case AttributeTypes.Intelligence:
                    return intelligence;
            }
            return 0;
        }

        public void IncrementAttribute(AttributeTypes attribute, byte val = 1)
        {
            switch(attribute) {
                case AttributeTypes.Constitution:
                    constitution += val;
                    break;
                case AttributeTypes.Endurance:
                    endurance += val;
                    break;
                case AttributeTypes.Agility:
                    agility += val;
                    break;
                case AttributeTypes.Strength:
                    strength += val;
                    break;
                case AttributeTypes.Expertise:
                    expertise += val;
                    break;
                case AttributeTypes.Intelligence:
                    intelligence += val;
                    break;
            }
        }

        public void DecrementAttribute(AttributeTypes attribute, byte val = 1)
        {
            switch(attribute) {
                case AttributeTypes.Constitution:
                    constitution -= val;
                    break;
                case AttributeTypes.Endurance:
                    endurance -= val;
                    break;
                case AttributeTypes.Agility:
                    agility -= val;
                    break;
                case AttributeTypes.Strength:
                    strength -= val;
                    break;
                case AttributeTypes.Expertise:
                    expertise -= val;
                    break;
                case AttributeTypes.Intelligence:
                    intelligence -= val;
                    break;
            }
        }

        public void SetAttribute(AttributeTypes attribute, byte val)
        {
            val = (byte)Mathf.Clamp(val, MinAttributeLevel, MaxAttributeLevel);
            switch(attribute) {
                case AttributeTypes.Constitution:
                    constitution = val;
                    break;
                case AttributeTypes.Endurance:
                    endurance = val;
                    break;
                case AttributeTypes.Agility:
                    agility = val;
                    break;
                case AttributeTypes.Strength:
                    strength = val;
                    break;
                case AttributeTypes.Expertise:
                    expertise = val;
                    break;
                case AttributeTypes.Intelligence:
                    intelligence = val;
                    break;
            }
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if(serializer.IsWriter) {
                EquippableManager.TryGetNetID(equippable1, out equippable1NetID);
                EquippableManager.TryGetNetID(equippable2, out equippable2NetID);
                AbilityManager.TryGetNetID(ability1, out ability1NetID);
                AbilityManager.TryGetNetID(ability2, out ability2NetID);
                PerkManager.TryGetNetID(perk1, out perk1NetID);
                PerkManager.TryGetNetID(perk2, out perk2NetID);
                PerkManager.TryGetNetID(perk3, out perk3NetID);
                PerkManager.TryGetNetID(perk4, out perk4NetID);
                serializer.SerializeValue(ref equippable1NetID);
                serializer.SerializeValue(ref equippable2NetID);
                serializer.SerializeValue(ref ability1NetID);
                serializer.SerializeValue(ref ability2NetID);
                serializer.SerializeValue(ref perk1NetID);
                serializer.SerializeValue(ref perk2NetID);
                serializer.SerializeValue(ref perk3NetID);
                serializer.SerializeValue(ref perk4NetID);
            } else {
                serializer.SerializeValue(ref equippable1NetID);
                serializer.SerializeValue(ref equippable2NetID);
                serializer.SerializeValue(ref ability1NetID);
                serializer.SerializeValue(ref ability2NetID);
                serializer.SerializeValue(ref perk1NetID);
                serializer.SerializeValue(ref perk2NetID);
                serializer.SerializeValue(ref perk3NetID);
                serializer.SerializeValue(ref perk4NetID);
                EquippableManager.TryGetKey(equippable1NetID, out equippable1);
                EquippableManager.TryGetKey(equippable2NetID, out equippable2);
                AbilityManager.TryGetKey(ability1NetID, out ability1);
                AbilityManager.TryGetKey(ability2NetID, out ability2);
                PerkManager.TryGetKey(perk1NetID, out perk1);
                PerkManager.TryGetKey(perk2NetID, out perk2);
                PerkManager.TryGetKey(perk3NetID, out perk3);
                PerkManager.TryGetKey(perk4NetID, out perk4);
            }
            serializer.SerializeValue(ref constitution);
            serializer.SerializeValue(ref endurance);
            serializer.SerializeValue(ref agility);
            serializer.SerializeValue(ref strength);
            serializer.SerializeValue(ref expertise);
            serializer.SerializeValue(ref intelligence);
        }

        public bool AttributesChanged(PlayerBuildData other)
        {
            return other.constitution    != constitution 
                   || other.endurance    != endurance
                   || other.agility      != agility 
                   || other.strength     != strength
                   || other.expertise    != expertise 
                   || other.intelligence != intelligence;
        }

        public bool Equals(PlayerBuildData other) 
            => other.constitution    == constitution 
               && other.endurance    == endurance
               && other.agility      == agility
               && other.strength     == strength
               && other.expertise    == expertise
               && other.intelligence == intelligence
               && other.equippable1  == equippable1
               && other.equippable2  == equippable2
               && other.ability1     == ability1
               && other.ability2     == ability2
               && other.perk1        == perk1
               && other.perk2        == perk2
               && other.perk3        == perk3
               && other.perk4        == perk4;

        public override bool Equals(object obj) 
            => obj is PlayerBuildData build && Equals(build);
        
        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(constitution);
            hash.Add(endurance);
            hash.Add(agility);
            hash.Add(strength);
            hash.Add(expertise);
            hash.Add(intelligence);
            hash.Add(equippable1);
            hash.Add(equippable2);
            hash.Add(ability1);
            hash.Add(ability2);
            hash.Add(perk1);
            hash.Add(perk2);
            hash.Add(perk3);
            hash.Add(perk4);
            hash.Add(Level);
            return hash.ToHashCode();
        }

        public class JsonAdapter : IJsonAdapter<PlayerBuildData>, IJsonMigration<PlayerBuildData>
        {
            public int Version => 1;
            
            public void Serialize(in JsonSerializationContext<PlayerBuildData> context, PlayerBuildData value)
            {
                using JsonWriter.ObjectScope objScope = context.Writer.WriteObjectScope();
                context.Writer.WriteKeyValue("constitution", value.constitution);
                context.Writer.WriteKeyValue("endurance",    value.endurance);
                context.Writer.WriteKeyValue("agility",      value.agility);
                context.Writer.WriteKeyValue("expertise",    value.expertise);
                context.Writer.WriteKeyValue("strength",     value.strength);
                context.Writer.WriteKeyValue("intelligence", value.intelligence);
                context.Writer.WriteKeyValue("equippable1",  value.equippable1);
                context.Writer.WriteKeyValue("equippable2",  value.equippable2);
                context.Writer.WriteKeyValue("ability1",     value.ability1);
                context.Writer.WriteKeyValue("ability2",     value.ability2);
                context.Writer.WriteKeyValue("perk1",        value.perk1);
                context.Writer.WriteKeyValue("perk2",        value.perk2);
                context.Writer.WriteKeyValue("perk3",        value.perk3);
                context.Writer.WriteKeyValue("perk4",        value.perk4);
            }

            public PlayerBuildData Deserialize(in JsonDeserializationContext<PlayerBuildData> context)
            {
                SerializedValueView value = context.SerializedValue;
                return new PlayerBuildData {
                    constitution = (byte)value.GetValue("constitution").AsInt32(),
                    endurance    = (byte)value.GetValue("endurance").AsInt32(),
                    agility      = (byte)value.GetValue("agility").AsInt32(),
                    strength     = (byte)value.GetValue("strength").AsInt32(),
                    expertise    = (byte)value.GetValue("expertise").AsInt32(),
                    intelligence = (byte)value.GetValue("intelligence").AsInt32(),
                    equippable1  = value.GetValue("equippable1").ToString(),
                    equippable2  = value.GetValue("equippable2").ToString(),
                    ability1     = value.GetValue("ability1").ToString(),
                    ability2     = value.GetValue("ability2").ToString(),
                    perk1        = value.GetValue("perk1").ToString(),
                    perk2        = value.GetValue("perk2").ToString(),
                    perk3        = value.GetValue("perk3").ToString(),
                    perk4        = value.GetValue("perk4").ToString()
                };
            }
            
            public PlayerBuildData Migrate(in JsonMigrationContext context)
            {
                int version              = context.SerializedVersion;
                SerializedObjectView obj = context.SerializedObject;
                PlayerBuildData data     = context.Read<PlayerBuildData>(obj);

                return data;
            }
        }
    }
    
    public enum EquipSlotType
    {
        None       = 0,
        Equippable = 1,
        Ability    = 2,
        Perk       = 3
    }
}
