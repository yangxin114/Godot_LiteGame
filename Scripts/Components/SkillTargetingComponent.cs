using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Entities;
using Logs;

namespace Components
{
    /// <summary>
    /// 技能目标选择组件
    /// 负责处理技能目标的检测、筛选和选择逻辑，支持多种目标选择模式
    /// </summary>
    public partial class SkillTargetingComponent : LogicComponent
    {
        #region 字段和属性

        /// <summary>
        /// 目标选择模式
        /// </summary>
        public TargetSelectionMode SelectionMode { get; set; } = TargetSelectionMode.Closest;

        /// <summary>
        /// 最大目标数量
        /// </summary>
        public int MaxTargets { get; set; } = 1;

        /// <summary>
        /// 目标检测范围
        /// </summary>
        public float DetectionRange { get; set; } = 300.0f;

        /// <summary>
        /// 扇形角度（度）
        /// </summary>
        public float SectorAngle { get; set; } = 90.0f;

        /// <summary>
        /// 是否包含友军
        /// </summary>
        public bool IncludeAllies { get; set; } = false;

        /// <summary>
        /// 是否包含敌军
        /// </summary>
        public bool IncludeEnemies { get; set; } = true;

        /// <summary>
        /// 是否包含中立单位
        /// </summary>
        public bool IncludeNeutrals { get; set; } = false;

        /// <summary>
        /// 目标优先级权重
        /// </summary>
        public Dictionary<TargetPriority, float> PriorityWeights { get; set; } = new();

        /// <summary>
        /// 目标过滤条件
        /// </summary>
        public List<Func<CharacterEntity, bool>> CustomFilters { get; set; } = new();

        #endregion

        #region 私有字段

        private List<CharacterEntity> _detectedTargets = new();
        private List<CharacterEntity> _validTargets = new();
        private Timer _updateTimer;

        #endregion

        #region 事件

        /// <summary>
        /// 目标检测完成事件
        /// </summary>
        public event Action<TargetDetectionEventArgs> OnTargetsDetected;

        /// <summary>
        /// 目标选择变更事件
        /// </summary>
        public event Action<TargetSelectionEventArgs> OnTargetSelectionChanged;

        /// <summary>
        /// 无有效目标事件
        /// </summary>
        public event Action OnNoValidTargets;

        #endregion

        #region 生命周期

        public override void Initialize(CharacterEntity entity)
        {
            base.Initialize(entity);
            SetupUpdateTimer();
            InitializePriorityWeights();
            Logger2.Info("SkillTargetingComponent: 目标选择组件初始化完成");
        }

        public override void Update(float delta)
        {
            if (!IsEnabled) return;
            
            // 定期更新目标列表
            if (_updateTimer.IsStopped())
            {
                _updateTimer.Start(0.1f); // 每0.1秒更新一次
                UpdateTargetDetection();
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();
            _detectedTargets.Clear();
            _validTargets.Clear();
            CustomFilters.Clear();
            _updateTimer?.QueueFree();
            Logger2.Info("SkillTargetingComponent: 目标选择组件清理完成");
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 设置更新计时器
        /// </summary>
        private void SetupUpdateTimer()
        {
            _updateTimer = new Timer();
            _updateTimer.OneShot = true;
            Entity.AddChild(_updateTimer);
        }

        /// <summary>
        /// 初始化优先级权重
        /// </summary>
        private void InitializePriorityWeights()
        {
            PriorityWeights[TargetPriority.HealthLowest] = 1.0f;
            PriorityWeights[TargetPriority.DistanceClosest] = 0.8f;
            PriorityWeights[TargetPriority.ThreatLevel] = 0.6f;
            PriorityWeights[TargetPriority.Random] = 0.1f;
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 检测范围内的所有目标
        /// </summary>
        public List<CharacterEntity> DetectTargets(Vector2 centerPosition, Vector2 direction)
        {
            _detectedTargets.Clear();
            
            // 获取场景中的所有潜在目标
            var potentialTargets = GetPotentialTargets();
            
            foreach (var target in potentialTargets)
            {
                if (IsTargetInRange(centerPosition, target) && 
                    IsTargetInDirection(centerPosition, direction, target) &&
                    PassesTargetFilters(target))
                {
                    _detectedTargets.Add(target);
                }
            }

            // 触发检测完成事件
            OnTargetsDetected?.Invoke(new TargetDetectionEventArgs
            {
                DetectedTargets = new List<CharacterEntity>(_detectedTargets),
                CenterPosition = centerPosition,
                Direction = direction
            });

            Logger2.Debug($"SkillTargetingComponent: 检测到 {_detectedTargets.Count} 个目标");
            return new List<CharacterEntity>(_detectedTargets);
        }

        /// <summary>
        /// 选择技能目标
        /// </summary>
        public List<CharacterEntity> SelectTargets(Vector2 centerPosition, Vector2 direction)
        {
            // 首先检测目标
            DetectTargets(centerPosition, direction);
            
            // 根据选择模式筛选目标
            _validTargets = FilterAndSortTargets(centerPosition, direction);
            
            // 限制最大目标数量
            if (_validTargets.Count > MaxTargets)
            {
                _validTargets = _validTargets.Take(MaxTargets).ToList();
            }

            // 触发选择变更事件
            OnTargetSelectionChanged?.Invoke(new TargetSelectionEventArgs
            {
                SelectedTargets = new List<CharacterEntity>(_validTargets),
                CenterPosition = centerPosition,
                Direction = direction
            });

            if (_validTargets.Count == 0)
            {
                OnNoValidTargets?.Invoke();
                Logger2.Debug("SkillTargetingComponent: 未找到有效目标");
            }
            else
            {
                Logger2.Debug($"SkillTargetingComponent: 选择 {_validTargets.Count} 个目标");
            }

            return new List<CharacterEntity>(_validTargets);
        }

        /// <summary>
        /// 获取最近的单个目标
        /// </summary>
        public CharacterEntity GetClosestTarget(Vector2 centerPosition, Vector2 direction)
        {
            var targets = SelectTargets(centerPosition, direction);
            return targets.FirstOrDefault();
        }

        /// <summary>
        /// 检查是否有可用目标
        /// </summary>
        public bool HasValidTargets(Vector2 centerPosition, Vector2 direction)
        {
            return SelectTargets(centerPosition, direction).Count > 0;
        }

        /// <summary>
        /// 添加自定义过滤器
        /// </summary>
        public void AddCustomFilter(Func<CharacterEntity, bool> filter)
        {
            CustomFilters.Add(filter);
            Logger2.Debug("SkillTargetingComponent: 添加自定义过滤器");
        }

        /// <summary>
        /// 清除自定义过滤器
        /// </summary>
        public void ClearCustomFilters()
        {
            CustomFilters.Clear();
            Logger2.Debug("SkillTargetingComponent: 清除自定义过滤器");
        }

        /// <summary>
        /// 设置目标阵营偏好
        /// </summary>
        public void SetFactionPreference(bool allies, bool enemies, bool neutrals)
        {
            IncludeAllies = allies;
            IncludeEnemies = enemies;
            IncludeNeutrals = neutrals;
            Logger2.Debug($"SkillTargetingComponent: 设置阵营偏好 - 友军:{allies}, 敌军:{enemies}, 中立:{neutrals}");
        }

        #endregion

        #region 目标检测逻辑

        /// <summary>
        /// 获取潜在目标列表
        /// </summary>
        private List<CharacterEntity> GetPotentialTargets()
        {
            var targets = new List<CharacterEntity>();
            
            // 这里应该从游戏世界管理器获取所有角色
            // 暂时返回空列表，实际项目中需要实现
            Logger2.Debug("SkillTargetingComponent: 获取潜在目标列表");
            
            return targets;
        }

        /// <summary>
        /// 检查目标是否在范围内
        /// </summary>
        private bool IsTargetInRange(Vector2 center, CharacterEntity target)
        {
            if (target == null) return false;
            
            float distance = center.DistanceTo(target.GlobalPosition);
            return distance <= DetectionRange;
        }

        /// <summary>
        /// 检查目标是否在指定方向上
        /// </summary>
        private bool IsTargetInDirection(Vector2 center, Vector2 direction, CharacterEntity target)
        {
            if (target == null || direction == Vector2.Zero) return true;
            
            var toTarget = (target.GlobalPosition - center).Normalized();
            var angle = Mathf.Acos(toTarget.Dot(direction.Normalized()));
            var maxAngle = Mathf.DegToRad(SectorAngle / 2);
            
            return angle <= maxAngle;
        }

        /// <summary>
        /// 检查目标是否通过过滤条件
        /// </summary>
        private bool PassesTargetFilters(CharacterEntity target)
        {
            if (target == null) return false;

            // 阵营检查
            if (!IncludeAllies && IsAlly(target)) return false;
            if (!IncludeEnemies && IsEnemy(target)) return false;
            if (!IncludeNeutrals && IsNeutral(target)) return false;

            // 生存状态检查
            if (!IsTargetAlive(target)) return false;

            // 自定义过滤器检查
            foreach (var filter in CustomFilters)
            {
                if (!filter(target)) return false;
            }

            return true;
        }

        /// <summary>
        /// 过滤和排序目标
        /// </summary>
        private List<CharacterEntity> FilterAndSortTargets(Vector2 centerPosition, Vector2 direction)
        {
            var filteredTargets = new List<CharacterEntity>(_detectedTargets);
            
            // 根据选择模式排序
            switch (SelectionMode)
            {
                case TargetSelectionMode.Closest:
                    filteredTargets.Sort((a, b) => 
                        centerPosition.DistanceTo(a.GlobalPosition)
                        .CompareTo(centerPosition.DistanceTo(b.GlobalPosition)));
                    break;

                case TargetSelectionMode.LowestHealth:
                    filteredTargets.Sort((a, b) => 
                        GetTargetHealthPercentage(a).CompareTo(GetTargetHealthPercentage(b)));
                    break;

                case TargetSelectionMode.HighestThreat:
                    filteredTargets.Sort((a, b) => 
                        GetTargetThreatLevel(b).CompareTo(GetTargetThreatLevel(a)));
                    break;

                case TargetSelectionMode.PriorityBased:
                    filteredTargets = SortByPriorityWeights(filteredTargets, centerPosition);
                    break;

                case TargetSelectionMode.Random:
                    ShuffleList(filteredTargets);
                    break;
            }

            return filteredTargets;
        }

        /// <summary>
        /// 根据优先级权重排序
        /// </summary>
        private List<CharacterEntity> SortByPriorityWeights(List<CharacterEntity> targets, Vector2 centerPosition)
        {
            return targets.OrderByDescending(target => CalculatePriorityScore(target, centerPosition)).ToList();
        }

        /// <summary>
        /// 计算目标优先级分数
        /// </summary>
        private float CalculatePriorityScore(CharacterEntity target, Vector2 centerPosition)
        {
            float score = 0f;

            // 血量最低优先级
            if (PriorityWeights.ContainsKey(TargetPriority.HealthLowest))
            {
                score += (1 - GetTargetHealthPercentage(target)) * PriorityWeights[TargetPriority.HealthLowest];
            }

            // 距离最近优先级
            if (PriorityWeights.ContainsKey(TargetPriority.DistanceClosest))
            {
                float distanceRatio = centerPosition.DistanceTo(target.GlobalPosition) / DetectionRange;
                score += (1 - distanceRatio) * PriorityWeights[TargetPriority.DistanceClosest];
            }

            // 威胁等级优先级
            if (PriorityWeights.ContainsKey(TargetPriority.ThreatLevel))
            {
                score += GetTargetThreatLevel(target) * PriorityWeights[TargetPriority.ThreatLevel];
            }

            // 随机因素
            if (PriorityWeights.ContainsKey(TargetPriority.Random))
            {
                score += (float)new Random().NextDouble() * PriorityWeights[TargetPriority.Random];
            }

            return score;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 更新目标检测
        /// </summary>
        private void UpdateTargetDetection()
        {
            // 这里可以实现更智能的目标跟踪逻辑
            // 比如缓存目标、预测移动等
        }

        /// <summary>
        /// 洗牌列表
        /// </summary>
        private void ShuffleList<T>(List<T> list)
        {
            var random = new Random();
            int n = list.Count;
            for (int i = n - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        /// <summary>
        /// 获取目标血量百分比
        /// </summary>
        private float GetTargetHealthPercentage(CharacterEntity target)
        {
            // 这里应该调用目标的属性系统获取血量
            // 暂时返回固定值
            return 1.0f;
        }

        /// <summary>
        /// 获取目标威胁等级
        /// </summary>
        private float GetTargetThreatLevel(CharacterEntity target)
        {
            // 这里应该根据目标类型、等级等计算威胁值
            // 暂时返回固定值
            return 1.0f;
        }

        /// <summary>
        /// 检查是否为友军
        /// </summary>
        private bool IsAlly(CharacterEntity target)
        {
            // 这里应该检查阵营关系
            // 暂时返回false
            return false;
        }

        /// <summary>
        /// 检查是否为敌军
        /// </summary>
        private bool IsEnemy(CharacterEntity target)
        {
            // 这里应该检查阵营关系
            // 暂时返回true
            return true;
        }

        /// <summary>
        /// 检查是否为中立单位
        /// </summary>
        private bool IsNeutral(CharacterEntity target)
        {
            // 这里应该检查阵营关系
            // 暂时返回false
            return false;
        }

        /// <summary>
        /// 检查目标是否存活
        /// </summary>
        private bool IsTargetAlive(CharacterEntity target)
        {
            // 这里应该检查目标的生命值状态
            // 暂时返回true
            return true;
        }

        #endregion
    }

    #region 数据结构

    /// <summary>
    /// 目标检测事件参数
    /// </summary>
    public class TargetDetectionEventArgs
    {
        public List<CharacterEntity> DetectedTargets { get; set; }
        public Vector2 CenterPosition { get; set; }
        public Vector2 Direction { get; set; }
    }

    /// <summary>
    /// 目标选择事件参数
    /// </summary>
    public class TargetSelectionEventArgs
    {
        public List<CharacterEntity> SelectedTargets { get; set; }
        public Vector2 CenterPosition { get; set; }
        public Vector2 Direction { get; set; }
    }

    #endregion

    #region 枚举定义

    /// <summary>
    /// 目标选择模式
    /// </summary>
    public enum TargetSelectionMode
    {
        Closest,        // 最近目标
        LowestHealth,   // 血量最少
        HighestThreat,  // 威胁最大
        PriorityBased,  // 优先级排序
        Random          // 随机选择
    }

    /// <summary>
    /// 目标优先级类型
    /// </summary>
    public enum TargetPriority
    {
        HealthLowest,   // 血量最低
        DistanceClosest,// 距离最近
        ThreatLevel,    // 威胁等级
        Random          // 随机因素
    }

    #endregion
}