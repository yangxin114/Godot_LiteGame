using Godot;
using System;
using System.Collections.Generic;
using System.IO;

namespace Characters
{
    /// <summary>
    /// WorkshopLoader：从创意工坊目录加载所有玩家定义（JSON 文件）。
    /// - 支持传入 Godot 路径（例如 user://workshop）或绝对路径。
    /// - 使用 ProjectSettings.GlobalizePath 将 Godot 路径转换为可用于 System.IO 的绝对路径。
    /// - 解析失败的文件会被跳过并记录日志，保证不会抛出异常导致程序崩溃。
    /// </summary>
    public static class PlayerLoader
    {
        public static string PLAYER_DATA_PATH = "res://Data/Player";
        /// <summary>
        /// 从 workshopPath 读取所有 *.json，并反序列化为 PlayerData 列表。
        /// </summary>
        /// <summary>
        /// 加载所有玩家定义，来源包括：
        ///  - 项目内置路径：res://plugins/workshop （由作者随仓库提交的配置）
        ///  - 外挂目录：workshopPath（通常为 user://plugins/workshop，用户可写）
        /// 同名定义由外部用户目录覆盖（user 目录优先于内置）。
        /// </summary>
        public static List<PlayerData> LoadAll()
        {
            try
            {

                // 先加载内置配置（如果存在），再加载外部用户配置并覆盖同名项
                var dict = new Dictionary<string, PlayerData>(StringComparer.OrdinalIgnoreCase);

                try
                {
                    var absBuiltin = ProjectSettings.GlobalizePath(PLAYER_DATA_PATH);
                    if (Directory.Exists(absBuiltin))
                    {
                        var builtinFiles = Directory.GetFiles(absBuiltin, "*.json", SearchOption.TopDirectoryOnly);
                        foreach (var f in builtinFiles)
                        {
                            try
                            {
                                var text = File.ReadAllText(f);
                                // var pd = PlayerData.FromJson(text);
                                // if (pd != null && !string.IsNullOrEmpty(pd.Name))
                                //     dict[pd.Name] = pd;
                            }
                            catch (Exception e)
                            {
                                GD.PrintErr($"WorkshopLoader: failed to read builtin file {f}: {e.Message}");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    GD.PrintErr($"WorkshopLoader: error while loading builtin configs: {e.Message}");
                }

                // 将 Godot 路径 globalize 为绝对路径，便于使用 System.IO
                string absPath = ProjectSettings.GlobalizePath(PLAYER_DATA_PATH);

                if (!Directory.Exists(absPath))
                {
                    // 如果目录不存在，创建之（首次运行时会创建目录供用户放入文件）
                    try { Directory.CreateDirectory(absPath); } catch { }
                }

                try
                {
                    var files = Directory.Exists(absPath) ? Directory.GetFiles(absPath, "*.json", SearchOption.TopDirectoryOnly) : Array.Empty<string>();
                    foreach (var f in files)
                    {
                        try
                        {
                            var text = File.ReadAllText(f);
                            // var pd = PlayerData.FromJson(text);
                            // if (pd != null && !string.IsNullOrEmpty(pd.Name))
                            //     dict[pd.Name] = pd; // user overrides builtin
                        }
                        catch (Exception e)
                        {
                            GD.PrintErr($"WorkshopLoader: failed to read user file {f}: {e.Message}");
                        }
                    }
                }
                catch (Exception e)
                {
                    GD.PrintErr($"WorkshopLoader: error while loading user configs: {e.Message}");
                }

                // 返回合并后的列表（按键名排序以保证稳定性）
                var result = new List<PlayerData>(dict.Values);
                // result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
                return result;
            }
            catch (Exception e)
            {
                GD.PrintErr($"WorkshopLoader.LoadAll error: {e.Message}");
                return new List<PlayerData>();
            }
        }
    }
}
