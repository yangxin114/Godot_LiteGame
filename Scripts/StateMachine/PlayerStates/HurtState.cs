using Godot;
using System;
using StateMachine;

namespace StateMachine.PlayerStates
{
    /// <summary>
    /// 受伤状态（Hurt）。受伤后短暂无敌/僵直，然后返回 Idle 或根据血量进入 Dead。
    /// </summary>
    public class HurtState : State
    {
        private double elapsed = 0.0;
        private double duration = 0.5; // 受伤僵直时间（秒）

        public HurtState(Node owner) : base(owner) { }

        public override void Enter()
        {
            elapsed = 0.0;
            // 可以播放受伤动画或触发闪烁
        }

        public override void Update(double delta)
        {
            elapsed += delta;
            if (elapsed >= duration)
            {
                // 如果血量为 0 或更低，切换到 Dead，否则回到 Idle
                var bp = owner as Characters.BasePlayer;
                if (bp != null)
                {
                    if (bp.Health <= 0)
                        bp.SetState("Dead");
                    else
                        bp.SetState("Idle");
                }
            }
        }
    }
}
