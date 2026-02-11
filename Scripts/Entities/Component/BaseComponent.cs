namespace Entities
{
    /// <summary>
    /// 组件基类
    /// 提供默认实现
    /// </summary>
    public abstract class BaseComponent : IComponent
    {
        public Entity Entity { get; private set; }

        protected BaseComponent(Entity entity)
        {
            Entity = entity;
        }

        public virtual void Initialize() { }

        public virtual void Update(double delta) { }

        public virtual void Dispose() { }
    }
}