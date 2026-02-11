using Godot;
using System;
using StateMachine;
using Characters;
using Logs;

namespace StateMachine.PlayerStates
{
    /// <summary>
    /// 空闲状态（Idle）。
    /// 播放空闲动画，检测输入以切换到其他状态。
    /// </summary>
    public class IdleState : State
    {
        private Player _player;
        private double _idleTimer = 0.0;
        private const double BREATHE_ANIMATION_INTERVAL = 3.0; // 每3秒播放一次呼吸动画

        public IdleState(Node owner) : base(owner) 
        {
            _player = owner as Player;
        }

        public override void Enter()
        {
            Logger2.Debug("IdleState.Enter: 进入空闲状态");
            
            // 播放空闲动画
            if (_player?.AnimationSystem != null)
            {
                _player.AnimationSystem.PlayAnimation("idle");
            }
            
            // 重置计时器
            _idleTimer = 0.0;
        }

        public override void Update(double delta)
        {
            if (_player == null) return;
            
            // 更新计时器
            _idleTimer += delta;
            
            // 定期播放呼吸动画效果
            if (_idleTimer >= BREATHE_ANIMATION_INTERVAL)
            {
                // 可以在这里添加轻微的缩放动画或其他idle特效
                _idleTimer = 0.0;
            }
            
            // 检测输入状态并切换到相应状态
            CheckStateTransitions();
        }
        
        /// <summary>
        /// 检查状态转换条件
        /// </summary>
        private void CheckStateTransitions()
        {
            // 检查移动输入
            if (_player?.InputHandler?.IsMoving() == true)
            {
                _player.SetState("Move");
                return;
            }
            
            // 检查攻击输入
            if (_player?.InputHandler?.IsAttacking == true)
            {
                _player.SetState("Attack");
                return;
            }
            
            // 检查受伤状态（由外部系统设置）
            // 检查死亡状态（由外部系统设置）
        }
        
        public override void Exit()
        {
            Logger2.Debug("IdleState.Exit: 离开空闲状态");
        }
    }
}