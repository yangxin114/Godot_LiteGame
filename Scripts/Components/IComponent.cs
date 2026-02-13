using Entities;
namespace Components
{
    /// <summary>
    /// 组件接口 - 定义所有组件的通用行为
    /// </summary>
    public interface IComponent
    {
        /// <summary>
        /// 初始化组件
        /// </summary>
        void Initialize(CharacterEntity entity);

        /// <summary>
        /// 启动组件（所有组件初始化后调用）
        /// </summary>
        void Start();

        /// <summary>
        /// 每帧更新
        /// </summary>
        void Update(float delta);

        /// <summary>
        /// 物理帧更新
        /// </summary>
        void PhysicsUpdate(float delta);

        /// <summary>
        /// 清理资源
        /// </summary>
        void Cleanup();

        /// <summary>
        /// 组件是否启用
        /// </summary>
        bool IsEnabled { get; set; }
    }
}