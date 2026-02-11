using Godot;
using System;
using StateMachine;
using Characters;
using Logs;

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
            if (_player?.AnimationSystem != null)
            {
                _player.AnimationSystem.PlayAnimation("attack1", true); // 强制播放
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
            // - 检测敌人碰撞
            // - 应用伤害
            // - 播放攻击音效
            // - 显示攻击特效
            
            Logger2.Info("AttackState.ExecuteAttack: 执行攻击");
        }

        public override void Update(double delta)
        {
            if (_player == null) return;
            
            _elapsed += delta;
            
            // 攻击持续时间结束后转换状态
            if (_elapsed >= ATTACK_DURATION)
            {
                TransitionToNextState();
                return;
            }
            
            // 在攻击过程中也可以检查其他重要状态
            // 检查受伤状态
            // 检查死亡状态
        }
        
        /// <summary>
        /// 转换到下一个合适的状态
        /// </summary>
        private void TransitionToNextState()
        {
            if (_player == null) return;
            
            // 优先检查死亡状态
            if (ShouldBeDead())
            {
                _player.SetState("Dead");
                return;
            }
            
            // 检查受伤状态
            if (ShouldBeHurt())
            {
                _player.SetState("Hurt");
                return;
            }
            
            // 检查是否仍在移动
            if (_player.InputHandler?.IsMoving() == true)
            {
                _player.SetState("Move");
            }
            else
            {
                _player.SetState("Idle");
            }
        }
        
        /// <summary>
        /// 检查是否应该死亡
        /// </summary>
        private bool ShouldBeDead()
        {
            // 检查生命值
            var currentHealthDef = Numerical.StatDefDataLoader.Instance.GetStatDefById(Numerical.StatDefDataLoader.CurrentHealth);
            if (currentHealthDef != null && _player != null)
            {
                float health = _player.GetStatContainer().Get(currentHealthDef);
                return health <= 0;
            }
            return false;
        }
        
        /// <summary>
        /// 检查是否应该受伤
        /// </summary>
        private bool ShouldBeHurt()
        {
            // TODO: 实现受伤检测逻辑
            return false;
        }
        
        public override void Exit()
        {
            Logger2.Debug("AttackState.Exit: 离开攻击状态");
        }
    }
}