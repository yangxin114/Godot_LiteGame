using Godot;
using Logs;

namespace Skills
{
    /// <summary>
    /// 技能施法状态
    /// 技能正在施放中的状态
    /// </summary>
    public class SkillCastingState : SkillState
    {
        private double castProgress = 0.0;

        public SkillCastingState(BaseSkill skill) : base(skill) { }

        public override void Enter()
        {
            castProgress = 0.0;
            Logger2.Info("SkillCastingState: 技能 {0} 开始施法", skill.SkillName);
            
            // 可以在这里添加施法特效、音效等
            OnCastingStarted();
        }

        public override void StateUpdate(double delta)
        {
            castProgress += delta;
            
            // 施法进度更新（如果需要的话）
            float progress = (float)(castProgress / skill.CastTime);
            
            // 可以在这里更新施法进度条或其他UI元素
            OnCastingProgress(progress);
            
            // 检查是否施法完成（这个检查实际上在BaseSkill中已完成）
            // 这里主要是为了演示状态更新逻辑
        }

        public override void Exit()
        {
            Logger2.Info("SkillCastingState: 技能 {0} 施法结束", skill.SkillName);
            OnCastingEnded();
        }

        /// <summary>
        /// 施法开始时调用
        /// </summary>
        protected virtual void OnCastingStarted()
        {
            // 子类可以重写此方法添加施法开始时的特效
            // 例如：播放施法动画、显示施法条等
        }

        /// <summary>
        /// 施法进度更新时调用
        /// </summary>
        protected virtual void OnCastingProgress(float progress)
        {
            // 子类可以重写此方法处理施法进度
            // 例如：更新UI进度条
        }

        /// <summary>
        /// 施法结束时调用
        /// </summary>
        protected virtual void OnCastingEnded()
        {
            // 子类可以重写此方法添加施法结束时的处理
        }
    }
}