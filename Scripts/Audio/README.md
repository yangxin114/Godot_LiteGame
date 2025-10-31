# Audio 管理模块（Godot + C#）

此模块提供一个实用的音频管理器，适合集成到 Godot C# 项目中。

功能概览：
- 双 BGM 播放器与交叉淡入/淡出（Crossfade）
- 音效（SFX）播放，支持播放器池化与自动扩展
- SFX 优先级（priority）控制：当资源紧张时可中断低优先级音效
- 总线（Bus）音量控制与主静音
- 音频资源预加载与缓存
- 支持把此脚本作为 Autoload（单例）使用

文件：
- `AudioManager.cs` - 模块实现，包含详细注释与示例方法

快速开始：

1. 在 Godot 项目设置的 Audio -> Buses 中创建至少 `Master`、`SFX` 与 `BGM`（可选）三个总线，或根据项目命名调整。
2. 将 `Scripts/Audio/AudioManager.cs` 添加为 Autoload（Project -> Project Settings -> Autoload），或在 `Start` 节点中实例化并加入场景树。
3. 在代码中使用示例：

```csharp
// 交叉淡入新 BGM（当前播放的将会淡出）
Audio.AudioManager.Instance?.CrossfadeBGM("res://audio/bgm_theme.ogg", duration:1.0f, loop:true);

// 停止所有 BGM（带淡出）
Audio.AudioManager.Instance?.StopAllBGM(fadeOut:1.0f);

// 播放音效，带优先级（数值越大优先级越高）
// 若当前播放器不足且已达到最大池大小，会尝试中断优先级更低的音效
Audio.AudioManager.Instance?.PlaySFX("res://audio/jump.wav", volumeDb:-2.0f, priority:10);

// 设置 SFX 总线音量
Audio.AudioManager.Instance?.SetBusVolume("SFX", -6f);

// 全局静音
Audio.AudioManager.Instance?.SetMute(true);
```

注意事项与扩展建议：
- 当前实现已支持交叉淡入（CrossfadeBGM），建议使用该方法进行 BGM 切换。
- SFX 播放会优先使用池中播放器，池会在运行时根据 `InitialPoolSize` 与 `MaxPoolSize` 自动扩展，避免频繁创建销毁节点导致 GC 压力。
- 当池与活跃播放器数量达到上限时，播放请求会根据优先级决定是否中断最低优先级音效。
- 如果需要更精细的音量曲线（缓动），可以扩展 `TweenFade` 使用不同的缓动类型或自定义音量曲线。

后续可选项（我可以继续为你实现）：
- 将 `AudioManager` 直接注入到 `Scripts/Start.cs` 并创建示例调用；
- 添加音量混合器 UI（运行时可调节各总线音量）；
- 支持 3D 音频/定位声源；
- 支持播放队列、淡入淡出策略配置（例如交叉时长、互斥组）。

请告诉我你希望我继续实现哪一项，我会继续编码并在本仓库中提交修改。
