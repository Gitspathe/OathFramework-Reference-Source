using OathFramework.AbilitySystem;
using Cysharp.Threading.Tasks;
using OathFramework.Core;
using OathFramework.EquipmentSystem;
using OathFramework.PerkSystem;
using OathFramework.UI;
using OathFramework.UI.Builds;
using System;
using System.Collections.Generic;
using Unity.Serialization.Json;
using EquipmentSlot = OathFramework.EquipmentSystem.EquipmentSlot;

namespace OathFramework.Progression
{

    [Serializable]
    public class PlayerProfile
    {
        public byte level;
        public uint exp;
        public uint points;
        public byte loadoutIndex;
        public List<string> unlockedEquippables = new();
        public List<string> unlockedAbilities   = new();
        public List<string> unlockedPerks       = new();
        public List<PlayerBuildData> loadouts   = new();

        // Prototype
        public bool showLoadoutPopup;
        
        public uint ExpNeeded                 => ProgressionManager.GetExpNeeded(level);
        public float ExpProgress              => (float)exp / ExpNeeded;
        public PlayerBuildData CurrentLoadout => loadouts[loadoutIndex];

        public static PlayerProfile Default => new() {
            level               = PlayerBuildData.BaseLevel,
            exp                 = 0,
            points              = 0,
            showLoadoutPopup    = true,
            unlockedEquippables = new List<string> {
#if DEBUG
                "core:debug_gun",
                "core:debug_grenade",
#endif
                "core:soviet_rifle",
                "core:mark5",
                "core:mas75",
                "core:styer_agg",
                "core:gauss_rifle",
                "core:silenced_pistol",
                "core:jw_model_19",
                "core:opus",
                "core:sawed-off",
                "core:smg",
                "core:shotgun",
                "core:shotgun2",
                "core:shotgun3",
                "core:uzi"
            },
            unlockedAbilities = new List<string> {
                "core:heal_pool",
                "core:earthquake",
                "core:shield",
                "core:grenade",
                "core:flashbang",
                "core:gun_buff1"
            },
            unlockedPerks = new List<string> {
                "core:perk1",
                "core:perk2",
                "core:perk3",
                "core:perk4",
                "core:perk5",
                "core:perk6",
                "core:perk7",
                "core:perk8",
                "core:perk9",
                "core:perk10",
                "core:perk11",
                "core:perk12",
                "core:perk13",
                "core:perk14",
                "core:perk15",
                "core:perk16",
                "core:perk17",
                "core:perk18",
                "core:perk19",
                "core:perk20",
                "core:perk21",
                "core:perk22",
                "core:perk23",
                "core:perk24",
                "core:perk25",
                "core:perk26",
                "core:perk27",
                "core:perk28",
                "core:perk29",
                "core:perk30"
            },
            loadouts = new List<PlayerBuildData> { PlayerBuildData.Default }
        };

        public void GetUnlockedEquippables(EquipmentSlot slotType, List<string> collection)
        {
            foreach(string equippable in unlockedEquippables) {
                Equippable equip = EquippableManager.GetTemplate(equippable);
                if(equip == null || equip.Temporary)
                    continue;
                
                if(equip.Slot == slotType) {
                    collection.Add(equippable);
                }
            }
        }

        public void GetUnlockedAbilities(List<string> collection)
        {
            foreach(string ability in unlockedAbilities) {
                Ability a = AbilityManager.Get(ability);
                if(a == null)
                    continue;
                
                collection.Add(ability);
            }
        }

        public void GetUnlockedPerks(List<string> collection)
        {
            foreach(string perk in unlockedPerks) {
                Perk p = PerkManager.Get(perk);
                if(p == null)
                    continue;
                
                collection.Add(perk);
            }
        }
        
        public void AddExp(uint add)
        {
            if(level == PlayerBuildData.MaxLevel)
                return;

            bool levelGained = false;
            exp += add;
            while(true) {
                if(level == PlayerBuildData.MaxLevel || ExpProgress < 1.0f)
                    break;
                if(ExpProgress < 1.0f)
                    continue;

                exp -= ExpNeeded;
                level++;
                levelGained = true;
            }
            if(!levelGained)
                return;

            BuildMenuScript.Instance.Character.Tick();
            HUDScript.ExpPopup.Show();
            _ = Save();
        }

        public void UpdateLoadout(int loadoutIndex, PlayerBuildData loadout)
        {
            loadouts[loadoutIndex] = loadout;
            _ = Save();
        }

        // TODO: Multiple profiles, rather than just saving to 'profile.sav'.
        public async UniTask Initialize()
        {
            if(!FileIO.FileExists($"{FileIO.SavePath}profile.sav") && !FileIO.FileExists($"{FileIO.SavePath}profile.bak")) {
                await Save();
                return;
            }
            LoadResult result = await Load();
            switch(result) {
                case LoadResult.Success:
                    break;
                case LoadResult.Backup:
                    ModalUIScript.ShowGeneric(
                        title: ProgressionManager.Instance.PartialLoadFailureTitleStr, 
                        text: ProgressionManager.Instance.PartialLoadFailureMsgStr,
                        priority: ModalPriority.Critical
                    ); break;
                case LoadResult.Fail:
                    ModalUIScript.ShowGeneric(
                        title: ProgressionManager.Instance.FatalLoadFailureMsgStr,
                        text: ProgressionManager.Instance.PartialLoadFailureMsgStr, 
                        priority: ModalPriority.Critical
                    ); break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            HUDScript.ExpPopup.Setup(level, exp);
        }

        public async UniTask Save()
        {
            string json = JsonSerialization.ToJson(this);
            UniTask t1  = FileIO.SaveFile($"{FileIO.SavePath}profile.sav", json, noHeader: true);
            UniTask t2  = FileIO.SaveFile($"{FileIO.SavePath}profile.bak", json, noHeader: true);
            await UniTask.WhenAll(t1, t2);
        }

        public async UniTask<LoadResult> Load()
        {
            string data;
            bool backup;
            PlayerProfile profileCopy;
                
            try {
                // Attempt to load normally.
                data        = await FileIO.LoadFile($"{FileIO.SavePath}profile.sav", noHeader: true);
                profileCopy = JsonSerialization.FromJson<PlayerProfile>(data);
                backup      = false;
            } catch(Exception) {
                // Failed! Attempt to load backup.
                try {
                    data        = await FileIO.LoadFile($"{FileIO.SavePath}profile.bak", noHeader: true);
                    profileCopy = JsonSerialization.FromJson<PlayerProfile>(data);
                } catch(Exception) { return LoadResult.Fail; } // Complete fail.

                await FileIO.SaveFile($"{FileIO.SavePath}profile.sav", data, noHeader: true);
                backup = true;
            }

            level               = profileCopy.level;
            exp                 = profileCopy.exp;
            points              = profileCopy.points;
            loadoutIndex        = profileCopy.loadoutIndex;
            unlockedEquippables = profileCopy.unlockedEquippables;
            unlockedAbilities   = profileCopy.unlockedAbilities;
            loadouts            = profileCopy.loadouts;
            showLoadoutPopup    = profileCopy.showLoadoutPopup;
            return backup ? LoadResult.Backup : LoadResult.Success;
        }
        
        public class JsonAdapter : IJsonAdapter<PlayerProfile>, IJsonMigration<PlayerProfile>
        {
            public int Version => 1;
            
            public void Serialize(in JsonSerializationContext<PlayerProfile> context, PlayerProfile value)
            {
                using JsonWriter.ObjectScope objScope = context.Writer.WriteObjectScope();
                context.Writer.WriteKeyValue("level", value.level);
                context.Writer.WriteKeyValue("exp", value.exp);
                context.Writer.WriteKeyValue("points", value.points);
                context.Writer.WriteKeyValue("current_loadout", value.loadoutIndex);
                
                context.Writer.WriteKey("unlocked_equippables");
                using(context.Writer.WriteArrayScope()) {
                    foreach(string val in value.unlockedEquippables) {
                        context.Writer.WriteValue(val);
                    }
                }
                
                context.Writer.WriteKey("unlocked_abilities");
                using(context.Writer.WriteArrayScope()) {
                    foreach(string val in value.unlockedAbilities) {
                        context.Writer.WriteValue(val);
                    }
                }
                
                context.Writer.WriteKey("unlocked_perks");
                using(context.Writer.WriteArrayScope()) {
                    foreach(string val in value.unlockedPerks) {
                        context.Writer.WriteValue(val);
                    }
                }
                
                context.Writer.WriteKey("loadouts");
                using(context.Writer.WriteArrayScope()) {
                    foreach(PlayerBuildData loadout in value.loadouts) {
                        context.SerializeValue(loadout);
                    }
                }
                
                context.Writer.WriteKeyValue("show_loadout_popup", value.showLoadoutPopup);
            }

            public PlayerProfile Deserialize(in JsonDeserializationContext<PlayerProfile> context)
            {
                PlayerProfile profile = new() {
                    level        = (byte)context.SerializedValue.GetValue("level").AsInt32(), 
                    exp          = (uint)context.SerializedValue.GetValue("exp").AsUInt64(), 
                    points       = (uint)context.SerializedValue.GetValue("points").AsInt32(),
                    loadoutIndex = (byte)context.SerializedValue.GetValue("current_loadout").AsInt32()
                };
                List<string> unlockedEquippables = new();
                List<string> unlockedAbilities   = new();
                List<string> unlockedPerks       = new();
                List<PlayerBuildData> loadouts   = new();
                
                SerializedArrayView arr = context.SerializedValue.GetValue("unlocked_equippables").AsArrayView();
                foreach(SerializedValueView i in arr) {
                    unlockedEquippables.Add(i.ToString());
                }
                
                arr = context.SerializedValue.GetValue("unlocked_abilities").AsArrayView();
                foreach(SerializedValueView i in arr) {
                    unlockedAbilities.Add(i.ToString());
                }
                
                arr = context.SerializedValue.GetValue("unlocked_perks").AsArrayView();
                foreach(SerializedValueView i in arr) {
                    unlockedPerks.Add(i.ToString());
                }
                
                arr = context.SerializedValue.GetValue("loadouts").AsArrayView();
                foreach(SerializedValueView i in arr) {
                    loadouts.Add(context.DeserializeValue<PlayerBuildData>(i));
                }

                profile.unlockedEquippables = unlockedEquippables;
                profile.unlockedAbilities   = unlockedAbilities;
                profile.unlockedPerks       = unlockedPerks;
                profile.loadouts            = loadouts;
                profile.showLoadoutPopup    = true;
                if(context.SerializedValue.TryGetValue("show_loadout_popup", out SerializedValueView val)) {
                    profile.showLoadoutPopup = val.AsBoolean();
                }
                return profile;
            }

            public PlayerProfile Migrate(in JsonMigrationContext context)
            {
                int version              = context.SerializedVersion;
                SerializedObjectView obj = context.SerializedObject;
                PlayerProfile profile    = context.Read<PlayerProfile>(obj);
                
                return profile;
            }
        }
    }

}
