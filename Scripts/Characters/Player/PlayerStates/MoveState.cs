using Godot;
using System;
using StateMachine;
using Characters;
using Logs;

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
        private Node2D _playerNode;
        private Vector2 _lastMoveDirection = Vector2.Zero;
        private const float MIN_MOVEMENT_THRESHOLD = 0.1f;

        public MoveState(Node owner) : base(owner) 
        {
            _player = owner as Player;
        }

        public override void Enter()
        {
            Logger2.Debug("MoveState.Enter: 进入移动状态");
            
            // 获取Player的Node2D组件用于位置操作
            _playerNode = FindNode2DComponent(_player);
            
            // 播放跑步动画（初始方向）
            if (_player?.AnimationSystem != null)
            {
                _player.AnimationSystem.PlayAnimation("walk"); // 默认向右
            }
        }
        
        /// <summary>
        /// 查找Node2D组件
        /// </summary>
        private Node2D FindNode2DComponent(Node node)
        {
            if (node is Node2D node2D)
                return node2D;
                
            var parent = node.GetParent();
            while (parent != null)
            {
                if (parent is Node2D parent2D)
                    return parent2D;
                parent = parent.GetParent();
            }
            
            return null;
        }

        public override void Update(double delta)
        {
            if (_player == null || _playerNode == null) return;
            
            // 获取移动输入
            Vector2 moveDirection = _player.InputHandler?.MoveDirection ?? Vector2.Zero;
            
            // 更新方向动画（俯视角8方向）
            if (moveDirection.Length() > MIN_MOVEMENT_THRESHOLD)
            {
                _player.AnimationSystem?.PlayDirectionalAnimation(moveDirection, "walk");
                _lastMoveDirection = moveDirection.Normalized();
            }
            else if (moveDirection == Vector2.Zero)
            {
                // 停止移动时回到空闲状态
                _player.SetState("Idle");
                return;
            }
            
            // 检查攻击输入
            if (_player.InputHandler?.IsAttacking == true)
            {
                _player.SetState("Attack");
                return;
            }
            
            // 检查受伤状态
            // 检查死亡状态
        }
        
        public override void Exit()
        {
            Logger2.Debug("MoveState.Exit: 离开移动状态");
        }
    }
}