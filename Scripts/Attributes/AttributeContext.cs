using System.Collections.Generic;

namespace Attributes
{
    /// <summary>
    /// AttributeContext 用于描述“当前计算环境”
    ///
    /// 不参与数值运算，只用于条件判断
    /// </summary>
    public class AttributeContext
    {
        public AttributesOwner Source;
        public AttributesOwner Target;

        public HashSet<ContextTag> Tags = new();
    }
/// <summary>
/// 上下文标签
///
/// 可自由扩展，不影响数值核心
/// </summary>
public enum ContextTag
{
    Boss,
    Elite,
    Projectile,
    AoE,
    Fire,
    Cold,
    LowLife
}


}