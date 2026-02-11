using Godot;
using System;
using StateMachine;
using Characters;
using Logs;

namespace StateMachine.PlayerStates
{
    /// <summary>
    /// 死亡状态（Dead）。
    /// 播放死亡动画，停止所有行为，可以在此处播放死亡动画并释放节点。
    /// </summary>
    public class DeadState : State
    {
        private Player _player;
        private CharacterBody2D _playerNode;
        private double _deathTimer = 0.0;
        private const double DEATH_ANIMATION_DURATION = 1.5; // 死亡动画持续时间
        private bool _deathAnimationPlayed = false;

        public DeadState(Node owner) : base(owner) 
        {
            _player = owner as Player;
            _playerNode = _player as CharacterBody2D;
        }

        public override void Enter()
        {
            Logger2.Debug("DeadState.Enter: 进入死亡状态");
            
            _deathTimer = 0.0;
            _deathAnimationPlayed = false;
            
            // 播放死亡动画
            if (_player?.AnimationSystem != null)
            {
                _player.AnimationSystem.PlayAnimation("dead", true); // 强制播放
                _deathAnimationPlayed = true;
            }
            
            // 禁用玩家控制
            DisablePlayerControls();
            
            // TODO: 播放死亡音效
            // TODO: 显示死亡特效
            // TODO: 触发游戏结束逻辑
        }
        
        /// <summary>
        /// 禁用玩家控制
        /// </summary>
        private void DisablePlayerControls()
        {
            if (_playerNode == null) return;
            
            // 禁用物理处理
            _playerNode.SetPhysicsProcess(false);
            
            // 禁用输入处理（如果有的话）
            // 这取决于具体的输入处理方式
            
            Logger2.Info("DeadState.DisablePlayerControls: 玩家控制已禁用");
        }

        public override void Update(double delta)
        {
            if (_player == null) return;
            
            _deathTimer += delta;
            
            // 死亡动画播放完成后可以执行其他逻辑
            if (_deathTimer >= DEATH_ANIMATION_DURATION)
            {
                HandlePostDeath();
            }
        }
        
        /// <summary>
        /// 处理死亡后的逻辑
        /// </summary>
        private void HandlePostDeath()
        {
            // 可以在这里添加：
            // - 显示复活选项
            // - 触发关卡重置
            // - 显示游戏结束界面
            // - 移除玩家节点
            // - 触发其他游戏逻辑
            
            Logger2.Debug("DeadState.HandlePostDeath: 死亡后处理逻辑");
        }
        
        public override void Exit()
        {
            Logger2.Debug("DeadState.Exit: 离开死亡状态");
            
            // 重新启用控制（如果需要复活）
            if (_playerNode != null)
            {
                _playerNode.SetPhysicsProcess(true);
            }
        }
    }
}