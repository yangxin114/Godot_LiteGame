using Godot;
using System;
using System.Linq;
using Characters;

/*
 * Player.cs
 *
 * 简单的前端包装器，用于在场景中加载并显示可定制的玩家。
 * - 从创意工坊目录加载 PlayerData（JSON）列表
 * - 选择一个 PlayerData（若无则使用默认）
 * - 通过 PlayerFactory 创建运行时的 BasePlayer 并将其作为子节点添加到场景
 *
 * 说明：该文件仅负责加载和实例化玩家；玩家的行为、状态机、渲染等逻辑
 * 在 `Characters.BasePlayer` 与 `PlayerStateMachine` 中实现。
 */
public partial class Player : Node2D
{

	/// <summary>
	/// 运行时的玩家实例（由 PlayerFactory 创建的 BasePlayer 或派生类）。
	/// </summary>
	private Characters.BasePlayer runtimePlayer;

	/// <summary>
	/// 节点进入场景树时被调用：尝试从 workshop 加载玩家定义并实例化一个运行时玩家。
	/// </summary>
	public override void _Ready()
	{
		// 从工作坊目录加载所有玩家定义
		var players = PlayerLoader.LoadAll();
		PlayerData chosen = null;

		if (players != null && players.Count > 0)
			chosen = players[0];

		// 若没有可用的自定义玩家，则使用内置默认数据
		if (chosen == null)
			chosen = PlayerData.Default();

		// 使用工厂创建运行时玩家并将其加入场景
		runtimePlayer = Characters.PlayerFactory.Create(this, chosen);
		AddChild(runtimePlayer);
		runtimePlayer.Name = chosen.Name ?? "Player";
	}

	/// <summary>
	/// 每帧调用，将更新转发给运行时玩家（如果存在）。
	/// </summary>
	public override void _Process(double delta)
	{
		runtimePlayer?.Process(delta);
	}
}
