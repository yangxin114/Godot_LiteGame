using Godot;
using System;
using Logs;
using Numerical;
using Entities;

namespace Characters
{
	/// <summary>
	/// Player：玩家角色的主要实现类
	/// 整合了完整的玩家系统，针对俯视角2D游戏进行了完整优化
	/// </summary>
	public partial class Player : CombatEntity,IStatOwner
	{
		// 子系统引用
		private PlayerStatsManager _statsManager;
		private PlayerInputHandler _inputHandler;
		private PlayerCombatSystem _combatSystem;
		private PlayerMovementSystem _movementSystem;
		private PlayerStateManager _stateManager;
		private PlayerAnimationSystem _animationSystem;
		
		// 玩家数据
		[Export] public PlayerData PlayerData { get; set; }
		

		/// <summary>
		/// 节点进入场景树时被调用
		/// </summary>
		public override void _Ready()
		{
			
			InitializeSubsystems();
			SetupConnections();
		}
		
		/// <summary>
		/// 初始化所有子系统
		/// </summary>
		private void InitializeSubsystems()
		{
			try
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
				// _combatSystem = new PlayerCombatSystem();
				// AddChild(_combatSystem);
				// _combatSystem.Initialize(this);
				
				// 初始化移动系统
				_movementSystem = new PlayerMovementSystem();
				AddChild(_movementSystem);
				_movementSystem.Initialize(this);
				
				// 初始化状态管理器
				_stateManager = new PlayerStateManager();
				AddChild(_stateManager);
				_stateManager.Initialize(this);
				
				// 初始化动画系统
				_animationSystem = new PlayerAnimationSystem();
				AddChild(_animationSystem);
				_animationSystem.Initialize(this);
				
				// 初始化属性系统
				_statsManager.InitStats();
				
				Logger2.Info("Player: 所有子系统初始化完成");
			}
			catch (Exception ex)
			{
				Logger2.Error("Player.InitializeSubsystems: 初始化失败 - {0}", ex.Message);
				ErrorReporter.Report(ex, "Player子系统初始化");
			}
		}
		
		/// <summary>
		/// 设置系统间连接
		/// </summary>
		private void SetupConnections()
		{
			// 连接输入到移动系统
			if (_inputHandler != null && _movementSystem != null)
			{
				// 冲刺输入处理
				// 在_Process中处理，因为需要即时响应
			}
			
			// 连接状态管理器到动画系统
			if (_stateManager != null && _animationSystem != null)
			{
				_stateManager.EnableAutoAnimation = true;
			}
			
			Logger2.Info("Player: 系统连接设置完成");
		}
		
		/// <summary>
		/// 强制切换状态
		/// </summary>
		public void SetState(string stateName)
		{
			_stateManager?.ForceStateChange(stateName);
			// 同时播放对应的动画
			_animationSystem?.PlayAnimationForState(stateName);
		}
		
		/// <summary>
		/// 获取当前状态
		/// </summary>
		public string GetCurrentState()
		{
			return _stateManager?.GetCurrentState();
		}

		public StatContainer GetStatContainer()
        {
            return _statsManager.statContainer;
        }

		/// <summary>
		/// 每帧更新
		/// </summary>
		public override void _Process(double delta)
		{
			// 更新输入系统
			_inputHandler?.Update(delta);
			
			// 处理冲刺输入
			if (_inputHandler?.IsDashing ?? false)
			{
				_movementSystem?.PerformDash(_inputHandler.LastMoveDirection);
			}
			
			// 更新战斗系统
			_combatSystem?.Update(delta);
			
			// 更新移动系统
			_movementSystem?.Update(delta);
			
			// 更新状态管理器
			_stateManager?.Update(delta);
			
			// 更新动画系统
			UpdateAnimation();
			
			// 处理攻击输入
			if (_inputHandler?.IsAttacking ?? false)
			{
				_combatSystem?.PerformAttack();
			}
		}
		
		/// <summary>
		/// 更新动画系统
		/// </summary>
		private void UpdateAnimation()
		{
			if (_inputHandler == null || _animationSystem == null) return;
			
			string currentState = GetCurrentState();
			Vector2 moveDirection = _inputHandler.MoveDirection;
			
			// 调用AnimationSystem的核心更新逻辑
			_animationSystem.UpdateAnimation(currentState, moveDirection);
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
		public PlayerAnimationSystem AnimationSystem => _animationSystem;
	}
}