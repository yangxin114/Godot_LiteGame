using Godot;
using System;
using Audio;

public partial class Start : Node
{
	public override void _Ready()
	{
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
}
