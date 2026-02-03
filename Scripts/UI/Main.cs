using Godot;
using System;

public partial class Main : Node2D
{
	private Button startButton;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// 获取开始按钮节点
		startButton = GetNode<Button>("StartBtn"); // 假设按钮节点名为"StartBtn"
		
		// 连接按钮按下信号到方法
		startButton.Pressed += OnStartButtonPressed;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	// 开始按钮按下时触发的方法
	private void OnStartButtonPressed()
	{
		// 切换到游戏加载场景
		GetTree().ChangeSceneToFile("res://Scenes/GameLoad.tscn"); // 假设加载场景路径为"res://Scenes/GameLoad.tscn"
	}
}