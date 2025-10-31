# 场景管理模块（Godot + C#）

此模块提供基础的场景管理功能，包含场景切换、场景栈（Push/Pop）、添加式加载（Additive）以及可选的淡入/淡出过渡。

主要文件：
- `SceneManager.cs`：场景管理核心，建议将其作为 Autoload（单例），或在 `Start` 节点中实例化并加入根节点。
- `SceneTransition.cs`：简单的全屏遮罩淡入/淡出实现，可由 `SceneManager` 使用以实现过渡效果。

快速示例：

```csharp
// 切换到场景（带过渡）
await Scenes.SceneManager.Instance.ChangeScene("res://Scenes/Main.tscn", transitionDuration:0.5f);

// Push 当前场景并切换到新场景
await Scenes.SceneManager.Instance.PushScene("res://Scenes/Level1.tscn");

// Pop 回到上一个场景
await Scenes.SceneManager.Instance.PopScene();

// 添加式加载（在当前场景上叠加一个 UI 面板或临时子场景）
var node = Scenes.SceneManager.Instance.LoadAdditive("res://Scenes/Popup.tscn");
// 卸载
Scenes.SceneManager.Instance.UnloadAdditive(node);
```

注意事项：
- `ChangeScene` 当前采用 Godot 的同步 ChangeSceneToFile 接口进行场景替换；如果场景非常大导致主线程卡顿，可在以后迭代中添加异步加载（ResourceLoader.LoadInteractive）。
- `TransitionScene` 默认为内置的 `SceneTransition`，你也可以在 `SceneManager` 的导出项 `TransitionScene` 中指定自定义过渡场景（例如带进度条的加载界面）。
- `GetCurrentScenePath()` 依赖于 `CurrentScene.Filename`，在某些运行时或导出构建中可能为空。

下一步建议（可选）：
- 增加异步加载（ResourceInteractiveLoader）与加载进度回调；
- 提供带加载进度条的 LoadingScene 模板并在 `ChangeScene` 时使用；
- 支持场景依赖预加载与资源卸载策略。
