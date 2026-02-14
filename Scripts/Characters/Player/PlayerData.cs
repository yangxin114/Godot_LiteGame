using Numerical;
using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Characters
{
    /// <summary>
    /// PlayerData：用于序列化/反序列化玩家可定制数据的 POCO。
    /// 字段包括 Name、Speed、Health、SpritePath、ColorHex、Abilities 等。
    /// 可由创意工坊（WorkshopLoader）从 JSON 文件中读取并传递给 PlayerFactory。
    /// </summary>
    [GlobalClass]
    public partial class PlayerData : Resource
    {
        /// <summary>玩家名称</summary>
        [Export] public string Name { get; set; }

        /// <summary>移动速度（像素/秒，默认 200）</summary>
        [Export] public float MoveSpeed { get; set; }

        /// <summary>当前生命值</summary>
        [Export] public float CurrentHP { get; set; }

        /// <summary>最大生命值</summary>
        [Export] public float MaxHP { get; set; }

    }
}