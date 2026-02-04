using Godot;
using System.Collections.Generic;

/// <summary>
/// 场景配置类
/// 集中管理所有场景路径和配置选项
/// </summary>
public static class SceneConfig
{
    #region 场景路径定义
    
    // 主要场景
    public const string START_SCENE = "res://Scenes/Start.tscn";
    public const string MAIN_MENU_SCENE = "res://Scenes/MainMenu.tscn";
    public const string GAME_SCENE = "res://Scenes/Game.tscn";
    public const string LEVEL1_SCENE = "res://Scenes/Level1.tscn";
    
    // UI场景
    public const string PAUSE_MENU_SCENE = "res://Scenes/PauseMenu.tscn";
    public const string SETTINGS_SCENE = "res://Scenes/Settings.tscn";
    public const string GAME_OVER_SCENE = "res://Scenes/GameOver.tscn";
    public const string VICTORY_SCENE = "res://Scenes/Victory.tscn";
    
    // 特殊场景
    public const string LOADING_SCENE = "res://Scenes/Loading.tscn";
    public const string CREDITS_SCENE = "res://Scenes/Credits.tscn";
    
    #endregion

    #region 过渡配置
    
    // 默认过渡时间
    public const float DEFAULT_TRANSITION_TIME = 0.4f;
    
    // 快速过渡时间
    public const float FAST_TRANSITION_TIME = 0.2f;
    
    // 慢速过渡时间
    public const float SLOW_TRANSITION_TIME = 0.8f;
    
    // 默认过渡颜色
    public static readonly Color DEFAULT_TRANSITION_COLOR = Colors.Black;
    
    #endregion

    #region 预加载配置
    
    // 应该预加载的场景列表
    public static readonly string[] PRELOAD_SCENES = 
    {
        MAIN_MENU_SCENE,
        GAME_SCENE,
        PAUSE_MENU_SCENE,
        SETTINGS_SCENE
    };
    
    // 预加载优先级（数字越小优先级越高）
    public static readonly Dictionary<string, int> PRELOAD_PRIORITY = new Dictionary<string, int>
    {
        { MAIN_MENU_SCENE, 1 },
        { GAME_SCENE, 2 },
        { PAUSE_MENU_SCENE, 3 },
        { SETTINGS_SCENE, 4 }
    };
    
    #endregion

    #region 场景组定义
    
    /// <summary>
    /// 获取主菜单相关场景
    /// </summary>
    public static string[] GetMainMenuScenes()
    {
        return new[] { MAIN_MENU_SCENE, SETTINGS_SCENE, CREDITS_SCENE };
    }
    
    /// <summary>
    /// 获取游戏相关场景
    /// </summary>
    public static string[] GetGameScenes()
    {
        return new[] { GAME_SCENE, LEVEL1_SCENE, PAUSE_MENU_SCENE, GAME_OVER_SCENE, VICTORY_SCENE };
    }
    
    /// <summary>
    /// 获取UI相关场景
    /// </summary>
    public static string[] GetUIScenes()
    {
        return new[] { PAUSE_MENU_SCENE, SETTINGS_SCENE, GAME_OVER_SCENE, VICTORY_SCENE };
    }
    
    #endregion

    #region 场景验证
    
    /// <summary>
    /// 验证场景路径是否有效
    /// </summary>
    public static bool ValidateScenePath(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath))
            return false;
            
        // 检查是否以res://开头
        if (!scenePath.StartsWith("res://"))
            return false;
            
        // 检查是否以.tscn结尾
        if (!scenePath.EndsWith(".tscn"))
            return false;
            
        return true;
    }
    
    /// <summary>
    /// 获取场景名称（从路径中提取）
    /// </summary>
    public static string GetSceneName(string scenePath)
    {
        if (!ValidateScenePath(scenePath))
            return "InvalidScene";
            
        var fileName = scenePath.GetFile();
        return fileName.Replace(".tscn", "");
    }
    
    /// <summary>
    /// 获取场景目录
    /// </summary>
    public static string GetSceneDirectory(string scenePath)
    {
        if (!ValidateScenePath(scenePath))
            return "";
            
        return scenePath.GetBaseDir();
    }
    
    #endregion

    #region 常用场景组合
    
    /// <summary>
    /// 获取所有场景路径
    /// </summary>
    public static string[] GetAllScenes()
    {
        return new[]
        {
            START_SCENE,
            MAIN_MENU_SCENE,
            GAME_SCENE,
            LEVEL1_SCENE,
            PAUSE_MENU_SCENE,
            SETTINGS_SCENE,
            GAME_OVER_SCENE,
            VICTORY_SCENE,
            LOADING_SCENE,
            CREDITS_SCENE
        };
    }
    
    /// <summary>
    /// 根据场景类型获取对应场景
    /// </summary>
    public static string GetSceneByType(SceneType type)
    {
        switch (type)
        {
            case SceneType.Start: return START_SCENE;
            case SceneType.MainMenu: return MAIN_MENU_SCENE;
            case SceneType.Game: return GAME_SCENE;
            case SceneType.Level1: return LEVEL1_SCENE;
            case SceneType.PauseMenu: return PAUSE_MENU_SCENE;
            case SceneType.Settings: return SETTINGS_SCENE;
            case SceneType.GameOver: return GAME_OVER_SCENE;
            case SceneType.Victory: return VICTORY_SCENE;
            case SceneType.Loading: return LOADING_SCENE;
            case SceneType.Credits: return CREDITS_SCENE;
            default: return MAIN_MENU_SCENE;
        }
    }
    
    #endregion
}

/// <summary>
/// 场景类型枚举
/// </summary>
public enum SceneType
{
    Start,
    MainMenu,
    Game,
    Level1,
    PauseMenu,
    Settings,
    GameOver,
    Victory,
    Loading,
    Credits
}