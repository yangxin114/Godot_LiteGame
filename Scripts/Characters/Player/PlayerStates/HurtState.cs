using Godot;
using System;
using StateMachine;
using Characters;
using Logs;
using Components;

namespace StateMachine.PlayerStates
{
    /// <summary>
    /// 受伤状态（Hurt）。
    /// 播放受伤动画，短暂无敌/僵直，然后根据血量返回合适状态。
    /// </summary>
    public class HurtState : State
    {
        private Player _player;
        private double _elapsed = 0.0;
        private const double INVINCIBILITY_DURATION = 0.5; // 无敌持续时间（秒）

        public HurtState(Node owner) : base(owner) 
        {
            _player = owner as Player;
        }

        public override void Enter()
        {
            Logger2.Debug("HurtState.Enter: 进入受伤状态");
            
            _elapsed = 0.0;
            
            // 播放受伤动画
            if (_player?.AnimationComponent != null)
            {
                _player.AnimationComponent.Play("hurt", true); // 强制播放
            }
            
            // TODO: 播放受伤音效
            // TODO: 应用击退效果
        }

        public override void Exit()
        {
            Logger2.Debug("HurtState.Exit: 退出受伤状态");
            
            // 恢复正常状态
            _elapsed = 0.0;
        }

        public override void Update(double delta)
        {
            if (_player == null) return;
            
            _elapsed += delta;
            
            // 无敌时间结束后检查状态转换
            if (_elapsed >= INVINCIBILITY_DURATION)
            {
                // 检查是否死亡
                if (ShouldDie())
                {
                    GetStateMachine()?.ChangeState("Dead");
                }
                // 检查是否有移动输入
                else if (_player.InputComponent?.IsMoving == true)
                {
                    GetStateMachine()?.ChangeState("Move");
                }
                else
                {
                    GetStateMachine()?.ChangeState("Idle");
                }
            }
        }

        /// <summary>
        /// 检查是否应该死亡
        /// </summary>
        private bool ShouldDie()
        {
            // TODO: 实际的生命值检查
            // var healthDef = StatDefDataLoader.Instance.GetStatDefById(StatDefDataLoader.CurrentHealth);
            // if (healthDef != null && _player?.StatsManager?.statContainer != null)
            // {
            //     float health = _player.StatsManager.statContainer.Get(healthDef);
            //     return health <= 0;
            // }
            return false; // 临时返回false用于测试
        }

        /// <summary>
        /// 获取状态机引用
        /// </summary>
        private StateMachine GetStateMachine()
        {
            return _player?.StateManager?.StateMachine;
        }
    }
}