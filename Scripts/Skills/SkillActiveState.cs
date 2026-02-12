using Godot;
using Logs;

namespace Skills
{
    /// <summary>
    /// 技能激活状态
    /// 技能效果正在生效的状态
    /// </summary>
    public class SkillActiveState : SkillState
    {
        private double activeTimer = 0.0;
        private double duration = 0.0; // 技能持续时间

        public SkillActiveState(BaseSkill skill) : base(skill) { }

        public override void Enter()
        {
            activeTimer = 0.0;
            duration = GetSkillDuration();
            
            Logger2.Info("SkillActiveState: 技能 {0} 激活，持续时间: {1}s", skill.SkillName, duration);
            
            // 执行技能激活时的逻辑
            OnSkillActivated();
        }

        public override void StateUpdate(double delta)
        {
            activeTimer += delta;
            
            // 更新技能效果
            UpdateSkillEffect(delta);
            
            // 检查技能是否应该结束
            if (duration > 0 && activeTimer >= duration)
            {
                EndSkill();
            }
        }

        public override void Exit()
        {
            Logger2.Info("SkillActiveState: 技能 {0} 结束", skill.SkillName);
            OnSkillDeactivated();
        }

        /// <summary>
        /// 获取技能持续时间
        /// </summary>
        protected virtual double GetSkillDuration()
        {
            // 默认持续时间为0（瞬时技能）
            // 子类可以重写此方法返回具体的持续时间
            return 0.0;
        }

        /// <summary>
        /// 技能激活时调用
        /// </summary>
        protected virtual void OnSkillActivated()
        {
            // 子类可以重写此方法添加技能激活时的特效
            // 例如：播放技能特效、应用buff等
        }

        /// <summary>
        /// 更新技能效果
        /// </summary>
        protected virtual void UpdateSkillEffect(double delta)
        {
            // 子类可以重写此方法实现持续性的技能效果
            // 例如：持续伤害、持续治疗等
        }

        /// <summary>
        /// 技能停用时调用
        /// </summary>
        protected virtual void OnSkillDeactivated()
        {
            // 子类可以重写此方法添加技能结束时的清理工作
            // 例如：移除buff、停止特效等
        }

        /// <summary>
        /// 结束技能
        /// </summary>
        protected virtual void EndSkill()
        {
            // 技能自然结束，回到就绪状态
            skill.CancelSkill();
        }

        /// <summary>
        /// 强制中断技能
        /// </summary>
        public virtual void InterruptSkill()
        {
            Logger2.Info("SkillActiveState: 技能 {0} 被中断", skill.SkillName);
            skill.CancelSkill();
        }
    }
}