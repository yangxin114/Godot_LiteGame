using Godot;
using System;
using System.Collections.Generic;
using Logs;

namespace Skills
{
    /// <summary>
    /// 技能管理器
    /// 负责管理角色的所有技能，处理技能组合、冷却组等高级功能
    /// </summary>
    public partial class SkillManager : Node
    {
        private ISkillOwner _skillOwner;
        private Dictionary<string, BaseSkill> _skills = new();
        private Dictionary<string, List<string>> _cooldownGroups = new();
        
        // 事件
        public event Action<BaseSkill> OnAnySkillUsed;
        public event Action<BaseSkill> OnAnySkillCompleted;
        public event Action<string, float> OnCooldownGroupTriggered;

        public override void _Ready()
        {
            Logger2.Info("SkillManager: 技能管理器初始化");
        }

        /// <summary>
        /// 初始化技能管理器
        /// </summary>
        public void Initialize(ISkillOwner owner)
        {
            _skillOwner = owner ?? throw new ArgumentNullException(nameof(owner));
            Logger2.Info("SkillManager: 为 {0} 初始化技能管理器", owner.GetType().Name);
        }

        /// <summary>
        /// 添加技能
        /// </summary>
        public void AddSkill(BaseSkill skill)
        {
            if (skill == null) return;
            
            if (_skills.ContainsKey(skill.SkillId))
            {
                Logger2.Warn("SkillManager: 技能 {0} 已存在", skill.SkillId);
                return;
            }

            // 设置技能所有者
            skill.SetSkillOwner(_skillOwner);
            
            // 添加到场景树
            AddChild(skill);
            
            // 添加到技能字典
            _skills[skill.SkillId] = skill;
            
            // 订阅技能事件
            SubscribeToSkillEvents(skill);
            
            Logger2.Info("SkillManager: 添加技能 {0} ({1})", skill.SkillName, skill.SkillId);
        }

        /// <summary>
        /// 移除技能
        /// </summary>
        public void RemoveSkill(string skillId)
        {
            if (_skills.TryGetValue(skillId, out BaseSkill skill))
            {
                // 取消事件订阅
                UnsubscribeFromSkillEvents(skill);
                
                // 从场景树移除
                RemoveChild(skill);
                
                // 从字典移除
                _skills.Remove(skillId);
                
                Logger2.Info("SkillManager: 移除技能 {0}", skill.SkillName);
            }
        }

        /// <summary>
        /// 获取技能
        /// </summary>
        public BaseSkill GetSkill(string skillId)
        {
            _skills.TryGetValue(skillId, out BaseSkill skill);
            return skill;
        }

        /// <summary>
        /// 尝试使用技能
        /// </summary>
        public bool TryUseSkill(string skillId)
        {
            var skill = GetSkill(skillId);
            if (skill == null)
            {
                Logger2.Warn("SkillManager: 未找到技能 {0}", skillId);
                return false;
            }

            return skill.TryUseSkill();
        }

        /// <summary>
        /// 订阅技能事件
        /// </summary>
        private void SubscribeToSkillEvents(BaseSkill skill)
        {
            skill.OnSkillStarted += OnSkillStartedHandler;
            skill.OnSkillCompleted += OnSkillCompletedHandler;
            skill.OnSkillCancelled += OnSkillCancelledHandler;
            skill.OnCooldownStarted += OnSkillCooldownStartedHandler;
        }

        /// <summary>
        /// 取消订阅技能事件
        /// </summary>
        private void UnsubscribeFromSkillEvents(BaseSkill skill)
        {
            skill.OnSkillStarted -= OnSkillStartedHandler;
            skill.OnSkillCompleted -= OnSkillCompletedHandler;
            skill.OnSkillCancelled -= OnSkillCancelledHandler;
            skill.OnCooldownStarted -= OnSkillCooldownStartedHandler;
        }

        /// <summary>
        /// 技能开始处理
        /// </summary>
        private void OnSkillStartedHandler(BaseSkill skill)
        {
            Logger2.Info("SkillManager: 技能 {0} 开始", skill.SkillName);
            OnAnySkillUsed?.Invoke(skill);
            
            // 检查冷却组
            TriggerCooldownGroup(skill.SkillId);
        }

        /// <summary>
        /// 技能完成处理
        /// </summary>
        private void OnSkillCompletedHandler(BaseSkill skill)
        {
            Logger2.Info("SkillManager: 技能 {0} 完成", skill.SkillName);
            OnAnySkillCompleted?.Invoke(skill);
        }

        /// <summary>
        /// 技能取消处理
        /// </summary>
        private void OnSkillCancelledHandler(BaseSkill skill)
        {
            Logger2.Info("SkillManager: 技能 {0} 取消", skill.SkillName);
        }

        /// <summary>
        /// 技能冷却开始处理
        /// </summary>
        private void OnSkillCooldownStartedHandler(BaseSkill skill)
        {
            Logger2.Info("SkillManager: 技能 {0} 开始冷却", skill.SkillName);
        }

        /// <summary>
        /// 添加技能到冷却组
        /// </summary>
        public void AddSkillToCooldownGroup(string groupName, string skillId)
        {
            if (!_cooldownGroups.ContainsKey(groupName))
            {
                _cooldownGroups[groupName] = new List<string>();
            }

            if (!_cooldownGroups[groupName].Contains(skillId))
            {
                _cooldownGroups[groupName].Add(skillId);
                Logger2.Info("SkillManager: 技能 {0} 添加到冷却组 {1}", skillId, groupName);
            }
        }

        /// <summary>
        /// 从冷却组移除技能
        /// </summary>
        public void RemoveSkillFromCooldownGroup(string groupName, string skillId)
        {
            if (_cooldownGroups.ContainsKey(groupName))
            {
                _cooldownGroups[groupName].Remove(skillId);
                Logger2.Info("SkillManager: 技能 {0} 从冷却组 {1} 移除", skillId, groupName);
            }
        }

        /// <summary>
        /// 触发冷却组
        /// </summary>
        private void TriggerCooldownGroup(string triggerSkillId)
        {
            foreach (var group in _cooldownGroups)
            {
                if (group.Value.Contains(triggerSkillId))
                {
                    // 触发同组其他技能的冷却
                    foreach (string skillId in group.Value)
                    {
                        if (skillId != triggerSkillId)
                        {
                            var skill = GetSkill(skillId);
                            if (skill != null && skill.IsAvailable())
                            {
                                skill.StartCooldown(); // 需要在BaseSkill中添加公共方法
                                OnCooldownGroupTriggered?.Invoke(group.Key, skill.CooldownTime);
                                Logger2.Info("SkillManager: 冷却组 {0} 中的技能 {1} 被触发冷却", group.Key, skill.SkillName);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 获取所有技能
        /// </summary>
        public IEnumerable<BaseSkill> GetAllSkills()
        {
            return _skills.Values;
        }

        /// <summary>
        /// 获取可用技能列表
        /// </summary>
        public List<BaseSkill> GetAvailableSkills()
        {
            var availableSkills = new List<BaseSkill>();
            foreach (var skill in _skills.Values)
            {
                if (skill.IsAvailable())
                {
                    availableSkills.Add(skill);
                }
            }
            return availableSkills;
        }

        /// <summary>
        /// 检查技能是否可用
        /// </summary>
        public bool IsSkillAvailable(string skillId)
        {
            var skill = GetSkill(skillId);
            return skill?.IsAvailable() ?? false;
        }

        /// <summary>
        /// 获取技能剩余冷却时间
        /// </summary>
        public float GetSkillCooldown(string skillId)
        {
            var skill = GetSkill(skillId);
            return skill?.GetRemainingCooldown() ?? 0.0f;
        }

        /// <summary>
        /// 禁用所有技能
        /// </summary>
        public void DisableAllSkills()
        {
            foreach (var skill in _skills.Values)
            {
                skill.DisableSkill();
            }
            Logger2.Info("SkillManager: 所有技能已禁用");
        }

        /// <summary>
        /// 启用所有技能
        /// </summary>
        public void EnableAllSkills()
        {
            foreach (var skill in _skills.Values)
            {
                skill.EnableSkill();
            }
            Logger2.Info("SkillManager: 所有技能已启用");
        }

        /// <summary>
        /// 重置所有技能冷却
        /// </summary>
        public void ResetAllCooldowns()
        {
            foreach (var skill in _skills.Values)
            {
                // TODO: 需要在BaseSkill中添加ResetCooldown方法
                // skill.ResetCooldown();
            }
            Logger2.Info("SkillManager: 所有技能冷却已重置");
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public override void _ExitTree()
        {
            // 取消所有技能的事件订阅
            foreach (var skill in _skills.Values)
            {
                UnsubscribeFromSkillEvents(skill);
            }
            
            _skills.Clear();
            _cooldownGroups.Clear();
            
            Logger2.Info("SkillManager: 资源清理完成");
        }
    }
}