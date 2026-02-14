using Godot;
using System;
using System.Collections.Generic;
using Entities;
using Logs;

namespace Components
{
    /// <summary>
    /// 伤害计算组件
    /// 负责处理技能伤害的核心计算逻辑，包括基础伤害、暴击、属性相克等
    /// </summary>
    public partial class DamageCalculationComponent : LogicComponent
    {
        #region 字段和属性

        /// <summary>
        /// 伤害计算模式
        /// </summary>
        public DamageCalculationMode CalculationMode { get; set; } = DamageCalculationMode.Standard;

        /// <summary>
        /// 基础暴击率
        /// </summary>
        public float BaseCriticalChance { get; set; } = 0.05f;

        /// <summary>
        /// 基础暴击倍数
        /// </summary>
        public float BaseCriticalMultiplier { get; set; } = 2.0f;

        /// <summary>
        /// 伤害浮动范围（0.0-1.0）
        /// </summary>
        public float DamageVariance { get; set; } = 0.1f;

        /// <summary>
        /// 是否启用属性相克
        /// </summary>
        public bool EnableElementalAdvantage { get; set; } = true;

        /// <summary>
        /// 相克倍数
        /// </summary>
        public float AdvantageMultiplier { get; set; } = 1.5f;

        /// <summary>
        /// 相克减免倍数
        /// </summary>
        public float DisadvantageMultiplier { get; set; } = 0.5f;

        #endregion

        #region 私有字段

        private Random _random = new Random();

        #endregion

        #region 事件

        /// <summary>
        /// 伤害计算完成事件
        /// </summary>
        public event Action<DamageCalculationResult> OnDamageCalculated;

        /// <summary>
        /// 暴击发生事件
        /// </summary>
        public event Action<CriticalHitEventArgs> OnCriticalHit;

        /// <summary>
        /// 伤害免疫事件
        /// </summary>
        public event Action<DamageImmunityEventArgs> OnDamageImmune;

        #endregion

        #region 生命周期

        public override void Initialize(CharacterEntity entity)
        {
            base.Initialize(entity);
            Logger2.Info("DamageCalculationComponent: 伤害计算组件初始化完成");
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 计算技能伤害
        /// </summary>
        public DamageCalculationResult CalculateSkillDamage(SkillDamageInfo damageInfo)
        {
            var result = new DamageCalculationResult();
            result.SkillId = damageInfo.SkillId;
            result.Source = damageInfo.Source;
            result.Target = damageInfo.Target;
            result.BaseDamage = damageInfo.BaseDamage;
            result.DamageType = damageInfo.DamageType;

            // 1. 基础伤害计算
            float finalDamage = damageInfo.BaseDamage;

            // 2. 应用伤害浮动
            if (DamageVariance > 0)
            {
                float variance = (float)(_random.NextDouble() * DamageVariance * 2 - DamageVariance);
                finalDamage *= (1 + variance);
                result.DamageVariance = variance;
            }

            // 3. 应用攻击方属性加成
            if (damageInfo.AttackerStats != null)
            {
                finalDamage = ApplyAttackerBonuses(finalDamage, damageInfo, result);
            }

            // 4. 应用防御方减免
            if (damageInfo.DefenderStats != null)
            {
                finalDamage = ApplyDefenderReductions(finalDamage, damageInfo, result);
            }

            // 5. 应用暴击计算
            bool isCritical = RollCriticalHit(damageInfo);
            if (isCritical)
            {
                float critMultiplier = GetCriticalMultiplier(damageInfo);
                finalDamage *= critMultiplier;
                result.IsCritical = true;
                result.CriticalMultiplier = critMultiplier;

                OnCriticalHit?.Invoke(new CriticalHitEventArgs
                {
                    DamageInfo = damageInfo,
                    CriticalMultiplier = critMultiplier,
                    FinalDamage = finalDamage
                });
            }

            // 6. 应用属性相克
            if (EnableElementalAdvantage && damageInfo.AttackerElement != Element.None && damageInfo.DefenderElement != Element.None)
            {
                float advantageMultiplier = CalculateElementalAdvantage(damageInfo.AttackerElement, damageInfo.DefenderElement);
                finalDamage *= advantageMultiplier;
                result.ElementalMultiplier = advantageMultiplier;
                result.AttackerElement = damageInfo.AttackerElement;
                result.DefenderElement = damageInfo.DefenderElement;
            }

            // 7. 应用最终限制
            finalDamage = Mathf.Max(0, finalDamage);
            result.FinalDamage = finalDamage;

            // 8. 触发计算完成事件
            OnDamageCalculated?.Invoke(result);

            Logger2.Debug($"DamageCalculationComponent: 伤害计算完成 - 技能:{damageInfo.SkillId}, 基础:{damageInfo.BaseDamage:F1}, 最终:{finalDamage:F1}, 暴击:{isCritical}");
            return result;
        }

        /// <summary>
        /// 检查是否免疫伤害
        /// </summary>
        public bool CheckDamageImmunity(DamageImmunityCheckInfo immunityInfo)
        {
            // 检查无敌状态
            if (immunityInfo.TargetHasInvincibility)
            {
                OnDamageImmune?.Invoke(new DamageImmunityEventArgs
                {
                    Reason = ImmunityReason.Invincibility,
                    Target = immunityInfo.Target
                });
                return true;
            }

            // 检查伤害类型免疫
            if (immunityInfo.ImmuneDamageTypes.Contains(immunityInfo.DamageType))
            {
                OnDamageImmune?.Invoke(new DamageImmunityEventArgs
                {
                    Reason = ImmunityReason.DamageType,
                    Target = immunityInfo.Target,
                    DamageType = immunityInfo.DamageType
                });
                return true;
            }

            // 检查元素免疫
            if (immunityInfo.ImmuneElements.Contains(immunityInfo.AttackerElement))
            {
                OnDamageImmune?.Invoke(new DamageImmunityEventArgs
                {
                    Reason = ImmunityReason.Elemental,
                    Target = immunityInfo.Target,
                    Element = immunityInfo.AttackerElement
                });
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取暴击率
        /// </summary>
        public float GetCriticalChance(SkillDamageInfo damageInfo)
        {
            float critChance = BaseCriticalChance;

            // 应用攻击方暴击率加成
            if (damageInfo.AttackerStats?.ContainsKey("CriticalChance") == true)
            {
                critChance += (float)damageInfo.AttackerStats["CriticalChance"];
            }

            // 应用技能暴击率加成
            critChance += damageInfo.SkillCriticalChanceBonus;

            // 应用状态效果影响
            critChance += damageInfo.StatusEffectCriticalBonus;

            return Mathf.Clamp(critChance, 0f, 1f);
        }

        #endregion

        #region 计算方法

        /// <summary>
        /// 应用攻击方加成
        /// </summary>
        private float ApplyAttackerBonuses(float damage, SkillDamageInfo info, DamageCalculationResult result)
        {
            float bonusDamage = damage;

            // 攻击力加成
            if (info.AttackerStats?.ContainsKey("AttackPower") == true)
            {
                float attackBonus = (float)info.AttackerStats["AttackPower"];
                bonusDamage *= (1 + attackBonus * 0.01f);
                result.AttackPowerBonus = attackBonus;
            }

            // 技能伤害加成
            if (info.SkillDamageBonus > 0)
            {
                bonusDamage *= (1 + info.SkillDamageBonus * 0.01f);
                result.SkillDamageBonus = info.SkillDamageBonus;
            }

            // 状态效果加成
            if (info.StatusEffectDamageBonus > 0)
            {
                bonusDamage *= (1 + info.StatusEffectDamageBonus * 0.01f);
                result.StatusEffectBonus = info.StatusEffectDamageBonus;
            }

            return bonusDamage;
        }

        /// <summary>
        /// 应用防御方减免
        /// </summary>
        private float ApplyDefenderReductions(float damage, SkillDamageInfo info, DamageCalculationResult result)
        {
            float reducedDamage = damage;

            // 防御力减免
            if (info.DefenderStats?.ContainsKey("Defense") == true)
            {
                float defense = (float)info.DefenderStats["Defense"];
                float reduction = defense / (defense + 100); // 简单的减伤公式
                reducedDamage *= (1 - reduction);
                result.DefenseReduction = reduction * 100;
            }

            // 伤害减免
            if (info.DefenderStats?.ContainsKey("DamageReduction") == true)
            {
                float reduction = (float)info.DefenderStats["DamageReduction"];
                reducedDamage *= (1 - reduction * 0.01f);
                result.DamageReduction = reduction;
            }

            return reducedDamage;
        }

        /// <summary>
        /// 判定暴击
        /// </summary>
        private bool RollCriticalHit(SkillDamageInfo info)
        {
            float critChance = GetCriticalChance(info);
            return _random.NextDouble() < critChance;
        }

        /// <summary>
        /// 获取暴击倍数
        /// </summary>
        private float GetCriticalMultiplier(SkillDamageInfo info)
        {
            float multiplier = BaseCriticalMultiplier;

            // 应用攻击方暴击伤害加成
            if (info.AttackerStats?.ContainsKey("CriticalDamage") == true)
            {
                multiplier += (float)info.AttackerStats["CriticalDamage"] * 0.01f;
            }

            // 应用技能暴击伤害加成
            multiplier += info.SkillCriticalDamageBonus;

            return Mathf.Max(1.0f, multiplier);
        }

        /// <summary>
        /// 计算属性相克倍数
        /// </summary>
        private float CalculateElementalAdvantage(Element attacker, Element defender)
        {
            // 简单的属性相克系统：火克风，风克土，土克水，水克火
            if (attacker == Element.Fire && defender == Element.Wind) return AdvantageMultiplier;
            if (attacker == Element.Wind && defender == Element.Earth) return AdvantageMultiplier;
            if (attacker == Element.Earth && defender == Element.Water) return AdvantageMultiplier;
            if (attacker == Element.Water && defender == Element.Fire) return AdvantageMultiplier;

            // 逆向相克减伤
            if (attacker == Element.Wind && defender == Element.Fire) return DisadvantageMultiplier;
            if (attacker == Element.Earth && defender == Element.Wind) return DisadvantageMultiplier;
            if (attacker == Element.Water && defender == Element.Earth) return DisadvantageMultiplier;
            if (attacker == Element.Fire && defender == Element.Water) return DisadvantageMultiplier;

            return 1.0f;
        }

        #endregion
    }

    #region 数据结构

    /// <summary>
    /// 技能伤害信息
    /// </summary>
    public class SkillDamageInfo
    {
        public string SkillId { get; set; }
        public CharacterEntity Source { get; set; }
        public CharacterEntity Target { get; set; }
        public float BaseDamage { get; set; }
        public DamageType DamageType { get; set; } = DamageType.Physical;
        public Element AttackerElement { get; set; } = Element.None;
        public Element DefenderElement { get; set; } = Element.None;
        
        // 攻击方属性
        public Dictionary<string, object> AttackerStats { get; set; }
        
        // 防御方属性
        public Dictionary<string, object> DefenderStats { get; set; }
        
        // 加成参数
        public float SkillDamageBonus { get; set; } = 0f;
        public float SkillCriticalChanceBonus { get; set; } = 0f;
        public float SkillCriticalDamageBonus { get; set; } = 0f;
        public float StatusEffectDamageBonus { get; set; } = 0f;
        public float StatusEffectCriticalBonus { get; set; } = 0f;
    }

    /// <summary>
    /// 伤害计算结果
    /// </summary>
    public class DamageCalculationResult
    {
        public string SkillId { get; set; }
        public CharacterEntity Source { get; set; }
        public CharacterEntity Target { get; set; }
        public float BaseDamage { get; set; }
        public float FinalDamage { get; set; }
        public DamageType DamageType { get; set; }
        public bool IsCritical { get; set; } = false;
        
        // 详细信息
        public float DamageVariance { get; set; } = 0f;
        public float CriticalMultiplier { get; set; } = 1f;
        public float ElementalMultiplier { get; set; } = 1f;
        public Element AttackerElement { get; set; } = Element.None;
        public Element DefenderElement { get; set; } = Element.None;
        
        // 加成详情
        public float AttackPowerBonus { get; set; } = 0f;
        public float SkillDamageBonus { get; set; } = 0f;
        public float StatusEffectBonus { get; set; } = 0f;
        public float DefenseReduction { get; set; } = 0f;
        public float DamageReduction { get; set; } = 0f;
    }

    /// <summary>
    /// 伤害免疫检查信息
    /// </summary>
    public class DamageImmunityCheckInfo
    {
        public CharacterEntity Target { get; set; }
        public DamageType DamageType { get; set; }
        public Element AttackerElement { get; set; }
        public bool TargetHasInvincibility { get; set; }
        public HashSet<DamageType> ImmuneDamageTypes { get; set; } = new();
        public HashSet<Element> ImmuneElements { get; set; } = new();
    }

    /// <summary>
    /// 暴击事件参数
    /// </summary>
    public class CriticalHitEventArgs
    {
        public SkillDamageInfo DamageInfo { get; set; }
        public float CriticalMultiplier { get; set; }
        public float FinalDamage { get; set; }
    }

    /// <summary>
    /// 伤害免疫事件参数
    /// </summary>
    public class DamageImmunityEventArgs
    {
        public ImmunityReason Reason { get; set; }
        public CharacterEntity Target { get; set; }
        public DamageType? DamageType { get; set; }
        public Element? Element { get; set; }
    }

    #endregion

    #region 枚举定义

    /// <summary>
    /// 伤害计算模式
    /// </summary>
    public enum DamageCalculationMode
    {
        Standard,    // 标准模式
        Simplified,  // 简化模式
        Detailed     // 详细模式
    }

    /// <summary>
    /// 免疫原因
    /// </summary>
    public enum ImmunityReason
    {
        Invincibility,  // 无敌状态
        DamageType,     // 伤害类型免疫
        Elemental       // 元素免疫
    }

    /// <summary>
    /// 元素类型
    /// </summary>
    public enum Element
    {
        None,
        Fire,
        Water,
        Earth,
        Wind,
        Light,
        Dark
    }

    /// <summary>
    /// 伤害类型
    /// </summary>
    public enum DamageType
    {
        Physical,
        Magical,
        True,
        Poison,
        Fire,
        Ice,
        Lightning
    }

    #endregion
}