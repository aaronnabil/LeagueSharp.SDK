﻿#region

using System.Collections.Generic;
using System.Linq;
using LeagueSharp.CommonEx.Core.Extensions.SharpDX;
using Vector3 = SharpDX.Vector3;

#endregion

namespace LeagueSharp.CommonEx.Core.Extensions
{
    /// <summary>
    ///     Provides helpful extensions to Units.
    /// </summary>
    public static class Unit
    {
        #region IsValid

        /// <summary>
        ///     Checks if the Unit is valid.
        /// </summary>
        /// <param name="unit">Unit (Obj_AI_Base)</param>
        /// <returns>Boolean</returns>
        public static bool IsValid(this Obj_AI_Base unit)
        {
            return unit != null && unit.IsValid;
        }

        /// <summary>
        ///     Checks if the target unit is valid.
        /// </summary>
        /// <param name="unit">Unit</param>
        /// <param name="range">Range</param>
        /// <param name="checkTeam">Checks if the target is an enemy from the Player's side</param>
        /// <param name="from">Check From</param>
        /// <returns>Boolean</returns>
        public static bool IsValidTarget(this AttackableUnit unit,
            float range = float.MaxValue,
            bool checkTeam = true,
            Vector3 from = new Vector3())
        {
            if (unit == null || !unit.IsValid || unit.IsDead || !unit.IsVisible || !unit.IsTargetable ||
                unit.IsInvulnerable)
            {
                return false;
            }

            if (checkTeam && ObjectManager.Player.Team == unit.Team)
            {
                return false;
            }

            var @base = unit as Obj_AI_Base;
            var unitPosition = @base != null ? @base.ServerPosition : unit.Position;

            return (@from.IsValid())
                ? @from.DistanceSquared(unitPosition) < range * range
                : ObjectManager.Player.ServerPosition.DistanceSquared(unitPosition) < range * range;
        }

        #endregion

        #region Information

        /// <summary>
        ///     Returns the unit's total magic damage.
        /// </summary>
        /// <param name="unit">Extended unit</param>
        /// <returns>Returns the unit's total magic damage in float units</returns>
        public static float TotalMagicalDamage(this Obj_AI_Hero unit)
        {
            return unit.BaseAbilityDamage + unit.FlatMagicDamageMod;
        }

        /// <summary>
        ///     Returns the unit's total attack damage.
        /// </summary>
        /// <param name="unit">Extended unit</param>
        /// <returns>Returns the unit's total attack damage in float units</returns>
        public static float TotalAttackDamage(this Obj_AI_Hero unit)
        {
            return unit.BaseAttackDamage + unit.FlatPhysicalDamageMod;
        }

        /// <summary>
        ///     Returns the unit's total attack range.
        /// </summary>
        /// <param name="unit">Extended unit</param>
        /// <returns>Returns the unit's total attack range in float units</returns>
        public static float TotalAttackRange(this Obj_AI_Hero unit)
        {
            return unit.AttackRange + unit.BoundingRadius;
        }

        /// <summary>
        ///     Returns if the unit is recalling.
        /// </summary>
        /// <param name="unit">Extended unit</param>
        /// <returns>Returns if the unit is recalling (boolean)</returns>
        public static bool IsRecalling(this Obj_AI_Hero unit)
        {
            return unit.Buffs.Any(buff => buff.Name.ToLower().Contains("recall"));
        }

        #endregion

        #region IsFacing

        /// <summary>
        ///     Calculates if the source is facing the target.
        /// </summary>
        /// <param name="source">Extended source</param>
        /// <param name="target">Target</param>
        /// <returns>Returns if the source is facing the target (boolean)</returns>
        public static bool IsFacing(this Obj_AI_Base source, Obj_AI_Base target)
        {
            return (source.IsValid() && target.IsValid()) &&
                   source.Direction.AngleBetween(target.Position - source.Position) < 90;
        }

        /// <summary>
        ///     Calculates if the source and the target are facing each-other.
        /// </summary>
        /// <param name="source">Extended source</param>
        /// <param name="target">Target</param>
        /// <returns>Returns if the source and target are facing each-other (boolean)</returns>
        public static bool IsBothFacing(this Obj_AI_Base source, Obj_AI_Base target)
        {
            return source.IsFacing(target) && target.IsFacing(source);
        }

        #endregion

        #region Distance

        /// <summary>
        ///     Gets the distance between two GameObjects
        /// </summary>
        /// <param name="source">Source</param>
        /// <param name="target">Target</param>
        /// <returns>The distance between the two objects</returns>
        public static float Distance(this GameObject source, GameObject target)
        {
            return source.Position.Distance(target.Position);
        }

        /// <summary>
        ///     Gets the distance squared between two GameObjects
        /// </summary>
        /// <param name="source">Source</param>
        /// <param name="target">Target</param>
        /// <returns>The squared distance between the two objects</returns>
        public static float DistanceSquared(this GameObject source, GameObject target)
        {
            return source.Position.DistanceSquared(target.Position);
        }

        /// <summary>
        ///     Gets the distance between two Obj_AI_Bases using ServerPosition
        /// </summary>
        /// <param name="source">Source</param>
        /// <param name="target">Target</param>
        /// <returns>Distance</returns>
        public static float Distance(this Obj_AI_Base source, Obj_AI_Base target)
        {
            return source.ServerPosition.Distance(target.ServerPosition);
        }

        /// <summary>
        ///     Gets the distance squared between two Obj_AI_Bases using ServerPosition
        /// </summary>
        /// <param name="source">Source</param>
        /// <param name="target">Target</param>
        /// <returns>Distance Squared</returns>
        public static float DistanceSquared(this Obj_AI_Base source, Obj_AI_Base target)
        {
            return source.ServerPosition.DistanceSquared(target.ServerPosition);
        }

        #endregion

        #region Get/Count Heroes

        /// <summary>
        ///     Counts the number of allies(according to the source) in range.
        /// </summary>
        /// <param name="source">Hero to count allies around.</param>
        /// <param name="range">Range</param>
        /// <returns>The number of allies in range</returns>
        public static int CountAlliesInRange(this Obj_AI_Hero source, float range)
        {
            return
                ObjectManager.Get<Obj_AI_Hero>()
                    .FindAll(x => x.Team == source.Team && x.Distance(source) < range)
                    .Count;
        }

        /// <summary>
        ///     Counts the number of enemies(according to the source) in range.
        /// </summary>
        /// <param name="source">Hero to count enemies around</param>
        /// <param name="range">Range</param>
        /// <returns>The number of enemies in raange</returns>
        public static int CountEnemiesInRange(this Obj_AI_Hero source, float range)
        {
            return
                ObjectManager.Get<Obj_AI_Hero>()
                    .FindAll(x => x.Team != source.Team && x.Distance(source) < range)
                    .Count;
        }

        /// <summary>
        ///     Gets all the allies(according to the source) in the range.
        /// </summary>
        /// <param name="source">Source</param>
        /// <param name="range">Range</param>
        /// <returns>List of allies</returns>
        public static List<Obj_AI_Hero> GetAlliesInRange(this Obj_AI_Hero source, float range)
        {
            return ObjectManager.Get<Obj_AI_Hero>().FindAll(x => x.Team == source.Team && x.Distance(source) < range);
        }

        /// <summary>
        ///     Gets all the enemies(according to the source) in the range
        /// </summary>
        /// <param name="source">Source</param>
        /// <param name="range">Range</param>
        /// <returns>List of enemies</returns>
        public static List<Obj_AI_Hero> GetEnemiesInRange(this Obj_AI_Hero source, float range)
        {
            return ObjectManager.Get<Obj_AI_Hero>().FindAll(x => x.Team != source.Team && x.Distance(source) < range);
        }

        #endregion
    }
}