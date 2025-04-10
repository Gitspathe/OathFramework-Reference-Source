using OathFramework.Core;
using OathFramework.Data.StatParams;
using OathFramework.EntitySystem;
using OathFramework.EntitySystem.Players;
using OathFramework.Utility;
using UnityEngine;

namespace OathFramework.EquipmentSystem
{
    public partial class EntityEquipment
    {
        public struct Executor
        {
            private EntityEquipment equipment;
            private ExtBool hideWeapon;

            private InventorySlot CurrentSlot => equipment.CurrentSlot;

            public Executor(EntityEquipment equipment)
            {
                this.equipment = equipment;
                hideWeapon     = new ExtBool(order: 2);
            }

            public void OnDie()
            {
                if(equipment.IsOwner && equipment.Model is IEntityModelThrow mThrow) {
                    ReturnTrajectory(mThrow);
                }
            }

            public void ProcessEquippable()
            {
                UpdateRecoil();
                switch(CurrentSlot.EquippableType) {
                    case EquippableTypes.Grenade:
                        if(equipment.Model is IEntityModelThrow mThrow) {
                            ProcessAimTrajectory(mThrow);
                        }
                        break;

                    case EquippableTypes.None:
                    case EquippableTypes.Ranged:
                    case EquippableTypes.RangedMultiShot:
                    default:
                        return;
                }
            }

            public void UseEquippable()
            {
                switch(CurrentSlot.EquippableType) {
                    case EquippableTypes.Ranged:
                        UseProjectile();
                        break;
                    case EquippableTypes.RangedMultiShot:
                        UseProjectileMultiShot();
                        break;
                    case EquippableTypes.Grenade: {
                        UseThrow();
                        break;
                    }

                    case EquippableTypes.None:
                    default:
                        if(Game.ExtendedDebug) {
                            Debug.LogWarning("Invalid projectile type.");
                        }
                        return;
                }
            }

            public void UseEquippableLate()
            {
                switch(CurrentSlot.EquippableType) {
                    case EquippableTypes.Grenade:
                        if(equipment.IsOwner) {
                            CreateThrowable();
                        }
                        break;

                    case EquippableTypes.None:
                    case EquippableTypes.Ranged:
                    case EquippableTypes.RangedMultiShot:
                    default:
                        return;
                }
            }
            
            private void ApplyUseCooldown(float amt)
            {
                equipment.UseCooldown = amt;
            }
            
            private void AddRecoil(float pauseTime, float amt, float max)
            {
                equipment.CurRecoil      = Mathf.Clamp(equipment.CurRecoil + amt, 0.0f, max);
                equipment.CurRecoilDelay = pauseTime;
            }

            private void UpdateRecoil()
            {
                if(equipment.IsDead)
                    return;

                if(CurrentSlot.IsEmpty || !CurrentSlot.HasAccuracy) {
                    equipment.CurRecoil      = 0.0f;
                    equipment.CurRecoilDelay = 0.0f;
                    return;
                }

                IStatsAccuracy iAcc       = CurrentSlot.GetStatsInterface<IStatsAccuracy>();
                equipment.CurRecoilDelay -= Time.deltaTime;
                if(equipment.CurRecoilDelay <= 0.0f) {
                    equipment.CurRecoil = Mathf.Clamp(equipment.CurRecoil - (iAcc.RecoilLoss * Time.deltaTime), 0.0f, iAcc.MaxRecoil);
                }
            }
            
            private void DecrementAmmo()
            {
                if(!equipment.IsOwner || !CurrentSlot.HasAmmoCount)
                    return;
            
                int ammo = CurrentSlot.Ammo - 1;
                if(ammo <= 0 && CurrentSlot.HideOnEmpty) {
                    equipment.SwapToPrevious();
                    return;
                }
                equipment.GetNetInventorySlot(CurrentSlot.SlotID).Value = new NetInventorySlot(
                    CurrentSlot.SlotID,
                    CurrentSlot.Equippable.ID,
                    (ushort)Mathf.Clamp((ushort)ammo, 0, ushort.MaxValue)
                );
            }
            
            private void ReloadIfNeeded()
            {
                if(CurrentSlot.Ammo == 0 && equipment.IsOwner) {
                    IStatsReload reload = CurrentSlot.GetStatsInterface<IStatsReload>();
                    if(reload != null && reload.ReloadImmediately) {
                        equipment.BeginReload();
                    }
                }
            }

            private void UseProjectile()
            {
                if(ReferenceEquals(equipment.Controller.AimTarget, null) || ReferenceEquals(equipment.ThirdPersonEquippableModel, null))
                    return;

                if(equipment.IsOwner) {
                    IStatsProjectile iProj = CurrentSlot.GetStatsInterface<IStatsProjectile>();
                    GetAimParams(out Vector3 position, out Quaternion rotation);
                    equipment.Projectiles.CreateProjectile(
                        iProj.Projectile.ID,
                        position,
                        rotation, 
                        CurrentSlot.EquippableNetID
                    );
                }
                ApplyCooldownAndRecoil();
                DecrementAmmo();
                ReloadIfNeeded();
            }

            private void UseProjectileMultiShot()
            {
                if(ReferenceEquals(equipment.Controller.AimTarget, null) || ReferenceEquals(equipment.ThirdPersonEquippableModel, null))
                    return;

                if(equipment.IsOwner) {
                    IStatsProjectile iProj   = CurrentSlot.GetStatsInterface<IStatsProjectile>();
                    IStatsAccuracy iAccuracy = CurrentSlot.GetStatsInterface<IStatsAccuracy>();
                    GetAimParams(out Vector3 position, out Quaternion rotation);
                    int pellets = iProj.Pellets;
                    for(int i = 0; i < pellets; i++) {
                        Vector2 spread    = iAccuracy != null ? EquippableManager.RollPelletSpread(iAccuracy.PelletSpread) : Vector2.zero;
                        Quaternion oldRot = rotation;
                        if(spread != Vector2.zero) {
                            rotation *= Quaternion.Euler(spread.y, spread.x, 0.0f);
                        }
                        equipment.Projectiles.CreateProjectile(
                            iProj.Projectile.ID, 
                            position, 
                            rotation, 
                            CurrentSlot.EquippableNetID
                        );
                        rotation = oldRot;
                    }
                }
                ApplyCooldownAndRecoil();
                DecrementAmmo();
                ReloadIfNeeded();
            }

            private void GetAimParams(out Vector3 position, out Quaternion rotation)
            {
                IStatsAccuracy iAcc                 = CurrentSlot.GetStatsInterface<IStatsAccuracy>();
                IEquipmentUserController controller = equipment.Controller;
                position = equipment.ThirdPersonEquippableModel.ProjectileSpawnPoint.position;
                Vector3 aimPos = controller.AimTarget.position;
                if(iAcc == null) {
                    // No accuracy stats interface. Always 100% accurate.
                    aimPos   = new Vector3(aimPos.x, equipment.ProjectileSpawnY, aimPos.z);
                    position = new Vector3(position.x, equipment.ProjectileSpawnY, position.z);
                    rotation = Quaternion.LookRotation(aimPos - position, Vector3.up);
                    return;
                }

                float accuracy = iAcc.Accuracy;

                // Movement penalty.
                if(iAcc.MoveAccuracyPenalty > 0.001f && iAcc.MoveAccuracyPenaltyTime > 0.001f) {
                    float malus     = Mathf.Clamp(controller.TimeSinceMoving, 0.001f, iAcc.MoveAccuracyPenaltyTime) / iAcc.MoveAccuracyPenaltyTime;
                    float magnitude = iAcc.MoveAccuracyPenalty * (1.0f - malus);
                    accuracy       -= magnitude;
                }

                // Recoil & accuracy.
                float baseYVariance = EquippableManager.RollBaseYVariance();
                float inaccAngle    = EquippableManager.RollInaccuracyAngle(accuracy);
                float inaccHeight   = EquippableManager.RollInaccuracyHeight(accuracy);
                float yVarRand      = inaccHeight + baseYVariance + EquippableManager.RollRecoilHeight(equipment.CurRecoil);
                float varAngle      = inaccAngle + EquippableManager.RollRecoilAngle(equipment.CurRecoil);
                varAngle           /= equipment.CurEntityStats.GetParam(AccuracyMult.Instance);
                yVarRand           /= equipment.CurEntityStats.GetParam(AccuracyMult.Instance);

                // Y correction.
                aimPos   = new Vector3(aimPos.x, equipment.ProjectileSpawnY + yVarRand, aimPos.z);
                position = new Vector3(position.x, equipment.ProjectileSpawnY, position.z);

                // Calculate final angle.
                Vector3 direction = aimPos - position;
                rotation =  Quaternion.LookRotation(direction, Vector3.up);
                rotation *= Quaternion.Euler(0.0f, varAngle, 0.0f);
            }

            private void ApplyCooldownAndRecoil()
            {
                IEquippableStats stats = CurrentSlot.GetStatsInterface<IEquippableStats>();
                IStatsAccuracy iAcc    = CurrentSlot.GetStatsInterface<IStatsAccuracy>();
                float rateMult         = equipment.Controller.Entity.CurStats.GetParam(AttackSpeedMult.Instance);
                ApplyUseCooldown(stats.GetTimeBetweenUses(rateMult));
                if(iAcc == null)
                    return;

                AddRecoil(iAcc.RecoilTime, iAcc.Recoil, iAcc.MaxRecoil);
            }

            private void UseThrow()
            {
                IEntityModelThrow mThrow = equipment.Model as IEntityModelThrow;
                if(mThrow == null) {
                    Debug.LogError($"{equipment.Model} does not implement {nameof(IEntityModelThrow)}");
                    return;
                }
                
                mThrow.PlayThrow();
                ReturnTrajectory(equipment.Model);
                ApplyCooldownAndRecoil();
            }

            private void CreateThrowable()
            {
                IStatsProjectile iProj   = CurrentSlot.GetStatsInterface<IStatsProjectile>();
                IEntityModelThrow mThrow = equipment.Model as IEntityModelThrow;
                Transform mTransform     = mThrow.ThrowOffsetTransform;
                equipment.Projectiles.CreateProjectile(
                    iProj.Projectile.ID,
                    mTransform.position,
                    mTransform.rotation, 
                    CurrentSlot.EquippableNetID
                );
                DecrementAmmo();
            }

            private void ProcessAimTrajectory(IEntityModelThrow parent)
            {
                PlayerController pc = equipment.Controller as PlayerController;
                bool hideAsNonOwner = !equipment.IsOwner && (ReferenceEquals(pc, null) || pc.Mode != PlayerControllerMode.Spectating);
                if(hideAsNonOwner || equipment.UseBlocked) {
                    equipment.ThirdPersonEquippableModel.IsVisible.Add(hideWeapon);
                    ReturnTrajectory(parent);
                    return;
                }
                
                if(parent.TrajectoryArc == null) {
                    equipment.ThirdPersonEquippableModel.IsVisible.Remove(hideWeapon);
                    parent.TrajectoryArc = EquippableManager.RetrieveTrajectoryArc(parent.ThrowOffsetTransform).Initialize(parent);
                }
            }

            private void ReturnTrajectory(IEntityModelThrow parent)
            {
                if(parent.TrajectoryArc == null)
                    return;
                
                EquippableManager.ReturnTrajectoryArc(parent.TrajectoryArc);
            }
        }
    }
}
