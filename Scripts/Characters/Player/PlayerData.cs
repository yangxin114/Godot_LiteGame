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
    public class PlayerData
    {
        /// <summary>玩家名称</summary>
        public string Name { get; set; }

        /// <summary>移动速度（像素/秒，默认 200）</summary>
        public float Speed { get; set; } = 200f;

        /// <summary>生命值</summary>
        public int Health { get; set; } = 10;

        /// <summary>Sprite 资源路径（推荐 res:// 路径），若为空则使用 ColorHex 创建占位 ColorRect。</summary>
        public string SpritePath { get; set; }

        /// <summary>颜色的十六进制字符串（例如 "#RRGGBBAA" 或 "#RRGGBB"）</summary>
        public string ColorHex { get; set; } = "#FFFFFFFF";

        /// <summary>玩家技能标识列表（可在 BasePlayer 中映射为具体行为）</summary>
        public List<string> Abilities { get; set; } = new();

        /// <summary>
        /// 将 ColorHex 转换为 Godot 的 Color 对象（便于在场景中直接使用）。
        /// </summary>
        public Color Color => ColorFromHex(ColorHex);

        /// <summary>
        /// 返回一个默认的 PlayerData 实例（用于回退）。
        /// </summary>
        public static PlayerData Default()
        {
            return new PlayerData
            {
                Name = "Default",
                Speed = 200f,
                Health = 10,
                ColorHex = "#FFDDAAFF",
            };
        }

        /// <summary>
        /// 从 JSON 反序列化为 PlayerData。出现错误时返回 null 并记录日志。
        /// </summary>
        public static PlayerData FromJson(string json)
        {
            try
            {
                var doc = JsonSerializer.Deserialize<PlayerData>(json);
                return doc ?? Default();
            }
            catch (Exception e)
            {
                GD.PrintErr($"PlayerData.FromJson parse error: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 将十六进制颜色字符串解析为 Color（支持 #RRGGBB 和 #RRGGBBAA）。
        /// 若解析失败返回白色。
        /// </summary>
        private static Color ColorFromHex(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return Colors.White;
            try
            {
                if (hex.StartsWith("#"))
                {
                    var s = hex.Substring(1);
                    uint v = Convert.ToUInt32(s, 16);
                    if (s.Length == 8)
                    {
                        byte a = (byte)(v & 0xFF);
                        byte b = (byte)((v >> 8) & 0xFF);
                        byte g = (byte)((v >> 16) & 0xFF);
                        byte r = (byte)((v >> 24) & 0xFF);
                        return new Color(r / 255.0f, g / 255.0f, b / 255.0f, a / 255.0f);
                    }
                    else if (s.Length == 6)
                    {
                        byte b = (byte)(v & 0xFF);
                        byte g = (byte)((v >> 8) & 0xFF);
                        byte r = (byte)((v >> 16) & 0xFF);
                        return new Color(r / 255.0f, g / 255.0f, b / 255.0f, 1.0f);
                    }
                }
            }
            catch { }
            return Colors.White;
        }
    }
}
