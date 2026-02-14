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
            Logger2.Info($"InputHandlerComponent: 组件初始化完成 - Entity: {entity?.Name ?? "null"}");
        }

        public override void Start()
        {
            base.Start();
            Logger2.Info("InputHandlerComponent: 组件启动完成");
            LogCurrentConfiguration();
        }

        public override void Update(float delta)
        {
            if (!IsEnabled || !InputEnabled) 
            {
                if (!IsEnabled)
                    Logger2.Debug("InputHandlerComponent: 组件被禁用，跳过更新");
                if (!InputEnabled)
                    Logger2.Debug("InputHandlerComponent: 输入处理被禁用，跳过更新");
                return;
            }

            ProcessMovementInput();
            ProcessActionInput();
        }

        public override void Cleanup()
        {
            Logger2.Info("InputHandlerComponent: 开始清理组件");
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

            // 记录输入状态变化
            if (directionChanged)
            {
                Logger2.Debug($"InputHandlerComponent: 移动方向改变 - 从 {MoveDirection} 到 {newDirection}");
            }

            // 更新移动方向
            if (newDirection != Vector2.Zero)
            {
                MoveDirection = newDirection;
                LastMoveDirection = newDirection;
                if (directionChanged)
                {
                    Logger2.Debug($"InputHandlerComponent: 更新移动方向 - 当前: {MoveDirection}, 最后: {LastMoveDirection}");
                }
            }
            else
            {
                MoveDirection = Vector2.Zero;
            }

            // 触发方向改变事件
            if (directionChanged)
            {
                Logger2.Debug($"InputHandlerComponent: 触发OnMoveDirectionChanged事件 - 方向: {MoveDirection}");
                OnMoveDirectionChanged?.Invoke(MoveDirection);
            }

            // 处理奔跑输入
            bool wasRunning = IsRunning;
            IsRunning = Input.IsActionPressed(RunAction);
            if (wasRunning != IsRunning)
            {
                Logger2.Debug($"InputHandlerComponent: 奔跑状态改变 - 从 {wasRunning} 到 {IsRunning}");
                OnRunStateChanged?.Invoke(IsRunning);
            }

            // 处理冲刺输入（按下瞬间触发）
            if (Input.IsActionJustPressed(DashAction))
            {
                IsDashing = true;
                Logger2.Debug($"InputHandlerComponent: 冲刺输入触发 - 方向: {LastMoveDirection}");
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
                Logger2.Debug("InputHandlerComponent: 攻击输入触发");
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
                Logger2.Debug("InputHandlerComponent: 跳跃输入触发");
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
            float angle = LastMoveDirection.Angle();
            Logger2.Debug($"InputHandlerComponent: 获取移动角度 - 方向: {LastMoveDirection}, 角度: {angle} rad");
            return angle;
        }

        /// <summary>
        /// 获取方向索引（0-7，俯视角8方向）
        /// </summary>
        public int GetDirectionIndex()
        {
            if (LastMoveDirection == Vector2.Zero)
            {
                Logger2.Debug("InputHandlerComponent: 获取方向索引 - 无移动方向，默认返回0");
                return 0; // 默认向右
            }

            float angle = LastMoveDirection.Angle();
            if (angle < 0) angle += 2 * Mathf.Pi;

            int index = Mathf.RoundToInt(angle / (2 * Mathf.Pi / 8)) % 8;
            Logger2.Debug($"InputHandlerComponent: 获取方向索引 - 方向: {LastMoveDirection}, 角度: {angle:F3} rad, 索引: {index}");
            return index;
        }

        /// <summary>
        /// 获取朝向指定点的方向
        /// </summary>
        public Vector2 GetDirectionToPoint(Vector2 targetPoint)
        {
            if (Entity != null)
            {
                Vector2 direction = (targetPoint - Entity.GlobalPosition).Normalized();
                Logger2.Debug($"InputHandlerComponent: 计算指向点的方向 - 当前位置: {Entity.GlobalPosition}, 目标点: {targetPoint}, 方向: {direction}");
                return direction;
            }
            
            Logger2.Warn("InputHandlerComponent: 无法计算指向点方向 - Entity为空");
            return Vector2.Zero;
        }

        /// <summary>
        /// 重置所有输入状态
        /// </summary>
        public void ResetInputs()
        {
            Logger2.Debug($"InputHandlerComponent: 重置所有输入状态 - 之前状态: MoveDir={MoveDirection}, Attacking={IsAttacking}, Running={IsRunning}, Jumping={IsJumping}, Dashing={IsDashing}");
            
            MoveDirection = Vector2.Zero;
            IsAttacking = false;
            IsRunning = false;
            IsJumping = false;
            IsDashing = false;
            
            Logger2.Debug("InputHandlerComponent: 输入状态重置完成");
        }

        /// <summary>
        /// 强制设置移动方向
        /// </summary>
        public void SetMoveDirection(Vector2 direction)
        {
            Vector2 normalizedDirection = direction.Normalized();
            Vector2 oldDirection = MoveDirection;
            
            MoveDirection = normalizedDirection;
            if (normalizedDirection != Vector2.Zero)
            {
                LastMoveDirection = normalizedDirection;
            }
            
            Logger2.Debug($"InputHandlerComponent: 强制设置移动方向 - 从 {oldDirection} 设置为 {normalizedDirection}");
            OnMoveDirectionChanged?.Invoke(normalizedDirection);
        }

        /// <summary>
        /// 模拟攻击输入
        /// </summary>
        public void SimulateAttack()
        {
            Logger2.Debug("InputHandlerComponent: 模拟攻击输入");
            IsAttacking = true;
            OnAttackPressed?.Invoke();
        }

        /// <summary>
        /// 模拟冲刺输入
        /// </summary>
        public void SimulateDash(Vector2 direction)
        {
            Vector2 normalizedDir = direction.Normalized();
            Logger2.Debug($"InputHandlerComponent: 模拟冲刺输入 - 方向: {normalizedDir}");
            IsDashing = true;
            OnDashPressed?.Invoke(normalizedDir);
        }

        /// <summary>
        /// 模拟移动输入（用于测试）
        /// </summary>
        public void SimulateMovement(Vector2 direction)
        {
            Vector2 normalizedDirection = direction.Normalized();
            
            // 直接设置移动方向
            MoveDirection = normalizedDirection;
            if (normalizedDirection != Vector2.Zero)
            {
                LastMoveDirection = normalizedDirection;
            }
            
            Logger2.Debug($"InputHandlerComponent: 模拟移动输入 - 方向: {normalizedDirection}");
            
            // 触发移动事件
            OnMoveDirectionChanged?.Invoke(normalizedDirection);
        }

        /// <summary>
        /// 记录当前配置信息
        /// </summary>
        private void LogCurrentConfiguration()
        {
            Logger2.Debug($"InputHandlerComponent: 当前配置 - " +
                         $"MoveActions: [{MoveLeftAction}, {MoveRightAction}, {MoveUpAction}, {MoveDownAction}], " +
                         $"OtherActions: [Attack={AttackAction}, Run={RunAction}, Jump={JumpAction}, Dash={DashAction}], " +
                         $"InputEnabled: {InputEnabled}");
        }

        /// <summary>
        /// 获取当前输入状态摘要
        /// </summary>
        public string GetInputStateSummary()
        {
            return $"MoveDir:{MoveDirection}, LastDir:{LastMoveDirection}, " +
                   $"Moving:{IsMoving}, Attacking:{IsAttacking}, Running:{IsRunning}, " +
                   $"Jumping:{IsJumping}, Dashing:{IsDashing}";
        }

        #endregion
    }
}