using Godot;
using Logs;

namespace Skills
{
    /// <summary>
    /// 技能禁用状态
    /// 技能被禁用无法使用的状态
    /// </summary>
    public class SkillDisabledState : SkillState
    {
        public SkillDisabledState(BaseSkill skill) : base(skill) { }

        public override void Enter()
        {
            Logger2.Info("SkillDisabledState: 技能 {0} 被禁用", skill.SkillName);
            
            // 可以在这里添加禁用特效
            OnSkillDisabled();
        }

        public override void StateUpdate(double delta)
        {
            // 禁用状态下不执行任何操作
            // 等待EnableSkill调用来恢复
        }

        public override void Exit()
        {
            Logger2.Info("SkillDisabledState: 技能 {0} 解除禁用", skill.SkillName);
            OnSkillEnabled();
        }

        /// <summary>
        /// 技能被禁用时调用
        /// </summary>
        protected virtual void OnSkillDisabled()
        {
            // 子类可以重写此方法添加禁用时的特效
            // 例如：显示禁用图标、灰色化技能按钮等
        }

        /// <summary>
        /// 技能被启用时调用
        /// </summary>
        protected virtual void OnSkillEnabled()
        {
            // 子类可以重写此方法添加启用时的处理
            // 例如：移除禁用特效、恢复正常颜色等
        }
    }
}