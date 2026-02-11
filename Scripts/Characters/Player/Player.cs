using Godot;
using System;
using System.Linq;
using Characters;
using Numerical;
using Logs;

namespace Characters
{
	/*
	 * Player.cs
	 *
	 * 玩家主控制器类
	 * 负责协调各个子系统的协作
	 */
	public partial class Player : StatOwner
	{
		[Export] public PlayerData PlayerData { get; set; }
		[Export] public PlayerViewData PlayerViewData { get; set; }
		
		// 子系统组件
		private PlayerStatsManager _statsManager;
		private PlayerInputHandler _inputHandler;
		private PlayerCombatSystem _combatSystem;
		private PlayerMovementSystem _movementSystem;
		private PlayerStateManager _stateManager;

		/// <summary>
		/// 节点进入场景树时被调用
		/// </summary>
		public override void _Ready()
		{
			Logger2.Info("Player._Ready: 开始初始化玩家系统");
			
			// 初始化各个子系统
			InitializeSubsystems();
				
			Logger2.Info("Player._Ready: 玩家系统初始化完成");
		}
		
		/// <summary>
		/// 初始化所有子系统组件
		/// </summary>
		private void InitializeSubsystems()
		{
			// 初始化属性管理器
			_statsManager = new PlayerStatsManager();
			AddChild(_statsManager);
			_statsManager.Initialize(this);
			
			// 初始化输入处理器
			_inputHandler = new PlayerInputHandler();
			AddChild(_inputHandler);
			_inputHandler.Initialize(this);
			
			// 初始化战斗系统
			_combatSystem = new PlayerCombatSystem();
			AddChild(_combatSystem);
			_combatSystem.Initialize(this);
			
			// 初始化移动系统
			_movementSystem = new PlayerMovementSystem();
			AddChild(_movementSystem);
			_movementSystem.Initialize(this);
			
			// 初始化状态管理器
			_stateManager = new PlayerStateManager();
			AddChild(_stateManager);
			_stateManager.Initialize(this);
			
			Logger2.Info("Player: 所有子系统初始化完成");
		}

		/// <summary>
		/// 初始化玩家属性系统（委托给StatsManager）
		/// </summary>
		public void InitStats()
		{
			_statsManager?.InitStats();
		}
		
		/// <summary>
		/// 设置玩家状态
		/// </summary>
		public void SetState(string stateName)
		{
			_stateManager?.ForceStateChange(stateName);
		}

		/// <summary>
		/// 每帧更新
		/// </summary>
		public override void _Process(double delta)
		{
			// 更新输入系统
			_inputHandler?.Update(delta);
			
			// 更新战斗系统
			_combatSystem?.Update(delta);
			
			// 更新移动系统
			_movementSystem?.Update(delta);
			
			// 更新状态管理器
			_stateManager?.Update(delta);
			
			// 处理攻击输入
			if (_inputHandler?.IsAttacking ?? false)
			{
				_combatSystem?.PerformAttack();
			}
			
			// 处理移动输入
			if (_inputHandler?.IsMoving() ?? false)
			{
				// TODO: 通知移动系统处理移动
			}
		}
		
		/// <summary>
		/// 物理更新
		/// </summary>
		public override void _PhysicsProcess(double delta)
		{
			// 物理相关的更新逻辑
		}
		
		/// <summary>
		/// 清理资源
		/// </summary>
		public override void _ExitTree()
		{
			Logger2.Info("Player: 开始清理资源");
			
			// 子系统会自动清理（因为是子节点）
			
			Logger2.Info("Player: 资源清理完成");
		}
		
		// 公共属性访问器
		public PlayerStatsManager StatsManager => _statsManager;
		public PlayerInputHandler InputHandler => _inputHandler;
		public PlayerCombatSystem CombatSystem => _combatSystem;
		public PlayerMovementSystem MovementSystem => _movementSystem;
		public PlayerStateManager StateManager => _stateManager;
	}
}