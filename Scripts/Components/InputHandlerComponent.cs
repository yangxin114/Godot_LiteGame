using Godot;
using System;
using Entities;
using Logs;

namespace Components
{
    /// <summary>
    /// 输入处理组件
    /// 负责处理键盘、鼠标、手柄等输入设备的输入解析和状态管理
    /// 适用于所有需要输入控制的游戏实体
    /// </summary>
    public class InputHandlerComponent : LogicComponent
    {
        #region 输入状态

        /// <summary>
        /// 当前移动方向
        /// </summary>
        public Vector2 MoveDirection { get; private set; } = Vector2.Zero;

        /// <summary>
        /// 最后一次移动方向（用于朝向计算）
        /// </summary>
        public Vector2 LastMoveDirection { get; private set; } = Vector2.Right;

        /// <summary>
        /// 是否正在攻击
        /// </summary>
        public bool IsAttacking { get; private set; } = false;

        /// <summary>
        /// 是否正在奔跑
        /// </summary>
        public bool IsRunning { get; private set; } = false;

        /// <summary>
        /// 是否正在跳跃
        /// </summary>
        public bool IsJumping { get; private set; } = false;

        /// <summary>
        /// 是否正在冲刺
        /// </summary>
        public bool IsDashing { get; private set; } = false;

        /// <summary>
        /// 是否正在移动
        /// </summary>
        public bool IsMoving => MoveDirection != Vector2.Zero;

        #endregion

        #region 输入配置

        /// <summary>
        /// 左移动作名称
        /// </summary>
        public string MoveLeftAction { get; set; } = "move_left";

        /// <summary>
        /// 右移动作名称
        /// </summary>
        public string MoveRightAction { get; set; } = "move_right";

        /// <summary>
        /// 上移动作名称
        /// </summary>
        public string MoveUpAction { get; set; } = "move_up";

        /// <summary>
        /// 下移动作名称
        /// </summary>
        public string MoveDownAction { get; set; } = "move_down";

        /// <summary>
        /// 攻击动作名称
        /// </summary>
        public string AttackAction { get; set; } = "attack";

        /// <summary>
        /// 奔跑动作名称
        /// </summary>
        public string RunAction { get; set; } = "run";

        /// <summary>
        /// 跳跃动作名称
        /// </summary>
        public string JumpAction { get; set; } = "jump";

        /// <summary>
        /// 冲刺动作名称
        /// </summary>
        public string DashAction { get; set; } = "dash";

        /// <summary>
        /// 是否启用输入处理
        /// </summary>
        public bool InputEnabled { get; set; } = true;

        #endregion

        #region 事件

        /// <summary>
        /// 移动方向改变时触发
        /// </summary>
        public event Action<Vector2> OnMoveDirectionChanged;

        /// <summary>
        /// 攻击输入触发时触发
        /// </summary>
        public event Action OnAttackPressed;

        /// <summary>
        /// 奔跑状态改变时触发
        /// </summary>
        public event Action<bool> OnRunStateChanged;

        /// <summary>
        /// 跳跃输入触发时触发
        /// </summary>
        public event Action OnJumpPressed;

        /// <summary>
        /// 冲刺输入触发时触发
        /// </summary>
        public event Action<Vector2> OnDashPressed;

        #endregion

        #region 生命周期

        public override void Initialize(CharacterEntity entity)
        {
            base.Initialize(entity);
            Logger2.Info("InputHandlerComponent: 组件初始化完成");
        }

        public override void Start()
        {
            base.Start();
            Logger2.Info("InputHandlerComponent: 组件启动完成");
        }

        public override void Update(float delta)
        {
            if (!IsEnabled || !InputEnabled) return;

            ProcessMovementInput();
            ProcessActionInput();
        }

        public override void Cleanup()
        {
            ResetInputs();
            base.Cleanup();
            Logger2.Info("InputHandlerComponent: 组件清理完成");
        }

        #endregion

        #region 输入处理方法

        /// <summary>
        /// 处理移动输入
        /// </summary>
        private void ProcessMovementInput()
        {
            float moveX = 0f;
            float moveY = 0f;

            // 获取方向输入
            if (Input.IsActionPressed(MoveLeftAction))
                moveX -= 1f;
            if (Input.IsActionPressed(MoveRightAction))
                moveX += 1f;
            if (Input.IsActionPressed(MoveUpAction))
                moveY -= 1f;
            if (Input.IsActionPressed(MoveDownAction))
                moveY += 1f;

            Vector2 newDirection = new Vector2(moveX, moveY).Normalized();

            // 检查移动方向是否发生变化
            bool directionChanged = newDirection != MoveDirection;

            // 更新移动方向
            if (newDirection != Vector2.Zero)
            {
                MoveDirection = newDirection;
                LastMoveDirection = newDirection;
            }
            else
            {
                MoveDirection = Vector2.Zero;
            }

            // 触发方向改变事件
            if (directionChanged)
            {
                OnMoveDirectionChanged?.Invoke(MoveDirection);
            }

            // 处理奔跑输入
            bool wasRunning = IsRunning;
            IsRunning = Input.IsActionPressed(RunAction);
            if (wasRunning != IsRunning)
            {
                OnRunStateChanged?.Invoke(IsRunning);
            }

            // 处理冲刺输入（按下瞬间触发）
            if (Input.IsActionJustPressed(DashAction))
            {
                IsDashing = true;
                OnDashPressed?.Invoke(LastMoveDirection);
            }
            else
            {
                IsDashing = false;
            }
        }

        /// <summary>
        /// 处理动作输入
        /// </summary>
        private void ProcessActionInput()
        {
            // 攻击输入（按下瞬间触发）
            if (Input.IsActionJustPressed(AttackAction))
            {
                IsAttacking = true;
                OnAttackPressed?.Invoke();
            }
            else
            {
                IsAttacking = false;
            }

            // 跳跃输入（按下瞬间触发）
            if (Input.IsActionJustPressed(JumpAction))
            {
                IsJumping = true;
                OnJumpPressed?.Invoke();
            }
            else
            {
                IsJumping = false;
            }
        }

        #endregion

        #region 公共方法

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
            if (Entity != null)
            {
                return (targetPoint - Entity.GlobalPosition).Normalized();
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
        /// 强制设置移动方向
        /// </summary>
        public void SetMoveDirection(Vector2 direction)
        {
            Vector2 normalizedDirection = direction.Normalized();
            MoveDirection = normalizedDirection;
            if (normalizedDirection != Vector2.Zero)
            {
                LastMoveDirection = normalizedDirection;
            }
            OnMoveDirectionChanged?.Invoke(normalizedDirection);
        }

        /// <summary>
        /// 模拟攻击输入
        /// </summary>
        public void SimulateAttack()
        {
            IsAttacking = true;
            OnAttackPressed?.Invoke();
        }

        /// <summary>
        /// 模拟冲刺输入
        /// </summary>
        public void SimulateDash(Vector2 direction)
        {
            IsDashing = true;
            OnDashPressed?.Invoke(direction.Normalized());
        }

        #endregion
    }
}