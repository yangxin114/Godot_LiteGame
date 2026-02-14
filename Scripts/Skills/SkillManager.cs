using Godot;
using System;
using System.Collections.Generic;
using Logs;

namespace Skills
{
    /// <summary>
    /// 技能管理器
    /// 负责统一管理所有技能实例，协调技能释放和状态
    /// </summary>
    public partial class SkillManager : Node
    {
        #region 字段和属性

        /// <summary>
        /// 技能拥有者
        /// </summary>
        public ISkill SkillOwner { get; set; }

        /// <summary>
        /// 已学习的技能字典
        /// </summary>
        private Dictionary<string, BaseSkill> _learnedSkills = new();

        /// <summary>
        /// 技能冷却管理器
        /// </summary>
        private SkillCooldownManager _cooldownManager;

        /// <summary>
        /// 当前正在施法的技能
        /// </summary>
        private BaseSkill _castingSkill;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        private bool _isInitialized = false;

        #endregion

        #region 生命周期

        public override void _Ready()
        {
            Initialize();
        }

        public override void _Process(double delta)
        {
            if (!_isInitialized) return;
            
            UpdateSkills(delta);
        }

        public override void _ExitTree()
        {
            Cleanup();
        }

        #endregion

        #region 初始化和设置

        /// <summary>
        /// 初始化技能管理器
        /// </summary>
        private void Initialize()
        {
            if (_isInitialized)
            {
                Logger2.Warn("SkillManager: 已经初始化过了");
                return;
            }

            if (SkillOwner == null)
            {
                Logger2.Error("SkillManager: 技能拥有者未设置");
                return;
            }

            // 初始化冷却管理器
            _cooldownManager = new SkillCooldownManager();
            AddChild(_cooldownManager);

            _isInitialized = true;
            
            Logger2.Info("SkillManager: 初始化完成");
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        private void Cleanup()
        {
            // 清理所有技能实例
            foreach (var skill in _learnedSkills.Values)
            {
                if (IsInstanceValid(skill))
                {
                    skill.QueueFree();
                }
            }
            _learnedSkills.Clear();

            Logger2.Info("SkillManager: 资源清理完成");
        }

        #endregion

        #region 技能学习和管理

        /// <summary>
        /// 学习新技能
        /// </summary>
        public bool LearnSkill(string skillId, SkillData skillData)
        {
            if (!_isInitialized)
            {
                Logger2.Error("SkillManager: 未初始化");
                return false;
            }

            if (_learnedSkills.ContainsKey(skillId))
            {
                Logger2.Warn($"SkillManager: 技能 {skillId} 已经学会");
                return false;
            }

            // 创建技能实例
            var skillInstance = new BaseSkill();
            skillInstance.SkillData = skillData;
            skillInstance.SkillOwner = SkillOwner;
            
            AddChild(skillInstance);
            _learnedSkills[skillId] = skillInstance;

            // 订阅技能事件
            SubscribeToSkillEvents(skillInstance);

            Logger2.Info($"SkillManager: 学会技能 {skillData.SkillName} ({skillId})");
            return true;
        }

        /// <summary>
        /// 忘记技能
        /// </summary>
        public bool ForgetSkill(string skillId)
        {
            if (!_learnedSkills.TryGetValue(skillId, out var skill))
            {
                Logger2.Warn($"SkillManager: 未学会技能 {skillId}");
                return false;
            }

            // 取消事件订阅
            UnsubscribeFromSkillEvents(skill);

            // 移除技能实例
            if (IsInstanceValid(skill))
            {
                skill.QueueFree();
            }
            
            _learnedSkills.Remove(skillId);

            Logger2.Info($"SkillManager: 忘记技能 {skillId}");
            return true;
        }

        /// <summary>
        /// 升级技能
        /// </summary>
        public bool UpgradeSkill(string skillId)
        {
            if (!_learnedSkills.TryGetValue(skillId, out var skill))
            {
                Logger2.Warn($"SkillManager: 未学会技能 {skillId}");
                return false;
            }

            return skill.UpgradeSkill(skillId);
        }

        /// <summary>
        /// 获取技能实例
        /// </summary>
        public BaseSkill GetSkill(string skillId)
        {
            return _learnedSkills.GetValueOrDefault(skillId);
        }

        /// <summary>
        /// 获取所有已学技能
        /// </summary>
        public BaseSkill[] GetAllSkills()
        {
            return new List<BaseSkill>(_learnedSkills.Values).ToArray();
        }

        #endregion

        #region 技能释放控制

        /// <summary>
        /// 尝试释放技能
        /// </summary>
        public async void CastSkill(string skillId, ISkill target = null, Vector2? targetPosition = null)
        {
            if (!_isInitialized)
            {
                Logger2.Error("SkillManager: 未初始化");
                return;
            }

            if (!_learnedSkills.TryGetValue(skillId, out var skill))
            {
                Logger2.Warn($"SkillManager: 未学会技能 {skillId}");
                return;
            }

            // 检查是否已经在施法
            if (_castingSkill != null)
            {
                Logger2.Warn($"SkillManager: 正在施放技能 {_castingSkill.SkillData.SkillName}");
                return;
            }

            // 检查技能是否可以释放
            if (!skill.CanCastSkill(skillId))
            {
                Logger2.Warn($"SkillManager: 技能 {skillId} 无法释放");
                return;
            }

            // 设置当前施法技能
            _castingSkill = skill;

            try
            {
                // 执行技能释放
                bool success = await skill.CastSkill(skillId, target, targetPosition);
                
                if (!success)
                {
                    Logger2.Warn($"SkillManager: 技能 {skillId} 释放失败");
                }
            }
            finally
            {
                // 重置施法状态
                _castingSkill = null;
            }
        }

        /// <summary>
        /// 中断当前施法
        /// </summary>
        public void InterruptCurrentCast()
        {
            if (_castingSkill != null)
            {
                _castingSkill.InterruptSkill();
                _castingSkill = null;
                Logger2.Info("SkillManager: 中断当前施法");
            }
        }

        /// <summary>
        /// 检查是否可以释放技能
        /// </summary>
        public bool CanCastAnySkill()
        {
            return _castingSkill == null;
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 订阅技能事件
        /// </summary>
        private void SubscribeToSkillEvents(BaseSkill skill)
        {
            skill.OnSkillCastStart += OnSkillCastStart;
            skill.OnSkillCastComplete += OnSkillCastComplete;
            skill.OnSkillActivated += OnSkillActivated;
            skill.OnSkillEnded += OnSkillEnded;
            skill.OnSkillInterrupted += OnSkillInterrupted;
            skill.OnSkillCooldownStart += OnSkillCooldownStart;
            skill.OnSkillCooldownEnd += OnSkillCooldownEnd;
            skill.OnResourceInsufficient += OnResourceInsufficient;
            skill.OnSkillEffectApplied += OnSkillEffectApplied;
            skill.OnSkillEffectRemoved += OnSkillEffectRemoved;
        }

        /// <summary>
        /// 取消订阅技能事件
        /// </summary>
        private void UnsubscribeFromSkillEvents(BaseSkill skill)
        {
            skill.OnSkillCastStart -= OnSkillCastStart;
            skill.OnSkillCastComplete -= OnSkillCastComplete;
            skill.OnSkillActivated -= OnSkillActivated;
            skill.OnSkillEnded -= OnSkillEnded;
            skill.OnSkillInterrupted -= OnSkillInterrupted;
            skill.OnSkillCooldownStart -= OnSkillCooldownStart;
            skill.OnSkillCooldownEnd -= OnSkillCooldownEnd;
            skill.OnResourceInsufficient -= OnResourceInsufficient;
            skill.OnSkillEffectApplied -= OnSkillEffectApplied;
            skill.OnSkillEffectRemoved -= OnSkillEffectRemoved;
        }

        #endregion

        #region 技能事件回调

        private void OnSkillCastStart(SkillEventData data)
        {
            Logger2.Debug($"SkillManager: 技能开始施放 - {data.SkillId}");
            // 可以在这里添加全局施法开始逻辑
        }

        private void OnSkillCastComplete(SkillEventData data)
        {
            Logger2.Debug($"SkillManager: 技能施放完成 - {data.SkillId}");
            // 可以在这里添加全局施法完成逻辑
        }

        private void OnSkillActivated(SkillEventData data)
        {
            Logger2.Debug($"SkillManager: 技能激活 - {data.SkillId}");
            // 可以在这里添加全局技能激活逻辑
        }

        private void OnSkillEnded(SkillEventData data)
        {
            Logger2.Debug($"SkillManager: 技能结束 - {data.SkillId}");
            // 可以在这里添加全局技能结束逻辑
        }

        private void OnSkillInterrupted(SkillInterruptEventData data)
        {
            Logger2.Debug($"SkillManager: 技能被中断 - {data.SkillId}, 原因: {data.Reason}");
            // 可以在这里添加全局技能中断逻辑
        }

        private void OnSkillCooldownStart(SkillCooldownEventData data)
        {
            Logger2.Debug($"SkillManager: 技能进入冷却 - {data.SkillId}, 时长: {data.CooldownDuration}s");
            // 可以在这里添加全局冷却开始逻辑
        }

        private void OnSkillCooldownEnd(SkillCooldownEventData data)
        {
            Logger2.Debug($"SkillManager: 技能冷却结束 - {data.SkillId}");
            // 可以在这里添加全局冷却结束逻辑
        }

        private void OnResourceInsufficient(ResourceInsufficientEventData data)
        {
            Logger2.Warn($"SkillManager: 资源不足 - {data.ResourceType}, 需要: {data.RequiredAmount}, 当前: {data.CurrentAmount}");
            // 可以在这里添加全局资源不足处理逻辑
        }

        private void OnSkillEffectApplied(SkillEffectEventData data)
        {
            Logger2.Debug($"SkillManager: 技能效果应用 - {data.EffectId} 到 {data.Target}");
            // 可以在这里添加全局效果应用逻辑
        }

        private void OnSkillEffectRemoved(SkillEffectEventData data)
        {
            Logger2.Debug($"SkillManager: 技能效果移除 - {data.EffectId} 从 {data.Target}");
            // 可以在这里添加全局效果移除逻辑
        }

        #endregion

        #region 更新逻辑

        /// <summary>
        /// 更新所有技能
        /// </summary>
        private void UpdateSkills(double delta)
        {
            // 更新冷却管理器
            _cooldownManager?.Update((float)delta);

            // 可以在这里添加其他全局技能更新逻辑
        }

        #endregion
    }
}