using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Components;

namespace Entities
{

    using Godot;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// 游戏实体基类 - 管理纯逻辑组件
    /// 2D俯视角游戏专用
    /// </summary>
    public partial class CharacterEntity : CharacterBody2D
    {
        #region 实体属性

        /// <summary>
        /// 实体唯一ID
        /// </summary>
        public string EntityId { get; private set; }

        /// <summary>
        /// 实体类型
        /// </summary>
        public EntityType Type { get; set; } = EntityType.Other;

        /// <summary>
        /// 实体阵营
        /// </summary>
        public FactionType Faction { get; set; } = FactionType.Neutral;

        /// <summary>
        /// 实体是否已初始化
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// 实体是否已启动
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// 实体是否已销毁
        /// </summary>
        public bool IsDestroyed { get; private set; }

        #endregion

        #region 组件管理

        /// <summary>
        /// 组件缓存字典（按类型索引）
        /// </summary>
        private Dictionary<Type, IComponent> _componentCache = new();

        /// <summary>
        /// 所有组件列表（按添加顺序）
        /// </summary>
        private List<IComponent> _components = new();

        /// <summary>
        /// 等待初始化的组件队列
        /// </summary>
        private Queue<IComponent> _pendingComponents = new();

        /// <summary>
        /// 是否正在批量添加组件
        /// </summary>
        private bool _isBatchAdding = false;

        #endregion

        #region 生命周期

        public override void _Ready()
        {
            // 生成唯一ID
            EntityId = Guid.NewGuid().ToString();

            // 设置物理属性（俯视角游戏通用设置）
            MotionMode = MotionModeEnum.Floating; // 浮动模式，不受重力影响

            // 调用子类的设置方法
            OnSetup();

            // 初始化实体（处理所有已添加的组件）
            InitializeEntity();
        }

        /// <summary>
        /// 子类重写此方法来添加组件和配置实体
        /// 在_Ready中调用，在组件初始化之前
        /// </summary>
        protected virtual void OnSetup()
        {
            // 子类在这里添加组件
            // 例如：AddComponent<MovementComponent>();
        }

        /// <summary>
        /// 初始化实体（两阶段初始化）
        /// </summary>
        private void InitializeEntity()
        {
            if (IsInitialized)
            {
                GD.PrintErr($"实体 {Name} 已经初始化过了！");
                return;
            }

            try
            {
                // 处理等待队列中的组件
                while (_pendingComponents.Count > 0)
                {
                    var component = _pendingComponents.Dequeue();
                    _components.Add(component);
                }

                // 第一阶段：初始化所有组件
                foreach (var component in _components)
                {
                    if (component is LogicComponent logicComp && !logicComp.IsInitialized)
                    {
                        component.Initialize(this);
                    }
                }

                IsInitialized = true;

                // 第二阶段：启动所有组件（此时可以安全访问其他组件）
                foreach (var component in _components)
                {
                    if (component is LogicComponent logicComp && !logicComp.IsStarted)
                    {
                        component.Start();
                    }
                }

                IsStarted = true;

                // 调用实体启动回调
                OnEntityStarted();
            }
            catch (Exception ex)
            {
                GD.PrintErr($"实体 {Name} 初始化失败: {ex.Message}");
                GD.PrintErr(ex.StackTrace);
            }
        }

        /// <summary>
        /// 实体启动完成后的回调
        /// </summary>
        protected virtual void OnEntityStarted()
        {
            // 子类可以重写此方法，在所有组件初始化完成后执行逻辑
        }

        public override void _Process(double delta)
        {
            if (IsDestroyed || !IsStarted) return;

            float dt = (float)delta;

            // 更新所有组件
            for (int i = _components.Count - 1; i >= 0; i--)
            {
                if (i < _components.Count && _components[i].IsEnabled)
                {
                    try
                    {
                        _components[i].Update(dt);
                    }
                    catch (Exception ex)
                    {
                        GD.PrintErr($"组件 {_components[i].GetType().Name} 更新失败: {ex.Message}");
                    }
                }
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            if (IsDestroyed || !IsStarted) return;

            float dt = (float)delta;

            // 物理更新所有组件
            for (int i = _components.Count - 1; i >= 0; i--)
            {
                if (i < _components.Count && _components[i].IsEnabled)
                {
                    try
                    {
                        _components[i].PhysicsUpdate(dt);
                    }
                    catch (Exception ex)
                    {
                        GD.PrintErr($"组件 {_components[i].GetType().Name} 物理更新失败: {ex.Message}");
                    }
                }
            }
        }

        public override void _ExitTree()
        {
            if (IsDestroyed) return;

            IsDestroyed = true;

            // 清理所有组件
            foreach (var component in _components)
            {
                try
                {
                    component.Cleanup();
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"组件 {component.GetType().Name} 清理失败: {ex.Message}");
                }
            }

            _components.Clear();
            _componentCache.Clear();
            _pendingComponents.Clear();
        }

        #endregion

        #region 组件管理API

        /// <summary>
        /// 开始批量添加组件（优化性能）
        /// </summary>
        public void BeginBatchAddComponents()
        {
            _isBatchAdding = true;
        }

        /// <summary>
        /// 结束批量添加组件并初始化
        /// </summary>
        public void EndBatchAddComponents()
        {
            _isBatchAdding = false;

            // 如果实体已初始化，立即初始化新添加的组件
            if (IsInitialized)
            {
                InitializePendingComponents();
            }
        }

        /// <summary>
        /// 添加组件（无参构造）
        /// </summary>
        public T AddComponent<T>() where T : IComponent, new()
        {
            return AddComponent(new T());
        }

        /// <summary>
        /// 添加组件（已创建实例）
        /// </summary>
        public T AddComponent<T>(T component) where T : IComponent
        {
            if (component == null)
            {
                GD.PrintErr("尝试添加空组件！");
                return default(T);
            }

            Type componentType = typeof(T);

            // 检查是否已存在同类型组件
            if (_componentCache.ContainsKey(componentType))
            {
                GD.PrintErr($"组件 {componentType.Name} 已存在！");
                return (T)_componentCache[componentType];
            }

            // 添加到缓存
            _componentCache[componentType] = component;

            // 如果实体未初始化或正在批量添加，加入等待队列
            if (!IsInitialized || _isBatchAdding)
            {
                _pendingComponents.Enqueue(component);
            }
            else
            {
                // 立即初始化组件
                _components.Add(component);

                try
                {
                    component.Initialize(this);
                    component.Start();
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"组件 {componentType.Name} 初始化失败: {ex.Message}");
                }
            }

            return component;
        }

        /// <summary>
        /// 初始化等待队列中的组件
        /// </summary>
        private void InitializePendingComponents()
        {
            while (_pendingComponents.Count > 0)
            {
                var component = _pendingComponents.Dequeue();
                _components.Add(component);

                try
                {
                    component.Initialize(this);
                    component.Start();
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"组件初始化失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 获取组件
        /// </summary>
        public T GetComponent<T>() where T : class, IComponent
        {
            Type componentType = typeof(T);

            if (_componentCache.TryGetValue(componentType, out var component))
            {
                return component as T;
            }

            // 如果没有找到精确类型，尝试查找继承类型
            foreach (var kvp in _componentCache)
            {
                if (kvp.Value is T result)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// 尝试获取组件
        /// </summary>
        public bool TryGetComponent<T>(out T component) where T : class, IComponent
        {
            component = GetComponent<T>();
            return component != null;
        }

        /// <summary>
        /// 检查是否有组件
        /// </summary>
        public bool HasComponent<T>() where T : IComponent
        {
            Type componentType = typeof(T);

            if (_componentCache.ContainsKey(componentType))
            {
                return true;
            }

            // 检查是否有继承类型
            foreach (var kvp in _componentCache)
            {
                if (kvp.Value is T)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 获取所有指定类型的组件（包括子类）
        /// </summary>
        public List<T> GetComponents<T>() where T : class, IComponent
        {
            return _components.OfType<T>().ToList();
        }

        /// <summary>
        /// 移除组件
        /// </summary>
        public void RemoveComponent<T>() where T : IComponent
        {
            Type componentType = typeof(T);

            if (_componentCache.TryGetValue(componentType, out var component))
            {
                // 清理组件
                try
                {
                    component.Cleanup();
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"组件 {componentType.Name} 清理失败: {ex.Message}");
                }

                // 从缓存中移除
                _components.Remove(component);
                _componentCache.Remove(componentType);
            }
        }

        /// <summary>
        /// 移除所有组件
        /// </summary>
        public void RemoveAllComponents()
        {
            foreach (var component in _components)
            {
                try
                {
                    component.Cleanup();
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"组件清理失败: {ex.Message}");
                }
            }

            _components.Clear();
            _componentCache.Clear();
            _pendingComponents.Clear();
        }

        /// <summary>
        /// 获取所有组件（调试用）
        /// </summary>
        public IReadOnlyList<IComponent> GetAllComponents()
        {
            return _components.AsReadOnly();
        }

        #endregion

        #region 实用方法

        /// <summary>
        /// 检查是否是敌对阵营
        /// </summary>
        public bool IsEnemy(CharacterEntity other)
        {
            if (other == null || other.IsDestroyed) return false;

            // 玩家 vs 敌人
            if (Faction == FactionType.Player && other.Faction == FactionType.Enemy)
                return true;

            // 敌人 vs 玩家
            if (Faction == FactionType.Enemy && other.Faction == FactionType.Player)
                return true;

            return false;
        }

        /// <summary>
        /// 检查是否是友方阵营
        /// </summary>
        public bool IsAlly(CharacterEntity other)
        {
            if (other == null || other.IsDestroyed) return false;
            return Faction == other.Faction && Faction != FactionType.Neutral;
        }

        /// <summary>
        /// 获取与目标的距离
        /// </summary>
        public float GetDistanceTo(CharacterEntity other)
        {
            if (other == null) return float.MaxValue;
            return GlobalPosition.DistanceTo(other.GlobalPosition);
        }

        /// <summary>
        /// 获取与目标的方向向量
        /// </summary>
        public Vector2 GetDirectionTo(CharacterEntity other)
        {
            if (other == null) return Vector2.Zero;
            return (other.GlobalPosition - GlobalPosition).Normalized();
        }

        /// <summary>
        /// 销毁实体
        /// </summary>
        public virtual void Destroy()
        {
            if (IsDestroyed) return;

            IsDestroyed = true;
            QueueFree();
        }

        #endregion
    }

    /// <summary>
    /// 实体类型枚举
    /// </summary>
    public enum EntityType
    {
        Player,      // 玩家
        Enemy,       // 敌人
        Projectile,  // 投射物
        Summon,      // 召唤物
        Pickup,      // 掉落物
        Obstacle,    // 障碍物
        Interactive, // 可交互物体
        NPC,         // NPC
        Other        // 其他
    }

    /// <summary>
    /// 阵营类型
    /// </summary>
    public enum FactionType
    {
        Player,   // 玩家阵营
        Enemy,    // 敌人阵营
        Neutral   // 中立
    }
}