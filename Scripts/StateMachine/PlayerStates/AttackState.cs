using Godot;
using System;
using PlayerStateMachine;

namespace PlayerStateMachine.PlayerStates
{
    /// <summary>
    /// 攻击状态（Attack）。
    /// 进入后短暂保持攻击状态，随后返回 Idle。
    /// </summary>
    public class AttackState : State
    {
        private double elapsed = 0.0;
        private double duration = 0.4; // 攻击持续时间（秒）

        public AttackState(Node owner) : base(owner) { }

        public override void Enter()
        {
            elapsed = 0.0;
            // 这里可触发攻击动画/音效
        }

        public override void Update(double delta)
        {
            elapsed += delta;
            if (elapsed >= duration)
            {
                // 回到空闲状态
                (owner as Characters.BasePlayer)?.SetState("Idle");
            }
        }
    }
}
