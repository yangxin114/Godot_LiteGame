using Godot;
using System;
using Numerical;
using Logs;

namespace Characters
{
    /// <summary>
    /// 玩家移动系统
    /// 负责处理玩家的移动、转向、冲刺等移动相关逻辑
    /// </summary>
    public partial class PlayerMovementSystem : Node
    {
        private Player _player;
        private Node2D _playerNode;  // 引用用于访问Position和Rotation的Node2D组件
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
            _playerNode = FindNode2DComponent(player);
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
        private Node2D FindNode2DComponent(Node node)
        {
            // 如果当前节点就是Node2D
            if (node is Node2D node2D)
                return node2D;
                
            // 向上查找父节点
            var parent = node.GetParent();
            while (parent != null)
            {
                if (parent is Node2D parent2D)
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
            if (_playerNode == null) return;
            
            // 获取移动输入
            Vector2 inputDirection = GetMoveInput();
            _isMoving = inputDirection != Vector2.Zero;
            
            // 计算移动速度
            float speed = CalculateMoveSpeed();
            
            // 应用移动
            if (_isMoving)
            {
                _velocity = inputDirection * speed;
                // 更新位置
                _playerNode.Position += _velocity * (float)delta;
                
                // 处理转向
                HandleRotation(inputDirection);
            }
            else
            {
                // 停止移动时逐渐减速
                _velocity = _velocity.MoveToward(Vector2.Zero, speed * 5 * (float)delta);
                _playerNode.Position += _velocity * (float)delta;
            }
        }
        
        /// <summary>
        /// 获取移动输入方向
        /// </summary>
        private Vector2 GetMoveInput()
        {
            // TODO: 从PlayerInputHandler获取输入
            // 暂时返回零向量，实际应该从输入系统获取
            return Vector2.Zero;
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
                    speed += _player.Stats.Get(moveSpeedDef);
                }
            }
            
            // 应用奔跑倍数
            // TODO: 检查是否正在奔跑
            // if (isRunning) speed *= RunMultiplier;
            
            // 应用冲刺速度
            if (_isDashing)
            {
                speed = DashSpeed;
            }
            
            return speed;
        }
        
        /// <summary>
        /// 处理角色转向
        /// </summary>
        private void HandleRotation(Vector2 direction)
        {
            if (direction == Vector2.Zero || _playerNode == null) return;
            
            // 计算目标角度
            float targetAngle = direction.Angle();
            
            // 平滑转向到目标角度
            // TODO: 实现平滑转向逻辑
            _playerNode.Rotation = targetAngle;
        }
        
        /// <summary>
        /// 执行冲刺
        /// </summary>
        public void PerformDash(Vector2 direction)
        {
            if (!_isInitialized || _isDashing || _dashCooldownTimer > 0 || _playerNode == null)
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