using Godot;
using System;
using Numerical;
using Logs;

namespace Characters
{
    /// <summary>
    /// 玩家移动系统
    /// 负责处理玩家的移动、冲刺等移动相关逻辑
    /// 针对俯视角2D游戏优化，不包含旋转逻辑
    /// </summary>
    public partial class PlayerMovementSystem : Node
    {
        private Player _player;
        private CharacterBody2D _playerCharacterBody2D;  // 引用用于访问Position和Rotation的Node2D组件
        private bool _isInitialized = false;
        
        // 移动状态
        private Vector2 _velocity = Vector2.Zero;
        private bool _isMoving = false;
        private bool _isDashing = false;
        
        // 移动配置
        [Export] public float BaseMoveSpeed { get; set; } = 200.0f;
        [Export] public float RunMultiplier { get; set; } = 1.5f;
        [Export] public float DashSpeed { get; set; } = 600.0f;
        [Export] public float DashDuration { get; set; } = 0.2f;
        [Export] public float DashCooldown { get; set; } = 1.0f;
        
        // 内部计时器
        private double _dashTimer = 0.0;
        private double _dashCooldownTimer = 0.0;
        private const float MIN_MOVEMENT_THRESHOLD = 0.1f; // 最小移动阈值

        /// <summary>
        /// 初始化移动系统
        /// </summary>
        public void Initialize(Player player)
        {
            if (_isInitialized)
            {
                Logger2.Warn("PlayerMovementSystem: 已经初始化过了");
                return;
            }

            _player = player ?? throw new ArgumentNullException(nameof(player));
            
            // 查找Player节点的Node2D组件
            _playerCharacterBody2D = FindCharacterBody2DComponent(player);
            _isInitialized = true;
            
            Logger2.Info("PlayerMovementSystem: 初始化完成");
        }

        /// <summary>
        /// 每帧更新移动逻辑
        /// </summary>
        public void Update(double delta)
        {
            if (!_isInitialized) return;
            
            UpdateDashState(delta);
            ApplyMovement(delta);
        }
        
        /// <summary>
        /// 查找Node2D组件
        /// </summary>
        private CharacterBody2D FindCharacterBody2DComponent(Node node)
        {
            Logger2.Info("FindCharacterBody2DComponent: node name: {0}, type: {1}", node.Name, node.GetType().Name);
            // 如果当前节点就是CharacterBody2D
            if (node is CharacterBody2D node2D)
                return node2D;
                
            // 向上查找父节点
            var parent = node.GetParent();
            while (parent != null)
            {
                Logger2.Info("FindCharacterBody2DComponent: parent name: {0}, type: {1}", parent.Name, parent.GetType().Name);
                if (parent is CharacterBody2D parent2D)
                    return parent2D;
                parent = parent.GetParent();
            }
            
            // 如果找不到，创建一个临时的Node2D用于位置操作
            Logger2.Warn("PlayerMovementSystem: 未找到Node2D组件，将使用相对位置计算");
            return null;
        }
        
        /// <summary>
        /// 应用移动
        /// </summary>
        private void ApplyMovement(double delta)
        {
            if (_playerCharacterBody2D == null) return;
            
            // 获取移动输入
            Vector2 inputDirection = GetMoveInput();
            bool wasMoving = _isMoving;
            _isMoving = inputDirection.Length() > MIN_MOVEMENT_THRESHOLD;
            
            // 计算移动速度
            float speed = CalculateMoveSpeed();
            
            // 应用移动
            if (_isMoving)
            {
                _velocity = inputDirection * speed;
                _playerCharacterBody2D.Velocity = _velocity;
                // 更新位置
                // _playerNode.Position += _velocity * (float)delta;
                _playerCharacterBody2D.MoveAndSlide();
                
                // 通知其他系统移动状态变化（用于动画等）
                if (!wasMoving)
                {
                    OnMovementStarted(inputDirection);
                }
            }
            else
            {
                // 停止移动时逐渐减速
                _velocity = _velocity.MoveToward(Vector2.Zero, speed * 5 * (float)delta);
                // _playerNode.Position += _velocity * (float)delta;
                _playerCharacterBody2D.Velocity = _velocity;

                // _playerCharacterBody2D.MoveAndCollide(_velocity * (float)delta);
                _playerCharacterBody2D.MoveAndSlide();
                // 通知其他系统移动停止
                if (wasMoving)
                {
                    OnMovementStopped();
                }
            }
        }
        
        /// <summary>
        /// 移动开始事件
        /// </summary>
        private void OnMovementStarted(Vector2 direction)
        {
            // 可以在这里通知动画系统或其他系统
            Logger2.Debug("PlayerMovementSystem: 开始移动，方向: ({0:F2}, {1:F2})", direction.X, direction.Y);
        }
        
        /// <summary>
        /// 移动停止事件
        /// </summary>
        private void OnMovementStopped()
        {
            // 可以在这里通知动画系统或其他系统
            Logger2.Debug("PlayerMovementSystem: 停止移动");
        }
        
        /// <summary>
        /// 获取移动输入方向
        /// </summary>
        private Vector2 GetMoveInput()
        {
            // 从PlayerInputHandler获取输入方向
            if (_player?.InputHandler != null)
            {
                return _player.InputHandler.MoveDirection;
            }
            
            // 如果没有输入处理器，使用键盘输入作为后备方案
            Vector2 direction = Vector2.Zero;
            
            if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left))
                direction.X -= 1;
            if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right))
                direction.X += 1;
            if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up))
                direction.Y -= 1;
            if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down))
                direction.Y += 1;
                
            return direction.Normalized();
        }
        
        /// <summary>
        /// 计算移动速度
        /// </summary>
        private float CalculateMoveSpeed()
        {
            float speed = BaseMoveSpeed;
            
            // 从属性系统获取速度加成
            if (_player != null)
            {
                var moveSpeedDef = StatDefDataLoader.Instance.GetStatDefById(StatDefDataLoader.MoveSpeed);
                if (moveSpeedDef != null)
                {
                    speed += _player.GetStatContainer().Get(moveSpeedDef);
                }
            }
            
            // 应用奔跑倍数
            if (_player?.InputHandler?.IsRunning ?? false)
            {
                speed *= RunMultiplier;
            }
            
            // 应用冲刺速度
            if (_isDashing)
            {
                speed = DashSpeed;
            }
            
            return speed;
        }
        
        /// <summary>
        /// 执行冲刺
        /// </summary>
        public void PerformDash(Vector2 direction)
        {
            if (!_isInitialized || _isDashing || _dashCooldownTimer > 0 || _playerCharacterBody2D == null)
                return;
                
            if (direction == Vector2.Zero)
            {
                Logger2.Warn("PlayerMovementSystem: 冲刺方向不能为零");
                return;
            }
            
            Logger2.Info("PlayerMovementSystem: 执行冲刺");
            
            _isDashing = true;
            _dashTimer = DashDuration;
            // 冲刺方向标准化
            _velocity = direction.Normalized() * DashSpeed;
        }
        
        /// <summary>
        /// 更新冲刺状态
        /// </summary>
        private void UpdateDashState(double delta)
        {
            // 更新冲刺计时器
            if (_isDashing)
            {
                _dashTimer -= delta;
                if (_dashTimer <= 0)
                {
                    _isDashing = false;
                    _dashCooldownTimer = DashCooldown;
                    Logger2.Debug("PlayerMovementSystem: 冲刺结束");
                }
            }
            
            // 更新冲刺冷却计时器
            if (_dashCooldownTimer > 0)
            {
                _dashCooldownTimer -= delta;
                if (_dashCooldownTimer <= 0)
                {
                    _dashCooldownTimer = 0;
                    Logger2.Debug("PlayerMovementSystem: 冲刺冷却结束");
                }
            }
        }
        
        /// <summary>
        /// 停止移动
        /// </summary>
        public void StopMovement()
        {
            _velocity = Vector2.Zero;
            _isMoving = false;
        }
        
        /// <summary>
        /// 击退效果
        /// </summary>
        public void ApplyKnockback(Vector2 force, double duration = 0.3)
        {
            // TODO: 实现击退逻辑
            Logger2.Info("PlayerMovementSystem: 应用击退效果，力度: {0}", force);
        }
        
        /// <summary>
        /// 检查是否正在移动
        /// </summary>
        public bool IsMoving()
        {
            return _isMoving;
        }
        
        /// <summary>
        /// 检查是否正在冲刺
        /// </summary>
        public bool IsDashing()
        {
            return _isDashing;
        }
        
        /// <summary>
        /// 获取当前速度
        /// </summary>
        public Vector2 GetVelocity()
        {
            return _velocity;
        }
        
        /// <summary>
        /// 获取当前移动方向
        /// </summary>
        public Vector2 GetCurrentMovement()
        {
            return _isMoving ? _velocity.Normalized() : Vector2.Zero;
        }
        
        /// <summary>
        /// 获取冲刺冷却剩余时间
        /// </summary>
        public double GetDashCooldownRemaining()
        {
            return _dashCooldownTimer;
        }
        
        /// <summary>
        /// 清理资源
        /// </summary>
        public override void _ExitTree()
        {
            StopMovement();
            Logger2.Info("PlayerMovementSystem: 资源清理完成");
        }
    }
}