using System.Collections.Generic;

namespace Attributes
{
    /// <summary>
    /// AttributeInstance 表示：
    /// “某个实体身上的某个属性”
    ///
    /// 例如：
    /// - 玩家身上的生命
    /// - 子弹身上的伤害
    ///
    /// ⚠️ 核心原则：
    /// AttributeInstance 只存在于 AttributeContainer 中
    /// </summary>
    public class AttributeInstance
    {
        /// <summary>
        /// 对应的属性定义
        /// </summary>
        public AttributeDef Def;

        /// <summary>
        /// 基础值（未修正）
        /// </summary>
        public float BaseValue;

        /// <summary>
        /// 最终值（计算后）
        /// </summary>
        public float FinalValue;

        /// <summary>
        /// 是否需要重新计算
        /// </summary>
        internal bool Dirty = true;

        /// <summary>
        /// 所有影响该属性的 Modifier
        /// </summary>
        public readonly List<ModifierInstance> Modifiers = new();
    }


}

