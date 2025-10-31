using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace Scenes
{
    /// <summary>
    /// 场景管理器（简化版）。
    /// 功能：
    /// - 场景切换（替换场景）
    /// - 场景压栈/出栈（Push/Pop）
    /// - 可选淡入/淡出过渡（使用 SceneTransition）
    /// - 添加式加载（Additive load）
    /// 使用方法：将此节点作为 Autoload 或在 Start 中创建并加入场景树。
    /// </summary>
    public class SceneManager : Node
    {
        public static SceneManager Instance { get; private set; }

        // 场景栈，保存历史场景路径
        private readonly Stack<string> _sceneStack = new Stack<string>();

        // 可配置的过渡节点路径（可设置为自定义过渡场景）
        [Export]
        public PackedScene TransitionScene;

        private SceneTransition _transitionNode;

        public override void _Ready()
        {
            if (Instance != null && Instance != this)
            {
                GD.PrintWarn("SceneManager: 已存在另一个实例。新的实例将覆盖全局 Instance 引用。");
            }
            Instance = this;

            // 如果在项目中提供了过渡场景则实例化，否则创建默认 SceneTransition
            if (TransitionScene != null)
            {
                var inst = TransitionScene.Instantiate() as Node;
                inst.Name = "SceneTransition";
                GetTree().Root.AddChild(inst);
                _transitionNode = inst as SceneTransition;
            }
            else
            {
                _transitionNode = new SceneTransition();
                _transitionNode.Name = "SceneTransition";
                GetTree().Root.AddChild(_transitionNode);
            }
        }

        /// <summary>
        /// 替换当前场景到指定路径（同步），可使用过渡。
        /// </summary>
        public async Task ChangeScene(string path, float transitionDuration = 0.4f)
        {
            if (string.IsNullOrEmpty(path)) return;

            // 可选过渡
            if (_transitionNode != null)
            {
                await _transitionNode.FadeIn(transitionDuration);
            }

            var error = GetTree().ChangeSceneToFile(path);
            if (error != Error.Ok)
            {
                GD.PrintErr($"SceneManager: 切换场景失败：{path} -> {error}");
            }

            if (_transitionNode != null)
            {
                await _transitionNode.FadeOut(transitionDuration);
            }
        }

        /// <summary>
        /// 将当前场景路径压栈并切换到新场景（便于返回）。
        /// </summary>
        public async Task PushScene(string path, float transitionDuration = 0.4f)
        {
            var current = GetCurrentScenePath();
            if (!string.IsNullOrEmpty(current))
            {
                _sceneStack.Push(current);
            }
            await ChangeScene(path, transitionDuration);
        }

        /// <summary>
        /// 弹出上一个场景并返回（如果存在）。
        /// </summary>
        public async Task<bool> PopScene(float transitionDuration = 0.4f)
        {
            if (_sceneStack.Count == 0) return false;
            var prev = _sceneStack.Pop();
            await ChangeScene(prev, transitionDuration);
            return true;
        }

        /// <summary>
        /// 添加式加载：加载一个 PackedScene 并作为子节点加入根节点（不会替换当前场景）。
        /// 返回已实例化的节点引用。
        /// </summary>
        public Node? LoadAdditive(string path)
        {
            var packed = GD.Load<PackedScene>(path);
            if (packed == null)
            {
                GD.PrintErr($"SceneManager: 无法加载场景（Additive）：{path}");
                return null;
            }
            var inst = packed.Instantiate();
            GetTree().Root.AddChild(inst);
            return inst;
        }

        /// <summary>
        /// 卸载由 LoadAdditive 加载的节点。
        /// </summary>
        public void UnloadAdditive(Node node)
        {
            if (node == null) return;
            node.QueueFree();
        }

        /// <summary>
        /// 获取当前活动场景的文件路径（如果可用）。
        /// </summary>
        public string GetCurrentScenePath()
        {
            var current = GetTree().CurrentScene;
            if (current == null) return string.Empty;
            // CurrentScene 不总是保存原始文件路径；尝试读取拥有者或场景文件名
            if (current.Filename != null) return current.Filename;
            return string.Empty;
        }
    }
}
