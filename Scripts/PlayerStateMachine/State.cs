using Godot;
using System;

namespace PlayerStateMachine
{
    /// <summary>
    /// 状态基类（State）。
    /// 每个具体状态应继承此类并实现 Enter/Exit/Update 等生命周期方法。
    /// - owner 表示状态所属的节点（通常是 BasePlayer 或其派生类）。
    /// - HandleInput 提供一个可选的输入处理钩子，用于将输入事件传递给当前状态。
    /// </summary>
    public abstract class State
    {
        /// <summary>
        /// 状态所属的节点引用（通常为 BasePlayer）。
        /// </summary>
        protected Node owner;

        public State(Node owner)
        {
            this.owner = owner;
        }

        /// <summary>
        /// 进入状态时调用（一次）。可用于开始动画、播放音效等。
        /// </summary>
        public virtual void Enter() { }

        /// <summary>
        /// 退出状态时调用（一次）。用于清理或停止动画。
        /// </summary>
        public virtual void Exit() { }

        /// <summary>
        /// 每帧更新调用。实现状态的行为逻辑。
        /// </summary>
        /// <param name="delta">帧间隔（秒）</param>
        public virtual void Update(double delta) { }

        /// <summary>
        /// 可选：传递输入事件给当前状态以响应按键/鼠标等。
        /// </summary>
        public virtual void HandleInput(InputEvent e) { }
    }
}
