using Godot;
namespace Attributes
{
    /// <summary>
    /// AttributeDef 是“属性的定义”，
    /// 它描述的是一种数值规则本身，而不是某个角色的数值。
    ///
    /// 例如：
    /// - 生命
    /// - 最大生命
    /// - 攻击力
    /// - 火焰伤害
    ///
    /// ⚠️ 重要规则：
    /// AttributeDef 永远是“静态的”，
    /// 不存角色状态、不存运行时数值。
    /// </summary>
    public partial class AttributeDef : Resource
    {
        /// <summary>
        /// 属性唯一 ID，用于系统内部索引
        /// </summary>
        [Export] public string Id;

        /// <summary>
    /// 显示用名称（UI / Debug）
    /// </summary>
    [Export] public string DisplayName;

        /// <summary>
    /// 属性分类（仅用于组织和 UI）
    /// </summary>
    [Export] public AttributeCategory Category;

        /// <summary>
    /// 默认基础值
    /// </summary>
    [Export] public float DefaultValue;

        /// <summary>
    /// 是否是派生属性（由公式计算）
    /// </summary>
    [Export] public bool IsDerived;

        /// <summary>
        /// 是否启用最终数值 Clamp
        /// </summary>
        [Export] public bool ClampEnabled;

        [Export] public float MinValue;
        [Export] public float MaxValue;
    }

    /// <summary>
    /// 属性分类，不影响数值逻辑
    /// </summary>
    public enum AttributeCategory
    {
        Survival,   // 生存（生命、护盾）
        Combat,     // 战斗（伤害、暴击）
        Mechanic,   // 机制（投射物数、范围）
        Meta        // 元属性（冷却恢复等）
    }

}

