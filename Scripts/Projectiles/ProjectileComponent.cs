using Godot;
using System;
using Components;
using Logs;

namespace Projectiles
{
    /// <summary>
    /// 投射物组件
    /// 处理单个投射物的行为、物理模拟和生命周期管理
    /// </summary>
    public partial class ProjectileComponent : Node2D
    {
        #region 字段和属性

        /// <summary>
        /// 投射物数据
        /// </summary>
        public ProjectileData Data { get; private set; }

        /// <summary>
        /// 发射者
        /// </summary>
        public Node2D Caster { get; private set; }

        /// <summary>
        /// 当前速度
        /// </summary>
        public Vector2 Velocity { get; private set; }

        /// <summary>
        /// 当前加速度
        /// </summary>
        public Vector2 Acceleration { get; private set; }

        /// <summary>
        /// 是否已激活
        /// </summary>
        public bool IsActive { get; private set; } = false;

        /// <summary>
        /// 是否已过期（需要回收）
        /// </summary>
        public bool IsExpired { get; private set; } = false;

        /// <summary>
        /// 已穿透的目标数量
        /// </summary>
        public int PenetrationCount { get; private set; } = 0;

        /// <summary>
        /// 生存时间计时器
        /// </summary>
        private float _lifetimeTimer = 0f;

        /// <summary>
        /// 爆炸延迟计时器
        /// </summary>
        private float _explosionTimer = 0f;

        /// <summary>
        /// 碰撞形状
        /// </summary>
        private CollisionShape2D _collisionShape;

        /// <summary>
        /// 碰撞体
        /// </summary>
        private CollisionObject2D _collisionBody;

        /// <summary>
        /// 视觉效果节点
        /// </summary>
        private Node2D _visualNode;

        /// <summary>
        /// 已命中的目标列表（避免重复命中）
        /// </summary>
        private Godot.Collections.Array<Node2D> _hitTargets = new();

        #endregion

        #region 事件

        /// <summary>
        /// 投射物命中事件
        /// </summary>
        public event Action<ProjectileImpactEventArgs> OnImpact;

        /// <summary>
        /// 投射物过期事件
        /// </summary>
        public event Action<ProjectileComponent> OnExpired;

        /// <summary>
        /// 爆炸事件
        /// </summary>
        public event Action<Vector2> OnExplode;

        #endregion

        #region 生命周期

        public override void _Ready()
        {
            SetupCollision();
            SetupVisuals();
        }

        public override void _Process(double delta)
        {
            if (!IsActive || IsExpired) return;

            float deltaTime = (float)delta;
            
            UpdatePhysics(deltaTime);
            UpdateLifetime(deltaTime);
            UpdateExplosionTimer(deltaTime);
            CheckBounds();
        }

        public override void _ExitTree()
        {
            Cleanup();
        }

        #endregion

        #region 初始化和配置

        /// <summary>
        /// 配置投射物
        /// </summary>
        public void Configure(ProjectileData data, Node2D caster, Vector2 direction, Vector2 startPosition)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Caster = caster;
            
            // 设置初始位置和方向
            GlobalPosition = startPosition;
            Velocity = direction * data.InitialSpeed;
            
            // 设置碰撞形状
            SetupCollisionShape();
            
            // 重置状态
            ResetState();
            
            Logger2.Debug($"ProjectileComponent: 配置投射物 {data.Name}");
        }

        /// <summary>
        /// 激活投射物
        /// </summary>
        public void Activate()
        {
            if (Data == null)
            {
                Logger2.Error("ProjectileComponent: 投射物未配置");
                return;
            }

            IsActive = true;
            IsExpired = false;
            _lifetimeTimer = 0f;
            _explosionTimer = 0f;
            
            // 应用初始效果
            ApplyInitialEffects();
            
            Logger2.Debug($"ProjectileComponent: 激活投射物 {Data.Name}");
        }

        /// <summary>
        /// 重置状态
        /// </summary>
        private void ResetState()
        {
            IsActive = false;
            IsExpired = false;
            PenetrationCount = 0;
            _lifetimeTimer = 0f;
            _explosionTimer = 0f;
            _hitTargets.Clear();
            Velocity = Vector2.Zero;
            Acceleration = Vector2.Zero;
        }

        /// <summary>
        /// 设置碰撞体
        /// </summary>
        private void SetupCollision()
        {
            // 创建区域检测（用于触发器）
            var area = new Area2D();
            area.Name = "ProjectileArea";
            AddChild(area);
            
            // 设置碰撞层和遮罩
            area.CollisionLayer = 1 << 1; // 投射物层
            area.CollisionMask = 1 << 0 | 1 << 2; // 玩家层 + 环境层
            
            // 连接碰撞事件
            area.AreaEntered += OnAreaEntered;
            area.BodyEntered += OnBodyEntered;
            
            _collisionBody = area;
        }

        /// <summary>
        /// 设置碰撞形状
        /// </summary>
        private void SetupCollisionShape()
        {
            if (_collisionShape != null)
            {
                _collisionShape.QueueFree();
            }

            _collisionShape = new CollisionShape2D();
            _collisionShape.Name = "CollisionShape";
            
            // 使用Godot的Shape类型
            Godot.Shape2D shape = Data.Shape switch
            {
                CollisionShape.Circle => new CircleShape2D { Radius = Data.CollisionRadius },
                CollisionShape.Rectangle => new RectangleShape2D { Size = new Vector2(Data.CollisionRadius * 2, Data.CollisionRadius * 2) },
                CollisionShape.Polygon => new CapsuleShape2D { Height = Data.CollisionRadius * 2, Radius = Data.CollisionRadius },
                _ => new CircleShape2D { Radius = Data.CollisionRadius }
            };
            
            _collisionShape.Shape = shape;
            _collisionBody.AddChild(_collisionShape);
        }

        /// <summary>
        /// 设置视觉效果
        /// </summary>
        private void SetupVisuals()
        {
            // 这里可以根据Data中的特效路径加载视觉效果
            // 暂时创建一个简单的视觉表示
            _visualNode = new Node2D();
            _visualNode.Name = "Visuals";
            AddChild(_visualNode);
            
            // 添加一个简单的精灵作为占位符
            var sprite = new Sprite2D();
            sprite.Texture = GD.Load<Texture2D>("res://Assets/placeholder_projectile.png"); // 占位符路径
            _visualNode.AddChild(sprite);
        }

        #endregion

        #region 物理更新

        /// <summary>
        /// 更新物理状态
        /// </summary>
        private void UpdatePhysics(float deltaTime)
        {
            // 更新加速度
            UpdateAcceleration();
            
            // 更新速度
            Velocity += Acceleration * deltaTime;
            
            // 应用最大速度限制
            if (Velocity.Length() > Data.MaxSpeed)
            {
                Velocity = Velocity.Normalized() * Data.MaxSpeed;
            }
            
            // 更新位置
            Position += Velocity * deltaTime;
            
            // 更新旋转（面向运动方向）
            if (Velocity.Length() > 1f)
            {
                Rotation = Velocity.Angle();
            }
        }

        /// <summary>
        /// 更新加速度
        /// </summary>
        private void UpdateAcceleration()
        {
            Acceleration = Vector2.Zero;
            
            // 应用重力
            if (Data.GravityScale > 0)
            {
                Acceleration += Vector2.Down * 980f * Data.GravityScale; // 重力加速度
            }
            
            // 应用空气阻力
            if (Data.DragCoefficient > 0 && Velocity.Length() > 0)
            {
                var dragForce = -Velocity.Normalized() * Velocity.LengthSquared() * Data.DragCoefficient;
                Acceleration += dragForce / Data.Mass;
            }
            
            // 应用主动加速度
            if (Data.Acceleration != 0 && Velocity.Length() > 0)
            {
                Acceleration += Velocity.Normalized() * Data.Acceleration;
            }
        }

        #endregion

        #region 生命周期管理

        /// <summary>
        /// 更新生存时间
        /// </summary>
        private void UpdateLifetime(float deltaTime)
        {
            _lifetimeTimer += deltaTime;
            
            if (_lifetimeTimer >= Data.MaxLifetime)
            {
                Expire();
            }
        }

        /// <summary>
        /// 更新爆炸计时器
        /// </summary>
        private void UpdateExplosionTimer(float deltaTime)
        {
            if (!Data.HasExplosion || Data.ExplosionDelay <= 0) return;
            
            _explosionTimer += deltaTime;
            
            if (_explosionTimer >= Data.ExplosionDelay)
            {
                Explode();
            }
        }

        /// <summary>
        /// 检查边界
        /// </summary>
        private void CheckBounds()
        {
            // 简单的边界检查，超出屏幕一定距离则过期
            var viewport = GetViewportRect();
            var margin = 100f;
            var bounds = new Rect2(
                viewport.Position - Vector2.One * margin,
                viewport.Size + Vector2.One * margin * 2
            );
            
            if (!bounds.HasPoint(GlobalPosition))
            {
                Expire();
            }
        }

        /// <summary>
        /// 过期处理
        /// </summary>
        public void Expire()
        {
            if (IsExpired) return;
            
            IsExpired = true;
            IsActive = false;
            
            // 触发爆炸（如果设置了延时爆炸）
            if (Data.HasExplosion && Data.ExplosionDelay <= 0)
            {
                Explode();
            }
            
            OnExpired?.Invoke(this);
            Logger2.Debug($"ProjectileComponent: 投射物过期 {Data?.Name ?? "Unknown"}");
        }

        #endregion

        #region 碰撞处理

        /// <summary>
        /// 区域进入回调
        /// </summary>
        private void OnAreaEntered(Area2D area)
        {
            HandleCollision(area);
        }

        /// <summary>
        /// 刚体进入回调
        /// </summary>
        private void OnBodyEntered(Node2D body)
        {
            HandleCollision(body);
        }

        /// <summary>
        /// 处理碰撞
        /// </summary>
        private void HandleCollision(Node2D collider)
        {
            if (IsExpired || _hitTargets.Contains(collider)) return;
            
            // 检查是否可以命中该目标
            if (!CanHitTarget(collider)) return;
            
            // 记录命中目标
            _hitTargets.Add(collider);
            
            // 应用伤害和效果
            ApplyImpactEffects(collider);
            
            // 处理穿透
            if (Data.IsPenetrating && PenetrationCount < Data.MaxPenetrationCount)
            {
                PenetrationCount++;
                Logger2.Debug($"ProjectileComponent: 穿透目标，当前穿透次数: {PenetrationCount}");
            }
            else
            {
                // 非穿透投射物或达到最大穿透次数，过期
                Expire();
            }
            
            // 触发命中事件
            OnImpact?.Invoke(new ProjectileImpactEventArgs
            {
                Projectile = this,
                Data = Data,
                Target = collider,
                ImpactPosition = GlobalPosition,
                IsDestroyed = !Data.IsPenetrating || PenetrationCount >= Data.MaxPenetrationCount
            });
        }

        /// <summary>
        /// 检查是否可以命中目标
        /// </summary>
        private bool CanHitTarget(Node2D target)
        {
            // 避免命中发射者自己
            if (target == Caster) return false;
            
            // 可以添加更多的过滤逻辑，比如阵营检查等
            return true;
        }

        /// <summary>
        /// 应用命中效果
        /// </summary>
        private void ApplyImpactEffects(Node2D target)
        {
            // 应用直接伤害
            ApplyDirectDamage(target);
            
            // 应用状态效果
            ApplyStatusEffects(target);
            
            // 应用击退效果
            ApplyKnockback(target);
            
            // 播放命中特效
            PlayImpactEffects();
        }

        /// <summary>
        /// 应用直接伤害
        /// </summary>
        private void ApplyDirectDamage(Node2D target)
        {
            if (Data.BaseDamage <= 0) return;
            
            // 这里应该调用实际的伤害系统
            Logger2.Debug($"ProjectileComponent: 对 {target.Name} 造成 {Data.BaseDamage} 点 {Data.DamageType} 伤害");
        }

        /// <summary>
        /// 应用状态效果
        /// </summary>
        private void ApplyStatusEffects(Node2D target)
        {
            foreach (var effectData in Data.StatusEffects)
            {
                // 这里应该调用状态效果系统
                Logger2.Debug($"ProjectileComponent: 对 {target.Name} 应用状态效果 {effectData.EffectName}");
            }
        }

        /// <summary>
        /// 应用击退效果
        /// </summary>
        private void ApplyKnockback(Node2D target)
        {
            if (Data.KnockbackForce <= 0) return;
            
            // 这里应该应用实际的击退力
            Logger2.Debug($"ProjectileComponent: 对 {target.Name} 应用击退力 {Data.KnockbackForce}");
        }

        /// <summary>
        /// 播放命中特效
        /// </summary>
        private void PlayImpactEffects()
        {
            if (!string.IsNullOrEmpty(Data.ImpactEffectPath))
            {
                // 加载并播放命中特效
                Logger2.Debug($"ProjectileComponent: 播放命中特效 {Data.ImpactEffectPath}");
            }
            
            if (!string.IsNullOrEmpty(Data.ImpactSoundPath))
            {
                // 播放命中音效
                Logger2.Debug($"ProjectileComponent: 播放命中音效 {Data.ImpactSoundPath}");
            }
        }

        #endregion

        #region 爆炸处理

        /// <summary>
        /// 爆炸处理
        /// </summary>
        private void Explode()
        {
            if (!Data.HasExplosion) return;
            
            Logger2.Debug($"ProjectileComponent: 投射物爆炸，位置: {GlobalPosition}, 半径: {Data.ExplosionRadius}");
            
            // 应用范围伤害
            ApplyAoEDamage();
            
            // 播放爆炸特效
            PlayExplosionEffects();
            
            // 触发爆炸事件
            OnExplode?.Invoke(GlobalPosition);
            
            // 爆炸后过期
            Expire();
        }

        /// <summary>
        /// 应用范围伤害
        /// </summary>
        private void ApplyAoEDamage()
        {
            if (Data.ExplosionDamage <= 0 || Data.ExplosionRadius <= 0) return;
            
            // 获取范围内的所有目标
            var spaceState = GetWorld2D().DirectSpaceState;
            var query = new PhysicsShapeQueryParameters2D();
            query.Transform = new Transform2D(0, GlobalPosition);
            query.Shape = new CircleShape2D { Radius = Data.ExplosionRadius };
            query.CollisionMask = 1 << 0 | 1 << 2; // 玩家层 + 环境层
            
            var results = spaceState.IntersectShape(query);
            
            foreach (var result in results)
            {
                if (result.TryGetValue("collider", out var colliderObj))
                {
                    // 检查是否为Node2D类型
                    if (colliderObj.Obj is Node2D collider)
                    {
                        // 应用爆炸伤害
                        Logger2.Debug($"ProjectileComponent: 爆炸伤害 {Data.ExplosionDamage} 作用于 {collider.Name}");
                    }
                }
            }
        }

        /// <summary>
        /// 播放爆炸特效
        /// </summary>
        private void PlayExplosionEffects()
        {
            // 播放爆炸视觉效果和音效
            Logger2.Debug("ProjectileComponent: 播放爆炸特效和音效");
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 应用初始效果
        /// </summary>
        private void ApplyInitialEffects()
        {
            if (!string.IsNullOrEmpty(Data.FlightEffectPath))
            {
                // 加载并播放飞行特效
                Logger2.Debug($"ProjectileComponent: 播放飞行特效 {Data.FlightEffectPath}");
            }
            
            if (!string.IsNullOrEmpty(Data.FlightSoundPath))
            {
                // 播放飞行音效
                Logger2.Debug($"ProjectileComponent: 播放飞行音效 {Data.FlightSoundPath}");
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        private void Cleanup()
        {
            OnImpact = null;
            OnExpired = null;
            OnExplode = null;
            _hitTargets.Clear();
            
            Logger2.Debug($"ProjectileComponent: 清理完成 {Data?.Name ?? "Unknown"}");
        }

        #endregion
    }
}