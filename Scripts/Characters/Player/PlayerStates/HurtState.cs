using Godot;
using System;
using StateMachine;
using Characters;
using Logs;

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
        private const double FLASH_INTERVAL = 0.1; // 闪烁间隔
        private double _flashTimer = 0.0;
        private bool _isVisible = true;
        private Node2D _visualNode; // 用于闪烁效果的可视节点

        public HurtState(Node owner) : base(owner) 
        {
            _player = owner as Player;
        }

        public override void Enter()
        {
            Logger2.Debug("HurtState.Enter: 进入受伤状态");
            
            _elapsed = 0.0;
            _flashTimer = 0.0;
            _isVisible = true;
            
            // 查找可视节点用于闪烁效果
            _visualNode = FindVisualNode(_player);
            
            // 播放受伤动画
            if (_player?.AnimationSystem != null)
            {
                _player.AnimationSystem.PlayAnimation("hurt", true); // 强制播放
            }
            
            // 开始闪烁效果
            StartFlashEffect();
            
            // TODO: 播放受伤音效
            // TODO: 应用击退效果
        }
        
        /// <summary>
        /// 查找用于视觉效果的节点
        /// </summary>
        private Node2D FindVisualNode(Node node)
        {
            // 优先查找AnimatedSprite2D
            var sprite = node.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
            if (sprite != null) return sprite;
            
            // 查找Sprite2D
            var staticSprite = node.GetNodeOrNull<Sprite2D>("Sprite2D");
            if (staticSprite != null) return staticSprite;
            
            // 返回Node2D本身
            return node as Node2D;
        }
        
        /// <summary>
        /// 开始闪烁效果
        /// </summary>
        private void StartFlashEffect()
        {
            if (_visualNode != null)
            {
                _visualNode.Visible = _isVisible;
            }
        }

        public override void Update(double delta)
        {
            if (_player == null) return;
            
            _elapsed += delta;
            _flashTimer += delta;
            
            // 更新闪烁效果
            UpdateFlashEffect();
            
            // 无敌时间结束后转换状态
            if (_elapsed >= INVINCIBILITY_DURATION)
            {
                TransitionToNextState();
                return;
            }
        }
        
        /// <summary>
        /// 更新闪烁效果
        /// </summary>
        private void UpdateFlashEffect()
        {
            if (_flashTimer >= FLASH_INTERVAL && _visualNode != null)
            {
                _isVisible = !_isVisible;
                _visualNode.Visible = _isVisible;
                _flashTimer = 0.0;
            }
        }
        
        /// <summary>
        /// 转换到下一个合适的状态
        /// </summary>
        private void TransitionToNextState()
        {
            if (_player == null) return;
            
            // 停止闪烁效果
            if (_visualNode != null)
            {
                _visualNode.Visible = true;
            }
            
            // 优先检查死亡状态
            if (ShouldBeDead())
            {
                _player.SetState("Dead");
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
        
        public override void Exit()
        {
            Logger2.Debug("HurtState.Exit: 离开受伤状态");
            
            // 确保离开时可见
            if (_visualNode != null)
            {
                _visualNode.Visible = true;
            }
        }
    }
}