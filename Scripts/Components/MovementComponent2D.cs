using Godot;
using System.Collections.Generic;

namespace Components
{
    using Entities;
    using Godot;
    using System;

    /// <summary>
    /// 2D通用移动组件
    /// 适用于：玩家、敌人、投射物、召唤物、技能效果等所有需要移动的实体
    /// 支持多种移动模式和行为策略
    /// </summary>
    public class MovementComponent2D : LogicComponent
    {
        #region 基础移动属性

        /// <summary>
        /// 移动速度（单位/秒）
        /// </summary>
        public float Speed { get; set; } = 200f;

        /// <summary>
        /// 最大速度限制
        /// </summary>
        public float MaxSpeed { get; set; } = 500f;

        /// <summary>
        /// 加速度（单位/秒²）
        /// </summary>
        public float Acceleration { get; set; } = 1000f;

        /// <summary>
        /// 摩擦力/减速度（单位/秒²）
        /// </summary>
        public float Friction { get; set; } = 800f;

        /// <summary>
        /// 是否使用加速度系统（false则瞬间达到目标速度）
        /// </summary>
        public bool UseAcceleration { get; set; } = true;

        #endregion

        #region 移动模式配置

        /// <summary>
        /// 移动模式
        /// </summary>
        public MovementMode Mode { get; set; } = MovementMode.Free;

        /// <summary>
        /// 移动类型（影响碰撞行为）
        /// </summary>
        public MovementType MoveType { get; set; } = MovementType.CharacterBody;

        /// <summary>
        /// 方向约束模式
        /// </summary>
        public DirectionConstraint DirectionMode { get; set; } = DirectionConstraint.EightDirection;

        /// <summary>
        /// 8方向移动时是否归一化（避免斜向速度过快）
        /// </summary>
        public bool NormalizeDiagonalMovement { get; set; } = true;

        #endregion

        #region 旋转相关

        /// <summary>
        /// 是否自动旋转朝向移动方向
        /// </summary>
        public bool RotateToVelocity { get; set; } = false;

        /// <summary>
        /// 旋转速度（弧度/秒）
        /// </summary>
        public float RotationSpeed { get; set; } = 10f;

        /// <summary>
        /// 旋转偏移角度（用于调整精灵朝向）
        /// </summary>
        public float RotationOffset { get; set; } = 0f;

        /// <summary>
        /// 是否平滑旋转
        /// </summary>
        public bool SmoothRotation { get; set; } = true;

        #endregion

        #region 边界和限制

        /// <summary>
        /// 移动边界（空则不限制）
        /// </summary>
        public Rect2? MovementBounds { get; set; } = null;

        /// <summary>
        /// 边界处理模式
        /// </summary>
        public BoundsMode BoundsHandling { get; set; } = BoundsMode.Clamp;

        /// <summary>
        /// 反弹系数（用于Bounce模式）
        /// </summary>
        public float BounceCoefficient { get; set; } = 0.8f;

        #endregion

        #region 高级功能

        /// <summary>
        /// 是否受时间缩放影响
        /// </summary>
        public bool UseTimeScale { get; set; } = true;

        /// <summary>
        /// 自定义时间缩放
        /// </summary>
        public float CustomTimeScale { get; set; } = 1f;

        /// <summary>
        /// 移动优先级（用于碰撞推挤计算）
        /// </summary>
        public int MovementPriority { get; set; } = 0;

        /// <summary>
        /// 是否可被推动
        /// </summary>
        public bool CanBePushed { get; set; } = true;

        /// <summary>
        /// 推动阻力（0-1，0=完全不阻挡，1=完全阻挡）
        /// </summary>
        public float PushResistance { get; set; } = 0.5f;

        #endregion

        #region 私有字段

        private CharacterBody2D _body;
        private Vector2 _velocity = Vector2.Zero;
        private Vector2 _inputDirection = Vector2.Zero;
        private Vector2 _externalForce = Vector2.Zero; // 外部力（击退、爆炸等）
        private Vector2 _targetPosition = Vector2.Zero;
        private bool _hasTargetPosition = false;

        #endregion

        #region 公共属性（只读）

        /// <summary>
        /// 当前速度向量
        /// </summary>
        public Vector2 Velocity => _velocity;

        /// <summary>
        /// 当前速度大小
        /// </summary>
        public float CurrentSpeed => _velocity.Length();

        /// <summary>
        /// 移动方向（归一化）
        /// </summary>
        public Vector2 MoveDirection => _velocity.LengthSquared() > 0.01f ? _velocity.Normalized() : Vector2.Zero;

        /// <summary>
        /// 是否正在移动
        /// </summary>
        public bool IsMoving => _velocity.LengthSquared() > 1f;

        /// <summary>
        /// 朝向方向（用于动画系统）
        /// </summary>
        public Vector2 FacingDirection { get; private set; } = Vector2.Down;

        /// <summary>
        /// 4方向朝向
        /// </summary>
        public Direction4 Facing4 { get; private set; } = Direction4.Down;

        /// <summary>
        /// 8方向朝向
        /// </summary>
        public Direction8 Facing8 { get; private set; } = Direction8.Down;

        /// <summary>
        /// 上一帧的位置
        /// </summary>
        public Vector2 LastPosition { get; private set; }

        /// <summary>
        /// 本帧移动的距离
        /// </summary>
        public float DistanceMovedThisFrame { get; private set; }

        #endregion

        #region 事件

        /// <summary>
        /// 开始移动时触发
        /// </summary>
        public event Action OnMovementStarted;

        /// <summary>
        /// 停止移动时触发
        /// </summary>
        public event Action OnMovementStopped;

        /// <summary>
        /// 速度改变时触发（参数：新速度大小）
        /// </summary>
        public event Action<float> OnSpeedChanged;

        /// <summary>
        /// 方向改变时触发（参数：新方向向量）
        /// </summary>
        public event Action<Vector2> OnDirectionChanged;

        /// <summary>
        /// 碰撞到障碍物时触发（参数：碰撞信息）
        /// </summary>
        public event Action<KinematicCollision2D> OnCollision;

        /// <summary>
        /// 到达目标位置时触发
        /// </summary>
        public event Action OnReachedTarget;

        /// <summary>
        /// 触碰边界时触发（参数：触碰的边）
        /// </summary>
        public event Action<BoundsEdge> OnBoundsTouched;

        #endregion

        #region 生命周期

        public override void Initialize(CharacterEntity entity)
        {
            base.Initialize(entity);

            _body = entity as CharacterBody2D;
            if (_body == null)
            {
                GD.PrintErr($"MovementComponent2D 需要挂载在 CharacterBody2D 或其子类上！当前类型：{entity.GetType().Name}");
            }

            LastPosition = entity.GlobalPosition;
        }

        public override void PhysicsUpdate(float delta)
        {
            if (!IsEnabled || _body == null) return;

            // 保存上一帧状态
            bool wasMoving = IsMoving;
            Vector2 oldDirection = FacingDirection;
            LastPosition = _body.GlobalPosition;

            // 应用时间缩放
            float effectiveDelta = UseTimeScale ? delta * CustomTimeScale : delta;

            // 计算速度
            CalculateVelocity(effectiveDelta);

            // 应用移动
            ApplyMovement(effectiveDelta);

            // 处理边界
            if (MovementBounds.HasValue)
            {
                HandleBounds();
            }

            // 更新朝向
            UpdateFacing();

            // 计算移动距离
            DistanceMovedThisFrame = _body.GlobalPosition.DistanceTo(LastPosition);

            // 检查是否到达目标
            if (_hasTargetPosition)
            {
                CheckTargetReached();
            }

            // 触发事件
            TriggerEvents(wasMoving, oldDirection);
        }

        #endregion

        #region 公共方法 - 基础移动控制

        /// <summary>
        /// 设置移动输入方向（主要用于玩家和AI控制）
        /// </summary>
        /// <param name="direction">输入方向向量</param>
        public void SetInputDirection(Vector2 direction)
        {
            _inputDirection = ApplyDirectionConstraint(direction);
            _hasTargetPosition = false; // 清除目标位置模式
        }

        /// <summary>
        /// 移动（SetInputDirection的别名）
        /// </summary>
        public void Move(Vector2 direction)
        {
            SetInputDirection(direction);
        }

        /// <summary>
        /// 直接设置速度向量
        /// </summary>
        public void SetVelocity(Vector2 velocity)
        {
            _velocity = velocity;
            _inputDirection = Vector2.Zero;
            _hasTargetPosition = false;
        }

        /// <summary>
        /// 停止移动
        /// </summary>
        public void Stop()
        {
            _inputDirection = Vector2.Zero;
            _velocity = Vector2.Zero;
            _externalForce = Vector2.Zero;
            _hasTargetPosition = false;
        }

        #endregion

        #region 公共方法 - 高级移动控制

        /// <summary>
        /// 移动到指定世界坐标
        /// </summary>
        /// <param name="worldPosition">目标世界坐标</param>
        /// <param name="stopDistance">停止距离（到达此距离时停止）</param>
        public void MoveToPosition(Vector2 worldPosition, float stopDistance = 5f)
        {
            _targetPosition = worldPosition;
            _hasTargetPosition = true;

            Vector2 direction = (worldPosition - Entity.GlobalPosition);
            float distance = direction.Length();

            if (distance <= stopDistance)
            {
                Stop();
                OnReachedTarget?.Invoke();
                return;
            }

            SetInputDirection(direction.Normalized());
        }

        /// <summary>
        /// 朝向目标移动（但不设置目标，持续输入方向）
        /// </summary>
        public void MoveTowards(Vector2 worldPosition)
        {
            Vector2 direction = (worldPosition - Entity.GlobalPosition).Normalized();
            SetInputDirection(direction);
        }

        /// <summary>
        /// 沿指定方向移动指定距离
        /// </summary>
        public void MoveDistance(Vector2 direction, float distance)
        {
            Vector2 targetPos = Entity.GlobalPosition + direction.Normalized() * distance;
            MoveToPosition(targetPos);
        }

        /// <summary>
        /// 添加冲量（瞬间改变速度，用于击退、跳跃等）
        /// </summary>
        public void AddImpulse(Vector2 impulse)
        {
            _velocity += impulse;
        }

        /// <summary>
        /// 添加外部力（持续作用，用于风、传送带等）
        /// </summary>
        public void AddForce(Vector2 force)
        {
            _externalForce += force;
        }

        /// <summary>
        /// 设置外部力（替换当前外部力）
        /// </summary>
        public void SetExternalForce(Vector2 force)
        {
            _externalForce = force;
        }

        /// <summary>
        /// 清除外部力
        /// </summary>
        public void ClearExternalForce()
        {
            _externalForce = Vector2.Zero;
        }

        /// <summary>
        /// 击退效果
        /// </summary>
        /// <param name="direction">击退方向</param>
        /// <param name="force">击退力度</param>
        public void Knockback(Vector2 direction, float force)
        {
            _velocity = direction.Normalized() * force;
            _inputDirection = Vector2.Zero;
            _hasTargetPosition = false;
        }

        /// <summary>
        /// 从某个位置被击退
        /// </summary>
        public void KnockbackFrom(Vector2 sourcePosition, float force)
        {
            Vector2 direction = (Entity.GlobalPosition - sourcePosition).Normalized();
            Knockback(direction, force);
        }

        /// <summary>
        /// 传送到指定位置
        /// </summary>
        public void Teleport(Vector2 worldPosition)
        {
            if (_body != null)
            {
                _body.GlobalPosition = worldPosition;
                LastPosition = worldPosition;
            }
        }

        /// <summary>
        /// 推动实体（用于其他实体推动此实体）
        /// </summary>
        /// <param name="force">推力</param>
        /// <returns>实际移动的距离</returns>
        public float Push(Vector2 force)
        {
            if (!CanBePushed) return 0f;

            Vector2 pushVelocity = force * (1f - PushResistance);
            Vector2 oldPos = _body.GlobalPosition;

            _velocity += pushVelocity;

            return _body.GlobalPosition.DistanceTo(oldPos);
        }

        #endregion

        #region 公共方法 - 查询和工具

        /// <summary>
        /// 获取到目标的距离
        /// </summary>
        public float GetDistanceTo(Vector2 worldPosition)
        {
            return Entity.GlobalPosition.DistanceTo(worldPosition);
        }

        /// <summary>
        /// 获取到目标的方向
        /// </summary>
        public Vector2 GetDirectionTo(Vector2 worldPosition)
        {
            return (worldPosition - Entity.GlobalPosition).Normalized();
        }

        /// <summary>
        /// 预测N秒后的位置
        /// </summary>
        public Vector2 PredictPosition(float seconds)
        {
            return Entity.GlobalPosition + _velocity * seconds;
        }

        /// <summary>
        /// 检查是否在移动边界内
        /// </summary>
        public bool IsInBounds(Vector2 position)
        {
            if (!MovementBounds.HasValue) return true;
            return MovementBounds.Value.HasPoint(position);
        }

        /// <summary>
        /// 设置朝向（不改变移动方向）
        /// </summary>
        public void SetFacing(Vector2 direction)
        {
            if (direction.LengthSquared() > 0.01f)
            {
                FacingDirection = direction.Normalized();
                Facing4 = DirectionToEnum4(FacingDirection);
                Facing8 = DirectionToEnum8(FacingDirection);
            }
        }

        #endregion

        #region 私有方法 - 核心逻辑

        /// <summary>
        /// 计算速度
        /// </summary>
        private void CalculateVelocity(float delta)
        {
            Vector2 targetVelocity = _inputDirection * Speed;

            // 应用外部力
            if (_externalForce.LengthSquared() > 0.01f)
            {
                targetVelocity += _externalForce;
            }

            if (UseAcceleration)
            {
                // 使用加速度系统
                if (_inputDirection.LengthSquared() > 0.01f || _externalForce.LengthSquared() > 0.01f)
                {
                    // 加速到目标速度
                    _velocity = _velocity.MoveToward(targetVelocity, Acceleration * delta);
                }
                else
                {
                    // 摩擦减速
                    _velocity = _velocity.MoveToward(Vector2.Zero, Friction * delta);
                }
            }
            else
            {
                // 直接设置速度
                _velocity = targetVelocity;
            }

            // 限制最大速度
            if (_velocity.Length() > MaxSpeed)
            {
                _velocity = _velocity.Normalized() * MaxSpeed;
            }

            // 外部力逐渐衰减
            _externalForce = _externalForce.MoveToward(Vector2.Zero, Friction * delta);
        }

        /// <summary>
        /// 应用移动
        /// </summary>
        private void ApplyMovement(float delta)
        {
            if (_velocity.LengthSquared() < 0.01f) return;

            switch (MoveType)
            {
                case MovementType.Simple:
                    // 简单位置移动，无碰撞
                    Entity.GlobalPosition += _velocity * delta;
                    break;

                case MovementType.CharacterBody:
                    // 使用CharacterBody2D的碰撞系统
                    _body.Velocity = _velocity;
                    _body.MoveAndSlide();

                    // 同步速度（受碰撞影响）
                    _velocity = _body.Velocity;

                    // 处理碰撞
                    for (int i = 0; i < _body.GetSlideCollisionCount(); i++)
                    {
                        var collision = _body.GetSlideCollision(i);
                        OnCollision?.Invoke(collision);
                    }
                    break;

                case MovementType.Interpolated:
                    // 平滑插值移动
                    Vector2 targetPos = Entity.GlobalPosition + _velocity * delta;
                    Entity.GlobalPosition = Entity.GlobalPosition.Lerp(targetPos, 0.5f);
                    break;
            }

            // 应用旋转
            if (RotateToVelocity && _velocity.LengthSquared() > 0.01f)
            {
                float targetRotation = _velocity.Angle() + RotationOffset;

                if (SmoothRotation)
                {
                    Entity.Rotation = Mathf.LerpAngle(Entity.Rotation, targetRotation, RotationSpeed * delta);
                }
                else
                {
                    Entity.Rotation = targetRotation;
                }
            }
        }

        /// <summary>
        /// 处理移动边界
        /// </summary>
        private void HandleBounds()
        {
            if (!MovementBounds.HasValue) return;

            Rect2 bounds = MovementBounds.Value;
            Vector2 pos = Entity.GlobalPosition;
            bool hitBounds = false;
            BoundsEdge edge = BoundsEdge.None;

            switch (BoundsHandling)
            {
                case BoundsMode.Clamp:
                    // 限制在边界内
                    if (pos.X < bounds.Position.X)
                    {
                        pos.X = bounds.Position.X;
                        hitBounds = true;
                        edge = BoundsEdge.Left;
                        _velocity.X = 0;
                    }
                    else if (pos.X > bounds.Position.X + bounds.Size.X)
                    {
                        pos.X = bounds.Position.X + bounds.Size.X;
                        hitBounds = true;
                        edge = BoundsEdge.Right;
                        _velocity.X = 0;
                    }

                    if (pos.Y < bounds.Position.Y)
                    {
                        pos.Y = bounds.Position.Y;
                        hitBounds = true;
                        edge |= BoundsEdge.Top;
                        _velocity.Y = 0;
                    }
                    else if (pos.Y > bounds.Position.Y + bounds.Size.Y)
                    {
                        pos.Y = bounds.Position.Y + bounds.Size.Y;
                        hitBounds = true;
                        edge |= BoundsEdge.Bottom;
                        _velocity.Y = 0;
                    }

                    Entity.GlobalPosition = pos;
                    break;

                case BoundsMode.Bounce:
                    // 反弹
                    if (pos.X < bounds.Position.X || pos.X > bounds.Position.X + bounds.Size.X)
                    {
                        _velocity.X *= -BounceCoefficient;
                        pos.X = Mathf.Clamp(pos.X, bounds.Position.X, bounds.Position.X + bounds.Size.X);
                        hitBounds = true;
                        edge = pos.X <= bounds.Position.X ? BoundsEdge.Left : BoundsEdge.Right;
                    }

                    if (pos.Y < bounds.Position.Y || pos.Y > bounds.Position.Y + bounds.Size.Y)
                    {
                        _velocity.Y *= -BounceCoefficient;
                        pos.Y = Mathf.Clamp(pos.Y, bounds.Position.Y, bounds.Position.Y + bounds.Size.Y);
                        hitBounds = true;
                        edge |= pos.Y <= bounds.Position.Y ? BoundsEdge.Top : BoundsEdge.Bottom;
                    }

                    Entity.GlobalPosition = pos;
                    break;

                case BoundsMode.Wrap:
                    // 穿越到对面
                    if (pos.X < bounds.Position.X)
                        pos.X = bounds.Position.X + bounds.Size.X;
                    else if (pos.X > bounds.Position.X + bounds.Size.X)
                        pos.X = bounds.Position.X;

                    if (pos.Y < bounds.Position.Y)
                        pos.Y = bounds.Position.Y + bounds.Size.Y;
                    else if (pos.Y > bounds.Position.Y + bounds.Size.Y)
                        pos.Y = bounds.Position.Y;

                    Entity.GlobalPosition = pos;
                    break;

                case BoundsMode.Destroy:
                    // 离开边界时销毁实体
                    if (!bounds.HasPoint(pos))
                    {
                        Entity.Destroy();
                    }
                    break;
            }

            if (hitBounds)
            {
                OnBoundsTouched?.Invoke(edge);
            }
        }

        /// <summary>
        /// 更新朝向
        /// </summary>
        private void UpdateFacing()
        {
            // 只在有输入或移动时更新朝向
            if (_inputDirection.LengthSquared() > 0.01f)
            {
                FacingDirection = _inputDirection.Normalized();
            }
            else if (_velocity.LengthSquared() > 0.01f)
            {
                FacingDirection = _velocity.Normalized();
            }

            // 更新枚举朝向
            Facing4 = DirectionToEnum4(FacingDirection);
            Facing8 = DirectionToEnum8(FacingDirection);
        }

        /// <summary>
        /// 检查是否到达目标
        /// </summary>
        private void CheckTargetReached()
        {
            float distance = Entity.GlobalPosition.DistanceTo(_targetPosition);
            if (distance <= 5f) // 默认停止距离
            {
                Stop();
                OnReachedTarget?.Invoke();
            }
        }

        /// <summary>
        /// 触发相关事件
        /// </summary>
        private void TriggerEvents(bool wasMoving, Vector2 oldDirection)
        {
            bool isMovingNow = IsMoving;

            // 移动状态改变
            if (isMovingNow && !wasMoving)
            {
                OnMovementStarted?.Invoke();
            }
            else if (!isMovingNow && wasMoving)
            {
                OnMovementStopped?.Invoke();
            }

            // 速度改变
            if (wasMoving || isMovingNow)
            {
                OnSpeedChanged?.Invoke(CurrentSpeed);
            }

            // 方向改变（角度差超过5度）
            if (oldDirection.AngleTo(FacingDirection) > Mathf.DegToRad(5))
            {
                OnDirectionChanged?.Invoke(FacingDirection);
            }
        }

        #endregion

        #region 私有方法 - 辅助工具

        /// <summary>
        /// 应用方向约束
        /// </summary>
        private Vector2 ApplyDirectionConstraint(Vector2 direction)
        {
            if (direction.LengthSquared() < 0.01f) return Vector2.Zero;

            switch (DirectionMode)
            {
                case DirectionConstraint.FourDirection:
                    // 限制为4方向
                    if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
                        return new Vector2(Mathf.Sign(direction.X), 0);
                    else
                        return new Vector2(0, Mathf.Sign(direction.Y));

                case DirectionConstraint.EightDirection:
                    // 限制为8方向
                    Vector2 dir8 = new Vector2(
                        Mathf.Abs(direction.X) > 0.5f ? Mathf.Sign(direction.X) : 0,
                        Mathf.Abs(direction.Y) > 0.5f ? Mathf.Sign(direction.Y) : 0
                    );

                    if (NormalizeDiagonalMovement && dir8.LengthSquared() > 1.01f)
                        return dir8.Normalized();

                    return dir8;

                case DirectionConstraint.Free:
                default:
                    // 自由方向
                    return direction.Normalized();
            }
        }

        /// <summary>
        /// 向量转4方向枚举
        /// </summary>
        private Direction4 DirectionToEnum4(Vector2 direction)
        {
            if (direction.LengthSquared() < 0.01f) return Facing4; // 保持上次朝向

            if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
                return direction.X > 0 ? Direction4.Right : Direction4.Left;
            else
                return direction.Y > 0 ? Direction4.Down : Direction4.Up;
        }

        /// <summary>
        /// 向量转8方向枚举
        /// </summary>
        private Direction8 DirectionToEnum8(Vector2 direction)
        {
            if (direction.LengthSquared() < 0.01f) return Facing8; // 保持上次朝向

            float angle = direction.Angle();
            angle = Mathf.PosMod(angle, Mathf.Tau);
            int index = Mathf.RoundToInt(angle / (Mathf.Tau / 8f)) % 8;

            return index switch
            {
                0 => Direction8.Right,
                1 => Direction8.DownRight,
                2 => Direction8.Down,
                3 => Direction8.DownLeft,
                4 => Direction8.Left,
                5 => Direction8.UpLeft,
                6 => Direction8.Up,
                7 => Direction8.UpRight,
                _ => Direction8.Down
            };
        }

        #endregion
    }

    #region 枚举定义

    /// <summary>
    /// 移动模式
    /// </summary>
    public enum MovementMode
    {
        /// <summary>
        /// 手动控制（默认）
        /// </summary>
        Manual,

        /// <summary>
        /// 自由移动
        /// </summary>
        Free,

        /// <summary>
        /// 跟随目标
        /// </summary>
        Follow,

        /// <summary>
        /// 巡逻
        /// </summary>
        Patrol,

        /// <summary>
        /// 环绕
        /// </summary>
        Orbit
    }

    /// <summary>
    /// 移动类型（决定碰撞行为）
    /// </summary>
    public enum MovementType
    {
        /// <summary>
        /// 简单移动（无碰撞检测）
        /// </summary>
        Simple,

        /// <summary>
        /// 使用CharacterBody2D的碰撞系统
        /// </summary>
        CharacterBody,

        /// <summary>
        /// 插值平滑移动
        /// </summary>
        Interpolated
    }

    /// <summary>
    /// 方向约束
    /// </summary>
    public enum DirectionConstraint
    {
        /// <summary>
        /// 4方向（上下左右）
        /// </summary>
        FourDirection,

        /// <summary>
        /// 8方向（包含斜向）
        /// </summary>
        EightDirection,

        /// <summary>
        /// 自由方向（360度）
        /// </summary>
        Free
    }

    /// <summary>
    /// 边界处理模式
    /// </summary>
    public enum BoundsMode
    {
        /// <summary>
        /// 限制在边界内
        /// </summary>
        Clamp,

        /// <summary>
        /// 碰到边界反弹
        /// </summary>
        Bounce,

        /// <summary>
        /// 穿越到对面（如贪吃蛇）
        /// </summary>
        Wrap,

        /// <summary>
        /// 离开边界时销毁
        /// </summary>
        Destroy
    }

    /// <summary>
    /// 边界边缘（可组合标志）
    /// </summary>
    [Flags]
    public enum BoundsEdge
    {
        None = 0,
        Left = 1 << 0,
        Right = 1 << 1,
        Top = 1 << 2,
        Bottom = 1 << 3
    }

    /// <summary>
    /// 4方向
    /// </summary>
    public enum Direction4
    {
        Up,
        Down,
        Left,
        Right
    }

    /// <summary>
    /// 8方向
    /// </summary>
    public enum Direction8
    {
        Up,
        UpRight,
        Right,
        DownRight,
        Down,
        DownLeft,
        Left,
        UpLeft
    }

    #endregion
}