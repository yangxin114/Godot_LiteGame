using Godot;
using System;
using StateMachine;
using Characters;
using Logs;
using Components;

namespace StateMachine.PlayerStates
{
    /// <summary>
    /// 攻击状态（Attack）。
    /// 播放攻击动画，处理攻击逻辑，持续一段时间后返回合适状态。
    /// </summary>
    public class AttackState : State
    {
        private Player _player;
        private double _elapsed = 0.0;
        private const double ATTACK_DURATION = 0.4; // 攻击持续时间（秒）
        private bool _hasAttacked = false;

        public AttackState(Node owner) : base(owner) 
        {
            _player = owner as Player;
        }

        public override void Enter()
        {
            Logger2.Debug("AttackState.Enter: 进入攻击状态");
            
            _elapsed = 0.0;
            _hasAttacked = false;
            
            // 播放攻击动画
            if (_player?.AnimationComponent != null)
            {
                _player.AnimationComponent.Play("attack1", true); // 强制播放
            }
            
            // 执行攻击逻辑
            ExecuteAttack();
        }
        
        /// <summary>
        /// 执行攻击逻辑
        /// </summary>
        private void ExecuteAttack()
        {
            if (_player == null) return;
            
            _hasAttacked = true;
            
            // TODO: 实际的攻击逻辑
            // 可以调用战斗系统或其他组件
            Logger2.Info("AttackState: 执行攻击");
        }

        public override void Exit()
        {
            Logger2.Debug("AttackState.Exit: 退出攻击状态");
            _hasAttacked = false;
        }

        public override void Update(double delta)
        {
            if (_player == null) return;
            
            _elapsed += delta;
            
            // 攻击结束后检查是否应该转换到其他状态
            if (_elapsed >= ATTACK_DURATION)
            {
                // 检查是否有移动输入
                if (_player.InputComponent?.IsMoving == true)
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
        /// 获取状态机引用
        /// </summary>
        private StateMachine GetStateMachine()
        {
            return _player?.StateManager?.StateMachine;
        }
    }
}