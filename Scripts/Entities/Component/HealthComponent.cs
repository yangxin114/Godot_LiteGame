using Godot;
using System;


namespace Entities
{

    /// <summary>
    /// 生命系统
    /// 管理血量、死亡、受伤
    /// </summary>
    public class HealthComponent : BaseComponent
    {
        public HealthComponent(Entity entity) : base(entity: entity)
        {
        }
        
    }

}