using Godot;
using Audio;
using System.IO;
using Logs;
public partial class Start : Node
{
	public override void _Ready()
	{
		//init log system
		initLog();

		// 在游戏启动时初始化并加入 AudioManager（若你更喜欢 Autoload，可以在项目设置中设置）
		if (AudioManager.Instance == null)
		{
			var audio = new AudioManager();
			audio.Name = "AudioManager";
			// 将其挂到根节点，保证在全局可访问
			GetTree().Root.AddChild(audio);
		}

		// 初始化 SceneManager（如果未在 Autoload 中配置）
		if (Scenes.SceneManager.Instance == null)
		{
			var sm = new Scenes.SceneManager();
			sm.Name = "SceneManager";
			GetTree().Root.AddChild(sm);
		}

		// 这里可以加入其它启动初始化逻辑
	}



	public void initLog(){
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
		// var perf = new Logs.PerformanceMonitor { LogInterval = 5.0f };
		// GetTree().Root.AddChild(perf);

		// 可选：添加屏幕面板用于实时调试
		// var panel = new Logs.FPSMemoryPanel { StartVisible = true };
		// GetTree().Root.AddChild(panel);

		Logs.Logger.Info("游戏已启动");
	}
}
