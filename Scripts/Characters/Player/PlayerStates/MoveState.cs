using Godot;
using System;
using StateMachine;
using Characters;
using Logs;
using Components;

namespace StateMachine.PlayerStates
{
    /// <summary>
    /// 移动状态（Move）。
    /// 播放跑步动画，处理角色移动和转向。
    /// 支持俯视角8方向动画。
    /// </summary>
    public class MoveState : State
    {
        private Player _player;
        private Vector2 _lastMoveDirection = Vector2.Zero;
        private const float MIN_MOVEMENT_THRESHOLD = 0.1f;

        public MoveState(Node owner) : base(owner) 
        {
            _player = owner as Player;
        }

        public override void Enter()
        {
            Logger2.Debug("MoveState.Enter: 进入移动状态");
            
            // 播放跑步动画（初始方向）
            if (_player?.AnimationComponent != null)
            {
                _player.AnimationComponent.Play("walk"); // 默认向右
            }
        }

        public override void Update(double delta)
        {
            if (_player == null) return;
            
            // 获取移动输入
            Vector2 moveDirection = _player.InputComponent?.MoveDirection ?? Vector2.Zero;
            
            // 更新方向动画（俯视角8方向）
            if (moveDirection.Length() > MIN_MOVEMENT_THRESHOLD)
            {
                // 使用动画组件播放对应方向的动画
                PlayDirectionalWalkAnimation(moveDirection);
                _lastMoveDirection = moveDirection.Normalized();
            }
            else if (moveDirection == Vector2.Zero)
            {
                // 停止移动时回到空闲状态
                GetStateMachine()?.ChangeState("Idle");
                return;
            }
            
            // 检查攻击输入
            if (_player.InputComponent?.IsAttacking == true)
            {
                GetStateMachine()?.ChangeState("Attack");
                return;
            }
            
            // 检查受伤状态
            // 检查死亡状态
        }
        
        /// <summary>
        /// 播放方向性行走动画
        /// </summary>
        private void PlayDirectionalWalkAnimation(Vector2 direction)
        {
            if (_player?.AnimationComponent == null) return;
            
            // 根据方向选择合适的动画
            string animationName = "walk"; // 默认动画
            
            // 可以根据具体需求实现8方向动画选择
            // 这里简化为基本的左右翻转处理
            if (Math.Abs(direction.X) > Math.Abs(direction.Y))
            {
                // 水平移动为主
                if (direction.X > 0)
                {
                    _player.AnimationComponent.SetFlipH(false); // 向右
                }
                else
                {
                    _player.AnimationComponent.SetFlipH(true); // 向左
                }
            }
            // 垂直移动的处理可以根据需要添加
            
            _player.AnimationComponent.Play(animationName);
        }
        
        public override void Exit()
        {
            Logger2.Debug("MoveState.Exit: 离开移动状态");
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