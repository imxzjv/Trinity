﻿//using System;
//using System.Collections.Generic;
//using Trinity.Cache;
//using Trinity.Combat.Abilities;
//using Trinity.Config.Combat;
//using Trinity.Coroutines.Town;
//using Trinity.Framework;
//using Trinity.Helpers;
//using Trinity.Items;
//using Trinity.Technicals;
//using Zeta.Bot.Settings;
//using Zeta.Common;
//using Zeta.Game;
//using Zeta.Game.Internals.Actors;
//using Zeta.Game.Internals.Actors.Gizmos;
//using Zeta.Game.Internals.SNO;
//using Logger = Trinity.Technicals.Logger;

//namespace Trinity
//{
//    public partial class TrinityPlugin
//    {
//        /// <summary>
//        /// This will eventually be come our single source of truth and we can get rid of most/all of the below "c_" variables
//        /// </summary>
//        private static TrinityCacheObject CurrentCacheObject = new TrinityCacheObject();

//        private static double c_HitPointsPct = 0d;
//        private static double c_HitPoints = 0d;
//        private static float c_ZDiff = 0f;
//        private static string c_ItemDisplayName = "";
//        private static string c_IgnoreReason = "";
//        private static string c_IgnoreSubStep = "";
//        private static int c_ItemLevel = 0;
//        private static string c_ItemLink = String.Empty;
//        private static int c_GoldStackSize = 0;
//        private static bool c_IsOneHandedItem = false;
//        private static bool c_IsTwoHandedItem = false;
//        private static bool c_IsAncient = false;
//        private static ItemQuality c_ItemQuality = ItemQuality.Invalid;
//        private static ItemType c_DBItemType = ItemType.Unknown;
//        private static ItemBaseType c_DBItemBaseType = ItemBaseType.None;
//        private static FollowerType c_item_tFollowerType = FollowerType.None;
//        private static TrinityItemType _cItemTinityItemType = TrinityItemType.Unknown;
//        private static MonsterSize c_unit_MonsterSize = MonsterSize.Unknown;
//        private static DiaObject c_diaObject = null;
//        private static DiaGizmo c_diaGizmo = null;
//        private static SNOAnim c_CurrentAnimation = SNOAnim.Invalid;
//        private static bool c_unit_IsElite = false;
//        private static bool c_unit_IsRare = false;
//        private static bool c_unit_IsUnique = false;
//        private static bool c_unit_IsMinion = false;
//        private static bool c_unit_IsTreasureGoblin = false;
//        private static bool c_IsEliteRareUnique = false;
//        private static bool c_unit_IsAttackable = false;
//        private static bool c_unit_HasShieldAffix = false;
//        private static bool c_IsObstacle = false;
//        private static bool c_HasBeenNavigable = false;
//        private static bool c_HasBeenRaycastable = false;
//        private static bool c_HasBeenInLoS = false;
//        private static string c_ItemMd5Hash = string.Empty;
//        private static bool c_HasDotDPS = false;
//        private static MonsterAffixes c_MonsterAffixes = MonsterAffixes.None;
//        //private static bool c_IsFacingPlayer;

//        private static bool TryCacheActor(DiaObject freshObject, out TrinityCacheObject cacheObject)
//        {
//            cacheObject = new TrinityCacheObject(freshObject);

//            if (!ShouldCacheCommon(cacheObject))
//                return false;

//            switch (cacheObject.Type)
//            {
//                case TrinityObjectType.Player:
//                case TrinityObjectType.PowerGlobe:
//                case TrinityObjectType.HealthGlobe:
//                case TrinityObjectType.ProgressionGlobe:
//                    return true;

//                case TrinityObjectType.Unit:
//                    return ShouldCacheUnit(cacheObject);

//                case TrinityObjectType.Item:
//                    return ShouldCacheItem(cacheObject);

//                case TrinityObjectType.Gold:
//                    return ShouldIncludeGold(cacheObject);

//                case TrinityObjectType.ClientEffect:
//                    cacheObject.AddCacheInfo("ClientEffect");
//                    return DataDictionary.AllowedClientEffects.Contains(freshObject.ActorSnoId);

//                case TrinityObjectType.Avoidance:
//                    cacheObject.AddCacheInfo("Avoidance");
//                    return false;

//                case TrinityObjectType.Destructible:
//                case TrinityObjectType.Door:
//                case TrinityObjectType.Barricade:
//                case TrinityObjectType.Container:
//                case TrinityObjectType.Shrine:
//                case TrinityObjectType.Interactable:
//                case TrinityObjectType.HealthWell:
//                case TrinityObjectType.CursedChest:
//                case TrinityObjectType.CursedShrine:
//                    return ShouldCacheGizmo(cacheObject);

//                default:
//                    cacheObject.AddCacheInfo($"UnknownType {cacheObject.Type}");
//                    return false;
//            }
//        }

//        private static bool ShouldCacheCommon(TrinityCacheObject cacheObject)
//        {
//            if (!cacheObject.IsValid)
//            {
//                cacheObject.AddCacheInfo("Invalid");
//                return false;
//            }

//            if (cacheObject.IsIgnoreName)
//            {
//                cacheObject.AddCacheInfo("IgnoreName");
//                return false;
//            }

//            if (cacheObject.IsDead)
//            {
//                cacheObject.AddCacheInfo("Dead");
//                return false;
//            }

//            if (cacheObject.IsObstacle)
//            {
//                cacheObject.AddCacheInfo("Obstacle");
//                return false;
//            }

//            if (cacheObject.IsBlacklisted)
//            {
//                cacheObject.AddCacheInfo("Blacklisted");
//                return false;
//            }

//            //if (cacheObject.ZDiff > ZDiffLimit(cacheObject))
//            //{
//            //    cacheObject.AddCacheInfo($"ZDiff");
//            //    return false;
//            //}

//            //if (cacheObject.ZDiff > ZDiffLimit(cacheObject))
//            //{
//            //    cacheObject.AddCacheInfo($"ZDiff");
//            //    return false;
//            //}

//            if (!cacheObject.IsInLos)
//            {
//                cacheObject.AddCacheInfo($"HasntBeenInLoS");
//                return false;
//            }
//            return true;
//        }

//        private static bool ShouldCacheUnit(TrinityCacheObject cacheObject)
//        {
//            throw new NotImplementedException();
//        }

//        private static bool ShouldIncludeGold(TrinityCacheObject cacheObject)
//        {
//            if (!Settings.Loot.Pickup.PickupGold)
//            {
//                cacheObject.AddCacheInfo("GoldPickupDisabled");
//                return false;
//            }
//            if (cacheObject.GoldAmount < Settings.Loot.Pickup.MinimumGoldStack)
//            {
//                cacheObject.AddCacheInfo("NotEnoughGold");
//                return false;
//            }
//            return true;
//        }

//        private static bool ShouldCacheItem(TrinityCacheObject cacheObject)
//        {
//            if (!cacheObject.IsPickupNoClick && !TrinityItemManager.CachedIsValidTwoSlotBackpackLocation)
//            {
//                cacheObject.AddCacheInfo("BackpackFull");
//                return false;
//            }

//            if (cacheObject.IsMyDroppedItem)
//            {
//                cacheObject.AddCacheInfo("DroppedItem");
//                return false;
//            }

//            if (cacheObject.IsUntargetable)
//            {
//                cacheObject.AddCacheInfo("Untargetable");
//                return false;
//            }

//            if (cacheObject.ItemQuality <= ItemQuality.Rare4 && cacheObject.Distance > CharacterSettings.Instance.LootRadius)
//            {
//                cacheObject.AddCacheInfo($"OutOfRange Limit={CharacterSettings.Instance.LootRadius}");
//                return false;
//            }

//            if (!TrinityItemManager.ShouldPickupItem(cacheObject))
//            {
//                cacheObject.AddCacheInfo("TrinityItemManager");
//                return false;
//            }

//            return true;
//        }

//        private static bool ShouldCacheGizmo(TrinityCacheObject cacheObject)
//        {
//            if (cacheObject.IsUsed)
//            {
//                cacheObject.AddCacheInfo("UsedGizmo");
//                return false;
//            }

//            if (cacheObject.IsPlayerHeadstone && !Settings.WorldObject.AllowPlayerResurection)
//            {
//                cacheObject.AddCacheInfo("AllowResurectionSetting");
//                return false;
//            }

//            if (cacheObject.IsGizmoDisabledByScript)
//            {
//                cacheObject.AddCacheInfo("DisabledByScript");
//                return false;
//            }

//            if (!Settings.WorldObject.UseShrine)
//            {
//                cacheObject.AddCacheInfo("UseShrineSetting");
//                return false;
//            }

//            if (!DataDictionary.ForceDestructibles.Contains(cacheObject.ActorSNO) && Settings.WorldObject.DestructibleOption == DestructibleIgnoreOption.ForceIgnore)
//            {
//                cacheObject.AddCacheInfo("ForceIgnoreDestructibles");
//                return false;
//            }

//            if (TrinityTownRun.IsWantingTownRun && cacheObject.Distance > 10f)
//            {
//                cacheObject.AddCacheInfo("WantToTownRun");
//                return false;
//            }

//            if (cacheObject.IsContainer && cacheObject.RadiusDistance > Settings.WorldObject.ContainerOpenRange)
//            {
//                cacheObject.AddCacheInfo("ContainerOpenRange");
//                return false;
//            }

//            if (cacheObject.IsRareChest && !Settings.WorldObject.OpenRareChests)
//            {
//                cacheObject.AddCacheInfo("OpenRareChestsSetting");
//                return false;
//            }

//            if (cacheObject.IsChest && !Settings.WorldObject.OpenChests)
//            {
//                cacheObject.AddCacheInfo("OpenChestsSetting");
//                return false;
//            }

//            if (cacheObject.IsCorpse && !Settings.WorldObject.InspectCorpses)
//            {
//                cacheObject.AddCacheInfo("InspectCorpsesSetting");
//                return false;
//            }

//            if (cacheObject.IsGroundClicky && !Settings.WorldObject.InspectGroundClicky)
//            {
//                cacheObject.AddCacheInfo("GroundClickySetting");
//                return false;
//            }

//            if (cacheObject.IsWeaponRack && !Settings.WorldObject.InspectWeaponRacks)
//            {
//                cacheObject.AddCacheInfo("WeaponRacksSetting");
//                return false;
//            }

//            return true;
//        }


//        private static bool CacheDiaObject(DiaObject freshObject)
//        {
//            if (!freshObject.IsValid)
//            {
//                c_IgnoreReason = "InvalidRActor";
//                return false;
//            }
//            if (freshObject.CommonData == null)
//            {
//                if (!DataDictionary.AllowedClientEffects.Contains(freshObject.ActorSnoId))
//                {
//                    c_IgnoreReason = "ACDNull";
//                    return false;
//                }
//            }
//            else if (!freshObject.CommonData.IsValid)
//            {
//                c_IgnoreReason = "InvalidACD";
//                return false;
//            }

//            /*
//             *  Initialize Variables
//             */
//            bool AddToCache = true;

//            RefreshStepInit();
//            /*
//             *  Get primary reference objects and keys
//             */
//            //CurrentCacheObject.Object = c_diaObject = freshObject;
//            //CurrentCacheObject.ActorSNO = freshObject.ActorSnoId;
//            //var commonData = freshObject.CommonData;

//            if (DataDictionary.AvoidanceSNO.Contains(CurrentCacheObject.ActorSNO))
//            {
//                c_IgnoreReason = "AvoidanceSNO";
//                return false;
//            }

//            try
//            {
//                //var rActorGuid = freshObject.RActorId;
//                //CurrentCacheObject.RActorGuid = rActorGuid;
//                //var annId = 0;
//                //if (commonData != null)
//                //{
//                //    annId = commonData.AnnId;
//                //}
//                //CurrentCacheObject.AnnId = annId;
//                //CurrentCacheObject.ACDGuid = freshObject.ACDId;
//                //CurrentCacheObject.CommonData = commonData;
//                //CurrentCacheObject.InternalName = NameNumberTrimRegex.Replace(freshObject.Name, "");
//                //CurrentCacheObject.InternalNameLowerCase = CurrentCacheObject.InternalName.ToLower();                
//                //CurrentCacheObject.ActorType = freshObject.ActorType;
//                //CurrentCacheObject.ACDGuid = freshObject.ACDId;
//                //CurrentCacheObject.GizmoType = CurrentCacheObject.Object.ActorInfo.GizmoType;                
//                //CurrentCacheObject.Animation = anim;
//                //CurrentCacheObject.AnimationNameLowerCase = DataDictionary.GetAnimationNameLowerCase(anim);

//                CurrentCacheObject = new TrinityCacheObject(freshObject);

//            }
//            catch (Exception ex)
//            {
//                c_IgnoreReason = "Error reading IDs";
//                return false;
//            }

//            c_diaObject = CurrentCacheObject.Object;

//            //CurrentCacheObject.LastSeenTime = DateTime.UtcNow;
//            //CurrentCacheObject.Position = c_diaObject.Position;
//            //CurrentCacheObject.Distance = Player.Position.Distance(CurrentCacheObject.Position);

//            if (CurrentCacheObject.ActorType == ActorType.ClientEffect)
//            {
//                return true;
//            }

//            //if (CurrentCacheObject.ActorType == ActorType.Item && DropItems.DroppedItemAnnIds.Contains(freshObject.CommonData.AnnId))
//            if (CurrentCacheObject.IsMyDroppedItem)
//            {
//                c_IgnoreReason = "DroppedThisItem";
//                return false;
//            }

//            //float radius;
//            //if (!DataDictionary.CustomObjectRadius.TryGetValue(CurrentCacheObject.ActorSNO, out radius))
//            //{
//            //    try
//            //    {
//            //        radius = c_diaObject.CollisionSphere.Radius;
//            //    }
//            //    catch (Exception ex)
//            //    {
//            //        Logger.LogError(LogCategory.CacheManagement, "Error refreshing Radius: {0}", ex.Message);
//            //    }
//            //}

//            //CurrentCacheObject.CollisionRadius = c_diaObject.ActorInfo.AxialCylinder.Ax1 * 0.6f;

//            // Radius Distance
//            //CurrentCacheObject.Radius = radius;

//            // Have ActorSnoId Check for SNO based navigation obstacle hashlist
//            //c_IsObstacle = DataDictionary.NavigationObstacleIds.Contains(CurrentCacheObject.ActorSNO);

//            //// Object Type
//            //CurrentCacheObject.Type = StaticPropertyUtils.GetObjectType(
//            //    CurrentCacheObject.CommonData,
//            //    CurrentCacheObject.ActorType,
//            //    CurrentCacheObject.ActorSNO,
//            //    CurrentCacheObject.GizmoType,
//            //    CurrentCacheObject.InternalName);

//            if (CurrentCacheObject.IsUsed)
//            {
//                c_IgnoreSubStep = "Used Gizmo";
//                return false;
//            }

//            //var isGizmo = CurrentCacheObject.Object is DiaGizmo;
//            //if (isGizmo)
//            //{
//            //    var isShrine = CurrentCacheObject.Object is GizmoShrine;
//            //    if (isShrine)
//            //    {
//            //        //if (CurrentCacheObject.CommonData.GetAttribute<int>(ActorAttributeType.GizmoState) > 0)
//            //        if (CurrentCacheObject.CommonData.GizmoState > 0)
//            //        {
//            //            c_IgnoreSubStep = "GizmoState1";
//            //            return false;
//            //        }

//            //        //if (CurrentCacheObject.CommonData.GetAttribute<int>(ActorAttributeType.GizmoHasBeenOperated) > 0)
//            //        if (CurrentCacheObject.CommonData.GizmoHasBeenOperated > 0)
//            //        {
//            //            c_IgnoreSubStep = "GizmoHasBeenOperated ";
//            //            return false;
//            //        }

//            //        //if (CurrentCacheObject.CommonData.GetAttribute<int>(ActorAttributeType.GizmoOperatorACDID) > 0)
//            //        if (CurrentCacheObject.CommonData.GizmoOperatorACDId > 0)
//            //        {
//            //            c_IgnoreSubStep = "GizmoHasBeenOperated ";
//            //            return false;
//            //        }
//            //    }
//            //}

//            if (CurrentCacheObject.CommonData == null || !CurrentCacheObject.CommonData.IsValid || CurrentCacheObject.CommonData.IsDisposed)
//            {
//                c_IgnoreSubStep = "Disposed";
//                return false;
//            }

//            //CurrentCacheObject.GameBalanceID = freshObject.CommonData.GameBalanceId;
//            //if (CurrentCacheObject.GameBalanceID == -1)
//            //{
//            //    Logger.Log("Invalid GameBalanceId (-1) for {0} SNO={1} ACDId={2}", 
//            //        CurrentCacheObject.InternalName, CurrentCacheObject.ActorSnoId, CurrentCacheObject.ACDId);
//            //}

//            if (CurrentCacheObject.IsDead)
//            {
//                c_IgnoreSubStep = "Dead!";
//                return false;
//            }

//            //if (CurrentCacheObject.ActorType == ActorType.Monster)
//            //{
//            //    CurrentCacheObject.AnimationState = commonData.AnimationState;
//            //    if (CurrentCacheObject.AnimationState == AnimationState.Dead)
//            //    {
//            //        c_IgnoreSubStep = "Dead (AnimationState)";
//            //        return false;
//            //    }

//            //    var lowerAnim = CurrentCacheObject.AnimationNameLowerCase;
//            //    if (lowerAnim != null && (lowerAnim.Contains("_dead") || (lowerAnim.Contains("_death") && !lowerAnim.Contains("deathmaiden") && !lowerAnim.Contains("death_orb"))))
//            //    {
//            //        c_IgnoreSubStep = "Dead (CurrentAnimation)";
//            //        return false;
//            //    }

//            //    ////if (CurrentCacheObject.CommonData.GetAttribute<int>(ActorAttributeType.DeletedOnServer) > 0)
//            //    //if (CurrentCacheObject.CommonData.DeletedOnServer > 0)                    
//            //    //{
//            //    //    c_IgnoreSubStep = "DeletedOnServer";
//            //    //    return false;
//            //    //}
//            //}

//            if (CurrentCacheObject.IsObstacle)
//            {
//                c_IgnoreReason = "NavigationObstacle";
//                AddToCache = false;
//                return AddToCache;
//            }

//            //// Add Cell Weight for Obstacle
//            //if (c_IsObstacle)
//            //{
//            //    Vector3 pos;
//            //    if (!CacheData.Position.TryGetValue(CurrentCacheObject.RActorGuid, out pos))
//            //    {
//            //        CurrentCacheObject.Position = c_diaObject.Position;
//            //        //CacheData.Position.Add(CurrentCacheObject.RActorId, CurrentCacheObject.Position);
//            //    }
//            //    if (pos != Vector3.Zero)
//            //        CurrentCacheObject.Position = pos;

//            //    CacheData.NavigationObstacles.Add(new CacheObstacleObject()
//            //    {
//            //        ActorSNO = CurrentCacheObject.ActorSNO,
//            //        Name = CurrentCacheObject.InternalName,
//            //        Position = CurrentCacheObject.Position,
//            //        Radius = CurrentCacheObject.Radius,
//            //        ObjectType = CurrentCacheObject.Type,
//            //    });

//            //    // Forge Still being walked into, increasing radius.
//            //    // [228D379C] Type: Monster Name: a3_Battlefield_demonic_forge-661599 ActorSnoId: 174900, Distance: 9.246254
//            //    var forceIds = new HashSet<int> {174900, 185391};
//            //    var r = forceIds.Contains(CurrentCacheObject.ActorSNO) ? 25f : CurrentCacheObject.Radius;

//            //    MainGridProvider.AddCellWeightingObstacle(CurrentCacheObject.ActorSNO, r);

//            //    c_IgnoreReason = "NavigationObstacle";
//            //    AddToCache = false;
//            //    return AddToCache;
//            //}

//            if (CurrentCacheObject.IsIgnoreName)
//            {
//                c_IgnoreReason = "IgnoreName";
//                AddToCache = false;
//                return AddToCache;
//            }

//            if (!CurrentCacheObject.IsValid)
//            {
//                c_IgnoreReason = "Invalid";
//                AddToCache = false;
//                return AddToCache;
//            }

//            //using (new PerformanceLogger("RefreshDiaObject.CachedType"))
//            //{
//            //    /*
//            //     * Set Object Type
//            //     */
//            //    AddToCache = RefreshStepCachedObjectType(AddToCache);
//            //    if (!AddToCache) { c_IgnoreReason = "CachedObjectType"; return AddToCache; }
//            //}

//            var unit = CurrentCacheObject.Unit;
//            //if (unit != null)
//            //{
//            //    CurrentCacheObject.IsHostile = unit.IsHostile;
//            //}

//            //CurrentCacheObject.TeamId = CurrentCacheObject.CommonData.TeamId;

//            // Summons by the player 
//            AddToCache = RefreshStepCachedSummons(unit);
//            if (!AddToCache) { c_IgnoreReason = "CachedPlayerSummons"; return false; }

//            CurrentCacheObject.Type = CurrentCacheObject.Type;
//            if (CurrentCacheObject.Type != TrinityObjectType.Item)
//            {
//                CurrentCacheObject.ObjectHash = HashGenerator.GenerateWorldObjectHash(CurrentCacheObject.ActorSNO, CurrentCacheObject.Position, CurrentCacheObject.Type.ToString(), CurrentWorldDynamicId);
//            }

//            // Check Blacklists
//            AddToCache = RefreshStepCheckBlacklists(AddToCache);
//            if (!AddToCache) { c_IgnoreReason = "CheckBlacklists"; return AddToCache; }

//            if (CurrentCacheObject.Type == TrinityObjectType.Item)
//            {
//                if (GenericBlacklist.ContainsKey(CurrentCacheObject.ObjectHash))
//                {
//                    AddToCache = false;
//                    c_IgnoreReason = "GenericBlacklist";
//                    return AddToCache;
//                }
//            }

//            if (DataDictionary.AvoidanceSNO.Contains(CurrentCacheObject.ActorSNO))
//            {
//                // Avoidance is handled elsewhere, exclude avoidance actors from this cache so 
//                // they dont get targetted with spells. (frozen pulse monster etc)
//                AddToCache = false;
//                c_IgnoreReason = "Avoidance";
//                return AddToCache;
//            }

//            // Always Refresh ZDiff for every object
//            AddToCache = RefreshStepObjectTypeZDiff(AddToCache);
//            if (!AddToCache) { c_IgnoreReason = "ZDiff"; return AddToCache; }


//            if (CurrentCacheObject.IsObstacle)
//            {
//                var radius = DataDictionary.PathFindingObstacles[CurrentCacheObject.ActorSNO];
//                MainGridProvider.AddCellWeightingObstacle(CurrentCacheObject.ActorSNO, radius);
//            }

//            if (!ShouldAddToCacheByType(CurrentCacheObject.Type))
//            {
//                AddToCache = false;
//                c_IgnoreReason = "NotValidType";
//                return AddToCache;
//            }

//            //using (new PerformanceLogger("RefreshDiaObject.MainObjectType"))
//            //{
//            //    /* 
//            //     * Main Switch on Object Type - Refresh individual object types (Units, Items, Gizmos)
//            //     */
//            //    ShouldAddToCacheByType(ref AddToCache);
//            //    if (!AddToCache) { c_IgnoreReason = "MainObjectType"; return AddToCache; }
//            //}

//            if (CurrentCacheObject.IsBlacklisted)
//            {
//                AddToCache = false;
//                c_IgnoreReason = "Blacklisted";
//                return AddToCache;
//            }

//            //if (CurrentCacheObject.ObjectHash != String.Empty && GenericBlacklist.ContainsKey(CurrentCacheObject.ObjectHash))
//            //{
//            //    AddToCache = false;
//            //    c_IgnoreSubStep = "GenericBlacklist";
//            //    return AddToCache;
//            //}

//            // Ignore anything unknown
//            AddToCache = RefreshStepIgnoreUnknown(AddToCache);
//            if (!AddToCache) { c_IgnoreReason = "IgnoreUnknown"; return AddToCache; }

//            using (new PerformanceLogger("RefreshDiaObject.LoS"))
//            {
//                // Ignore all LoS
//                AddToCache = RefreshStepIgnoreLoS(AddToCache);
//                if (!AddToCache) { c_IgnoreReason = "IgnoreLoS"; return AddToCache; }
//            }
//            string extraData = "";



//            switch (CurrentCacheObject.Type)
//            {
//                case TrinityObjectType.Unit:
//                    {
//                        if (c_IsEliteRareUnique)
//                            extraData += " IsElite " + c_MonsterAffixes;

//                        if (c_unit_HasShieldAffix)
//                            extraData += " HasAffixShielded";

//                        if (c_HasDotDPS)
//                            extraData += " HasDotDPS";

//                        if (c_HasBeenInLoS)
//                            extraData += " HasBeenInLoS";

//                        if (CurrentCacheObject.IsUnit)
//                            extraData += " HP=" + c_HitPoints.ToString("0") + " (" + c_HitPointsPct.ToString("0.00") + ")";
//                    }
//                    break;
//                case TrinityObjectType.Avoidance:
//                    {
//                        extraData += _standingInAvoidance ? "InAoE " : "";
//                        break;
//                    }
//            }

//            CurrentCacheObject.ExtraInfo = extraData;

//            // If it's a unit, add it to the monster cache
//            AddUnitToMonsterObstacleCache();

//            c_IgnoreReason = "";

//            //CurrentCacheObject.ACDGuid = CurrentCacheObject.ACDGuid;
//            //CurrentCacheObject.ActorSNO = CurrentCacheObject.ActorSNO;
//            //CurrentCacheObject.DBItemBaseType = c_DBItemBaseType;
//            //CurrentCacheObject.DBItemType = c_DBItemType;
//            //CurrentCacheObject.Distance = CurrentCacheObject.Distance;
//            //CurrentCacheObject.DynamicID = CurrentCacheObject.AnnId;
//            //CurrentCacheObject.FollowerType = c_item_tFollowerType;
//            //CurrentCacheObject.GameBalanceID = CurrentCacheObject.GameBalanceID;
//            //CurrentCacheObject.GoldAmount = c_GoldStackSize;
//            CurrentCacheObject.HasBeenInLoS = c_HasBeenInLoS;
//            //CurrentCacheObject.HasBeenNavigable = c_HasBeenNavigable;
//            //CurrentCacheObject.HasBeenRaycastable = c_HasBeenRaycastable;
//            //CurrentCacheObject.HasDotDPS = c_HasDotDPS;
//            //CurrentCacheObject.HitPoints = c_HitPoints;
//            //CurrentCacheObject.HitPointsPct = c_HitPointsPct;
//            //CurrentCacheObject.InternalName = CurrentCacheObject.InternalName;
//            //CurrentCacheObject.IsAttackable = c_unit_IsAttackable;
//            //CurrentCacheObject.IsElite = c_unit_IsElite;
//            //CurrentCacheObject.IsElite = c_IsEliteRareUnique;
//            //CurrentCacheObject.IsMinion = c_unit_IsMinion;
//            //CurrentCacheObject.IsRare = c_unit_IsRare;
//            //CurrentCacheObject.IsTreasureGoblin = c_unit_IsTreasureGoblin;
//            //CurrentCacheObject.IsUnique = c_unit_IsUnique;
//            //CurrentCacheObject.ItemLevel = c_ItemLevel;
//            //CurrentCacheObject.ItemLink = c_ItemLink;
//            //CurrentCacheObject.ItemQuality = c_ItemQuality;
//            //CurrentCacheObject.MonsterAffixes = c_MonsterAffixes;
//            //CurrentCacheObject.MonsterSize = c_unit_MonsterSize;
//            //CurrentCacheObject.OneHanded = c_IsOneHandedItem;
//            //CurrentCacheObject.RActorGuid = CurrentCacheObject.RActorGuid;
//            //CurrentCacheObject.Radius = CurrentCacheObject.Radius;
//            //CurrentCacheObject.ItemType = _cItemTinityItemType;
//            //CurrentCacheObject.TwoHanded = c_IsTwoHandedItem;
//            //CurrentCacheObject.Type = CurrentCacheObject.Type;
//            //CurrentCacheObject.IsAncient = c_IsAncient;
//            return true;
//        }

//        private static void AddGizmoToNavigationObstacleCache()
//        {
//            switch (CurrentCacheObject.Type)
//            {
//                case TrinityObjectType.Barricade:
//                case TrinityObjectType.Container:
//                case TrinityObjectType.Destructible:
//                case TrinityObjectType.Door:
//                case TrinityObjectType.HealthWell:
//                case TrinityObjectType.Interactable:
//                case TrinityObjectType.Shrine:
//                    CacheData.NavigationObstacles.Add(new CacheObstacleObject()
//                    {
//                        ActorSNO = CurrentCacheObject.ActorSNO,
//                        Radius = CurrentCacheObject.Radius,
//                        Position = CurrentCacheObject.Position,
//                        Name = CurrentCacheObject.InternalName,
//                        ObjectType = CurrentCacheObject.Type,
//                    });
//                    break;
//            }
//        }
//        /// <summary>
//        /// Adds a unit to cache hashMonsterObstacleCache
//        /// </summary>
//        private static void AddUnitToMonsterObstacleCache()
//        {
//            if (CurrentCacheObject.Type == TrinityObjectType.Unit)
//            {
//                // Add to the collision-list
//                CacheData.MonsterObstacles.Add(new CacheObstacleObject()
//                {
//                    ActorSNO = CurrentCacheObject.ActorSNO,
//                    Name = CurrentCacheObject.InternalName,
//                    Position = CurrentCacheObject.Position,
//                    Radius = CurrentCacheObject.Radius,
//                    ObjectType = CurrentCacheObject.Type,
//                });
//            }
//        }
//        /// <summary>
//        /// Initializes variable set for single object refresh
//        /// </summary>
//        private static void RefreshStepInit()
//        {
//            CurrentCacheObject = new TrinityCacheObject();
//            // Start this object as off as unknown type
//            CurrentCacheObject.Type = TrinityObjectType.Unknown;
//            CurrentCacheObject.GizmoType = GizmoType.None;
//            CurrentCacheObject.Distance = 0f;
//            CurrentCacheObject.Radius = 0f;
//            c_ZDiff = 0f;
//            c_ItemDisplayName = "";
//            c_ItemLink = "";
//            CurrentCacheObject.InternalName = "";
//            c_IgnoreReason = "";
//            c_IgnoreSubStep = "";
//            CurrentCacheObject.ACDGuid = -1;
//            CurrentCacheObject.RActorGuid = -1;
//            //CurrentCacheObject.DynamicID = -1;
//            CurrentCacheObject.GameBalanceID = -1;
//            CurrentCacheObject.ActorSNO = -1;
//            c_ItemLevel = -1;
//            c_GoldStackSize = -1;
//            c_HitPointsPct = -1;
//            c_HitPoints = -1;
//            c_IsOneHandedItem = false;
//            c_IsTwoHandedItem = false;
//            c_unit_IsElite = false;
//            c_unit_IsRare = false;
//            c_unit_IsUnique = false;
//            c_unit_IsMinion = false;
//            c_unit_IsTreasureGoblin = false;
//            c_unit_IsAttackable = false;
//            c_unit_HasShieldAffix = false;
//            c_IsEliteRareUnique = false;
//            c_IsObstacle = false;
//            c_HasBeenNavigable = false;
//            c_HasBeenRaycastable = false;
//            c_HasBeenInLoS = false;
//            c_ItemMd5Hash = string.Empty;
//            c_ItemQuality = ItemQuality.Invalid;
//            c_DBItemBaseType = ItemBaseType.None;
//            c_DBItemType = ItemType.Unknown;
//            c_item_tFollowerType = FollowerType.None;
//            _cItemTinityItemType = TrinityItemType.Unknown;
//            c_unit_MonsterSize = MonsterSize.Unknown;
//            c_diaObject = null;
//            c_diaGizmo = null;
//            c_CurrentAnimation = SNOAnim.Invalid;
//            c_HasDotDPS = false;
//            c_MonsterAffixes = MonsterAffixes.None;
//        }

//        private static bool RefreshStepIgnoreNullCommonData(bool AddToCache)
//        {
//            // Null Common Data makes a DiaUseless!
//            if (CurrentCacheObject.Type == TrinityObjectType.Unit || CurrentCacheObject.Type == TrinityObjectType.Item || CurrentCacheObject.Type == TrinityObjectType.Gold)
//            {
//                if (c_diaObject.CommonData == null)
//                {
//                    AddToCache = false;
//                }
//                if (c_diaObject.CommonData != null && !c_diaObject.CommonData.IsValid)
//                {
//                    AddToCache = false;
//                }
//            }
//            return AddToCache;
//        }

//        private static bool RefreshStepCachedObjectType(bool AddToCache)
//        {
//            // Set the object type
//            // begin with default... 
//            CurrentCacheObject.Type = TrinityObjectType.Unknown;

//            foreach (var ignoreName in DataDictionary.ActorIgnoreNames)
//            {
//                if (CurrentCacheObject.InternalNameLowerCase.StartsWith(ignoreName))
//                {
//                    AddToCache = false;
//                    c_IgnoreSubStep = "IgnoreNames";
//                    return AddToCache;
//                }
//            }

//            // Either get the cached object type, or calculate it fresh
//            if (!c_IsObstacle)
//            {
//                // Calculate the object type of this object
//                if (c_diaObject.ActorType == ActorType.Monster)
//                //if (c_diaObject is DiaUnit)
//                {
//                    using (new PerformanceLogger("RefreshCachedType.1"))
//                    {
//                        if (c_diaObject.CommonData == null)
//                        {
//                            c_IgnoreSubStep = "InvalidUnitCommonData";
//                            AddToCache = false;
//                        }
//                        else if (c_diaObject.ACDId != c_diaObject.CommonData.ACDId)
//                        {
//                            c_IgnoreSubStep = "InvalidUnitACDGuid";
//                            AddToCache = false;
//                        }
//                        else
//                        {
//                            CurrentCacheObject.Type = TrinityObjectType.Unit;
//                        }
//                    }
//                }
//                else if (c_diaObject.ActorType == ActorType.Player)
//                {
//                    CurrentCacheObject.Type = TrinityObjectType.Player;
//                }
//                else if (DataDictionary.ForceToItemOverrideIds.Contains(CurrentCacheObject.ActorSNO) || (c_diaObject.ActorType == ActorType.Item))
//                {
//                    using (new PerformanceLogger("RefreshCachedType.2"))
//                    {
//                        CurrentCacheObject.Type = TrinityObjectType.Item;

//                        if (c_diaObject.CommonData == null)
//                        {
//                            AddToCache = false;
//                        }
//                        if (c_diaObject.CommonData != null && c_diaObject.ACDId != c_diaObject.CommonData.ACDId)
//                        {
//                            AddToCache = false;
//                        }

//                        if (CurrentCacheObject.InternalNameLowerCase.StartsWith("gold"))
//                        {
//                            CurrentCacheObject.Type = TrinityObjectType.Gold;
//                        }
//                    }
//                }
//                else if (DataDictionary.InteractWhiteListIds.Contains(CurrentCacheObject.ActorSNO))
//                {
//                    CurrentCacheObject.Type = TrinityObjectType.Interactable;
//                }
//                else if (c_diaObject is DiaGizmo && c_diaObject.ActorType == ActorType.Gizmo && CurrentCacheObject.Distance <= 90)
//                {

//                    c_diaGizmo = (DiaGizmo)c_diaObject;

//                    if (CurrentCacheObject.InternalName.Contains("CursedChest"))
//                    {
//                        CurrentCacheObject.Type = TrinityObjectType.CursedChest;
//                        return true;
//                    }

//                    if (CurrentCacheObject.InternalName.Contains("CursedShrine"))
//                    {
//                        CurrentCacheObject.Type = TrinityObjectType.CursedShrine;
//                        return true;
//                    }

//                    if (c_diaGizmo.IsBarricade)
//                    {
//                        CurrentCacheObject.Type = TrinityObjectType.Barricade;
//                    }
//                    else
//                    {
//                        switch (c_diaGizmo.ActorInfo.GizmoType)
//                        {
//                            case GizmoType.HealingWell:
//                                CurrentCacheObject.Type = TrinityObjectType.HealthWell;
//                                break;
//                            case GizmoType.Door:
//                                CurrentCacheObject.Type = TrinityObjectType.Door;
//                                break;
//                            case GizmoType.PoolOfReflection:
//                            case GizmoType.PowerUp:
//                                CurrentCacheObject.Type = TrinityObjectType.Shrine;
//                                break;
//                            case GizmoType.Chest:
//                                CurrentCacheObject.Type = TrinityObjectType.Container;
//                                break;
//                            case GizmoType.BreakableDoor:
//                                CurrentCacheObject.Type = TrinityObjectType.Barricade;
//                                break;
//                            case GizmoType.BreakableChest:
//                                CurrentCacheObject.Type = TrinityObjectType.Destructible;
//                                break;
//                            case GizmoType.DestroyableObject:
//                                CurrentCacheObject.Type = TrinityObjectType.Destructible;
//                                break;
//                            case GizmoType.PlacedLoot:
//                            case GizmoType.Switch:
//                            case GizmoType.Headstone:
//                                CurrentCacheObject.Type = TrinityObjectType.Interactable;
//                                break;
//                            default:
//                                CurrentCacheObject.Type = TrinityObjectType.Unknown;
//                                break;
//                        }
//                    }
//                }
//                else
//                {
//                    CurrentCacheObject.Type = TrinityObjectType.Unknown;
//                }

//                if (CurrentCacheObject.Type != TrinityObjectType.Unknown)
//                {  // Now cache the object type if it's on the screen and we know what it is
//                    //CacheData.ObjectType.Add(CurrentCacheObject.RActorId, CurrentCacheObject.Type);
//                }

//            }
//            return AddToCache;
//        }

//        private static bool ShouldAddToCacheByType(TrinityObjectType trinityObjectType)
//        {
//            switch (trinityObjectType)
//            {
//                case TrinityObjectType.Player:
//                    //if (!CurrentCacheObject.IsMe)
//                    //    return RefreshUnit();
//                    return true;
//                case TrinityObjectType.Unit:
//                    return RefreshUnit();
//                case TrinityObjectType.Item:
//                    {
//                        _cItemTinityItemType = TrinityItemManager.DetermineItemType(CurrentCacheObject.InternalName, c_DBItemType, c_item_tFollowerType);
//                        var isPickupNoClick = DataDictionary.NoPickupClickItemTypes.Contains(_cItemTinityItemType);
//                        if (!isPickupNoClick && !TrinityItemManager.CachedIsValidTwoSlotBackpackLocation)
//                        {
//                            c_IgnoreSubStep = "NoFreeSlots";
//                            return false;
//                        }
//                        return RefreshItem();
//                    }
//                case TrinityObjectType.Gold:
//                    return RefreshGold();
//                case TrinityObjectType.PowerGlobe:
//                case TrinityObjectType.HealthGlobe:
//                case TrinityObjectType.ProgressionGlobe:
//                    return true;
//                case TrinityObjectType.Avoidance:
//                    return false;
//                case TrinityObjectType.Destructible:
//                case TrinityObjectType.Door:
//                case TrinityObjectType.Barricade:
//                case TrinityObjectType.Container:
//                case TrinityObjectType.Shrine:
//                case TrinityObjectType.Interactable:
//                case TrinityObjectType.HealthWell:
//                case TrinityObjectType.CursedChest:
//                case TrinityObjectType.CursedShrine:
//                    return RefreshGizmo();
//                default:
//                    DebugUtil.LogUnknown(c_diaObject);
//                    c_IgnoreSubStep = "Unknown." + c_diaObject.ActorType;
//                    return false;
//            }
//        }


//        /// <summary>
//        /// Special handling for whether or not we want to cache an object that's not in LoS
//        /// </summary>
//        /// <param name="c_diaObject"></param>
//        /// <param name="AddToCache"></param>
//        /// <returns></returns>
//        private static bool RefreshStepIgnoreLoS(bool AddToCache = false)
//        {
//            try
//            {
//                // todo figure out why this method causes massive lockups if called while player is dead
//                if (TrinityPlugin.Player.IsDead)
//                    return true;

//                if (CurrentCacheObject.Type == TrinityObjectType.Item || CurrentCacheObject.Type == TrinityObjectType.Gold)
//                    return true;

//                // No need for raycasting in certain level areas (rift trial for example)
//                if (DataDictionary.NeverRaycastLevelAreaIds.Contains(Player.LevelAreaId))
//                {
//                    c_HasBeenRaycastable = true;
//                    if (!CacheData.HasBeenRayCasted.ContainsKey(CurrentCacheObject.RActorGuid))
//                        CacheData.HasBeenRayCasted.Add(CurrentCacheObject.RActorGuid, c_HasBeenRaycastable);
//                    return true;
//                }

//                if (!DataDictionary.AlwaysRaycastWorlds.Contains(Player.WorldSnoId))
//                {
//                    // Bounty Objectives should always be on the weight list
//                    if (CurrentCacheObject.IsBountyObjective)
//                        return true;

//                    // Quest Monsters should get LoS white-listed
//                    if (CurrentCacheObject.IsQuestMonster)
//                        return true;

//                    // Always LoS Units during events
//                    if (CurrentCacheObject.Type == TrinityObjectType.Unit && Player.InActiveEvent)
//                        return true;
//                }

//                // Always pickup globes
//                if (CurrentCacheObject.ItemType == TrinityItemType.ProgressionGlobe || CurrentCacheObject.Type == TrinityObjectType.ProgressionGlobe)
//                {
//                    c_IgnoreSubStep = "NoLoSProgressionGlobe";
//                    return true;
//                }

//                // Everything except items and the current target
//                if (CurrentCacheObject.RActorGuid != LastTargetRactorGUID && CurrentCacheObject.Type != TrinityObjectType.Unknown)
//                {
//                    var importantItem = CurrentCacheObject.Type == TrinityObjectType.ProgressionGlobe || CurrentCacheObject.ItemQuality == ItemQuality.Legendary;

//                    if (CurrentCacheObject.Distance < 95 && !(importantItem && CurrentCacheObject.Distance <= 200f))
//                    {
//                        using (new PerformanceLogger("RefreshLoS.2"))
//                        {
//                            // Get whether or not this RActor has ever been in a path line with AllowWalk. If it hasn't, don't add to cache and keep rechecking
//                            if (!CacheData.HasBeenRayCasted.TryGetValue(CurrentCacheObject.RActorGuid, out c_HasBeenRaycastable) || DataDictionary.AlwaysRaycastWorlds.Contains(Player.WorldSnoId))
//                            {
//                                if (CurrentCacheObject.Distance >= 1f && CurrentCacheObject.Distance <= 5f)
//                                {
//                                    c_HasBeenRaycastable = true;
//                                    if (!CacheData.HasBeenRayCasted.ContainsKey(CurrentCacheObject.RActorGuid))
//                                        CacheData.HasBeenRayCasted.Add(CurrentCacheObject.RActorGuid, c_HasBeenRaycastable);
//                                }
//                                else if (Settings.Combat.Misc.UseNavMeshTargeting)
//                                {
//                                    Vector3 myPos = new Vector3(Player.Position.X, Player.Position.Y, Player.Position.Z + 8f);
//                                    Vector3 cPos = new Vector3(CurrentCacheObject.Position.X, CurrentCacheObject.Position.Y, CurrentCacheObject.Position.Z + 8f);
//                                    cPos = MathEx.CalculatePointFrom(cPos, myPos, CurrentCacheObject.Radius + 1f);

//                                    if (Single.IsNaN(cPos.X) || Single.IsNaN(cPos.Y) || Single.IsNaN(cPos.Z))
//                                        cPos = CurrentCacheObject.Position;

//                                    var withinCollisionRadius = CurrentCacheObject.CollisionRadius + TrinityPlugin.Player.Radius <= CurrentCacheObject.Distance;
//                                    var barricadeWithinRadius = CurrentCacheObject.Type == TrinityObjectType.Barricade && withinCollisionRadius;
//                                    if (!barricadeWithinRadius && !NavHelper.CanRayCast(myPos, cPos))
//                                    {
//                                        AddToCache = false;
//                                        c_IgnoreSubStep = "UnableToRayCast";
//                                    }
//                                    else
//                                    {
//                                        c_HasBeenRaycastable = true;
//                                        if (!CacheData.HasBeenRayCasted.ContainsKey(CurrentCacheObject.RActorGuid))
//                                            CacheData.HasBeenRayCasted.Add(CurrentCacheObject.RActorGuid, c_HasBeenRaycastable);
//                                    }
//                                }
//                                else
//                                {
//                                    if (c_ZDiff > 14f)
//                                    {
//                                        AddToCache = false;
//                                        c_IgnoreSubStep = "LoS.ZDiff";
//                                    }
//                                    else
//                                    {
//                                        c_HasBeenRaycastable = true;
//                                        if (!CacheData.HasBeenRayCasted.ContainsKey(CurrentCacheObject.RActorGuid))
//                                            CacheData.HasBeenRayCasted.Add(CurrentCacheObject.RActorGuid, c_HasBeenRaycastable);
//                                    }

//                                }
//                            }
//                        }
//                        using (new PerformanceLogger("RefreshLoS.3"))
//                        {
//                            // Get whether or not this RActor has ever been in "Line of Sight" (as determined by Demonbuddy). If it hasn't, don't add to cache and keep rechecking
//                            if (!CacheData.HasBeenInLoS.TryGetValue(CurrentCacheObject.RActorGuid, out c_HasBeenInLoS) || DataDictionary.AlwaysRaycastWorlds.Contains(Player.WorldSnoId))
//                            {
//                                var alwaysLoSCheckTypes = new HashSet<TrinityObjectType>
//                                {
//                                    TrinityObjectType.ProgressionGlobe,
//                                    TrinityObjectType.Shrine,
//                                };

//                                // Ignore units not in LoS except bosses

//                                // Experiment: Grid RayCast uses AllowProjectile instead of Walk to cast accross unwalkable areas where possible.
//                                //var inLineOfSight = CombatBase.CurrentPower != null && CombatBase.CurrentPower.MinimumRange > CurrentCacheObject.Distance ? Core.Avoidance.Grid.CanRayCast(Player.Position, CurrentCacheObject.Position) : c_diaObject.InLineOfSight;
//                                var inLineOfSight = c_diaObject.InLineOfSight;

//                                if (!CurrentCacheObject.IsBoss && !inLineOfSight && !alwaysLoSCheckTypes.Contains(CurrentCacheObject.Type) && !DataDictionary.LineOfSightWhitelist.Contains(CurrentCacheObject.ActorSNO))
//                                {
//                                    AddToCache = false;
//                                    c_IgnoreSubStep = "NotInLoS";
//                                }
//                                else
//                                {
//                                    c_HasBeenInLoS = true;
//                                    if (!CacheData.HasBeenInLoS.ContainsKey(CurrentCacheObject.RActorGuid))
//                                        CacheData.HasBeenInLoS.Add(CurrentCacheObject.RActorGuid, c_HasBeenInLoS);
//                                }

//                            }
//                        }
//                    }
//                    else
//                    {
//                        AddToCache = false;
//                        c_IgnoreSubStep = "LoS-OutOfRange";
//                    }


//                    // always set true for bosses nearby
//                    if (CurrentCacheObject.IsBoss || CurrentCacheObject.IsQuestMonster || CurrentCacheObject.IsBountyObjective)
//                    {
//                        AddToCache = true;
//                        c_IgnoreSubStep = "";
//                    }
//                    // always take the current target even if not in LoS
//                    if (CurrentCacheObject.RActorGuid == LastTargetRactorGUID)
//                    {
//                        AddToCache = true;
//                        c_IgnoreSubStep = "";
//                    }
//                }

//                // Simple whitelist for LoS 
//                if (DataDictionary.LineOfSightWhitelist.Contains(CurrentCacheObject.ActorSNO))
//                {
//                    AddToCache = true;
//                    c_IgnoreSubStep = "";
//                }
//                // Always pickup Infernal Keys whether or not in LoS
//                if (DataDictionary.ForceToItemOverrideIds.Contains(CurrentCacheObject.ActorSNO))
//                {
//                    AddToCache = true;
//                    c_IgnoreSubStep = "";
//                }

//            }
//            catch (Exception ex)
//            {
//                AddToCache = true;
//                c_IgnoreSubStep = "IgnoreLoSException";
//                Logger.Log(TrinityLogLevel.Debug, LogCategory.CacheManagement, "{0}", ex);
//            }
//            return AddToCache;
//        }

//        private static bool RefreshStepIgnoreUnknown(bool AddToCache)
//        {
//            // We couldn't get a valid object type, so ignore it
//            if (!c_IsObstacle && CurrentCacheObject.Type == TrinityObjectType.Unknown)
//            {
//                AddToCache = false;
//            }
//            return AddToCache;
//        }

//        public static float GetZDiff(TrinityCacheObject obj)
//        {
//            if (ZetaDia.Me == null)
//            {
//                return 0f;
//            }
//            return Math.Abs((float)(obj.Position.Z - Player.Position.Z));
//        }




//        private static bool RefreshStepObjectTypeZDiff(bool AddToCache)
//        {
//            c_ZDiff = GetZDiff(CurrentCacheObject);
//            //c_diaObject.ZDiff; // Doesnt use cached player position

//            // always take current target regardless if ZDiff changed
//            if (CurrentCacheObject.RActorGuid == LastTargetRactorGUID)
//            {
//                AddToCache = true;
//                return AddToCache;
//            }

//            // Special whitelist for always getting stuff regardless of ZDiff or LoS
//            if (DataDictionary.LineOfSightWhitelist.Contains(CurrentCacheObject.ActorSNO))
//            {
//                AddToCache = true;
//                return AddToCache;
//            }
//            // Ignore stuff which has a Z-height-difference too great, it's probably on a different level etc. - though not avoidance!
//            if (CurrentCacheObject.Type != TrinityObjectType.Avoidance)
//            {
//                // Calculate the z-height difference between our current position, and this object's position
//                switch (CurrentCacheObject.Type)
//                {
//                    case TrinityObjectType.Door:
//                    case TrinityObjectType.Unit:
//                    case TrinityObjectType.Barricade:
//                        // Ignore monsters (units) who's Z-height is 14 foot or more than our own z-height except bosses
//                        if (c_ZDiff >= 14f && !CurrentCacheObject.IsBoss)
//                        {
//                            AddToCache = false;
//                        }
//                        break;
//                    case TrinityObjectType.Item:
//                    case TrinityObjectType.HealthWell:
//                        // Items at 26+ z-height difference (we don't want to risk missing items so much)
//                        if (c_ZDiff >= 26f)
//                        {
//                            AddToCache = false;
//                        }
//                        break;
//                    case TrinityObjectType.Gold:
//                    case TrinityObjectType.HealthGlobe:
//                    case TrinityObjectType.PowerGlobe:
//                        // Gold/Globes at 11+ z-height difference
//                        if (c_ZDiff >= 11f)
//                        {
//                            AddToCache = false;
//                        }
//                        break;
//                    case TrinityObjectType.Destructible:
//                    case TrinityObjectType.Shrine:
//                    case TrinityObjectType.Container:
//                        // Destructibles, shrines and containers are the least important, so a z-height change of only 7 is enough to ignore (help avoid stucks at stairs etc.)
//                        if (c_ZDiff >= 7f)
//                        {
//                            AddToCache = false;
//                        }
//                        break;
//                    case TrinityObjectType.Interactable:
//                        // Special interactable objects
//                        if (c_ZDiff >= 9f)
//                        {
//                            AddToCache = false;
//                        }
//                        break;
//                    case TrinityObjectType.Unknown:
//                    default:
//                        {
//                            // Don't touch it!
//                        }
//                        break;
//                }
//            }
//            else
//            {
//                AddToCache = true;
//            }
//            return AddToCache;
//        }

//        private static bool RefreshStepCheckBlacklists(bool AddToCache)
//        {
//            if (!DataDictionary.Avoidances.Contains(CurrentCacheObject.ActorSNO) && !DataDictionary.ButcherFloorPanels.Contains(CurrentCacheObject.ActorSNO) && !CurrentCacheObject.IsBountyObjective && !CurrentCacheObject.IsQuestMonster)
//            {
//                // See if it's something we should always ignore like ravens etc.
//                if (!c_IsObstacle && DataDictionary.BlackListIds.Contains(CurrentCacheObject.ActorSNO))
//                {
//                    AddToCache = false;
//                    c_IgnoreSubStep = "Blacklist";
//                    return AddToCache;
//                }
//                // Temporary ractor GUID ignoring, to prevent 2 interactions in a very short time which can cause stucks
//                if (_ignoreTargetForLoops > 0 && _ignoreRactorGuid == CurrentCacheObject.AnnId)
//                {
//                    AddToCache = false;
//                    c_IgnoreSubStep = "IgnoreRactorGUID";
//                    return AddToCache;
//                }
//                // Check our extremely short-term destructible-blacklist
//                if (_destructible3SecBlacklist.Contains(CurrentCacheObject.AnnId))
//                {
//                    AddToCache = false;
//                    c_IgnoreSubStep = "Destructible3SecBlacklist";
//                    return AddToCache;
//                }
//                // Check our extremely short-term destructible-blacklist
//                if (Blacklist3Seconds.Contains(CurrentCacheObject.AnnId))
//                {
//                    AddToCache = false;
//                    c_IgnoreSubStep = "hashRGUIDBlacklist3";
//                    return AddToCache;
//                }
//                // See if it's on our 90 second blacklist (from being stuck targeting it), as long as it's distance is not extremely close
//                if (Blacklist90Seconds.Contains(CurrentCacheObject.AnnId))
//                {
//                    AddToCache = false;
//                    c_IgnoreSubStep = "Blacklist90Seconds";
//                    return AddToCache;
//                }
//                // 60 second blacklist
//                if (Blacklist60Seconds.Contains(CurrentCacheObject.AnnId))
//                {
//                    AddToCache = false;
//                    c_IgnoreSubStep = "Blacklist60Seconds";
//                    return AddToCache;
//                }
//                // 15 second blacklist
//                if (Blacklist15Seconds.Contains(CurrentCacheObject.AnnId))
//                {
//                    AddToCache = false;
//                    c_IgnoreSubStep = "Blacklist15Seconds";
//                    return AddToCache;
//                }
//            }
//            else
//            {
//                AddToCache = true;
//            }
//            return AddToCache;
//        }



//        private static string UtilSpacedConcat(params object[] args)
//        {
//            string output = "";
//            foreach (object o in args)
//            {
//                output += o.ToString() + ", ";
//            }
//            return output;
//        }


//        private static void RefreshCachedHealth(int iLastCheckedHealth, double dThisCurrentHealth, bool bHasCachedHealth)
//        {
//            if (!bHasCachedHealth)
//            {
//                CacheData.CurrentUnitHealth.Add(CurrentCacheObject.RActorGuid, dThisCurrentHealth);
//                CacheData.LastCheckedUnitHealth.Add(CurrentCacheObject.RActorGuid, iLastCheckedHealth);
//            }
//            else
//            {
//                CacheData.CurrentUnitHealth[CurrentCacheObject.RActorGuid] = dThisCurrentHealth;
//                CacheData.LastCheckedUnitHealth[CurrentCacheObject.RActorGuid] = iLastCheckedHealth;
//            }
//        }

//        private static void CacheObjectMinimapActive()
//        {
//            try
//            {
//                //CurrentCacheObject.IsMinimapActive = c_diaObject.CommonData.GetAttribute<int>(ActorAttributeType.MinimapActive) > 0;
//                //CurrentCacheObject.IsMinimapActive = c_diaObject.CommonData.MinimapActive > 0;
//            }
//            catch
//            {
//                // Stuff it
//            }
//        }

//        private static void CacheObjectIsBountyObjective()
//        {
//            try
//            {
//                //CurrentCacheObject.IsBountyObjective = (c_diaObject.CommonData.GetAttribute<int>(ActorAttributeType.BountyObjective) != 0);
//                //CurrentCacheObject.IsBountyObjective = c_diaObject.CommonData.BountyObjective != 0;

//                if (CurrentCacheObject.IsBountyObjective)
//                    CurrentCacheObject.KillRange = CurrentCacheObject.RadiusDistance + 10f;
//            }
//            catch
//            {
//                // Stuff it
//            }
//        }



//    }
//}