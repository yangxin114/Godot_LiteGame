using Godot;
using Skills;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Logs;
using Entities;

namespace Skills
{
    /// <summary>
    /// 技能基类
    /// 实现ISkill接口，作为单个技能实例的核心实现类
    /// 负责技能的施放、状态管理、资源消耗和效果处理等核心功能
    /// 
    /// 设计理念：
    /// - 专注于单个技能的生命周期管理
    /// - 与技能使用者(ISkillUser)和技能管理器(ISkillManager)分离职责
    /// - 通过事件系统与其他系统解耦
    /// - 遵循接口隔离原则，只实现技能本身应有的功能
    /// </summary>
    public partial class BaseSkill : CharacterEntity, ISkill
    {
        #region 字段和属性

        /// <summary>
        /// 技能拥有者引用
        /// 指向使用此技能的实体（如Player、Enemy等）
        /// </summary>
        public ISkillOwner SkillOwner { get; set; }

        /// <summary>
        /// 技能数据
        /// 包含技能的所有配置信息，如消耗、冷却时间、效果等
        /// 通过Godot编辑器导出，支持可视化配置
        /// </summary>
        [Export] public SkillData SkillData { get; set; }

        /// <summary>
        /// 当前技能状态
        /// 表示技能所处的生命周期阶段
        /// Ready -> Casting -> Active -> Cooldown -> Ready 循环
        /// </summary>
        public SkillState CurrentState { get; private set; } = SkillState.Ready;

        /// <summary>
        /// 技能冷却计时器
        /// 用于管理技能冷却时间的倒计时
        /// </summary>
        private Timer _cooldownTimer;

        /// <summary>
        /// 施法计时器
        /// 用于管理需要引导时间的技能施法过程
        /// 瞬发技能不会使用此计时器
        /// </summary>
        private Timer _castTimer;

        /// <summary>
        /// 当前施法目标
        /// 记录本次技能施放的目标实体引用
        /// </summary>
        private ISkillTarget _currentTarget;

        /// <summary>
        /// 当前施法位置
        /// 记录本次技能施放的目标位置坐标
        /// </summary>
        private Vector2 _currentTargetPosition;

        /// <summary>
        /// 激活中的效果列表
        /// 存储当前技能施放后激活的所有效果实例
        /// 用于效果的生命周期管理和清理
        /// </summary>
        private List<ActiveEffect> _activeEffects = new();

        /// <summary>
        /// 技能是否已初始化完成
        /// 用于防止在初始化完成前调用技能功能
        /// </summary>
        private bool _isInitialized = false;

        #endregion

        #region 生命周期方法

        /// <summary>
        /// 节点准备完成回调
        /// 执行技能系统的初始化工作
        /// </summary>
        public override void _Ready()
        {
            base._Ready();
            Initialize();
        }

        /// <summary>
        /// 每帧处理回调
        /// 更新技能的实时逻辑，如效果持续时间、状态检查等
        /// </summary>
        /// <param name="delta">帧间隔时间（秒）</param>
        public override void _Process(double delta)
        {   
            base._Process(delta);
            if (!_isInitialized) return;
            
            UpdateSkill(delta);
        }

        /// <summary>
        /// 物理帧处理回调
        /// 处理与物理系统相关的技能逻辑
        /// 如碰撞检测、轨迹计算等
        /// </summary>
        /// <param name="delta">物理帧间隔时间（秒）</param>
        public override void _PhysicsProcess(double delta)
        {
            base._PhysicsProcess(delta);
            // 这里可以处理与物理相关的技能逻辑，例如碰撞检测等
        }

        /// <summary>
        /// 节点退出场景树回调
        /// 执行资源清理工作，移除所有激活的效果
        /// </summary>
        public override void _ExitTree()
        {
            Cleanup();
        }

        #endregion

        #region 初始化和设置

        /// <summary>
        /// 初始化技能系统
        /// 设置计时器、初始状态和必要的组件
        /// </summary>
        private void Initialize()
        {
            if (_isInitialized)
            {
                Logger2.Warn($"BaseSkill: 技能 {SkillData?.SkillName ?? "Unknown"} 已经初始化过了");
                return;
            }

            if (SkillData == null)
            {
                Logger2.Error("BaseSkill: 技能数据未设置");
                return;
            }

            // 创建计时器
            SetupTimers();
            
            // 初始化状态
            CurrentState = SkillState.Ready;
            
            _isInitialized = true;
            
            Logger2.Info($"BaseSkill: 技能 {SkillData.SkillName} 初始化完成");
        }

        /// <summary>
        /// 设置技能所需的计时器组件
        /// 包括冷却计时器和施法计时器
        /// </summary>
        private void SetupTimers()
        {
            // 冷却计时器
            _cooldownTimer = new Timer();
            _cooldownTimer.OneShot = true;
            _cooldownTimer.Timeout += OnCooldownFinished;
            AddChild(_cooldownTimer);

            // 施法计时器（仅当技能有施法时间时创建）
            if (SkillData.CastTime > 0)
            {
                _castTimer = new Timer();
                _castTimer.OneShot = true;
                _castTimer.Timeout += OnCastCompleted;
                AddChild(_castTimer);
            }
        }

        /// <summary>
        /// 清理技能资源
        /// 移除所有激活的效果，释放占用的内存
        /// </summary>
        private void Cleanup()
        {
            // 移除所有激活效果
            foreach (var effect in _activeEffects.ToArray())
            {
                RemoveSkillEffect(effect.EffectId);
            }

            Logger2.Info($"BaseSkill: 技能 {SkillData?.SkillName ?? "Unknown"} 资源清理完成");
        }

        #endregion

        #region ISkill接口实现 - 技能基础操作

        /// <summary>
        /// 施放技能的核心方法
        /// 执行完整的技能施放流程：验证→消耗→施法→激活→冷却
        /// </summary>
        /// <param name="target">技能施放的目标实体（可选）</param>
        /// <returns>施放是否成功</returns>
        public async Task<bool> CastSkill(ISkillTarget target = null)
        {
            if (!ValidateSkillCast())
                return false;

            // 设置目标信息
            _currentTarget = target;
            _currentTargetPosition = target?.GetNode2DTarget().Position ?? Vector2.Zero;

            // 检查施法条件
            if (!CanCastSkill())
            {
                Logger2.Warn($"BaseSkill: 技能 {SkillData.SkillId}:{SkillData.SkillName} 无法释放 - 条件不满足");
                return false;
            }

            // 消耗资源
            if (!ConsumeRequiredResources())
            {
                OnResourceInsufficient?.Invoke(new ResourceInsufficientEventData
                {
                    ResourceType = GetPrimaryResourceType(),
                    RequiredAmount = GetSkillResourceCost().Amount,
                    CurrentAmount = GetCurrentResourceAmount(GetPrimaryResourceType())
                });
                return false;
            }

            // 进入施法状态
            SetSkillState(SkillState.Casting);

            // 触发施法开始事件
            OnSkillCastStart?.Invoke(new SkillEventData
            {
                Timestamp = (float)Time.GetTicksMsec() / 1000f
            });

            // 处理施法时间
            if (SkillData.CastTime > 0)
            {
                _castTimer.Start(SkillData.CastTime);
                await ToSignal(_castTimer, "timeout");
            }
            else
            {
                // 瞬发技能立即完成施法
                OnCastCompleted();
            }

            return true;
        }

        /// <summary>
        /// 检查技能是否可以施放
        /// 验证所有施放前置条件是否满足
        /// </summary>
        /// <returns>是否可以施放</returns>
        public bool CanCastSkill()
        {
            // 检查技能状态
            if (CurrentState != SkillState.Ready)
                return false;

            // 检查冷却状态
            if (IsSkillOnCooldown())
                return false;

            // 检查资源
            if (!HasEnoughResources())
                return false;

            // 检查施法距离（如果有目标）
            if (_currentTarget != null && !IsWithinCastRange(_currentTargetPosition))
                return false;

            return true;
        }

        /// <summary>
        /// 中断正在进行的技能施法
        /// 通常由外部事件触发（如受到攻击、移动等）
        /// </summary>
        public void InterruptSkill()
        {
            if (CurrentState != SkillState.Casting)
                return;

            // 停止施法计时器
            _castTimer?.Stop();

            // 触发中断事件
            OnSkillInterrupted?.Invoke(new SkillInterruptEventData
            {
                Timestamp = (float)Time.GetTicksMsec() / 1000f,
                InterruptSource = "Manual",
                Reason = InterruptReason.Manual
            });

            // 重置状态
            ResetSkillState();
        }

        /// <summary>
        /// 取消技能（根据当前状态执行不同的取消逻辑）
        /// 处理施法中断或效果取消等不同场景
        /// </summary>
        public void CancelSkill()
        {
            switch (CurrentState)
            {
                case SkillState.Casting:
                    InterruptSkill();
                    break;
                case SkillState.Active:
                    EndSkillActivation();
                    break;
            }
        }

        #endregion

        #region ISkill接口实现 - 状态查询

        /// <summary>
        /// 获取技能当前状态
        /// </summary>
        /// <returns>当前技能状态枚举值</returns>
        public SkillState GetSkillState()
        {
            return CurrentState;
        }

        /// <summary>
        /// 检查技能是否处于冷却状态
        /// </summary>
        /// <returns>是否在冷却中</returns>
        public bool IsSkillOnCooldown()
        {
            return CurrentState == SkillState.Cooldown;
        }

        /// <summary>
        /// 获取技能剩余冷却时间
        /// </summary>
        /// <returns>剩余冷却时间（秒），如果不在冷却中则返回0</returns>
        public float GetSkillCooldownRemaining()
        {
            if (!_cooldownTimer.IsStopped())
            {
                return (float)_cooldownTimer.TimeLeft;
            }
            return 0f;
        }

        /// <summary>
        /// 检查是否正在施法过程中
        /// </summary>
        /// <returns>是否正在施法</returns>
        public bool IsCastingSkill()
        {
            return CurrentState == SkillState.Casting;
        }

        #endregion

        #region ISkill接口实现 - 资源管理

        /// <summary>
        /// 检查是否有足够的资源施放技能
        /// 验证所有类型的资源消耗需求
        /// </summary>
        /// <returns>资源是否充足</returns>
        public bool HasEnoughResources()
        {
            // 检查各种资源消耗
            return HasResource(ResourceType.Mana, SkillData.GetActualManaCost()) &&
                   HasResource(ResourceType.Stamina, SkillData.StaminaCost) &&
                   HasResource(ResourceType.Rage, SkillData.RageCost);
        }

        /// <summary>
        /// 获取技能的资源消耗详情
        /// </summary>
        /// <returns>包含资源类型、数量和是否可负担的信息</returns>
        public SkillResourceCost GetSkillResourceCost()
        {
            return new SkillResourceCost
            {
                Type = GetPrimaryResourceType(),
                Amount = SkillData.GetActualManaCost(),
                CanAfford = HasEnoughResources()
            };
        }

        /// <summary>
        /// 检查特定类型资源是否充足
        /// </summary>
        /// <param name="resourceType">资源类型</param>
        /// <param name="amount">需要的数量</param>
        /// <returns>资源是否充足</returns>
        public bool HasResource(ResourceType resourceType, float amount)
        {
            float currentAmount = GetCurrentResourceAmount(resourceType);
            return currentAmount >= amount;
        }

        /// <summary>
        /// 消耗指定类型的资源
        /// </summary>
        /// <param name="resourceType">资源类型</param>
        /// <param name="amount">消耗数量</param>
        /// <returns>消耗是否成功</returns>
        public bool ConsumeResource(ResourceType resourceType, float amount)
        {
            // 这里应该调用实际的资源管理系统
            // 暂时返回true表示消耗成功
            Logger2.Debug($"BaseSkill: 消耗资源 {resourceType}: {amount}");
            return true;
        }

        #endregion

        #region ISkill接口实现 - 技能管理

        /// <summary>
        /// 升级技能
        /// 增加技能等级并更新相关属性
        /// </summary>
        /// <returns>升级是否成功</returns>
        public bool UpgradeSkill()
        {
            if (SkillData.Level >= SkillData.MaxLevel)
                return false;

            SkillData.Level++;
            Logger2.Info($"BaseSkill: 技能 {SkillData.SkillName} 升级到 {SkillData.Level} 级");
            return true;
        }

        #endregion

        #region ISkill接口实现 - 效果处理

        /// <summary>
        /// 应用技能效果到当前技能实例
        /// 处理效果的添加、计时和事件触发
        /// </summary>
        /// <param name="effect">要应用的技能效果数据</param>
        public void ApplySkillEffect(SkillEffect effect)
        {
            if (effect == null) return;

            var activeEffect = new ActiveEffect
            {
                EffectId = effect.EffectId,
                Type = effect.Type,
                Value = effect.Value,
                RemainingDuration = effect.Duration,
                IsDebuff = effect.IsDebuff
            };

            _activeEffects.Add(activeEffect);

            // 如果有效果持续时间，启动计时器
            if (effect.Duration > 0)
            {
                var effectTimer = new Timer();
                effectTimer.WaitTime = effect.Duration;
                effectTimer.OneShot = true;
                effectTimer.Timeout += () => RemoveSkillEffect(effect.EffectId);
                AddChild(effectTimer);
                effectTimer.Start();
            }

            // 触发效果应用事件
            OnSkillEffectApplied?.Invoke(new SkillEffectEventData
            {
                EffectId = effect.EffectId,
                EffectType = effect.Type,
                Value = effect.Value,
                Duration = effect.Duration
            });

            Logger2.Debug($"BaseSkill: 应用效果 {effect.Name} ({effect.EffectId})");
        }

        /// <summary>
        /// 移除指定的技能效果
        /// 清理效果相关的计时器和数据
        /// </summary>
        /// <param name="effectId">要移除的效果ID</param>
        public void RemoveSkillEffect(string effectId)
        {
            var effectToRemove = _activeEffects.Find(e => 
                e.EffectId == effectId );

            if (effectToRemove != null)
            {
                _activeEffects.Remove(effectToRemove);

                // 触发效果移除事件
                OnSkillEffectRemoved?.Invoke(new SkillEffectEventData
                {
                    EffectId = effectId,
                    EffectType = effectToRemove.Type,
                    Value = effectToRemove.Value,
                    Duration = effectToRemove.RemainingDuration
                });

                Logger2.Debug($"BaseSkill: 移除效果 {effectId}");
            }
        }

        /// <summary>
        /// 检查是否拥有指定效果
        /// </summary>
        /// <param name="effectId">效果ID</param>
        /// <returns>是否拥有该效果</returns>
        public bool HasEffect(string effectId)
        {
            return _activeEffects.Exists(e => e.EffectId == effectId);
        }

        /// <summary>
        /// 获取当前激活的所有效果
        /// </summary>
        /// <returns>激活效果数组</returns>
        public ActiveEffect[] GetActiveEffects()
        {
            return _activeEffects.ToArray();
        }

        #endregion

        #region 核心技能逻辑

        /// <summary>
        /// 验证技能施放的基本条件
        /// 检查技能是否已完成初始化
        /// </summary>
        /// <returns>验证是否通过</returns>
        private bool ValidateSkillCast()
        {
            if (!_isInitialized)
            {
                Logger2.Error("BaseSkill: 技能未初始化");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 消耗施放技能所需的全部资源
        /// 按照技能配置依次消耗各种资源类型
        /// </summary>
        /// <returns>资源消耗是否成功</returns>
        private bool ConsumeRequiredResources()
        {
            if (!HasEnoughResources())
                return false;

            // 消耗各种资源
            bool success = true;
            
            if (SkillData.ManaCost > 0)
                success &= ConsumeResource(ResourceType.Mana, SkillData.GetActualManaCost());
            
            if (SkillData.StaminaCost > 0)
                success &= ConsumeResource(ResourceType.Stamina, SkillData.StaminaCost);
            
            if (SkillData.RageCost > 0)
                success &= ConsumeResource(ResourceType.Rage, SkillData.RageCost);

            return success;
        }

        /// <summary>
        /// 获取技能的主要资源消耗类型
        /// 用于UI显示和资源不足提示
        /// </summary>
        /// <returns>主要资源类型</returns>
        private ResourceType GetPrimaryResourceType()
        {
            if (SkillData.ManaCost > 0) return ResourceType.Mana;
            if (SkillData.StaminaCost > 0) return ResourceType.Stamina;
            if (SkillData.RageCost > 0) return ResourceType.Rage;
            return ResourceType.Mana; // 默认
        }

        /// <summary>
        /// 获取指定资源类型的当前数量
        /// 应该连接到实际的属性系统获取真实数值
        /// </summary>
        /// <param name="resourceType">资源类型</param>
        /// <returns>当前资源数量</returns>
        private float GetCurrentResourceAmount(ResourceType resourceType)
        {
            // 这里应该连接到实际的属性系统
            // 暂时返回固定值用于测试
            return 100f;
        }

        /// <summary>
        /// 检查目标是否在技能施法范围内
        /// </summary>
        /// <param name="targetPosition">目标位置</param>
        /// <returns>是否在施法范围内</returns>
        private bool IsWithinCastRange(Vector2 targetPosition)
        {
            if (SkillData.CastRange <= 0) return true;
            
            float distance = Position.DistanceTo(targetPosition);
            return distance <= SkillData.CastRange;
        }

        /// <summary>
        /// 设置技能状态并记录日志
        /// </summary>
        /// <param name="newState">新的技能状态</param>
        private void SetSkillState(SkillState newState)
        {
            Logger2.Debug($"BaseSkill: 技能状态变更 {CurrentState} -> {newState}");
            CurrentState = newState;
        }

        /// <summary>
        /// 重置技能到就绪状态
        /// 清理目标信息和相关状态
        /// </summary>
        private void ResetSkillState()
        {
            CurrentState = SkillState.Ready;
            _currentTarget = null;
            _currentTargetPosition = Vector2.Zero;
        }

        /// <summary>
        /// 更新技能的实时逻辑
        /// 处理效果持续时间、状态检查等每帧需要执行的任务
        /// </summary>
        /// <param name="delta">帧间隔时间</param>
        private void UpdateSkill(double delta)
        {
            // 更新激活效果的剩余时间
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                _activeEffects[i].RemainingDuration -= (float)delta;
                if (_activeEffects[i].RemainingDuration <= 0)
                {
                    RemoveSkillEffect(_activeEffects[i].EffectId);
                }
            }
        }

        /// <summary>
        /// 施法完成处理
        /// 触发完成事件，激活技能效果，进入冷却状态
        /// </summary>
        private void OnCastCompleted()
        {
            SetSkillState(SkillState.Active);

            // 触发施法完成事件
            OnSkillCastComplete?.Invoke(new SkillEventData
            {
                Timestamp = (float)Time.GetTicksMsec() / 1000f
            });

            // 激活技能效果
            ActivateSkillEffects();

            // 进入冷却状态
            EnterCooldown();
        }

        /// <summary>
        /// 激活技能配置的效果
        /// 将技能数据中的效果配置转换为实际激活的效果
        /// </summary>
        private void ActivateSkillEffects()
        {
            // 应用技能自带的效果
            foreach (var effectData in SkillData.StatusEffects)
            {
                var effect = new SkillEffect
                {
                    EffectId = effectData.EffectId,
                    Name = effectData.EffectName,
                    Type = MapStatusEffectTypeToEffectType(effectData.EffectType),
                    Value = effectData.Power,
                    Duration = effectData.Duration,
                    IsDebuff = effectData.IsDebuff
                };

                ApplySkillEffect(effect);
            }

            // 触发技能激活事件
            OnSkillActivated?.Invoke(new SkillEventData
            {
                Timestamp = (float)Time.GetTicksMsec() / 1000f
            });

            Logger2.Info($"BaseSkill: 技能 {SkillData.SkillName} 激活完成");
        }

        /// <summary>
        /// 结束技能激活状态
        /// 将技能重置为就绪状态
        /// </summary>
        private void EndSkillActivation()
        {
            SetSkillState(SkillState.Ready);

            OnSkillEnded?.Invoke(new SkillEventData
            {
                Timestamp = (float)Time.GetTicksMsec() / 1000f
            });

            Logger2.Debug($"BaseSkill: 技能 {SkillData.SkillName} 结束激活");
        }

        /// <summary>
        /// 进入技能冷却状态
        /// 启动冷却计时器，触发冷却开始事件
        /// </summary>
        private void EnterCooldown()
        {
            float actualCooldown = SkillData.GetActualCooldown();
            if (actualCooldown > 0)
            {
                SetSkillState(SkillState.Cooldown);
                _cooldownTimer.Start(actualCooldown);

                OnSkillCooldownStart?.Invoke(new SkillCooldownEventData
                {
                    Timestamp = (float)Time.GetTicksMsec() / 1000f,
                    CooldownDuration = actualCooldown,
                    RemainingTime = actualCooldown
                });

                Logger2.Debug($"BaseSkill: 技能 {SkillData.SkillName} 进入冷却，时长: {actualCooldown}s");
            }
            else
            {
                // 无冷却时间，直接回到就绪状态
                ResetSkillState();
            }
        }

        /// <summary>
        /// 冷却完成处理
        /// 重置技能状态，触发冷却结束事件
        /// </summary>
        private void OnCooldownFinished()
        {
            ResetSkillState();

            OnSkillCooldownEnd?.Invoke(new SkillCooldownEventData
            {
                Timestamp = (float)Time.GetTicksMsec() / 1000f,
                CooldownDuration = SkillData.GetActualCooldown(),
                RemainingTime = 0f
            });

            Logger2.Debug($"BaseSkill: 技能 {SkillData.SkillName} 冷却完成");
        }

        /// <summary>
        /// 映射状态效果类型到效果类型
        /// 将数据配置中的枚举转换为系统使用的枚举
        /// </summary>
        /// <param name="statusType">状态效果类型</param>
        /// <returns>对应的技能效果类型</returns>
        private EffectType MapStatusEffectTypeToEffectType(StatusEffectType statusType)
        {
            return statusType switch
            {
                StatusEffectType.Buff => EffectType.Buff,
                StatusEffectType.Debuff => EffectType.Debuff,
                StatusEffectType.DamageOverTime => EffectType.Damage,
                StatusEffectType.HealOverTime => EffectType.Heal,
                StatusEffectType.StatModifier => EffectType.StatModifier,
                StatusEffectType.Immunity => EffectType.Buff,
                _ => EffectType.Buff
            };
        }

        #endregion

        #region 事件声明

        /// <summary>
        /// 技能开始施法时触发
        /// </summary>
        public event Action<SkillEventData> OnSkillCastStart;

        /// <summary>
        /// 技能施法完成时触发
        /// </summary>
        public event Action<SkillEventData> OnSkillCastComplete;

        /// <summary>
        /// 技能激活时触发
        /// </summary>
        public event Action<SkillEventData> OnSkillActivated;

        /// <summary>
        /// 技能结束时触发
        /// </summary>
        public event Action<SkillEventData> OnSkillEnded;

        /// <summary>
        /// 技能被中断时触发
        /// </summary>
        public event Action<SkillInterruptEventData> OnSkillInterrupted;

        /// <summary>
        /// 技能进入冷却时触发
        /// </summary>
        public event Action<SkillCooldownEventData> OnSkillCooldownStart;

        /// <summary>
        /// 技能冷却结束时触发
        /// </summary>
        public event Action<SkillCooldownEventData> OnSkillCooldownEnd;

        /// <summary>
        /// 资源不足时触发
        /// </summary>
        public event Action<ResourceInsufficientEventData> OnResourceInsufficient;

        /// <summary>
        /// 技能效果应用时触发
        /// </summary>
        public event Action<SkillEffectEventData> OnSkillEffectApplied;

        /// <summary>
        /// 技能效果移除时触发
        /// </summary>
        public event Action<SkillEffectEventData> OnSkillEffectRemoved;

        #endregion
    }
}