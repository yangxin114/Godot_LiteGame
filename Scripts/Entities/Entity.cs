
using Godot;
using System;
using System.Collections.Generic;

namespace Entities
{

    /// <summary>
    /// 纯逻辑实体
    /// 不继承Godot
    /// 只负责组件管理与逻辑生命周期
    /// </summary>
    public partial class Entity : CharacterBody2D
    {
        /// <summary>
        /// 唯一ID（用于存档）
        /// </summary>
        public string Id { get; private set; }

        public Entity()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}