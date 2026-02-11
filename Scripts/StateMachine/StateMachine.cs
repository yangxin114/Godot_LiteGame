using Godot;
using System;
using System.Collections.Generic;

namespace StateMachine
{
    /// <summary>
    /// 简单的状态机实现：
    /// - 使用 Register 注册状态名称与工厂方法（延迟构造状态实例）
    /// - ChangeState 切换状态（会调用 Exit/Enter）
    /// - Update 在每帧调用当前状态的 Update
    ///
    /// 说明：此状态机保持轻量且可扩展。具体游戏逻辑的状态可以在 BasePlayer 中注册。
    /// </summary>
    public class StateMachine
    {
        private Node owner;
        private State current;
        private Dictionary<string, Func<State>> factories = new();

        public StateMachine(Node owner)
        {
            this.owner = owner;
        }

        /// <summary>
        /// 注册一个状态名称与对应工厂（当切换到该状态时工厂会被调用创建状态实例）。
        /// </summary>
        /// <param name="name">状态名称</param>
        /// <param name="factory">返回 State 实例的函数</param>
        public void Register(string name, Func<State> factory)
        {
            factories[name] = factory;
        }

        /// <summary>
        /// 切换到指定状态（按名称）。若状态未注册会记录错误并忽略切换。
        /// </summary>
        /// <param name="name">目标状态名称</param>
        public void ChangeState(string name)
        {
            if (name == null)
                return;

            if (!factories.ContainsKey(name))
            {
                GD.PrintErr($"StateMachine: state '{name}' not registered");
                return;
            }

            current?.Exit();
            current = factories[name]?.Invoke();
            current?.Enter();
        }

        /// <summary>
        /// 在父对象的每帧 Update 中被调用以推进当前状态逻辑。
        /// </summary>
        public void Update(double delta)
        {
            current?.Update(delta);
        }

        /// <summary>
        /// 当前状态名称（类型名），如果没有当前状态则返回 null。
        /// </summary>
        public string CurrentName => current?.GetType().Name;
    }
}
