using Godot;
using System;
using StateMachine;
using Characters;
using Logs;
using Components;

namespace StateMachine.PlayerStates
{
    /// <summary>
    /// 死亡状态（Dead）。
    /// 播放死亡动画，禁用所有输入和移动，可能触发游戏结束逻辑。
    /// </summary>
    public class DeadState : State
    {
        private Player _player;

        public DeadState(Node owner) : base(owner) 
        {
            _player = owner as Player;
        }

        public override void Enter()
        {
            Logger2.Debug("DeadState.Enter: 进入死亡状态");
            
            // 播放死亡动画
            if (_player?.AnimationComponent != null)
            {
                _player.AnimationComponent.Play("dead", true); // 强制播放死亡动画
            }
            
            // 禁用移动组件
            _player?.MovementComponent?.Disable();
            
            // 禁用输入组件
            _player?.InputComponent?.Disable();
            
            // TODO: 触发死亡相关逻辑
            // - 显示死亡UI
            // - 播放死亡音效
            // - 触发游戏结束检查
            Logger2.Info("DeadState: 玩家死亡");
        }

        public override void Exit()
        {
            Logger2.Debug("DeadState.Exit: 退出死亡状态");
            
            // 重新启用移动组件
            _player?.MovementComponent?.Enable();
            
            // 重新启用输入组件
            _player?.InputComponent?.Enable();
        }

        public override void Update(double delta)
        {
            // 死亡状态下通常不需要更新逻辑
            // 但可以处理一些持续效果，如尸体渐隐等
        }

    }
}