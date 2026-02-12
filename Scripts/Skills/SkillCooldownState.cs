using Godot;
using Logs;

namespace Skills
{
    /// <summary>
    /// 技能冷却状态
    /// 技能正在冷却中的状态
    /// </summary>
    public class SkillCooldownState : SkillState
    {
        private double cooldownProgress = 0.0;

        public SkillCooldownState(BaseSkill skill) : base(skill) { }

        public override void Enter()
        {
            cooldownProgress = 0.0;
            Logger2.Info("SkillCooldownState: 技能 {0} 开始冷却", skill.SkillName);
            
            // 可以在这里添加冷却特效
            OnCooldownStarted();
        }

        public override void StateUpdate(double delta)
        {
            cooldownProgress += delta;
            
            // 冷却进度更新
            float progress = (float)(cooldownProgress / skill.CooldownTime);
            
            // 更新冷却进度（如果需要）
            OnCooldownProgress(progress);
            
            // 冷却结束的检查在BaseSkill中处理
        }

        public override void Exit()
        {
            Logger2.Info("SkillCooldownState: 技能 {0} 冷却结束", skill.SkillName);
            OnCooldownEnded();
        }

        /// <summary>
        /// 冷却开始时调用
        /// </summary>
        protected virtual void OnCooldownStarted()
        {
            // 子类可以重写此方法添加冷却开始时的特效
            // 例如：显示冷却图标、播放冷却音效等
        }

        /// <summary>
        /// 冷却进度更新时调用
        /// </summary>
        protected virtual void OnCooldownProgress(float progress)
        {
            // 子类可以重写此方法处理冷却进度
            // 例如：更新UI冷却条
        }

        /// <summary>
        /// 冷却结束时调用
        /// </summary>
        protected virtual void OnCooldownEnded()
        {
            // 子类可以重写此方法添加冷却结束时的处理
            // 例如：播放冷却结束特效
        }
    }
}