using Godot;
using System;
namespace Attributes
{
    /// <summary>
    /// ModifierDef 描述“一种数值修改规则”
    ///
    /// 例如：
    /// - +10 生命
    /// - 增加 20% 火焰伤害
    ///
    /// ⚠️ ModifierDef 本身不生效，
    /// 必须实例化成 ModifierInstance
    /// </summary>
    /// <summary>
    /// ModifierDef 描述“一种数值修改规则”
    ///
    /// 例如：
    /// - +10 生命
    /// - 增加 20% 火焰伤害
    ///
    /// ⚠️ ModifierDef 本身不生效，
    /// 必须实例化成 ModifierInstance
    /// </summary>
    [GlobalClass]
    public partial class ModifierDef : Resource
    {
        /// <summary>
        /// 影响的目标属性
        /// </summary>
        [Export] public AttributeDef Target;

        /// <summary>
        /// 运算方式
        /// </summary>
        [Export] public ModifierOp Operation;

        /// <summary>
        /// 所属计算阶段
        /// </summary>
        [Export] public ModifierPhase Phase;

        /// <summary>
        /// 修正值（百分比用 0.2 表示 20%）
        /// </summary>
        [Export] public float Value;

        /// <summary>
        /// 是否需要上下文条件
        /// </summary>
        [Export] public bool UseContext;

        /// <summary>
        /// 需要的上下文标签
        /// </summary>
        [Export] public ContextTag RequiredTag;

        /// <summary>
        /// 叠加模式：是否可以堆叠、按来源唯一，或添加时刷新持续时间
        /// </summary>
        [Export] public ModifierStackMode StackMode = ModifierStackMode.Stackable;

        /// <summary>
        /// 持续时间（秒），<=0 表示永久
        /// </summary>
        [Export] public float Duration = 0f;

        /// <summary>
        /// 优先级，影响同一阶段内的处理顺序（值越大越先处理）
        /// </summary>
        [Export] public int Priority = 0;
    }

    /// <summary>
    /// Modifier 的堆叠/唯一性规则
    /// </summary>
    public enum ModifierStackMode
    {
        Stackable,
        UniquePerSource,
        UniqueGlobal,
        RefreshDuration
    }
    /// <summary>
    /// Modifier 的计算阶段
    ///
    /// 显式拆分计算顺序，防止数值失控
    ///
    /// 顺序：
    /// Base → Increase → More → Override → Clamp
    /// </summary>
    public enum ModifierPhase
    {
        Base,
        Increase,
        More,
        Override,
        Clamp
    }
    /// <summary>
    /// Modifier 的运算方式
    ///
    /// 对应 PoE 的核心规则：
    /// - Flat       → +X
    /// - Increased → +X%
    /// - More      → ×X
    /// - Override  → 直接覆盖
    /// </summary>
    public enum ModifierOp
    {
        Flat,
        Increased,
        More,
        Override,
        Clamp
    }
}

