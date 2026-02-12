using Godot;
using StateMachine;
using System;

namespace Skills
{
    /// <summary>
    /// 技能状态基类
    /// 所有技能状态都应该继承此类
    /// </summary>
    public abstract class SkillState : State
    {
        protected BaseSkill skill;

        public SkillState(BaseSkill skill) : base(skill)
        {
            this.skill = skill;
        }

        /// <summary>
        /// 状态特定的更新逻辑
        /// </summary>
        public virtual void StateUpdate(double delta) { }

        /// <summary>
        /// 重写基类Update方法，调用状态特定的更新
        /// </summary>
        public override void Update(double delta)
        {
            base.Update(delta);
            StateUpdate(delta);
        }
    }
}