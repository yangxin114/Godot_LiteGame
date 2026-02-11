using Godot;
using System;
using Logs;

namespace Characters
{
    /// <summary>
    /// 玩家输入处理器
    /// 负责处理键盘、鼠标、手柄等输入设备的输入解析和状态管理
    /// 针对俯视角2D游戏优化
    /// </summary>
    public partial class PlayerInputHandler : Node
    {
        private Player _player;
        private bool _isInitialized = false;
        
        // 输入状态
        public Vector2 MoveDirection { get; private set; } = Vector2.Zero;
        public Vector2 LastMoveDirection { get; private set; } = Vector2.Right; // 记录最后的移动方向
        public bool IsAttacking { get; private set; } = false;
        public bool IsRunning { get; private set; } = false;
        public bool IsJumping { get; private set; } = false;
        public bool IsDashing { get; private set; } = false;
        
        // 输入配置
        [Export] public string MoveLeftAction { get; set; } = "move_left";
        [Export] public string MoveRightAction { get; set; } = "move_right";
        [Export] public string MoveUpAction { get; set; } = "move_up";
        [Export] public string MoveDownAction { get; set; } = "move_down";
        [Export] public string AttackAction { get; set; } = "attack";
        // [Export] public string RunAction { get; set; } = "run";
        // [Export] public string JumpAction { get; set; } = "jump";
        // [Export] public string DashAction { get; set; } = "dash";

        /// <summary>
        /// 初始化输入处理器
        /// </summary>
        public void Initialize(Player player)
        {
            if (_isInitialized)
            {
                Logger2.Warn("PlayerInputHandler: 已经初始化过了");
                return;
            }

            _player = player ?? throw new ArgumentNullException(nameof(player));
            _isInitialized = true;
            
            Logger2.Info("PlayerInputHandler: 初始化完成");
        }

        /// <summary>
        /// 每帧处理输入
        /// </summary>
        public void Update(double delta)
        {
            if (!_isInitialized) return;
            
            ProcessMovementInput();
            ProcessActionInput();
        }
        
        /// <summary>
        /// 处理移动输入
        /// </summary>
        private void ProcessMovementInput()
        {
            // 获取方向输入
            float moveX = 0f;
            float moveY = 0f;
            
            if (Input.IsActionPressed(MoveLeftAction))
                moveX -= 1f;
            if (Input.IsActionPressed(MoveRightAction))
                moveX += 1f;
            if (Input.IsActionPressed(MoveUpAction))
                moveY -= 1f;
            if (Input.IsActionPressed(MoveDownAction))
                moveY += 1f;
                
            Vector2 newDirection = new Vector2(moveX, moveY).Normalized();
            
            // 只有当移动方向发生显著变化时才更新
            if (newDirection != Vector2.Zero)
            {
                MoveDirection = newDirection;
                LastMoveDirection = newDirection; // 更新最后移动方向
            }
            else
            {
                MoveDirection = Vector2.Zero;
            }

            // 处理奔跑输入
            // IsRunning = Input.IsActionPressed(RunAction);
            
            // 处理冲刺输入（按下瞬间触发）
            // IsDashing = Input.IsActionJustPressed(DashAction);
        }
        
        /// <summary>
        /// 处理动作输入
        /// </summary>
        private void ProcessActionInput()
        {
            // 攻击输入（按下瞬间触发）
            IsAttacking = Input.IsActionJustPressed(AttackAction);
            
            // 跳跃输入（按下瞬间触发）
            // IsJumping = Input.IsActionJustPressed(JumpAction);
        }
        
        /// <summary>
        /// 检查是否正在移动
        /// </summary>
        public bool IsMoving()
        {
            return MoveDirection != Vector2.Zero;
        }
        
        /// <summary>
        /// 获取移动方向角度（弧度）
        /// </summary>
        public float GetMoveAngle()
        {
            return LastMoveDirection.Angle();
        }
        
        /// <summary>
        /// 获取方向索引（0-7，俯视角8方向）
        /// </summary>
        public int GetDirectionIndex()
        {
            if (LastMoveDirection == Vector2.Zero)
                return 0; // 默认向右
                
            float angle = LastMoveDirection.Angle();
            if (angle < 0) angle += 2 * Mathf.Pi;
            
            return Mathf.RoundToInt(angle / (2 * Mathf.Pi / 8)) % 8;
        }
        
        /// <summary>
        /// 获取朝向指定点的方向
        /// </summary>
        public Vector2 GetDirectionToPoint(Vector2 targetPoint)
        {
            if (_player?.GetNode<Node2D>(".") is Node2D playerNode)
            {
                return (targetPoint - playerNode.GlobalPosition).Normalized();
            }
            return Vector2.Zero;
        }
        
        /// <summary>
        /// 重置所有输入状态
        /// </summary>
        public void ResetInputs()
        {
            MoveDirection = Vector2.Zero;
            IsAttacking = false;
            IsRunning = false;
            IsJumping = false;
            IsDashing = false;
        }
        
        /// <summary>
        /// 清理资源
        /// </summary>
        public override void _ExitTree()
        {
            ResetInputs();
            Logger2.Info("PlayerInputHandler: 资源清理完成");
        }
    }
}