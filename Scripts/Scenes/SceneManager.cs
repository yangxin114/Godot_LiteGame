using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Logs;

namespace Scenes
{
    /// <summary>
    /// 场景管理器
    /// 提供完整的场景管理功能，包括切换、栈管理、过渡效果等
    /// </summary>
    public partial class SceneManager : Node
    {
        #region 单例模式
        
        public static SceneManager Instance { get; private set; }
        
        #endregion

        #region 私有字段
        
        // 场景栈，保存历史场景路径
        private readonly Stack<string> _sceneStack = new Stack<string>();
        
        // 当前活动场景的路径
        private string _currentScenePath = string.Empty;
        
        // 记录由LoadAdditive创建的实例
        private readonly List<Node> _additiveInstances = new List<Node>();
        
        // 场景切换状态标志
        private bool _isChangingScene = false;
        
        // 可配置的过渡节点
        [Export]
        public PackedScene TransitionScene { get; set; }
        
        private SceneTransition _transitionNode;
        
        // 场景加载进度回调
        public event Action<float> SceneLoadingProgress;
        
        // 场景切换事件
        public event Action<string, string> SceneChanging; // (oldPath, newPath)
        public event Action<string> SceneChanged; // newPath
        public event Action<string, Error> SceneChangeFailed; // (attemptedPath, error)
        
        #endregion

        #region 生命周期
        
        public override void _Ready()
        {
            try
            {
                InitializeSingleton();
                InitializeTransitionSystem();
                Logger2.Info("SceneManager: 初始化完成");
            }
            catch (Exception ex)
            {
                Logger2.Error($"SceneManager: 初始化失败 - {ex.Message}");
                GD.PrintErr($"SceneManager initialization failed: {ex.Message}");
            }
        }

        private void InitializeSingleton()
        {
            if (Instance != null && Instance != this)
            {
                Logger2.Warn("SceneManager: 检测到重复实例，覆盖原有引用");
                Instance.QueueFree();
            }
            Instance = this;
        }

        private void InitializeTransitionSystem()
        {
            // 如果在项目中提供了过渡场景则实例化，否则创建默认SceneTransition
            if (TransitionScene != null)
            {
                try
                {
                    var inst = TransitionScene.Instantiate() as Node;
                    if (inst != null)
                    {
                        inst.Name = "SceneTransition";
                        GetTree().Root.AddChild(inst);
                        _transitionNode = inst as SceneTransition;
                        Logger2.Info("SceneManager: 使用自定义过渡场景");
                    }
                }
                catch (Exception ex)
                {
                    Logger2.Error($"SceneManager: 自定义过渡场景实例化失败 - {ex.Message}");
                }
            }
            
            // 如果没有自定义过渡场景，则创建默认的
            if (_transitionNode == null)
            {
                _transitionNode = new SceneTransition();
                _transitionNode.Name = "SceneTransition";
                GetTree().Root.AddChild(_transitionNode);
                Logger2.Info("SceneManager: 创建默认过渡系统");
            }
        }

        public override void _ExitTree()
        {
            try
            {
                CleanupResources();
                Logger2.Info("SceneManager: 资源清理完成");
            }
            catch (Exception ex)
            {
                Logger2.Error($"SceneManager: 清理失败 - {ex.Message}");
            }
        }

        private void CleanupResources()
        {
            // 卸载所有additive实例
            foreach (var node in _additiveInstances)
            {
                if (node != null && node.IsInsideTree())
                {
                    node.QueueFree();
                }
            }
            _additiveInstances.Clear();

            // 移除过渡节点
            if (_transitionNode != null)
            {
                if (_transitionNode.GetParent() != null)
                {
                    _transitionNode.GetParent().RemoveChild(_transitionNode);
                }
                _transitionNode.QueueFree();
                _transitionNode = null;
            }

            Instance = null;
        }

        #endregion

        #region 基础场景切换
        
        /// <summary>
        /// 异步切换场景（带过渡效果）
        /// </summary>
        /// <param name="path">目标场景路径</param>
        /// <param name="transitionDuration">过渡持续时间</param>
        public async Task<Error> ChangeSceneAsync(string path, float transitionDuration = 0.4f)
        {
            if (string.IsNullOrEmpty(path))
            {
                Logger2.Error("SceneManager: 场景路径不能为空");
                return Error.InvalidParameter;
            }

            if (_isChangingScene)
            {
                Logger2.Warn("SceneManager: 场景切换已在进行中，跳过请求");
                return Error.Busy;
            }

            if (!ResourceLoader.Exists(path))
            {
                Logger2.Error($"SceneManager: 场景文件不存在 - {path}");
                SceneChangeFailed?.Invoke(path, Error.FileNotFound);
                return Error.FileNotFound;
            }

            try
            {
                _isChangingScene = true;
                var oldPath = _currentScenePath ?? string.Empty;
                
                Logger2.Info($"SceneManager: 开始切换场景 {oldPath} -> {path}");
                
                // 触发切换开始事件
                SceneChanging?.Invoke(oldPath, path);

                // 淡入过渡
                if (_transitionNode != null)
                {
                    await _transitionNode.FadeIn(transitionDuration);
                }

                // 执行场景切换
                var error = GetTree().ChangeSceneToFile(path);
                if (error != Error.Ok)
                {
                    Logger2.Error($"SceneManager: 场景切换失败 {path} -> {error}");
                    SceneChangeFailed?.Invoke(path, error);
                    
                    // 如果失败，尝试淡出回到交互状态
                    if (_transitionNode != null)
                    {
                        await _transitionNode.FadeOut(transitionDuration);
                    }
                    
                    return error;
                }

                // 更新当前场景路径
                _currentScenePath = path;
                SceneChanged?.Invoke(path);

                // 淡出过渡
                if (_transitionNode != null)
                {
                    await _transitionNode.FadeOut(transitionDuration);
                }

                Logger2.Info($"SceneManager: 场景切换完成 -> {path}");
                return Error.Ok;
            }
            catch (Exception ex)
            {
                Logger2.Error($"SceneManager: 场景切换异常 - {ex.Message}");
                SceneChangeFailed?.Invoke(path, Error.Bug);
                return Error.Bug;
            }
            finally
            {
                _isChangingScene = false;
            }
        }

        /// <summary>
        /// 同步切换场景（无过渡效果）
        /// </summary>
        /// <param name="path">目标场景路径</param>
        public Error ChangeScene(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Logger2.Error("SceneManager: 场景路径不能为空");
                return Error.InvalidParameter;
            }

            if (!ResourceLoader.Exists(path))
            {
                Logger2.Error($"SceneManager: 场景文件不存在 - {path}");
                SceneChangeFailed?.Invoke(path, Error.FileNotFound);
                return Error.FileNotFound;
            }

            try
            {
                var oldPath = _currentScenePath ?? string.Empty;
                Logger2.Info($"SceneManager: 同步切换场景 {oldPath} -> {path}");
                
                SceneChanging?.Invoke(oldPath, path);
                
                var error = GetTree().ChangeSceneToFile(path);
                if (error == Error.Ok)
                {
                    _currentScenePath = path;
                    SceneChanged?.Invoke(path);
                    Logger2.Info($"SceneManager: 同步场景切换完成 -> {path}");
                }
                else
                {
                    Logger2.Error($"SceneManager: 同步场景切换失败 {path} -> {error}");
                    SceneChangeFailed?.Invoke(path, error);
                }
                
                return error;
            }
            catch (Exception ex)
            {
                Logger2.Error($"SceneManager: 同步场景切换异常 - {ex.Message}");
                SceneChangeFailed?.Invoke(path, Error.Bug);
                return Error.Bug;
            }
        }

        #endregion

        #region 场景栈管理
        
        /// <summary>
        /// 将当前场景压栈并切换到新场景
        /// </summary>
        /// <param name="path">目标场景路径</param>
        /// <param name="transitionDuration">过渡持续时间</param>
        public async Task<Error> PushSceneAsync(string path, float transitionDuration = 0.4f)
        {
            var current = GetCurrentScenePath();
            if (!string.IsNullOrEmpty(current))
            {
                _sceneStack.Push(current);
                Logger2.Debug($"SceneManager: 场景压栈 {current}, 栈深度 {_sceneStack.Count}");
            }
            
            return await ChangeSceneAsync(path, transitionDuration);
        }

        /// <summary>
        /// 弹出上一个场景并返回
        /// </summary>
        /// <param name="transitionDuration">过渡持续时间</param>
        /// <returns>是否成功弹出</returns>
        public async Task<bool> PopSceneAsync(float transitionDuration = 0.4f)
        {
            if (_sceneStack.Count == 0)
            {
                Logger2.Warn("SceneManager: 场景栈为空，无法弹出");
                return false;
            }

            var previousScene = _sceneStack.Pop();
            Logger2.Debug($"SceneManager: 场景弹栈 {previousScene}, 栈深度 {_sceneStack.Count}");
            
            var result = await ChangeSceneAsync(previousScene, transitionDuration);
            return result == Error.Ok;
        }

        /// <summary>
        /// 获取场景栈深度
        /// </summary>
        public int GetSceneStackDepth() => _sceneStack.Count;

        /// <summary>
        /// 清空场景栈
        /// </summary>
        public void ClearSceneStack()
        {
            _sceneStack.Clear();
            Logger2.Debug("SceneManager: 场景栈已清空");
        }

        #endregion

        #region 添加式场景加载
        
        /// <summary>
        /// 添加式加载场景（不会替换当前场景）
        /// </summary>
        /// <param name="path">场景路径</param>
        /// <returns>加载的节点实例</returns>
        public Node LoadAdditive(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Logger2.Error("SceneManager: 添加式加载路径不能为空");
                return null;
            }

            if (!ResourceLoader.Exists(path))
            {
                Logger2.Error($"SceneManager: 添加式场景文件不存在 - {path}");
                return null;
            }

            try
            {
                var packedScene = GD.Load<PackedScene>(path);
                if (packedScene == null)
                {
                    Logger2.Error($"SceneManager: 无法加载场景资源 - {path}");
                    return null;
                }

                var instance = packedScene.Instantiate();
                if (instance == null)
                {
                    Logger2.Error($"SceneManager: 场景实例化失败 - {path}");
                    return null;
                }

                GetTree().Root.AddChild(instance);
                _additiveInstances.Add(instance);
                
                Logger2.Info($"SceneManager: 添加式场景加载完成 - {path}");
                return instance;
            }
            catch (Exception ex)
            {
                Logger2.Error($"SceneManager: 添加式加载异常 - {path}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 卸载添加式加载的场景
        /// </summary>
        /// <param name="node">要卸载的节点</param>
        public void UnloadAdditive(Node node)
        {
            if (node == null)
            {
                Logger2.Warn("SceneManager: 尝试卸载空节点");
                return;
            }

            try
            {
                if (_additiveInstances.Contains(node))
                {
                    _additiveInstances.Remove(node);
                }
                
                if (node.IsInsideTree())
                {
                    node.QueueFree();
                    Logger2.Info("SceneManager: 添加式场景卸载完成");
                }
            }
            catch (Exception ex)
            {
                Logger2.Error($"SceneManager: 添加式卸载异常 - {ex.Message}");
            }
        }

        #endregion

        #region 场景信息查询
        
        /// <summary>
        /// 获取当前场景路径
        /// </summary>
        public string GetCurrentScenePath()
        {
            // 优先返回SceneManager管理的路径
            if (!string.IsNullOrEmpty(_currentScenePath))
                return _currentScenePath;

            // 回退到引擎的当前场景
            var currentScene = GetTree().CurrentScene;
            if (currentScene != null)
            {
                try
                {
                    return currentScene.SceneFilePath ?? string.Empty;
                }
                catch
                {
                    return string.Empty;
                }
            }
            
            return string.Empty;
        }

        /// <summary>
        /// 检查场景文件是否存在
        /// </summary>
        /// <param name="path">场景路径</param>
        public bool SceneExists(string path)
        {
            return ResourceLoader.Exists(path);
        }

        /// <summary>
        /// 获取场景栈内容（用于调试）
        /// </summary>
        public string[] GetSceneStackSnapshot()
        {
            return _sceneStack.ToArray();
        }

        #endregion

        #region 异步加载支持
        
        /// <summary>
        /// 异步预加载场景
        /// </summary>
        /// <param name="path">场景路径</param>
        public async Task<PackedScene> PreloadSceneAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Logger2.Error("SceneManager: 预加载路径不能为空");
                return null;
            }

            try
            {
                Logger2.Debug($"SceneManager: 开始预加载场景 - {path}");
                
                var resource = GD.Load<PackedScene>(path);
                if (resource != null)
                {
                    Logger2.Debug($"SceneManager: 预加载完成 - {path}");
                    return resource;
                }
                else
                {
                    Logger2.Error($"SceneManager: 预加载失败 - {path}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Logger2.Error($"SceneManager: 预加载异常 - {path}: {ex.Message}");
                return null;
            }
        }

        #endregion
    }
}
