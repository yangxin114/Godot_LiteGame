using System;

namespace Game.Systems.Attributes
{
    /// <summary>
    /// 属性类型定义
    /// 建议分为：核心属性(Core)、战斗属性(Combat)、功能属性(Utility)
    /// </summary>
    public enum AttributeType
    {
        // --- 核心生存 ---
        MaxHealth,      // 最大生命
        Defense,        // 防御力
        RegenRate,      // 生命回复/秒

        // --- 机动性 ---
        MoveSpeed,      // 移动速度

        // --- 战斗输出 ---
        AttackPower,    // 基础攻击
        AttackSpeed,    // 攻击速度 (次/秒)
        CritRate,       // 暴击率 (0.0 - 1.0)
        CritDamage,     // 暴击倍率 (e.g. 1.5)
        Range,          // 攻击/拾取范围

        // --- 特殊机制 ---
        CooldownReduction, // 冷却缩减
        Luck,              // 幸运值 (影响掉落/暴击)
    }

    /// <summary>
    /// 修饰符类型：决定计算公式中的位置
    /// 公式：(Base + Flat) * (1 + PercentAdd) * (TotalMul)
    /// </summary>
    public enum ModifierType
    {
        Flat,           // 固定值加成 (e.g. +10 攻击)
        PercentAdd,     // 叠加百分比 (e.g. +10% 攻击, 两个就是 +20%)
        TotalPercent    // 独立乘区 (e.g. 最终伤害翻倍 x2，极其稀有且强大)
    }
}