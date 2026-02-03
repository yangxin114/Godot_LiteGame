using Godot;
using System;

/// <summary>
/// The loading screen during game start.
/// </summary>
public partial class GameLoad : Control
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
    {
        GetTree().ChangeSceneToFile("res://Scenes/Lvl1.tscn");
    }
}
