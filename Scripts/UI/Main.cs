using Godot;
using System;
using Logs;
using Scenes;

public partial class Main : Control
{
	private Button _startButton;
	private const string LEVEL1_SCENE = "res://Scenes/Lvl1.tscn";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		try
		{
			// 获取开始按钮节点
			_startButton = GetNodeOrNull<Button>("Control/StartBtn");
			GD.Print(_startButton);
			if (_startButton == null)
			{
				Logger2.Error("Main: 无法找到开始按钮节点");
				return;
			}
			
			// 连接按钮按下信号到方法
			_startButton.Pressed += OnStartButtonPressed;
		}
		catch (Exception ex)
		{
			Logger2.Error($"Main: 初始化失败 - {ex.Message}");
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
			Logger2.Debug("Main: 开始按钮被按下，切换到游戏场景");
			
			// 检查场景文件是否存在
			if (!SceneManager.Instance.SceneExists(LEVEL1_SCENE))
			{
				Logger2.Error($"Main: 游戏场景文件不存在 - {LEVEL1_SCENE}");
				return;
			}
			
			// 使用场景管理器切换到游戏场景
			var result = await SceneManager.Instance.PushSceneAsync(LEVEL1_SCENE, 0.5f);
			
			if (result == Error.Ok)
			{
				Logger2.Debug("Main: 场景切换成功");
			}
			else
			{
				Logger2.Error($"Main: 场景切换失败 - {result}");
			}
		}
		catch (Exception ex)
		{
			Logger2.Error($"Main: 场景切换异常 - {ex.Message}");
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
			Logger2.Info("Main: 资源清理完成");
		}
		catch (Exception ex)
		{
			Logger2.Error($"Main: 清理失败 - {ex.Message}");
		}
	}
}
