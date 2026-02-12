using Godot;
using Logs;

namespace Skills
{
    /// <summary>
    /// 技能就绪状态
    /// 技能可以被使用的状态
    /// </summary>
    public class SkillReadyState : SkillState
    {
        public SkillReadyState(BaseSkill skill) : base(skill) { }

        public override void Enter()
        {
            Logger2.Debug("SkillReadyState: 技能 {0} 进入就绪状态", skill.SkillName);
        }

        public override void StateUpdate(double delta)
        {
            // 就绪状态下不需要特殊处理
            // 等待TryUseSkill调用来触发状态转换
        }

        public override void Exit()
        {
            Logger2.Debug("SkillReadyState: 技能 {0} 离开就绪状态", skill.SkillName);
        }
    }
}