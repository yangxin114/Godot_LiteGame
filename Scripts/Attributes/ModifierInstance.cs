using Godot;
using Logs;

namespace Attributes
{
    /// <summary>
    /// ModifierInstance 是 ModifierDef 的运行时实例
    ///
    /// 例如：
    /// - 某件装备上的词缀
    /// - 某个 Buff 提供的加成
    /// </summary>
    public class ModifierInstance
    {
        /// <summary>
        /// 修正规则定义
        /// </summary>
        public ModifierDef Def;

        /// <summary>
        /// 来源（装备 / Buff / 技能）
        /// </summary>
        public AttributesOwner Source;

        /// <summary>
        /// 上下文（命中目标、技能类型等）
        /// </summary>
        public AttributeContext Context;

        /// <summary>
        /// 是否已失效
        /// </summary>
        public bool Expired;

        /// <summary>
        /// 添加时间戳（秒）
        /// </summary>
        public float ApplyTime;

        /// <summary>
        /// 解析修正值
        /// （后期可以支持动态数值）
        /// </summary>
        public float ResolveValue(AttributeContainer container)
        {
            // 未来可加入动态计算（基于容器/上下文）
            return Def.Value;
        }

        /// <summary>
        /// 判断是否过期（基于 Def.Duration 与 ApplyTime）
        /// </summary>
        public bool IsExpired()
        {
            if (Expired) return true;
            if (Def == null) return true;
            if (Def.Duration <= 0f) return false;
            var now = (float)System.DateTime.UtcNow.Subtract(System.DateTime.UnixEpoch).TotalSeconds;
            return now - ApplyTime >= Def.Duration;
        }
    }
}