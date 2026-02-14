using Godot;
using System;
using System.Collections.Generic;
using Logs;

namespace Skills
{
    /// <summary>
    /// 技能冷却管理器
    /// 专门负责管理所有技能的冷却时间和状态
    /// </summary>
    public partial class SkillCooldownManager : Node
    {
        #region 数据结构

        /// <summary>
        /// 冷却信息结构
        /// </summary>
        private class CooldownInfo
        {
            public string SkillId { get; set; }
            public float TotalDuration { get; set; }
            public float RemainingTime { get; set; }
            public DateTime StartTime { get; set; }
        }

        #endregion

        #region 字段和属性

        /// <summary>
        /// 冷却中的技能字典
        /// </summary>
        private Dictionary<string, CooldownInfo> _cooldowns = new();

        /// <summary>
        /// 冷却完成回调字典
        /// </summary>
        private Dictionary<string, Action<string>> _cooldownCallbacks = new();

        #endregion

        #region 公共方法

        /// <summary>
        /// 开始技能冷却
        /// </summary>
        public void StartCooldown(string skillId, float duration, Action<string> onComplete = null)
        {
            if (duration <= 0)
            {
                Logger2.Debug($"SkillCooldownManager: 技能 {skillId} 无冷却时间");
                onComplete?.Invoke(skillId);
                return;
            }

            var cooldownInfo = new CooldownInfo
            {
                SkillId = skillId,
                TotalDuration = duration,
                RemainingTime = duration,
                StartTime = DateTime.Now
            };

            _cooldowns[skillId] = cooldownInfo;
            
            if (onComplete != null)
            {
                _cooldownCallbacks[skillId] = onComplete;
            }

            Logger2.Debug($"SkillCooldownManager: 技能 {skillId} 开始冷却，时长: {duration}s");
        }

        /// <summary>
        /// 检查技能是否在冷却中
        /// </summary>
        public bool IsOnCooldown(string skillId)
        {
            return _cooldowns.ContainsKey(skillId);
        }

        /// <summary>
        /// 获取技能剩余冷却时间
        /// </summary>
        public float GetRemainingCooldown(string skillId)
        {
            if (_cooldowns.TryGetValue(skillId, out var cooldown))
            {
                return Math.Max(0, cooldown.RemainingTime);
            }
            return 0f;
        }

        /// <summary>
        /// 获取技能冷却进度（0-1）
        /// </summary>
        public float GetCooldownProgress(string skillId)
        {
            if (_cooldowns.TryGetValue(skillId, out var cooldown))
            {
                return 1f - (cooldown.RemainingTime / cooldown.TotalDuration);
            }
            return 1f; // 不在冷却中视为已完成
        }

        /// <summary>
        /// 取消技能冷却
        /// </summary>
        public void CancelCooldown(string skillId)
        {
            if (_cooldowns.Remove(skillId))
            {
                _cooldownCallbacks.Remove(skillId);
                Logger2.Debug($"SkillCooldownManager: 取消技能 {skillId} 的冷却");
            }
        }

        /// <summary>
        /// 重置所有冷却
        /// </summary>
        public void ResetAllCooldowns()
        {
            _cooldowns.Clear();
            _cooldownCallbacks.Clear();
            Logger2.Debug("SkillCooldownManager: 重置所有技能冷却");
        }

        /// <summary>
        /// 获取所有冷却中的技能
        /// </summary>
        public string[] GetCooldownSkills()
        {
            return new List<string>(_cooldowns.Keys).ToArray();
        }

        /// <summary>
        /// 更新冷却时间
        /// </summary>
        public void Update(float deltaTime)
        {
            var completedCooldowns = new List<string>();

            foreach (var kvp in _cooldowns)
            {
                var skillId = kvp.Key;
                var cooldown = kvp.Value;

                cooldown.RemainingTime -= deltaTime;

                if (cooldown.RemainingTime <= 0)
                {
                    completedCooldowns.Add(skillId);
                }
            }

            // 处理完成的冷却
            foreach (var skillId in completedCooldowns)
            {
                CompleteCooldown(skillId);
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 完成冷却
        /// </summary>
        private void CompleteCooldown(string skillId)
        {
            _cooldowns.Remove(skillId);

            // 执行完成回调
            if (_cooldownCallbacks.TryGetValue(skillId, out var callback))
            {
                try
                {
                    callback?.Invoke(skillId);
                }
                catch (Exception ex)
                {
                    Logger2.Error($"SkillCooldownManager: 冷却完成回调执行失败 - {ex.Message}");
                }
                _cooldownCallbacks.Remove(skillId);
            }

            Logger2.Debug($"SkillCooldownManager: 技能 {skillId} 冷却完成");
        }

        #endregion
    }
}