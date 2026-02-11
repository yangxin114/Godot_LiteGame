using Godot;
using System;
using StateMachine;

namespace StateMachine.PlayerStates
{
    /// <summary>
    /// 空闲状态（Idle）。
    /// 示例实现：进入时停止移动动画，Update 中可以播放呼吸动画或播放空闲特效。
    /// 具体行为应由拥有者（BasePlayer）和动画系统进一步实现。
    /// </summary>
    public class IdleState : State
    {
        public IdleState(Node owner) : base(owner) { }

        public override void Enter()
        {
            // 进入空闲状态时的初始化逻辑，例如播放空闲动画或停止运动特效。
            if (owner is Node2D n)
            {
                // 默认不做实际渲染操作，由具体玩家类型决定。
            }
        }

        public override void Update(double delta)
        {
            // 空闲时的每帧逻辑（如检测输入切换到 Move/Attack 等）可在此实现。
        }
    }
}
