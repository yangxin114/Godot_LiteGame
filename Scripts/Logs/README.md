# 日志模块（Godot + C#）

本目录包含一个轻量的日志与性能监控模块，适用于 Godot C# 项目。模块设计目标为：易用、可扩展、低开销（文件写入异步）。

包含文件说明：
- `LogLevel.cs` - 日志等级枚举（Trace/Debug/Info/Warn/Error/Fatal）。
- `ILogSink.cs` - 输出端接口（实现此接口可扩展到文件/控制台/远程等）。
- `ConsoleSink.cs` - 将日志写到 Godot 控制台（开发阶段常用）。
- `FileSink.cs` - 异步文件写入实现，使用内部队列与后台任务以避免阻塞主线程。
- `LogManager.cs` - 日志管理中心，负责初始化、注册 sink、等级过滤与关闭。
- `Logger.cs` - 日志门面，提供 Trace/Debug/Info/... 的便捷方法。
- `PerformanceMonitor.cs` - 性能监控节点，定期记录 FPS 与内存到日志。
- `ErrorReporter.cs` - 异常上报入口，支持本地日志与可选的远程转发回调。
- `FPSMemoryPanel.cs` - 简易的屏幕面板用于显示 FPS/内存信息，便于运行时调试。

快速使用示例（在 `Start.cs` 或 Autoload 的 `_Ready` 中调用）：

```csharp
using Logs;
using Godot;
using System.IO;

public override void _Ready()
{
    // user:// 是 Godot 的可写路径，建议将日志放在 user:// 下
    var userPath = ProjectSettings.LocalizePath("user://");
    var logDir = Path.Combine(userPath, "logs");
    Directory.CreateDirectory(logDir);
    var logPath = Path.Combine(logDir, "game.log");

    // 初始化日志系统：设置最小等级、日志文件路径、是否打印到控制台
    LogManager.Initialize(LogLevel.Debug, filePath: logPath, enableConsole: true);

    // 注册远程上报回调（可选）
    ErrorReporter.RegisterRemoteReporter(payload =>
    {
        // 注意：不要在此回调中直接使用 Godot 对象（线程/上下文问题）
        // 可将 payload 放入队列或启动协程/线程进行网络发送
    });

    // 将性能监控节点加入场景树，以自动周期性记录性能数据
    var perf = new Logs.PerformanceMonitor { LogInterval = 5.0f };
    GetTree().Root.AddChild(perf);

    // 可选：添加屏幕面板用于实时调试
    var panel = new Logs.FPSMemoryPanel { StartVisible = true };
    GetTree().Root.AddChild(panel);

    Logger.Info("游戏已启动");
}
```

注意事项与建议：
- `FileSink` 是异步写入的，但仍建议在程序退出时调用 `LogManager.Shutdown()` 以确保缓冲日志落盘。
- `ErrorReporter.RegisterRemoteReporter` 应接收一个不会阻塞主线程的委托（例如将 payload 推入线程安全队列，由后台线程/协程发出）。
- 生产环境建议在发布包中将日志等级调整为 `Info` 或 `Warn`，并在需要时才启用文件或远程上报，以降低 IO 与网络开销。

如果需要，我可以：
- 自动将 `LogManager.Initialize(...)` 注入到你的 `Scripts/Start.cs` 文件；
- 提供一个示例 `RemoteSink`，使用 Godot 的 `HTTPRequest` 将崩溃信息上报到 HTTP 接口；
- 增加一个更完善的日志滚动/分割策略（按大小或日期滚动日志文件）。
