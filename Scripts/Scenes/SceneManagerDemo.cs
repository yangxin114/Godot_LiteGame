using Godot;
using System;
using System.Threading.Tasks;
using Logs;
using Scenes;

/// <summary>
/// 场景管理器使用示例和测试脚本
/// 演示SceneManager的各种功能用法
/// </summary>
public partial class SceneManagerDemo : Node
{
    // 场景路径常量
    private const string MAIN_MENU_SCENE = "res://Scenes/MainMenu.tscn";
    private const string GAME_SCENE = "res://Scenes/Game.tscn";
    private const string PAUSE_MENU_SCENE = "res://Scenes/PauseMenu.tscn";
    private const string SETTINGS_SCENE = "res://Scenes/Settings.tscn";
    
    public override void _Ready()
    {
        SetupEventListeners();
        Logger2.Info("SceneManagerDemo: 演示脚本已就绪");
    }

    private void SetupEventListeners()
    {
        if (SceneManager.Instance != null)
        {
            // 订阅场景切换事件
            SceneManager.Instance.SceneChanging += OnSceneChanging;
            SceneManager.Instance.SceneChanged += OnSceneChanged;
            SceneManager.Instance.SceneChangeFailed += OnSceneChangeFailed;
            SceneManager.Instance.SceneLoadingProgress += OnSceneLoadingProgress;
        }
    }

    #region 基础场景切换示例
    
    /// <summary>
    /// 基础场景切换示例
    /// </summary>
    public async Task BasicSceneChangeExample()
    {
        Logger2.Info("=== 基础场景切换示例 ===");
        
        // 同步切换（无过渡）
        var syncResult = SceneManager.Instance.ChangeScene(MAIN_MENU_SCENE);
        Logger2.Info($"同步切换结果: {syncResult}");
        
        await Task.Delay(2000); // 等待2秒
        
        // 异步切换（带动画过渡）
        var asyncResult = await SceneManager.Instance.ChangeSceneAsync(GAME_SCENE, 1.0f);
        Logger2.Info($"异步切换结果: {asyncResult}");
    }

    #endregion

    #region 场景栈管理示例
    
    /// <summary>
    /// 场景栈管理示例
    /// </summary>
    public async Task SceneStackExample()
    {
        Logger2.Info("=== 场景栈管理示例 ===");
        
        // 1. 从主菜单进入游戏
        await SceneManager.Instance.PushSceneAsync(GAME_SCENE);
        Logger2.Info($"当前栈深度: {SceneManager.Instance.GetSceneStackDepth()}");
        
        await Task.Delay(1000);
        
        // 2. 游戏中打开暂停菜单
        await SceneManager.Instance.PushSceneAsync(PAUSE_MENU_SCENE);
        Logger2.Info($"当前栈深度: {SceneManager.Instance.GetSceneStackDepth()}");
        
        await Task.Delay(1000);
        
        // 3. 从暂停菜单进入设置
        await SceneManager.Instance.PushSceneAsync(SETTINGS_SCENE);
        Logger2.Info($"当前栈深度: {SceneManager.Instance.GetSceneStackDepth()}");
        
        await Task.Delay(2000);
        
        // 4. 逐个返回
        await SceneManager.Instance.PopSceneAsync(); // 从设置返回暂停菜单
        Logger2.Info($"弹出后栈深度: {SceneManager.Instance.GetSceneStackDepth()}");
        
        await Task.Delay(1000);
        
        await SceneManager.Instance.PopSceneAsync(); // 从暂停菜单返回游戏
        Logger2.Info($"弹出后栈深度: {SceneManager.Instance.GetSceneStackDepth()}");
        
        await Task.Delay(1000);
        
        await SceneManager.Instance.PopSceneAsync(); // 从游戏返回主菜单
        Logger2.Info($"弹出后栈深度: {SceneManager.Instance.GetSceneStackDepth()}");
    }

    #endregion

    #region 添加式加载示例
    
    /// <summary>
    /// 添加式场景加载示例
    /// </summary>
    public void AdditiveLoadingExample()
    {
        Logger2.Info("=== 添加式加载示例 ===");
        
        // 加载一个UI面板而不替换当前场景
        var uiPanel = SceneManager.Instance.LoadAdditive("res://Scenes/UIPanel.tscn");
        if (uiPanel != null)
        {
            Logger2.Info("添加式UI面板加载成功");
            
            // 3秒后卸载
            GetTree().CreateTimer(3.0f).Timeout += () =>
            {
                SceneManager.Instance.UnloadAdditive(uiPanel);
                Logger2.Info("添加式UI面板已卸载");
            };
        }
    }

    #endregion

    #region 预加载示例
    
    /// <summary>
    /// 场景预加载示例
    /// </summary>
    public async Task PreloadingExample()
    {
        Logger2.Info("=== 场景预加载示例 ===");
        
        // 预加载多个场景
        var preloadTasks = new[]
        {
            SceneManager.Instance.PreloadSceneAsync(MAIN_MENU_SCENE),
            SceneManager.Instance.PreloadSceneAsync(GAME_SCENE),
            SceneManager.Instance.PreloadSceneAsync(PAUSE_MENU_SCENE)
        };
        
        // 等待所有预加载完成
        var results = await Task.WhenAll(preloadTasks);
        
        Logger2.Info($"预加载完成，成功加载 {results.Length} 个场景");
        
        // 现在可以快速切换到已预加载的场景
        await Task.Delay(1000);
        await SceneManager.Instance.ChangeSceneAsync(GAME_SCENE, 0.2f); // 很快的切换
    }

    #endregion

    #region 错误处理示例
    
    /// <summary>
    /// 错误处理示例
    /// </summary>
    public async Task ErrorHandlingExample()
    {
        Logger2.Info("=== 错误处理示例 ===");
        
        // 尝试加载不存在的场景
        var result = await SceneManager.Instance.ChangeSceneAsync("res://Scenes/NonExistent.tscn");
        Logger2.Info($"加载不存在场景的结果: {result}");
        
        // 尝试加载空路径
        result = SceneManager.Instance.ChangeScene("");
        Logger2.Info($"加载空路径的结果: {result}");
    }

    #endregion

    #region 实用工具方法
    
    /// <summary>
    /// 获取当前场景信息
    /// </summary>
    public void PrintCurrentSceneInfo()
    {
        var currentPath = SceneManager.Instance.GetCurrentScenePath();
        var stackDepth = SceneManager.Instance.GetSceneStackDepth();
        var stackSnapshot = SceneManager.Instance.GetSceneStackSnapshot();
        
        Logger2.Info($"=== 当前场景信息 ===");
        Logger2.Info($"当前场景路径: {currentPath}");
        Logger2.Info($"场景栈深度: {stackDepth}");
        Logger2.Info($"场景栈内容: [{string.Join(", ", stackSnapshot)}]");
    }

    /// <summary>
    /// 检查场景文件是否存在
    /// </summary>
    public void CheckSceneExistence()
    {
        var scenesToCheck = new[]
        {
            MAIN_MENU_SCENE,
            GAME_SCENE,
            "res://Scenes/NonExistent.tscn"
        };
        
        Logger2.Info("=== 场景存在性检查 ===");
        foreach (var scenePath in scenesToCheck)
        {
            var exists = SceneManager.Instance.SceneExists(scenePath);
            Logger2.Info($"{scenePath}: {(exists ? "存在" : "不存在")}");
        }
    }

    #endregion

    #region 事件处理
    
    private void OnSceneChanging(string oldPath, string newPath)
    {
        Logger2.Info($"场景切换中: {oldPath} -> {newPath}");
    }

    private void OnSceneChanged(string newPath)
    {
        Logger2.Info($"场景切换完成: -> {newPath}");
    }

    private void OnSceneChangeFailed(string path, Error error)
    {
        Logger2.Error($"场景切换失败: {path} - {error}");
    }

    private void OnSceneLoadingProgress(float progress)
    {
        Logger2.Debug($"场景加载进度: {progress:P0}");
    }

    #endregion

    #region 输入处理
    
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            switch (keyEvent.Keycode)
            {
                case Key.Key1:
                    _ = BasicSceneChangeExample();
                    break;
                case Key.Key2:
                    _ = SceneStackExample();
                    break;
                case Key.Key3:
                    AdditiveLoadingExample();
                    break;
                case Key.Key4:
                    _ = PreloadingExample();
                    break;
                case Key.Key5:
                    _ = ErrorHandlingExample();
                    break;
                case Key.Key6:
                    PrintCurrentSceneInfo();
                    break;
                case Key.Key7:
                    CheckSceneExistence();
                    break;
                case Key.Key0:
                    Logger2.Info("=== 场景管理器演示菜单 ===");
                    Logger2.Info("1 - 基础场景切换");
                    Logger2.Info("2 - 场景栈管理");
                    Logger2.Info("3 - 添加式加载");
                    Logger2.Info("4 - 预加载");
                    Logger2.Info("5 - 错误处理");
                    Logger2.Info("6 - 当前场景信息");
                    Logger2.Info("7 - 场景存在性检查");
                    Logger2.Info("0 - 显示此菜单");
                    break;
            }
        }
    }

    #endregion

    public override void _ExitTree()
    {
        // 清理事件订阅
        if (SceneManager.Instance != null)
        {
            SceneManager.Instance.SceneChanging -= OnSceneChanging;
            SceneManager.Instance.SceneChanged -= OnSceneChanged;
            SceneManager.Instance.SceneChangeFailed -= OnSceneChangeFailed;
            SceneManager.Instance.SceneLoadingProgress -= OnSceneLoadingProgress;
        }
    }
}