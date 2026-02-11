using Godot;
namespace Numerical
{
    /// <summary>
    /// StatDef 是“属性的定义”，
    /// 它描述的是一种数值规则本身，而不是某个角色的数值。
    ///
    /// 例如：
    /// - 生命
    /// - 最大生命
    /// - 攻击力
    /// - 火焰伤害
    ///
    /// ⚠️ 重要规则：
    /// StatDef 永远是“静态的”，
    /// 不存角色状态、不存运行时数值。
    /// </summary>
    [GlobalClass]
    public partial class StatDef : Resource
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
        [Export] public StatCategory Category;

        /// <summary>
        /// 默认基础值
        /// </summary>
        [Export] public float DefaultValue = 0;

        /// <summary>
        /// 是否是派生属性（由公式计算）
        /// </summary>
        [Export] public bool IsDerived = false;

        /// <summary>
        /// 是否启用最终数值 Clamp
        /// </summary>
        [Export] public bool ClampEnabled = true;

        [Export] public float MinValue = 0;
        [Export] public float MaxValue = float.MaxValue;
    }

    /// <summary>
    /// 属性分类，不影响数值逻辑
    /// </summary>
    public enum StatCategory
    {
        Survival,   // 生存（生命、护盾）
        Combat,     // 战斗（伤害、暴击）
        Mechanic,   // 机制（投射物数、范围）
        Defense,      // 防御
        Movement,     // 移动
        Elemental,    // 元素
        Skill,        // 技能
        Wealth,       // 财富/资源
        Status        // 状态效果
    }

}

