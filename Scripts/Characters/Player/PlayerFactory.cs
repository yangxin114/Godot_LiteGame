using Godot;
using System;

namespace Characters
{
    /// <summary>
    /// PlayerFactory：负责根据 PlayerData 创建合适的玩家节点。
    /// 当前实现简单返回 BasePlayer 的实例。后续可根据 PlayerData 中的类型字段
    /// （例如 "Melee"、"Ranged"）来实例化不同的 PackedScene。
    /// </summary>
    public static class PlayerFactory
    {
        /// <summary>
        /// 创建玩家实例并根据传入的 PlayerData 进行初始化。
        /// </summary>
        /// <param name="parent">将被添加到的父节点（暂未用于布局）。</param>
        /// <param name="data">玩家定义数据</param>
        /// <returns>已初始化的 BasePlayer</returns>
        public static BasePlayer Create(Node parent, PlayerData data)
        {
            var p = new BasePlayer();
            p.Init(data);
            return p;
        }
    }
}
