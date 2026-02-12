using Godot;
using System.Collections.Generic;
using Logs;

namespace Skills
{
    /// <summary>
    /// 技能输入处理器
    /// 处理技能快捷键输入，将按键映射到具体技能
    /// </summary>
    public partial class SkillInputHandler : Node
    {
        private SkillManager _skillManager;
        private Dictionary<Key, string> _keyToSkillMap = new();
        private Dictionary<string, Key> _skillToKeyMap = new();

        public override void _Ready()
        {
            Logger2.Info("SkillInputHandler: 技能输入处理器初始化");
        }

        /// <summary>
        /// 初始化输入处理器
        /// </summary>
        public void Initialize(SkillManager skillManager)
        {
            _skillManager = skillManager;
            Logger2.Info("SkillInputHandler: 输入处理器初始化完成");
        }

        /// <summary>
        /// 绑定按键到技能
        /// </summary>
        public void BindKeyToSkill(Key key, string skillId)
        {
            // 移除旧的绑定
            if (_keyToSkillMap.ContainsKey(key))
            {
                string oldSkillId = _keyToSkillMap[key];
                _skillToKeyMap.Remove(oldSkillId);
                Logger2.Debug("SkillInputHandler: 移除按键 {0} 的旧绑定 {1}", key, oldSkillId);
            }

            if (_skillToKeyMap.ContainsKey(skillId))
            {
                Key oldKey = _skillToKeyMap[skillId];
                _keyToSkillMap.Remove(oldKey);
                Logger2.Debug("SkillInputHandler: 移除技能 {0} 的旧按键 {1}", skillId, oldKey);
            }

            // 添加新绑定
            _keyToSkillMap[key] = skillId;
            _skillToKeyMap[skillId] = key;
            
            Logger2.Info("SkillInputHandler: 绑定按键 {0} 到技能 {1}", key, skillId);
        }

        /// <summary>
        /// 解除按键绑定
        /// </summary>
        public void UnbindKey(Key key)
        {
            if (_keyToSkillMap.ContainsKey(key))
            {
                string skillId = _keyToSkillMap[key];
                _keyToSkillMap.Remove(key);
                _skillToKeyMap.Remove(skillId);
                Logger2.Info("SkillInputHandler: 解除按键 {0} 的绑定", key);
            }
        }

        /// <summary>
        /// 解除技能绑定
        /// </summary>
        public void UnbindSkill(string skillId)
        {
            if (_skillToKeyMap.ContainsKey(skillId))
            {
                Key key = _skillToKeyMap[skillId];
                _skillToKeyMap.Remove(skillId);
                _keyToSkillMap.Remove(key);
                Logger2.Info("SkillInputHandler: 解除技能 {0} 的绑定", skillId);
            }
        }

        /// <summary>
        /// 获取技能绑定的按键
        /// </summary>
        public Key GetKeyForSkill(string skillId)
        {
            return _skillToKeyMap.GetValueOrDefault(skillId, Key.None);
        }

        /// <summary>
        /// 获取按键绑定的技能
        /// </summary>
        public string GetSkillForKey(Key key)
        {
            return _keyToSkillMap.GetValueOrDefault(key, "");
        }

        /// <summary>
        /// 处理输入事件
        /// </summary>
        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey keyEvent && keyEvent.Pressed)
            {
                if (_keyToSkillMap.ContainsKey(keyEvent.Keycode))
                {
                    string skillId = _keyToSkillMap[keyEvent.Keycode];
                    Logger2.Debug("SkillInputHandler: 按键 {0} 触发技能 {1}", keyEvent.Keycode, skillId);
                    
                    // 尝试使用技能
                    bool success = _skillManager?.TryUseSkill(skillId) ?? false;
                    
                    if (!success)
                    {
                        // 技能使用失败，可能是冷却中或条件不满足
                        HandleSkillUseFailure(skillId);
                    }
                }
            }
        }

        /// <summary>
        /// 处理技能使用失败
        /// </summary>
        private void HandleSkillUseFailure(string skillId)
        {
            var skill = _skillManager?.GetSkill(skillId);
            if (skill == null) return;

            if (skill.IsOnCooldown())
            {
                float remainingCooldown = skill.GetRemainingCooldown();
                Logger2.Info("SkillInputHandler: 技能 {0} 冷却中，剩余 {1:F1} 秒", 
                    skill.SkillName, remainingCooldown);
                // TODO: 显示冷却提示UI
            }
            else if (skill.GetCurrentState() == "Disabled")
            {
                Logger2.Info("SkillInputHandler: 技能 {0} 已被禁用", skill.SkillName);
                // TODO: 显示禁用提示UI
            }
            else if (!skill.CanUseSkill())
            {
                Logger2.Info("SkillInputHandler: 技能 {0} 条件不满足", skill.SkillName);
                // TODO: 显示条件不满足提示UI
            }
        }

        /// <summary>
        /// 获取所有按键绑定
        /// </summary>
        public Dictionary<Key, string> GetAllBindings()
        {
            return new Dictionary<Key, string>(_keyToSkillMap);
        }

        /// <summary>
        /// 重置所有绑定
        /// </summary>
        public void ResetAllBindings()
        {
            _keyToSkillMap.Clear();
            _skillToKeyMap.Clear();
            Logger2.Info("SkillInputHandler: 所有按键绑定已重置");
        }

        /// <summary>
        /// 加载按键配置
        /// </summary>
        public void LoadBindings(Dictionary<Key, string> bindings)
        {
            ResetAllBindings();
            foreach (var binding in bindings)
            {
                BindKeyToSkill(binding.Key, binding.Value);
            }
            Logger2.Info("SkillInputHandler: 按键配置已加载");
        }

        /// <summary>
        /// 保存按键配置
        /// </summary>
        public Dictionary<Key, string> SaveBindings()
        {
            return new Dictionary<Key, string>(_keyToSkillMap);
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public override void _ExitTree()
        {
            _keyToSkillMap.Clear();
            _skillToKeyMap.Clear();
            Logger2.Info("SkillInputHandler: 资源清理完成");
        }
    }
}