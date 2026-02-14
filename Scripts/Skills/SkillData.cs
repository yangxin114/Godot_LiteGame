using Godot;
using System;
using System.Collections.Generic;

namespace Skills
{
    /// <summary>
    /// 技能数据类
    /// 存储技能的所有配置信息和属性数据
    /// 
    /// 设计理念：
    /// - 作为纯数据类，不包含任何业务逻辑
    /// - 支持通过Godot编辑器进行可视化配置
    /// - 提供基于等级的属性计算方法
    /// - 与技能逻辑实现完全分离
    /// 
    /// 核心功能：
    /// 1. 存储技能的基础配置数据
    /// 2. 提供等级相关的属性计算
    /// 3. 管理技能效果和状态配置
    /// 4. 支持技能的可视化编辑
    /// </summary>
    [GlobalClass]
    public partial class SkillData : Resource
    {
        #region 基础信息

        /// <summary>
        /// 技能唯一标识符
        /// 用于在游戏中唯一识别此技能
        /// </summary>
        [Export] public string SkillId { get; set; } = "";

        /// <summary>
        /// 技能显示名称
        /// 在UI界面中展示给玩家的名称
        /// </summary>
        [Export] public string SkillName { get; set; } = "";

        /// <summary>
        /// 技能详细描述
        /// 包含技能效果、使用方法等说明信息
        /// </summary>
        [Export(PropertyHint.MultilineText)] 
        public string Description { get; set; } = "";

        /// <summary>
        /// 技能图标资源路径
        /// 用于UI界面显示技能图标的资源路径
        /// </summary>
        [Export] public string IconPath { get; set; } = "";

        #endregion

        #region 等级配置

        /// <summary>
        /// 当前技能等级
        /// 影响技能效果强度和资源消耗
        /// </summary>
        [Export] public int Level { get; set; } = 1;

        /// <summary>
        /// 技能最高等级
        /// 限制技能可以升级到的最大等级
        /// </summary>
        [Export] public int MaxLevel { get; set; } = 10;

        #endregion

        #region 资源消耗

        /// <summary>
        /// 基础法力消耗
        /// 技能施放所需的基础法力值
        /// </summary>
        [Export] public float ManaCost { get; set; } = 0f;

        /// <summary>
        /// 基础体力消耗
        /// 技能施放所需的基础体力值
        /// </summary>
        [Export] public float StaminaCost { get; set; } = 0f;

        /// <summary>
        /// 基础怒气消耗
        /// 技能施放所需的基础怒气值
        /// </summary>
        [Export] public float RageCost { get; set; } = 0f;

        /// <summary>
        /// 每级法力消耗减少量
        /// 技能升级时每级减少的法力消耗
        /// </summary>
        [Export] public float ManaCostReductionPerLevel { get; set; } = 0f;

        /// <summary>
        /// 每级体力消耗减少量
        /// 技能升级时每级减少的体力消耗
        /// </summary>
        [Export] public float StaminaCostReductionPerLevel { get; set; } = 0f;

        /// <summary>
        /// 每级怒气消耗减少量
        /// 技能升级时每级减少的怒气消耗
        /// </summary>
        [Export] public float RageCostReductionPerLevel { get; set; } = 0f;

        #endregion

        #region 时间配置

        /// <summary>
        /// 基础施法时间（秒）
        /// 技能施放所需的引导时间，0表示瞬发
        /// </summary>
        [Export] public float CastTime { get; set; } = 0f;

        /// <summary>
        /// 基础冷却时间（秒）
        /// 技能施放后的冷却时间
        /// </summary>
        [Export] public float Cooldown { get; set; } = 0f;

        /// <summary>
        /// 每级冷却时间减少量（秒）
        /// 技能升级时每级减少的冷却时间
        /// </summary>
        [Export] public float CooldownReductionPerLevel { get; set; } = 0f;

        /// <summary>
        /// 施法范围（像素）
        /// 技能有效作用的距离范围
        /// </summary>
        [Export] public float CastRange { get; set; } = 100f;

        #endregion

        #region 效果配置

        /// <summary>
        /// 技能效果列表
        /// 技能施放时会应用的所有状态效果
        /// </summary>
        [Export] public Godot.Collections.Array<StatusEffectData> StatusEffects { get; set; } = new();

        /// <summary>
        /// 技能伤害值
        /// 技能造成的直接伤害数值
        /// </summary>
        [Export] public float Damage { get; set; } = 0f;

        /// <summary>
        /// 每级伤害增加量
        /// 技能升级时每级增加的伤害值
        /// </summary>
        [Export] public float DamageIncreasePerLevel { get; set; } = 0f;

        /// <summary>
        /// 治疗数值
        /// 技能恢复的生命值数量
        /// </summary>
        [Export] public float HealAmount { get; set; } = 0f;

        /// <summary>
        /// 每级治疗增加量
        /// 技能升级时每级增加的治疗值
        /// </summary>
        [Export] public float HealIncreasePerLevel { get; set; } = 0f;

        #endregion

        #region 高级配置

        /// <summary>
        /// 是否为被动技能
        /// 被动技能会自动激活，无需手动施放
        /// </summary>
        [Export] public bool IsPassive { get; set; } = false;

        /// <summary>
        /// 是否可以被打断
        /// 控制技能施放过程中是否能被外部因素中断
        /// </summary>
        [Export] public bool CanBeInterrupted { get; set; } = true;

        /// <summary>
        /// 技能类别标签
        /// 用于技能分类和筛选的标签系统
        /// </summary>
        [Export] public Godot.Collections.Array<string> Tags { get; set; } = new();

        /// <summary>
        /// 前置技能要求
        /// 学习此技能前必须掌握的其他技能
        /// </summary>
        [Export] public Godot.Collections.Array<string> RequiredSkills { get; set; } = new();

        /// <summary>
        /// 学习所需角色等级
        /// 学习此技能所需的最低角色等级
        /// </summary>
        [Export] public int RequiredLevel { get; set; } = 1;

        #endregion

        #region 属性计算方法

        /// <summary>
        /// 获取实际冷却时间（考虑等级影响）
        /// 基于当前等级计算技能的实际冷却时间
        /// </summary>
        /// <returns>实际冷却时间（秒）</returns>
        public float GetActualCooldown()
        {
            float reduction = CooldownReductionPerLevel * (Level - 1);
            return Math.Max(0, Cooldown - reduction);
        }

        /// <summary>
        /// 获取实际法力消耗（考虑等级影响）
        /// 基于当前等级计算技能的实际法力消耗
        /// </summary>
        /// <returns>实际法力消耗值</returns>
        public float GetActualManaCost()
        {
            float reduction = ManaCostReductionPerLevel * (Level - 1);
            return Math.Max(0, ManaCost - reduction);
        }

        /// <summary>
        /// 获取实际体力消耗（考虑等级影响）
        /// 基于当前等级计算技能的实际体力消耗
        /// </summary>
        /// <returns>实际体力消耗值</returns>
        public float GetActualStaminaCost()
        {
            float reduction = StaminaCostReductionPerLevel * (Level - 1);
            return Math.Max(0, StaminaCost - reduction);
        }

        /// <summary>
        /// 获取实际怒气消耗（考虑等级影响）
        /// 基于当前等级计算技能的实际怒气消耗
        /// </summary>
        /// <returns>实际怒气消耗值</returns>
        public float GetActualRageCost()
        {
            float reduction = RageCostReductionPerLevel * (Level - 1);
            return Math.Max(0, RageCost - reduction);
        }

        /// <summary>
        /// 获取实际伤害值（考虑等级影响）
        /// 基于当前等级计算技能的实际伤害值
        /// </summary>
        /// <returns>实际伤害值</returns>
        public float GetActualDamage()
        {
            float increase = DamageIncreasePerLevel * (Level - 1);
            return Math.Max(0, Damage + increase);
        }

        /// <summary>
        /// 获取实际治疗值（考虑等级影响）
        /// 基于当前等级计算技能的实际治疗值
        /// </summary>
        /// <returns>实际治疗值</returns>
        public float GetActualHealAmount()
        {
            float increase = HealIncreasePerLevel * (Level - 1);
            return Math.Max(0, HealAmount + increase);
        }

        /// <summary>
        /// 检查是否满足学习条件
        /// 验证角色是否满足学习此技能的所有前置要求
        /// </summary>
        /// <param name="characterLevel">角色当前等级</param>
        /// <param name="learnedSkills">角色已学习的技能列表</param>
        /// <returns>是否满足学习条件</returns>
        public bool CanLearn(int characterLevel, HashSet<string> learnedSkills)
        {
            // 检查等级要求
            if (characterLevel < RequiredLevel)
                return false;

            // 检查前置技能要求
            foreach (string requiredSkill in RequiredSkills)
            {
                if (!learnedSkills.Contains(requiredSkill))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 获取技能的完整信息字符串
        /// 用于调试和日志输出
        /// </summary>
        /// <returns>技能信息字符串</returns>
        public string GetInfoString()
        {
            return $"Skill[{SkillId}]: {SkillName} (Lv.{Level}/{MaxLevel}) " +
                   $"CD:{GetActualCooldown():F1}s " +
                   $"Cost:{GetActualManaCost():F0}MP/{GetActualStaminaCost():F0}SP/{GetActualRageCost():F0}RP " +
                   $"DMG:{GetActualDamage():F0} HEAL:{GetActualHealAmount():F0}";
        }

        #endregion
    }

    #region 状态效果数据结构

    /// <summary>
    /// 状态效果数据类
    /// 定义技能施放时产生的各种状态效果
    /// </summary>
    [GlobalClass]
    public partial class StatusEffectData : Resource
    {
        /// <summary>
        /// 效果唯一标识符
        /// 用于唯一识别此状态效果
        /// </summary>
        [Export] public string EffectId { get; set; } = "";

        /// <summary>
        /// 效果显示名称
        /// 在UI中展示给玩家的名称
        /// </summary>
        [Export] public string EffectName { get; set; } = "";

        /// <summary>
        /// 效果类型
        /// 定义此效果的作用类型
        /// </summary>
        [Export] public StatusEffectType EffectType { get; set; } = StatusEffectType.Buff;

        /// <summary>
        /// 效果强度数值
        /// 效果的具体数值大小
        /// </summary>
        [Export] public float Power { get; set; } = 0f;

        /// <summary>
        /// 效果持续时间（秒）
        /// 效果持续生效的时间长度，0表示永久效果
        /// </summary>
        [Export] public float Duration { get; set; } = 0f;

        /// <summary>
        /// 是否为减益效果
        /// 标识此效果是对目标有害还是有益
        /// </summary>
        [Export] public bool IsDebuff { get; set; } = false;

        /// <summary>
        /// 效果堆叠层数
        /// 相同效果可以同时存在的最大层数
        /// </summary>
        [Export] public int MaxStacks { get; set; } = 1;

        /// <summary>
        /// 效果触发间隔（秒）
        /// 对于周期性效果，每次触发的时间间隔
        /// </summary>
        [Export] public float TickInterval { get; set; } = 1f;
    }

    /// <summary>
    /// 状态效果类型枚举
    /// 定义游戏中各种状态效果的分类
    /// </summary>
    public enum StatusEffectType
    {
        /// <summary>
        /// 增益效果 - 提升目标的能力或属性
        /// </summary>
        Buff,

        /// <summary>
        /// 减益效果 - 削弱目标的能力或造成持续伤害
        /// </summary>
        Debuff,

        /// <summary>
        /// 持续伤害 - 每隔一段时间造成伤害
        /// </summary>
        DamageOverTime,

        /// <summary>
        /// 持续治疗 - 每隔一段时间恢复生命值
        /// </summary>
        HealOverTime,

        /// <summary>
        /// 属性修饰 - 临时改变目标的基础属性
        /// </summary>
        StatModifier,

        /// <summary>
        /// 免疫效果 - 使目标免疫某些类型的伤害或效果
        /// </summary>
        Immunity,

        /// <summary>
        /// 控制效果 - 限制目标的行动能力
        /// </summary>
        CrowdControl
    }

    #endregion
}