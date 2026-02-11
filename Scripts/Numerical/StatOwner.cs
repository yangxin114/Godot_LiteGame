using System.Collections.Generic;
using Godot;

namespace Numerical
{
    /// <summary>
    /// 所有拥有数值的对象都继承它
    ///
    /// Player / Enemy / Projectile / Skill
    /// </summary>
    public abstract partial class StatOwner : Node
    {
        public StatContainer Stats = new();
    }
}