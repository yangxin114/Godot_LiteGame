using Godot;
using System;
using Logs;

namespace Characters
{
    /// <summary>
    /// Player动画系统
    /// 负责管理玩家的动画播放、状态同步和视觉表现
    /// 针对俯视角2D游戏进行了优化
    /// </summary>
    public partial class PlayerAnimationSystem : Node
    {
        private Player _player;
        private AnimatedSprite2D _animatedSprite;
        private bool _isInitialized = false;
        private string _currentAnimation = "idle";
        private Vector2 _lastMoveDirection = Vector2.Zero;
        private const float MIN_MOVEMENT_THRESHOLD = 0.01f; // 最小移动阈值
        
        /// <summary>
        /// 当前播放的动画名称
        /// </summary>
        public string CurrentAnimation => _currentAnimation;
        
        /// <summary>
        /// 动画是否正在播放
        /// </summary>
        public bool IsPlaying => _animatedSprite?.IsPlaying() ?? false;

        /// <summary>
        /// 初始化动画系统
        /// </summary>
        public void Initialize(Player player)
        {
            if (_isInitialized)
            {
                Logger2.Warn("PlayerAnimationSystem: 已经初始化过了");
                return;
            }

            _player = player ?? throw new ArgumentNullException(nameof(player));
            _isInitialized = true;
            
            // 查找AnimatedSprite2D节点
            _animatedSprite = _player.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
            if (_animatedSprite == null)
            {
                Logger2.Warn("PlayerAnimationSystem: 未找到AnimatedSprite2D节点，将在首次播放动画时尝试查找");
            }

            PlayAnimation("idle");
            
            Logger2.Info("PlayerAnimationSystem: 初始化完成");
        }
        
        /// <summary>
        /// 播放指定动画
        /// </summary>
        public void PlayAnimation(string animationName, bool force = false)
        {
            if (!_isInitialized)
            {
                Logger2.Error("PlayerAnimationSystem: 请先调用Initialize方法");
                return;
            }
            
            // 如果不是强制播放且当前正在播放相同动画，则不重复播放
            if (!force && _currentAnimation == animationName && IsPlaying)
            {
                return;
            }
            
            _currentAnimation = animationName;
            
            // 确保AnimatedSprite2D存在
            EnsureAnimatedSprite();
            
            if (_animatedSprite != null)
            {
                try
                {
                    _animatedSprite.Play(animationName);
                    Logger2.Debug("PlayerAnimationSystem: 播放动画 {0}", animationName);
                }
                catch (Exception ex)
                {
                    Logger2.Error("PlayerAnimationSystem.PlayAnimation: 播放动画失败 - {0}", ex.Message);
                }
            }
            else
            {
                Logger2.Error("PlayerAnimationSystem: 无法播放动画 {0}，AnimatedSprite2D未找到", animationName);
            }
        }
        
        /// <summary>
        /// 停止当前动画
        /// </summary>
        public void StopAnimation()
        {
            if (_animatedSprite != null && IsPlaying)
            {
                _animatedSprite.Stop();
                Logger2.Debug("PlayerAnimationSystem: 停止动画");
            }
        }
        
        /// <summary>
        /// 暂停动画
        /// </summary>
        public void PauseAnimation()
        {
            if (_animatedSprite != null && IsPlaying)
            {
                _animatedSprite.Pause();
                Logger2.Debug("PlayerAnimationSystem: 暂停动画");
            }
        }
        
        /// <summary>
        /// 恢复动画播放
        /// </summary>
        public void ResumeAnimation()
        {
            if (_animatedSprite != null && !_animatedSprite.IsPlaying())
            {
                _animatedSprite.Play();
                Logger2.Debug("PlayerAnimationSystem: 恢复动画");
            }
        }
        
        /// <summary>
        /// 设置水平翻转
        /// </summary>
        public void SetFlipH(bool flip)
        {
            if (_animatedSprite != null)
            {
                _animatedSprite.FlipH = flip;
            }
        }
        
        /// <summary>
        /// 设置垂直翻转
        /// </summary>
        public void SetFlipV(bool flip)
        {
            if (_animatedSprite != null)
            {
                _animatedSprite.FlipV = flip;
            }
        }
        
        /// <summary>
        /// 根据移动向量自动设置翻转（适用于侧视角）
        /// </summary>
        public void AutoFlip(Vector2 movement)
        {
            // 只有当移动足够明显时才更新翻转
            if (movement.Length() > MIN_MOVEMENT_THRESHOLD)
            {
                // 根据X轴方向设置水平翻转
                if (movement.X != 0)
                {
                    SetFlipH(movement.X < 0);
                }
                
                // 根据Y轴方向设置垂直翻转（可选）
                // if (movement.Y != 0)
                // {
                //     SetFlipV(movement.Y < 0);
                // }
                
                _lastMoveDirection = movement.Normalized();
            }
        }
        
        /// <summary>
        /// 根据移动方向播放对应的8方向动画（适用于俯视角）
        /// </summary>
        public void PlayDirectionalAnimation(Vector2 movement, string baseAnimationName = "walk")
        {
            if (movement.Length() <= MIN_MOVEMENT_THRESHOLD)
            {
                // 如果几乎没有移动，保持当前动画或播放闲置动画
                if (_currentAnimation.StartsWith(baseAnimationName))
                {
                    PlayAnimation("idle");
                }
                return;
            }
            
            Vector2 normalizedMovement = movement.Normalized();
            string directionalAnimation = GetDirectionalAnimationName(normalizedMovement, baseAnimationName);
            
            // 只有当方向真正改变时才切换动画
            // if (_currentAnimation != directionalAnimation)
            // {
                PlayAnimation(directionalAnimation);
                _lastMoveDirection = normalizedMovement;
                
                // 同时更新翻转状态以确保视觉一致性
                AutoFlip(normalizedMovement);
            // }
        }
        
        /// <summary>
        /// 根据方向向量获取对应的动画名称
        /// </summary>
        private string GetDirectionalAnimationName(Vector2 direction, string baseName)
        {
            // 计算角度（以右侧为0度，逆时针方向）
            // float angle = Mathf.Atan2(direction.Y, direction.X);
            // if (angle < 0) angle += 2 * Mathf.Pi;
            
            // // 将角度转换为8个方向
            // int directionIndex = Mathf.RoundToInt(angle / (2 * Mathf.Pi / 8)) % 8;
            
            // 返回对应的动画名称
            return $"{baseName}";
        }
        
        /// <summary>
        /// 获取方向索引（0-7，从右开始逆时针）
        /// </summary>
        public int GetDirectionIndex(Vector2 direction)
        {
            if (direction.Length() <= MIN_MOVEMENT_THRESHOLD)
                return -1; // 无方向
                
            float angle = Mathf.Atan2(direction.Y, direction.X);
            if (angle < 0) angle += 2 * Mathf.Pi;
            
            return Mathf.RoundToInt(angle / (2 * Mathf.Pi / 8)) % 8;
        }
        
        /// <summary>
        /// 确保AnimatedSprite2D节点存在
        /// </summary>
        private void EnsureAnimatedSprite()
        {
            if (_animatedSprite == null)
            {
                _animatedSprite = _player.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
                if (_animatedSprite == null)
                {
                    // 尝试递归查找
                    _animatedSprite = FindAnimatedSpriteRecursive(_player);
                }
                
                if (_animatedSprite != null)
                {
                    Logger2.Info("PlayerAnimationSystem: 找到AnimatedSprite2D节点");
                }
            }
        }
        
        /// <summary>
        /// 递归查找AnimatedSprite2D节点
        /// </summary>
        private AnimatedSprite2D FindAnimatedSpriteRecursive(Node parentNode)
        {
            foreach (Node child in parentNode.GetChildren())
            {
                if (child is AnimatedSprite2D sprite)
                {
                    return sprite;
                }
                
                AnimatedSprite2D found = FindAnimatedSpriteRecursive(child);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 根据玩家状态自动播放对应动画
        /// </summary>
        public void PlayAnimationForState(string stateName)
        {
            string animationName = stateName.ToLower() switch
            {
                "idle" => "idle",
                "move" or "walking" or "running" => "walk",
                "attack" => "attack1",
                "hurt" => "hurt",
                "dead" or "death" => "dead",
                "dash" => "dash",
                _ => "idle"
            };
            
            PlayAnimation(animationName);
        }
        
        /// <summary>
        /// 根据玩家状态和输入更新动画（核心动画更新逻辑）
        /// </summary>
        public void UpdateAnimation(string currentState, Vector2 moveDirection)
        {
            if (!_isInitialized || _player == null) return;
            
            switch (currentState?.ToLower())
            {
                case "move":
                case "walking":
                case "running":
                    // 移动状态：使用方向动画系统，并确保正确的翻转
                    if (moveDirection.Length() > MIN_MOVEMENT_THRESHOLD)
                    {
                        // 对于8方向动画，使用方向动画系统
                        PlayDirectionalAnimation(moveDirection, "walk");
                    }
                    else
                    {
                        // 停止移动时播放空闲动画
                        PlayAnimation("idle");
                    }
                    break;
                    
                case "idle":
                    // 空闲状态：根据移动输入决定是否开始移动动画
                    if (moveDirection.Length() > MIN_MOVEMENT_THRESHOLD)
                    {
                        PlayDirectionalAnimation(moveDirection, "walk");
                    }
                    else
                    {
                        PlayAnimation("idle");
                    }
                    break;
                    
                case "attack":
                    // 攻击状态：根据攻击方向设置动画方向
                    // 注意：这里需要PlayerCombatSystem的支持
                    PlayAnimation("attack1"); // 基础攻击动画，可以扩展为方向性攻击
                    break;
                    
                default:
                    // 其他状态：基本的翻转控制（适用于侧视角兼容）
                    AutoFlip(moveDirection);
                    break;
            }
        }
        
        /// <summary>
        /// 清理资源
        /// </summary>
        public override void _ExitTree()
        {
            StopAnimation();
            Logger2.Info("PlayerAnimationSystem: 资源清理完成");
        }
    }
}