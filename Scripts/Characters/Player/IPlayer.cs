using Godot;
using System;

namespace Characters
{
    /// <summary>
    /// IPlayer：玩家行为的接口契约。
    /// - Init 用于根据外部的 PlayerData 初始化玩家属性与外观。
    /// - Process 由前端包装器每帧调用以推进逻辑/状态机。
    /// - SetState 可以用于外部强制切换玩家状态（例如测试或 UI 调用）。
    ///
    /// 目的：提供一个最小公共接口，使不同的玩家实现类（BasePlayer、MeleePlayer 等）
    /// 能被统一创建与驱动。
    /// </summary>
    public interface IPlayer
    {
        /// <summary>
        /// 根据 PlayerData 初始化玩家（属性、外观、技能等）。
        /// </summary>
        /// <param name="data">从 workshop 或默认加载的 PlayerData 实例</param>
        void Init(PlayerData data);

        /// <summary>
        /// 每帧调用以推进玩家内部逻辑或驱动状态机。
        /// </summary>
        /// <param name="delta">与 Godot 的 delta 相同，单位为秒</param>
        void Process(double delta);

        /// <summary>
        /// 外部强制切换玩家当前状态（按名称）。实现应忽略未知状态并记录错误。
        /// </summary>
        /// <param name="stateName">要切换到的状态名称（例如 "Idle" 或 "Move"）</param>
        void SetState(string stateName);
    }
}
