using Godot;
using System;
using Logs;

namespace Skills
{
    /// <summary>
    /// 技能系统入口管理类
    /// 整合所有技能相关组件，提供统一的技能系统接口
    /// </summary>
    public partial class SkillSystem : Node
    {
        #region 字段和属性

        /// <summary>
        /// 技能拥有者
        /// </summary>
        public ISkill SkillOwner { get; set; }

        /// <summary>
        /// 技能管理器
        /// </summary>
        public SkillManager SkillManager { get; private set; }

        /// <summary>
        /// 技能效果处理器
        /// </summary>
        public SkillEffectProcessor EffectProcessor { get; private set; }

        /// <summary>
        /// 技能数据加载器（静态引用）
        /// </summary>
        public SkillDataLoader DataLoader => SkillDataLoader.Instance;

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

        public override void _ExitTree()
        {
            Cleanup();
        }

        #endregion

        #region 初始化和设置

        /// <summary>
        /// 初始化技能系统
        /// </summary>
        private void Initialize()
        {
            if (_isInitialized)
            {
                Logger2.Warn("SkillSystem: 已经初始化过了");
                return;
            }

            if (SkillOwner == null)
            {
                Logger2.Error("SkillSystem: 技能拥有者未设置");
                return;
            }

            // 创建技能管理器
            SkillManager = new SkillManager();
            SkillManager.SkillOwner = SkillOwner;
            AddChild(SkillManager);

            // 创建技能效果处理器
            EffectProcessor = new SkillEffectProcessor();
            AddChild(EffectProcessor);

            // 确保数据加载器已初始化
            EnsureDataLoader();

            _isInitialized = true;
            
            Logger2.Info("SkillSystem: 技能系统初始化完成");
        }

        /// <summary>
        /// 确保数据加载器已初始化
        /// </summary>
        private void EnsureDataLoader()
        {
            if (SkillDataLoader.Instance == null)
            {
                var dataLoader = new SkillDataLoader();
                GetTree().Root.AddChild(dataLoader);
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        private void Cleanup()
        {
            Logger2.Info("SkillSystem: 技能系统资源清理完成");
        }

        #endregion

        #region 技能操作接口

        /// <summary>
        /// 学习技能
        /// </summary>
        public bool LearnSkill(string skillId)
        {
            if (!_isInitialized) return false;

            var skillData = DataLoader.GetSkillData(skillId);
            if (skillData == null)
            {
                Logger2.Warn($"SkillSystem: 未找到技能数据 {skillId}");
                return false;
            }

            return SkillManager.LearnSkill(skillId, skillData);
        }

        /// <summary>
        /// 忘记技能
        /// </summary>
        public bool ForgetSkill(string skillId)
        {
            if (!_isInitialized) return false;
            return SkillManager.ForgetSkill(skillId);
        }

        /// <summary>
        /// 升级技能
        /// </summary>
        public bool UpgradeSkill(string skillId)
        {
            if (!_isInitialized) return false;
            return SkillManager.UpgradeSkill(skillId);
        }

        /// <summary>
        /// 释放技能
        /// </summary>
        public void CastSkill(string skillId, ISkill target = null, Vector2? targetPosition = null)
        {
            if (!_isInitialized) return;
            SkillManager.CastSkill(skillId, target, targetPosition);
        }

        /// <summary>
        /// 中断当前施法
        /// </summary>
        public void InterruptCurrentCast()
        {
            if (!_isInitialized) return;
            SkillManager.InterruptCurrentCast();
        }

        /// <summary>
        /// 检查是否可以释放技能
        /// </summary>
        public bool CanCastAnySkill()
        {
            if (!_isInitialized) return false;
            return SkillManager.CanCastAnySkill();
        }

        #endregion

        #region 效果处理接口

        /// <summary>
        /// 应用技能效果
        /// </summary>
        public void ApplyEffect(SkillEffect effect, ISkill caster, ISkill target, string sourceSkillId)
        {
            if (!_isInitialized) return;
            EffectProcessor.ApplyEffect(effect, caster, target, sourceSkillId);
        }

        /// <summary>
        /// 移除效果
        /// </summary>
        public void RemoveEffect(string effectId, ISkill target, string sourceSkillId = null)
        {
            if (!_isInitialized) return;
            EffectProcessor.RemoveEffect(effectId, target, sourceSkillId);
        }

        /// <summary>
        /// 移除目标的所有效果
        /// </summary>
        public void RemoveAllEffects(ISkill target)
        {
            if (!_isInitialized) return;
            EffectProcessor.RemoveAllEffects(target);
        }

        #endregion

        #region 系统状态查询

        /// <summary>
        /// 获取技能实例
        /// </summary>
        public BaseSkill GetSkill(string skillId)
        {
            if (!_isInitialized) return null;
            return SkillManager.GetSkill(skillId);
        }

        /// <summary>
        /// 获取所有已学技能
        /// </summary>
        public BaseSkill[] GetAllSkills()
        {
            if (!_isInitialized) return new BaseSkill[0];
            return SkillManager.GetAllSkills();
        }

        /// <summary>
        /// 检查是否已学会技能
        /// </summary>
        public bool HasLearnedSkill(string skillId)
        {
            if (!_isInitialized) return false;
            return GetSkill(skillId) != null;
        }

        /// <summary>
        /// 检查技能是否在冷却中
        /// </summary>
        public bool IsSkillOnCooldown(string skillId)
        {
            var skill = GetSkill(skillId);
            return skill?.IsSkillOnCooldown() ?? false;
        }

        /// <summary>
        /// 获取技能剩余冷却时间
        /// </summary>
        public float GetSkillCooldownRemaining(string skillId)
        {
            var skill = GetSkill(skillId);
            return skill?.GetSkillCooldownRemaining() ?? 0f;
        }

        #endregion

        #region 系统管理

        /// <summary>
        /// 重新加载技能数据
        /// </summary>
        public void ReloadSkillData()
        {
            DataLoader.ReloadAllData();
            Logger2.Info("SkillSystem: 重新加载技能数据");
        }

        /// <summary>
        /// 获取系统状态信息
        /// </summary>
        public string GetSystemStatus()
        {
            if (!_isInitialized)
                return "技能系统未初始化";

            var skillCount = GetAllSkills().Length;
            var dataCount = DataLoader.GetAllSkillData().Length;
            
            return $"技能系统状态 - 已学技能: {skillCount}, 数据库技能: {dataCount}";
        }

        #endregion
    }
}