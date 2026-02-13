using Godot;
using System;
using Entities;

namespace Components
{
    /// <summary>
    /// 逻辑组件基类 - 纯C#类，不继承Node
    /// 所有游戏组件的基础
    /// </summary>
    public abstract class LogicComponent : IComponent
    {
        /// <summary>
        /// 组件所属的游戏实体
        /// </summary>
        protected CharacterEntity Entity { get; private set; }

        /// <summary>
        /// 组件是否已初始化
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// 组件是否已启动
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// 组件是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 初始化组件
        /// </summary>
        public virtual void Initialize(CharacterEntity entity)
        {
            Entity = entity;
            IsInitialized = true;
        }

        /// <summary>
        /// 启动组件（此时可以安全访问其他组件）
        /// </summary>
        public virtual void Start()
        {
            IsStarted = true;
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        public virtual void Update(float delta) { }

        /// <summary>
        /// 物理帧更新
        /// </summary>
        public virtual void PhysicsUpdate(float delta) { }

        /// <summary>
        /// 清理资源
        /// </summary>
        public virtual void Cleanup() { }

        /// <summary>
        /// 启用组件
        /// </summary>
        public virtual void Enable()
        {
            IsEnabled = true;
        }

        /// <summary>
        /// 禁用组件
        /// </summary>
        public virtual void Disable()
        {
            IsEnabled = false;
        }
    }
}