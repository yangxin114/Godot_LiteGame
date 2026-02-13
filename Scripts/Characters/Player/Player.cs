using Godot;
using System;
using Logs;
using Numerical;
using Entities;
using Components;

namespace Characters
{
	/// <summary>
	/// Player：玩家角色的主要实现类
	/// 整合了完整的玩家系统，针对俯视角2D游戏进行了完整优化
	/// 现在使用新的组件系统实现动画和移动能力
	/// </summary>
	public partial class Player : CharacterEntity, IStatOwner
	{
		// 新的组件系统引用
		private PlayerStatsManager _statsManager;
		private PlayerStateManager _stateManager;
		
		// 组件引用（通过组件系统管理）
		private AnimationComponent2D _animationComponent;
		private MovementComponent2D _movementComponent;
		private InputHandlerComponent _inputComponent;
		
		// 玩家数据
		[Export] public PlayerData PlayerData { get; set; }

		/// <summary>
		/// 子类重写此方法来添加组件和配置实体
		/// 在_Ready中调用，在组件初始化之前
		/// </summary>
		protected override void OnSetup()
		{
			base.OnSetup();
			
			// 添加动画组件
			_animationComponent = AddComponent<AnimationComponent2D>();
			_animationComponent.SystemType = AnimationSystemType.AnimatedSprite;
			_animationComponent.AnimatedSpritePath = "AnimatedSprite2D";
			
			// 添加移动组件
			_movementComponent = AddComponent<MovementComponent2D>();
			_movementComponent.Speed = PlayerData?.MoveSpeed ?? 200f;
			_movementComponent.UseAcceleration = true;
			_movementComponent.Acceleration = 1000f;
			_movementComponent.Friction = 800f;
			_movementComponent.DirectionMode = DirectionConstraint.EightDirection;
			_movementComponent.NormalizeDiagonalMovement = true;
			
			// 添加输入组件
			_inputComponent = AddComponent<InputHandlerComponent>();
			_inputComponent.InputEnabled = true;
			
			// 订阅移动组件事件
			_movementComponent.OnBoundsTouched += OnBoundsTouched;
			
			// 订阅输入组件事件
			_inputComponent.OnMoveDirectionChanged += OnMoveDirectionChanged;
			_inputComponent.OnAttackPressed += OnAttackPressed;
			_inputComponent.OnDashPressed += OnDashPressed;
		}

		/// <summary>
		/// 节点进入场景树时被调用
		/// </summary>
		public override void _Ready()
		{
			base._Ready();

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
				
				// 初始化战斗系统
				// _combatSystem = new PlayerCombatSystem();
				// AddChild(_combatSystem);
				// _combatSystem.Initialize(this);
				
				// 初始化状态管理器
				_stateManager = new PlayerStateManager();
				AddChild(_stateManager);
				_stateManager.Initialize(this);
				
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
			// 连接状态管理器到动画组件
			if (_stateManager != null && _animationComponent != null)
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
			PlayAnimationForState(stateName);
		}
		
		/// <summary>
		/// 根据状态播放动画
		/// </summary>
		private void PlayAnimationForState(string stateName)
		{
			if (_animationComponent == null) return;
			
			string animationName = stateName.ToLower() switch
			{
				"idle" => "idle",
				"move" => "walk",
				"attack" => "attack",
				"hurt" => "hurt",
				"dead" => "death",
				_ => "idle"
			};
			
			_animationComponent.Play(animationName);
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
			// 输入处理由InputHandlerComponent自动处理
			
			// 处理移动输入（通过事件驱动）
			// 移动已经在OnMoveDirectionChanged中处理
			
			// 更新状态管理器
			_stateManager?.Update(delta);
			
			// 更新动画（基于移动方向和状态）
			UpdateAnimation();
		}

		/// <summary>
		/// 移动方向改变事件处理
		/// </summary>
		private void OnMoveDirectionChanged(Vector2 direction)
		{
			if (_movementComponent == null) return;
			
			// 设置移动组件的方向
			_movementComponent.SetInputDirection(direction);
		}
		
		/// <summary>
		/// 攻击输入事件处理
		/// </summary>
		private void OnAttackPressed()
		{
		}
		
		/// <summary>
		/// 冲刺输入事件处理
		/// </summary>
		private void OnDashPressed(Vector2 direction)
		{
			if (_movementComponent == null) return;
			
			// 使用移动组件的击退功能来实现冲刺效果
			_movementComponent.Knockback(direction, _movementComponent.Speed * 3f);
		}
		
		/// <summary>
		/// 更新动画系统
		/// </summary>
		private void UpdateAnimation()
		{
			if (_inputComponent == null || _animationComponent == null) return;
			
			string currentState = GetCurrentState();
			Vector2 moveDirection = _inputComponent.MoveDirection;
			
			// 根据当前状态和移动方向更新动画
			UpdateAnimationBasedOnState(currentState, moveDirection);
		}
		
		/// <summary>
		/// 根据状态和移动方向更新动画
		/// </summary>
		private void UpdateAnimationBasedOnState(string state, Vector2 moveDirection)
		{
			if (_animationComponent == null) return;
			
			string animationName = "idle";
			
			switch (state?.ToLower())
			{
				case "move":
					animationName = "walk";
					break;
				case "attack":
					animationName = "attack";
					break;
				case "hurt":
					animationName = "hurt";
					break;
				case "dead":
					animationName = "death";
					break;
				case "idle":
				default:
					animationName = "idle";
					break;
			}
			
			// 如果正在移动，确保播放行走动画
			if (moveDirection != Vector2.Zero && state?.ToLower() != "attack")
			{
				animationName = "walk";
			}
			
			_animationComponent.Play(animationName);
		}
		
		/// <summary>
		/// 边界触碰回调
		/// </summary>
		private void OnBoundsTouched(BoundsEdge edge)
		{
			Logger2.Debug("Player: 触碰到边界 {0}", edge);
			// 可以在这里添加边界碰撞的特殊处理
		}
		
		/// <summary>
		/// 物理更新
		/// </summary>
		public override void _PhysicsProcess(double delta)
		{
			// 组件系统会在CharacterEntity中自动调用PhysicsUpdate
			// 这里可以添加额外的物理逻辑
		}
		
		/// <summary>
		/// 清理资源
		/// </summary>
		public override void _ExitTree()
		{
			Logger2.Info("Player: 开始清理资源");
			
			// 组件系统会自动清理
			
			Logger2.Info("Player: 资源清理完成");
		}

		// 公共属性访问器
		public PlayerStatsManager StatsManager => _statsManager;
		public PlayerStateManager StateManager => _stateManager;
		
		// 组件访问器
		public AnimationComponent2D AnimationComponent => _animationComponent;
		public MovementComponent2D MovementComponent => _movementComponent;
		public InputHandlerComponent InputComponent => _inputComponent;
		
		// 兼容性方法（保持原有接口）
		public Vector2 GetVelocity() => _movementComponent?.Velocity ?? Vector2.Zero;
		public bool IsMoving() => _inputComponent?.IsMoving ?? false;
		public bool IsDashing() => _inputComponent?.IsDashing ?? false;
		public Vector2 MoveDirection => _inputComponent?.MoveDirection ?? Vector2.Zero;
	}
}