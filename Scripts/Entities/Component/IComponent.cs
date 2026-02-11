namespace Entities
{
    /// <summary>
    /// 所有组件接口
    /// </summary>
    public interface IComponent
    {
        void Initialize();

        void Update(double delta);

        void Dispose();
    }

}