using Godot;
using System;
using System.Threading.Tasks;
using Logs;
using Scenes;

/// <summary>
/// 场景管理工具类
/// 提供便捷的场景操作方法和常用功能
/// </summary>
public static class SceneUtils
{
    #region 快捷场景切换方法
    
    /// <summary>
    /// 快速切换到主菜单场景
    /// </summary>
    public static async Task<Error> GoToMainMenu(float transitionTime = 0.5f)
    {
        return await SceneManager.Instance.ChangeSceneAsync("res://Scenes/MainMenu.tscn", transitionTime);
    }
    
    /// <summary>
    /// 快速切换到游戏场景
    /// </summary>
    public static async Task<Error> GoToGame(float transitionTime = 0.5f)
    {
        return await SceneManager.Instance.ChangeSceneAsync("res://Scenes/Game.tscn", transitionTime);
    }
    
    /// <summary>
    /// 快速打开暂停菜单
    /// </summary>
    public static async Task<Error> OpenPauseMenu(float transitionTime = 0.3f)
    {
        return await SceneManager.Instance.PushSceneAsync("res://Scenes/PauseMenu.tscn", transitionTime);
    }
    
    /// <summary>
    /// 快速关闭暂停菜单（返回游戏）
    /// </summary>
    public static async Task<bool> ClosePauseMenu(float transitionTime = 0.3f)
    {
        return await SceneManager.Instance.PopSceneAsync(transitionTime);
    }
    
    /// <summary>
    /// 快速重启当前场景
    /// </summary>
    public static async Task<Error> RestartCurrentScene(float transitionTime = 0.5f)
    {
        var currentScene = SceneManager.Instance.GetCurrentScenePath();
        if (string.IsNullOrEmpty(currentScene))
        {
            Logger2.Error("SceneUtils: 无法获取当前场景路径");
            return Error.Failed;
        }
        
        return await SceneManager.Instance.ChangeSceneAsync(currentScene, transitionTime);
    }
    
    #endregion

    #region 场景状态检查
    
    /// <summary>
    /// 检查是否在主菜单场景
    /// </summary>
    public static bool IsInMainMenu()
    {
        var currentScene = SceneManager.Instance.GetCurrentScenePath();
        return currentScene.EndsWith("MainMenu.tscn");
    }
    
    /// <summary>
    /// 检查是否在游戏中
    /// </summary>
    public static bool IsInGame()
    {
        var currentScene = SceneManager.Instance.GetCurrentScenePath();
        return currentScene.EndsWith("Game.tscn") || currentScene.EndsWith("Level1.tscn");
    }
    
    /// <summary>
    /// 检查是否在暂停菜单
    /// </summary>
    public static bool IsInPauseMenu()
    {
        var currentScene = SceneManager.Instance.GetCurrentScenePath();
        return currentScene.EndsWith("PauseMenu.tscn");
    }
    
    #endregion

    #region 场景栈操作
    
    /// <summary>
    /// 安全返回上一场景（带错误处理）
    /// </summary>
    public static async Task<bool> SafeGoBack(float transitionTime = 0.4f)
    {
        try
        {
            if (SceneManager.Instance.GetSceneStackDepth() > 0)
            {
                return await SceneManager.Instance.PopSceneAsync(transitionTime);
            }
            else
            {
                Logger2.Warn("SceneUtils: 场景栈为空，无法返回");
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger2.Error($"SceneUtils: 返回上一场景失败 - {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 清空场景栈并返回主菜单
    /// </summary>
    public static async Task<Error> ClearStackAndGoHome(float transitionTime = 0.6f)
    {
        try
        {
            SceneManager.Instance.ClearSceneStack();
            return await GoToMainMenu(transitionTime);
        }
        catch (Exception ex)
        {
            Logger2.Error($"SceneUtils: 清空栈并返回主页失败 - {ex.Message}");
            return Error.Failed;
        }
    }
    
    #endregion

    #region 场景预加载管理
    
    private static readonly System.Collections.Generic.HashSet<string> _preloadedScenes = 
        new System.Collections.Generic.HashSet<string>();
    
    /// <summary>
    /// 预加载常用场景
    /// </summary>
    public static async Task PreloadCommonScenes()
    {
        var commonScenes = new[]
        {
            "res://Scenes/MainMenu.tscn",
            "res://Scenes/Game.tscn",
            "res://Scenes/PauseMenu.tscn",
            "res://Scenes/Settings.tscn"
        };
        
        Logger2.Info("SceneUtils: 开始预加载常用场景");
        
        foreach (var scenePath in commonScenes)
        {
            if (!_preloadedScenes.Contains(scenePath) && SceneManager.Instance.SceneExists(scenePath))
            {
                var packedScene = await SceneManager.Instance.PreloadSceneAsync(scenePath);
                if (packedScene != null)
                {
                    _preloadedScenes.Add(scenePath);
                    Logger2.Debug($"SceneUtils: 预加载完成 - {scenePath}");
                }
            }
        }
        
        Logger2.Info($"SceneUtils: 预加载完成，共 {_preloadedScenes.Count} 个场景");
    }
    
    /// <summary>
    /// 检查场景是否已预加载
    /// </summary>
    public static bool IsScenePreloaded(string scenePath)
    {
        return _preloadedScenes.Contains(scenePath);
    }
    
    #endregion

    #region 场景过渡效果
    
    /// <summary>
    /// 使用自定义颜色进行场景切换
    /// </summary>
    public static async Task<Error> ChangeSceneWithColor(string scenePath, Color transitionColor, float duration = 0.5f)
    {
        try
        {
            // 暂时使用默认过渡，因为SceneManager没有暴露GetTransitionNode方法
            return await SceneManager.Instance.ChangeSceneAsync(scenePath, duration);
        }
        catch (Exception ex)
        {
            Logger2.Error($"SceneUtils: 彩色过渡切换失败 - {ex.Message}");
            return Error.Failed;
        }
    }
    
    /// <summary>
    /// 快速淡入效果
    /// </summary>
    public static async Task QuickFadeIn(float duration = 0.2f)
    {
        try
        {
            // 需要访问过渡节点来实现
            await Task.Delay((int)(duration * 1000));
        }
        catch (Exception ex)
        {
            Logger2.Error($"SceneUtils: 快速淡入失败 - {ex.Message}");
        }
    }
    
    /// <summary>
    /// 快速淡出效果
    /// </summary>
    public static async Task QuickFadeOut(float duration = 0.2f)
    {
        try
        {
            // 需要访问过渡节点来实现
            await Task.Delay((int)(duration * 1000));
        }
        catch (Exception ex)
        {
            Logger2.Error($"SceneUtils: 快速淡出失败 - {ex.Message}");
        }
    }
    
    #endregion

    #region 调试和诊断
    
    /// <summary>
    /// 打印完整的场景状态信息
    /// </summary>
    public static void PrintSceneStatus()
    {
        try
        {
            Logger2.Info("=== 场景状态报告 ===");
            Logger2.Info($"当前场景: {SceneManager.Instance.GetCurrentScenePath()}");
            Logger2.Info($"场景栈深度: {SceneManager.Instance.GetSceneStackDepth()}");
            Logger2.Info($"预加载场景数: {_preloadedScenes.Count}");
            Logger2.Info($"是否正在切换: {IsSceneChanging()}");
            
            var stack = SceneManager.Instance.GetSceneStackSnapshot();
            if (stack.Length > 0)
            {
                Logger2.Info("场景栈内容:");
                for (int i = 0; i < stack.Length; i++)
                {
                    Logger2.Info($"  {i}: {stack[i]}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger2.Error($"SceneUtils: 打印场景状态失败 - {ex.Message}");
        }
    }
    
    /// <summary>
    /// 检查场景管理器是否正在切换场景
    /// </summary>
    public static bool IsSceneChanging()
    {
        // 这需要SceneManager暴露内部状态
        return false; // 占位实现
    }
    
    #endregion
}