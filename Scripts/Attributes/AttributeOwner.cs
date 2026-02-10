using System.Collections.Generic;
using Godot;

namespace Attributes
{
    /// <summary>
    /// 所有拥有数值的对象都继承它
    ///
    /// Player / Enemy / Projectile / Skill
    /// </summary>
    public abstract partial class AttributesOwner : Node
    {
        public AttributeContainer Attributes = new();
    }
}