namespace Skills
{
    /// <summary>
    /// 定义技能组件的接口，所有的技能各类组件均需要实现该接口
    /// </summary>
    public interface ISkillComponent
    {
        /// <summary>
        /// 组件执行方法，可以处理数值，播放动画，移动等等，根据不同技能组件的功能不同实现不同。
        /// </summary>
        /// <param name="context">技能组件执行所需的上下文</param>
        public void Execute(SkillContext context);

        /// <summary>
        /// 技能组件是否可以执行
        /// </summary>
        public bool CanExecute(SkillContext context);

        /// <summary>
        /// 技能组件退出方法，处理结束时的资源销毁等
        /// </summary>
        /// <param name="context">技能组件退出所需的上下文</param>
        public void Exit(SkillContext context);
    }
}