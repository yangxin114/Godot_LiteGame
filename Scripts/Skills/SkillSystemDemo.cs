using Godot;
using System;
using Logs;

namespace Skills
{
    /// <summary>
    /// 技能系统使用示例
    /// 展示如何在游戏实体中集成和使用技能系统
    /// </summary>
    public partial class SkillSystemDemo : Node2D, ISkillOwner
    {
        private SkillManager _skillManager;
        private SkillInputHandler _inputHandler;

        public override void _Ready()
        {
            InitializeSkillSystem();
            SetupExampleSkills();
        }

        /// <summary>
        /// 初始化技能系统
        /// </summary>
        private void InitializeSkillSystem()
        {
            // 创建技能管理器
            _skillManager = new SkillManager();
            AddChild(_skillManager);
            _skillManager.Initialize(this);

            // 创建输入处理器
            _inputHandler = new SkillInputHandler();
            AddChild(_inputHandler);
            _inputHandler.Initialize(_skillManager);

            Logger2.Info("SkillSystemDemo: 技能系统初始化完成");
        }

        /// <summary>
        /// 设置示例技能
        /// </summary>
        private void SetupExampleSkills()
        {
            // 创建火球术技能
            var fireballSkill = new FireballSkill();
            _skillManager.AddSkill(fireballSkill);
            
            // 绑定按键
            _inputHandler.BindKeyToSkill(Key.Key1, fireballSkill.SkillId);

            // 创建更多示例技能...
            // var healSkill = new HealSkill();
            // _skillManager.AddSkill(healSkill);
            // _inputHandler.BindKeyToSkill(Key.Key2, healSkill.SkillId);

            Logger2.Info("SkillSystemDemo: 示例技能设置完成");
        }

        /// <summary>
        /// 实现ISkillOwner接口
        /// </summary>
        public Node2D GetNode2DOwner()
        {
            return this;
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        public override void _Process(double delta)
        {
            // 技能管理器和输入处理器会自动处理更新
            // 这里可以添加其他游戏逻辑
        }

        /// <summary>
        /// 示例：手动使用技能
        /// </summary>
        public void UseSkillExample()
        {
            // 直接通过技能管理器使用技能
            bool success = _skillManager.TryUseSkill("fireball_basic");
            if (success)
            {
                Logger2.Info("SkillSystemDemo: 成功使用火球术");
            }
            else
            {
                Logger2.Info("SkillSystemDemo: 火球术使用失败");
            }
        }

        /// <summary>
        /// 示例：获取技能状态
        /// </summary>
        public void CheckSkillStatusExample()
        {
            var fireball = _skillManager.GetSkill("fireball_basic");
            if (fireball != null)
            {
                Logger2.Info("SkillSystemDemo: 火球术状态 - 可用: {0}, 冷却中: {1}, 冷却剩余: {2:F1}s", 
                    fireball.IsAvailable(), 
                    fireball.IsOnCooldown(), 
                    fireball.GetRemainingCooldown());
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public override void _ExitTree()
        {
            Logger2.Info("SkillSystemDemo: 清理技能系统资源");
        }
    }
}