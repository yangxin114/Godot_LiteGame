using Godot;
using System;
using Logs;
using Scenes;

public partial class StartMenu : Control
{
	private Button _startButton;
	private Button _exitButton;
	private const string LEVEL1_SCENE = "res://Scenes/Lvl1.tscn";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		try
		{
			// 获取开始按钮节点
			_startButton = GetNodeOrNull<Button>("Control/StartBtn");
			if (_startButton == null)
			{
				Logger2.Error("Main: Unable to find Start game button node");
				return;
			}

			_exitButton = GetNodeOrNull<Button>("Control/ExitBtn");
			if (_exitButton == null)
			{
				Logger2.Error("Main: Unable to find Exit button node");
				return;
			}
			
			// 连接按钮按下信号到方法
			_startButton.Pressed += OnStartButtonPressed;
			_exitButton.Pressed += OnExitButtonPressed;
		}
		catch (Exception ex)
		{
			Logger2.Error($"Main: Initialization failed - {ex.Message}");
		}
	}

    /// <summary>
    /// 退出按钮被按下时调用。
    /// 此方法会记录退出日志并尝试安全退出游戏（触发 Godot 引擎退出流程）。
    /// 可以在此处添加额外的保存或清理逻辑。
    /// </summary>
    private void OnExitButtonPressed()
    {
        try
        {
            Logger2.Info("Main: Exit button pressed, quitting game");
            // 中文注释：调用引擎退出方法，触发游戏退出流程
            GetTree().Quit();
        }
        catch (Exception ex)
        {
            Logger2.Error($"Main: Quit failed - {ex.Message}");
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
		// 游戏主循环逻辑
	}
	
	// 开始按钮按下时触发的方法
	private async void OnStartButtonPressed()
	{
		try
		{
			Logger2.Debug("Main: Start button pressed, switching to game scene");
			
			// 检查场景文件是否存在
			if (!SceneManager.Instance.SceneExists(LEVEL1_SCENE))
			{
				Logger2.Error($"Main: Game scene file does not exist - {LEVEL1_SCENE}");
				return;
			}
			
			// 使用场景管理器切换到游戏场景
			var result = await SceneManager.Instance.PushSceneAsync(LEVEL1_SCENE, 0.5f);
			
			if (result == Error.Ok)
			{
				Logger2.Debug("Main: Scene switch succeeded");
			}
			else
			{
				Logger2.Error($"Main: Scene switch failed - {result}");
			}
		}
		catch (Exception ex)
		{
			Logger2.Error($"Main: Scene switch exception - {ex.Message}");
		}
	}

	public override void _ExitTree()
	{
		try
		{
			// 清理事件订阅
			if (_startButton != null)
			{
				_startButton.Pressed -= OnStartButtonPressed;
			}
			if (_exitButton != null)
			{
				_exitButton.Pressed -= OnExitButtonPressed;
			}
			Logger2.Info("Main: Resources cleaned up");
		}
		catch (Exception ex)
		{
			Logger2.Error($"Main: Cleanup failed - {ex.Message}");
		}
	}
}
