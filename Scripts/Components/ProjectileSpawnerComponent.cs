using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Projectiles;
using Logs;
using Entities; // 使用正确的Entities命名空间

namespace Components
{
    /// <summary>
    /// 投射物发射组件
    /// 通用的投射物发射系统，支持多种发射模式和轨迹类型
    /// </summary>
    public partial class ProjectileSpawnerComponent : LogicComponent
    {
        #region 字段和属性

        /// <summary>
        /// 发射点偏移（相对于实体中心）
        /// </summary>
        public Vector2 SpawnOffset { get; set; } = Vector2.Zero;

        /// <summary>
        /// 默认发射方向
        /// </summary>
        public Vector2 DefaultDirection { get; set; } = Vector2.Right;

        /// <summary>
        /// 是否使用实体的面向方向
        /// </summary>
        public bool UseEntityFacing { get; set; } = true;

        /// <summary>
        /// 对象池大小
        /// </summary>
        public int PoolSize { get; set; } = 50;

        /// <summary>
        /// 投射物数据列表
        /// </summary>
        public List<ProjectileData> ProjectileDatas { get; set; } = new();

        /// <summary>
        /// 当前使用的投射物数据
        /// </summary>
        public ProjectileData CurrentProjectileData { get; private set; }

        /// <summary>
        /// 是否正在发射中
        /// </summary>
        public bool IsSpawning { get; private set; } = false;

        #endregion

        #region 私有字段

        private Node2D _spawnPoint;
        private Dictionary<string, Queue<Node2D>> _projectilePools = new();
        private List<Node2D> _activeProjectiles = new();
        private Timer _spawnTimer;
        private int _currentSpawnIndex = 0;

        #endregion

        #region 事件

        /// <summary>
        /// 投射物发射事件
        /// </summary>
        public event Action<ProjectileSpawnEventArgs> OnProjectileSpawned;

        /// <summary>
        /// 投射物命中事件
        /// </summary>
        public event Action<ProjectileImpactEventArgs> OnProjectileImpacted;

        /// <summary>
        /// 发射完成事件
        /// </summary>
        public event Action OnSpawnComplete;

        #endregion

        #region 生命周期

        public override void Initialize(CharacterEntity entity)
        {
            base.Initialize(entity);
            
            SetupSpawnPoint();
            InitializePools();
            SetupTimer();
            
            Logger2.Info("ProjectileSpawnerComponent: 初始化完成");
        }

        public override void Update(float delta)
        {
            if (!IsEnabled) return;
            
            UpdateActiveProjectiles(delta);
        }

        public override void Cleanup()
        {
            base.Cleanup();
            
            ClearAllProjectiles();
            _projectilePools.Clear();
            _activeProjectiles.Clear();
            
            Logger2.Info("ProjectileSpawnerComponent: 清理完成");
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 设置发射点
        /// </summary>
        private void SetupSpawnPoint()
        {
            _spawnPoint = new Node2D();
            _spawnPoint.Name = "ProjectileSpawnPoint";
            Entity.AddChild(_spawnPoint);
            
            Logger2.Debug("ProjectileSpawnerComponent: 发射点设置完成");
        }

        /// <summary>
        /// 初始化对象池
        /// </summary>
        private void InitializePools()
        {
            foreach (var projectileData in ProjectileDatas)
            {
                if (projectileData.IsValid())
                {
                    InitializePoolForProjectile(projectileData);
                }
            }
        }

        /// <summary>
        /// 为特定投射物初始化对象池
        /// </summary>
        private void InitializePoolForProjectile(ProjectileData data)
        {
            var pool = new Queue<Node2D>();
            
            for (int i = 0; i < PoolSize; i++)
            {
                var projectile = CreateProjectileInstance(data);
                if (projectile != null)
                {
                    projectile.Visible = false;
                    Entity.AddChild(projectile);
                    pool.Enqueue(projectile);
                }
            }
            
            _projectilePools[data.ProjectileId] = pool;
            Logger2.Debug($"ProjectileSpawnerComponent: 初始化对象池 {data.Name} (数量: {pool.Count})");
        }

        /// <summary>
        /// 创建投射物实例
        /// </summary>
        private Node2D CreateProjectileInstance(ProjectileData data)
        {
            try
            {
                var prefab = GD.Load<PackedScene>(data.PrefabPath);
                if (prefab != null)
                {
                    var instance = prefab.Instantiate<Node2D>();
                    instance.Name = $"{data.ProjectileId}_Projectile";
                    
                    // 添加投射物组件（如果还没有的话）
                    if (!instance.HasMeta("ProjectileComponent"))
                    {
                        var projectileComponent = new ProjectileComponent();
                        instance.AddChild(projectileComponent);
                        instance.SetMeta("ProjectileComponent", projectileComponent);
                    }
                    
                    return instance;
                }
                else
                {
                    Logger2.Warn($"ProjectileSpawnerComponent: 无法加载投射物预制体 {data.PrefabPath}");
                }
            }
            catch (Exception ex)
            {
                Logger2.Error($"ProjectileSpawnerComponent: 创建投射物实例失败 - {ex.Message}");
            }
            
            return null;
        }

        /// <summary>
        /// 设置发射计时器
        /// </summary>
        private void SetupTimer()
        {
            _spawnTimer = new Timer();
            _spawnTimer.OneShot = true;
            _spawnTimer.Timeout += OnSpawnTimerTimeout;
            Entity.AddChild(_spawnTimer);
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 设置当前投射物类型
        /// </summary>
        public bool SetCurrentProjectile(string projectileId)
        {
            var data = ProjectileDatas.FirstOrDefault(p => p.ProjectileId == projectileId);
            if (data != null && data.IsValid())
            {
                CurrentProjectileData = data;
                Logger2.Info($"ProjectileSpawnerComponent: 设置当前投射物为 {data.Name}");
                return true;
            }
            
            Logger2.Warn($"ProjectileSpawnerComponent: 未找到投射物数据 {projectileId}");
            return false;
        }

        /// <summary>
        /// 发射投射物
        /// </summary>
        public bool SpawnProjectile(Vector2? targetPosition = null, Vector2? direction = null)
        {
            if (CurrentProjectileData == null)
            {
                Logger2.Warn("ProjectileSpawnerComponent: 未设置当前投射物数据");
                return false;
            }

            if (IsSpawning)
            {
                Logger2.Warn("ProjectileSpawnerComponent: 正在发射中，请等待完成");
                return false;
            }

            return SpawnProjectileAsync(CurrentProjectileData, targetPosition, direction).Result;
        }

        /// <summary>
        /// 发射指定类型的投射物
        /// </summary>
        public bool SpawnProjectile(string projectileId, Vector2? targetPosition = null, Vector2? direction = null)
        {
            var data = ProjectileDatas.FirstOrDefault(p => p.ProjectileId == projectileId);
            if (data == null || !data.IsValid())
            {
                Logger2.Warn($"ProjectileSpawnerComponent: 无效的投射物数据 {projectileId}");
                return false;
            }

            return SpawnProjectileAsync(data, targetPosition, direction).Result;
        }

        /// <summary>
        /// 异步发射投射物（核心方法）
        /// </summary>
        private async System.Threading.Tasks.Task<bool> SpawnProjectileAsync(ProjectileData data, Vector2? targetPosition = null, Vector2? direction = null)
        {
            if (!data.IsValid())
            {
                Logger2.Error("ProjectileSpawnerComponent: 投射物数据无效");
                return false;
            }

            // 计算发射参数
            var spawnParams = CalculateSpawnParameters(data, targetPosition, direction);
            
            switch (data.Pattern)
            {
                case SpawnPattern.Single:
                    return SpawnSingleProjectile(data, spawnParams);
                    
                case SpawnPattern.Spread:
                    return await SpawnSpreadProjectiles(data, spawnParams);
                    
                case SpawnPattern.Circle:
                    return SpawnCircleProjectiles(data, spawnParams);
                    
                case SpawnPattern.Cone:
                    return SpawnConeProjectiles(data, spawnParams);
                    
                case SpawnPattern.Volley:
                    return SpawnVolleyProjectiles(data, spawnParams);
                    
                case SpawnPattern.Rain:
                    return await SpawnRainProjectiles(data, spawnParams);
                    
                default:
                    return SpawnSingleProjectile(data, spawnParams);
            }
        }

        #endregion

        #region 发射模式实现

        /// <summary>
        /// 单发射击
        /// </summary>
        private bool SpawnSingleProjectile(ProjectileData data, SpawnParameters parameters)
        {
            var projectile = GetProjectileFromPool(data);
            if (projectile == null) return false;

            ConfigureProjectile(projectile, data, parameters.Direction, parameters.StartPosition);
            ActivateProjectile(projectile, data);
            
            OnProjectileSpawned?.Invoke(new ProjectileSpawnEventArgs
            {
                Projectile = projectile,
                Data = data,
                Position = parameters.StartPosition,
                Direction = parameters.Direction
            });
            
            return true;
        }

        /// <summary>
        /// 散射模式
        /// </summary>
        private async System.Threading.Tasks.Task<bool> SpawnSpreadProjectiles(ProjectileData data, SpawnParameters parameters)
        {
            IsSpawning = true;
            _currentSpawnIndex = 0;
            
            for (int i = 0; i < data.SpawnCount; i++)
            {
                var spreadAngle = data.GetRandomSpread();
                var spreadDirection = parameters.Direction.Rotated(spreadAngle);
                
                if (data.SpawnInterval > 0 && i > 0)
                {
                    // 延迟发射
                    _spawnTimer.Start(data.SpawnInterval * i);
                    await _spawnTimer.ToSignal(_spawnTimer, "timeout");
                }
                
                SpawnSingleProjectile(data, new SpawnParameters
                {
                    StartPosition = parameters.StartPosition,
                    Direction = spreadDirection
                });
                
                _currentSpawnIndex++;
            }
            
            IsSpawning = false;
            OnSpawnComplete?.Invoke();
            return true;
        }

        /// <summary>
        /// 圆形发射
        /// </summary>
        private bool SpawnCircleProjectiles(ProjectileData data, SpawnParameters parameters)
        {
            IsSpawning = true;
            
            float angleStep = 2 * Mathf.Pi / data.SpawnCount;
            
            for (int i = 0; i < data.SpawnCount; i++)
            {
                float angle = angleStep * i;
                var circleDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                
                SpawnSingleProjectile(data, new SpawnParameters
                {
                    StartPosition = parameters.StartPosition + circleDirection * data.CircleRadius,
                    Direction = circleDirection
                });
            }
            
            IsSpawning = false;
            OnSpawnComplete?.Invoke();
            return true;
        }

        /// <summary>
        /// 锥形发射
        /// </summary>
        private bool SpawnConeProjectiles(ProjectileData data, SpawnParameters parameters)
        {
            IsSpawning = true;
            
            float coneAngle = Mathf.DegToRad(data.SpreadAngle);
            float angleStep = coneAngle / Mathf.Max(1, data.SpawnCount - 1);
            float startAngle = -coneAngle / 2;
            
            for (int i = 0; i < data.SpawnCount; i++)
            {
                float currentAngle = startAngle + angleStep * i;
                var coneDirection = parameters.Direction.Rotated(currentAngle);
                
                SpawnSingleProjectile(data, new SpawnParameters
                {
                    StartPosition = parameters.StartPosition,
                    Direction = coneDirection
                });
            }
            
            IsSpawning = false;
            OnSpawnComplete?.Invoke();
            return true;
        }

        /// <summary>
        /// 齐射模式
        /// </summary>
        private bool SpawnVolleyProjectiles(ProjectileData data, SpawnParameters parameters)
        {
            // 类似散射，但同时发射所有投射物
            IsSpawning = true;
            
            for (int i = 0; i < data.SpawnCount; i++)
            {
                var spreadAngle = data.GetRandomSpread();
                var spreadDirection = parameters.Direction.Rotated(spreadAngle);
                
                SpawnSingleProjectile(data, new SpawnParameters
                {
                    StartPosition = parameters.StartPosition,
                    Direction = spreadDirection
                });
            }
            
            IsSpawning = false;
            OnSpawnComplete?.Invoke();
            return true;
        }

        /// <summary>
        /// 雨点式发射
        /// </summary>
        private async System.Threading.Tasks.Task<bool> SpawnRainProjectiles(ProjectileData data, SpawnParameters parameters)
        {
            // 从上方随机位置发射
            IsSpawning = true;
            
            var random = new Random();
            var spawnArea = new Rect2(-200, -300, 400, 100); // 随机生成区域
            
            for (int i = 0; i < data.SpawnCount; i++)
            {
                var randomX = (float)(random.NextDouble() * spawnArea.Size.X + spawnArea.Position.X);
                var startPosition = new Vector2(randomX, spawnArea.Position.Y);
                var rainDirection = Vector2.Down;
                
                if (data.SpawnInterval > 0)
                {
                    _spawnTimer.Start(data.SpawnInterval * i);
                    await _spawnTimer.ToSignal(_spawnTimer, "timeout");
                }
                
                SpawnSingleProjectile(data, new SpawnParameters
                {
                    StartPosition = startPosition,
                    Direction = rainDirection
                });
            }
            
            IsSpawning = false;
            OnSpawnComplete?.Invoke();
            return true;
        }

        #endregion

        #region 投射物管理

        /// <summary>
        /// 从对象池获取投射物
        /// </summary>
        private Node2D GetProjectileFromPool(ProjectileData data)
        {
            if (!_projectilePools.TryGetValue(data.ProjectileId, out var pool))
            {
                Logger2.Warn($"ProjectileSpawnerComponent: 未找到投射物池 {data.ProjectileId}");
                return null;
            }

            if (pool.Count > 0)
            {
                return pool.Dequeue();
            }
            else
            {
                // 池为空时创建新实例
                Logger2.Debug($"ProjectileSpawnerComponent: 对象池耗尽，创建新实例 {data.ProjectileId}");
                return CreateProjectileInstance(data);
            }
        }

        /// <summary>
        /// 回收投射物到对象池
        /// </summary>
        public void ReturnProjectileToPool(Node2D projectile, string projectileId)
        {
            if (projectile == null) return;
            
            // 重置投射物状态
            projectile.Visible = false;
            projectile.Position = Vector2.Zero;
            projectile.Rotation = 0;
            
            if (_projectilePools.TryGetValue(projectileId, out var pool))
            {
                pool.Enqueue(projectile);
                _activeProjectiles.Remove(projectile);
                
                Logger2.Debug($"ProjectileSpawnerComponent: 回收投射物 {projectileId}");
            }
            else
            {
                // 如果找不到对应的池，直接销毁
                projectile.QueueFree();
                _activeProjectiles.Remove(projectile);
                Logger2.Warn($"ProjectileSpawnerComponent: 无法回收投射物 {projectileId}，直接销毁");
            }
        }

        /// <summary>
        /// 配置投射物
        /// </summary>
        private void ConfigureProjectile(Node2D projectile, ProjectileData data, Vector2 direction, Vector2 startPosition)
        {
            if (projectile.HasMeta("ProjectileComponent"))
            {
                var componentVariant = projectile.GetMeta("ProjectileComponent");
                if (componentVariant.As<ProjectileComponent>() is ProjectileComponent component)
                {
                    component.Configure(data, Entity, direction, startPosition);
                }
            }
            
            projectile.Position = startPosition;
            projectile.Visible = true;
        }

        /// <summary>
        /// 激活投射物
        /// </summary>
        private void ActivateProjectile(Node2D projectile, ProjectileData data)
        {
            if (projectile.HasMeta("ProjectileComponent"))
            {
                var componentVariant = projectile.GetMeta("ProjectileComponent");
                if (componentVariant.As<ProjectileComponent>() is ProjectileComponent component)
                {
                    component.Activate();
                }
            }
            
            _activeProjectiles.Add(projectile);
        }

        /// <summary>
        /// 更新激活的投射物
        /// </summary>
        private void UpdateActiveProjectiles(float delta)
        {
            for (int i = _activeProjectiles.Count - 1; i >= 0; i--)
            {
                var projectile = _activeProjectiles[i];
                if (projectile == null || !GodotObject.IsInstanceValid(projectile))
                {
                    _activeProjectiles.RemoveAt(i);
                    continue;
                }
                
                // 检查投射物组件状态
                if (projectile.HasMeta("ProjectileComponent"))
                {
                    var componentVariant = projectile.GetMeta("ProjectileComponent");
                    if (componentVariant.As<ProjectileComponent>() is ProjectileComponent component && component.IsExpired)
                    {
                        ReturnProjectileToPool(projectile, component.Data?.ProjectileId ?? "");
                    }
                }
            }
        }

        /// <summary>
        /// 清理所有投射物
        /// </summary>
        private void ClearAllProjectiles()
        {
            foreach (var projectile in _activeProjectiles)
            {
                if (GodotObject.IsInstanceValid(projectile))
                {
                    projectile.QueueFree();
                }
            }
            _activeProjectiles.Clear();
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 计算发射参数
        /// </summary>
        private SpawnParameters CalculateSpawnParameters(ProjectileData data, Vector2? targetPosition, Vector2? direction)
        {
            var parameters = new SpawnParameters();
            
            // 计算发射位置
            parameters.StartPosition = Entity.GlobalPosition + SpawnOffset;
            
            // 计算发射方向
            if (direction.HasValue)
            {
                parameters.Direction = direction.Value.Normalized();
            }
            else if (targetPosition.HasValue)
            {
                parameters.Direction = (targetPosition.Value - parameters.StartPosition).Normalized();
            }
            else if (UseEntityFacing && Entity is Node2D node2D)
            {
                parameters.Direction = new Vector2(Mathf.Cos(node2D.Rotation), Mathf.Sin(node2D.Rotation));
            }
            else
            {
                parameters.Direction = DefaultDirection.Normalized();
            }
            
            // 应用瞄准误差
            if (data.AimErrorAngle > 0)
            {
                var errorAngle = (float)(new Random().NextDouble() * data.AimErrorAngle - data.AimErrorAngle / 2);
                errorAngle = Mathf.DegToRad(errorAngle);
                parameters.Direction = parameters.Direction.Rotated(errorAngle);
            }
            
            return parameters;
        }

        /// <summary>
        /// 计时器超时回调
        /// </summary>
        private void OnSpawnTimerTimeout()
        {
            // 计时器超时处理
        }

        #endregion
    }

    #region 数据结构

    /// <summary>
    /// 发射参数结构
    /// </summary>
    public struct SpawnParameters
    {
        public Vector2 StartPosition;
        public Vector2 Direction;
    }

    /// <summary>
    /// 投射物发射事件参数
    /// </summary>
    public class ProjectileSpawnEventArgs
    {
        public Node2D Projectile { get; set; }
        public ProjectileData Data { get; set; }
        public Vector2 Position { get; set; }
        public Vector2 Direction { get; set; }
    }

    /// <summary>
    /// 投射物命中事件参数
    /// </summary>
    public class ProjectileImpactEventArgs
    {
        public Node2D Projectile { get; set; }
        public ProjectileData Data { get; set; }
        public Node2D Target { get; set; }
        public Vector2 ImpactPosition { get; set; }
        public bool IsDestroyed { get; set; }
    }

    #endregion
}