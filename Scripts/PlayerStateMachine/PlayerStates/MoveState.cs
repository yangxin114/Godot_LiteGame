using Godot;
using System;
using PlayerStateMachine;

namespace PlayerStateMachine.PlayerStates
{
    /// <summary>
    /// 移动状态（Move）。
    /// 示例：进入时开始移动动画，Update 中根据拥有者的速度或输入改变位置。
    /// 注意：此处为通用示例，具体移动实现应由 BasePlayer 或子类负责。
    /// </summary>
    public class MoveState : State
    {
        public MoveState(Node owner) : base(owner) { }

        public override void Enter()
        {
            // 启动移动动画或播放音效
        }

        public override void Update(double delta)
        {
            // 简单移动示例：如果 owner 有 Velocity 字段，则应在此应用它。
            // 本状态不直接改写位置，让具体玩家实现可覆盖行为。
        }
    }
}
