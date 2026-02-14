using Godot;
using System;
using StateMachine;
using StateMachine.PlayerStates;  // 添加PlayerStates命名空间
using Logs;
using Numerical;

namespace Characters
{
    /// <summary>
    /// 玩家状态管理器
    /// 负责管理玩家的各种状态（闲置、移动、攻击、受伤、死亡等）
    /// 并与动画系统协同工作
    /// </summary>
    public partial class PlayerStateManager : Node
    {
        #region 字段和属性

        private Player _player;
        private StateMachine.StateMachine _stateMachine;
        private bool _isInitialized = false;
        
        // 当前状态
        private string _currentState = "Idle";
        private string _previousState = "";
        
        // 状态配置
        [Export] public bool EnableStateLogging { get; set; } = true;
        [Export] public bool EnableAutoAnimation { get; set; } = true;  // 添加动画自动播放开关

        /// <summary>
        /// 状态机引用（供外部访问）
        /// </summary>
        public StateMachine.StateMachine StateMachine => _stateMachine;

        #endregion

        #region 生命周期方法

        /// <summary>
        /// 清理资源
        /// </summary>
        public override void _ExitTree()
        {
            _stateMachine = null;
            Logger2.Info("PlayerStateManager: 资源清理完成");
        }

        #endregion

        #region 初始化和设置

        /// <summary>
        /// 初始化状态管理器
        /// </summary>
        public void Initialize(Player player)
        {
            if (_isInitialized)
            {
                Logger2.Warn("PlayerStateManager: 已经初始化过了");
                return;
            }

            _player = player ?? throw new ArgumentNullException(nameof(player));
            
            // 初始化状态机
            _stateMachine = new StateMachine.StateMachine(_player);
            SetupStates();
            
            _isInitialized = true;
            
            Logger2.Info("PlayerStateManager: 初始化完成，初始状态: {0}", _currentState);
        }
        
        /// <summary>
        /// 设置所有状态 - 使用专业的PlayerStates类
        /// </summary>
        private void SetupStates()
        {
            // 注册专业的状态实现类
            _stateMachine.Register("Idle", () => new IdleState(_player));
            _stateMachine.Register("Move", () => new MoveState(_player));
            _stateMachine.Register("Attack", () => new AttackState(_player));
            _stateMachine.Register("Hurt", () => new HurtState(_player));
            _stateMachine.Register("Dead", () => new DeadState(_player));
            
            // 设置初始状态
            _stateMachine.ChangeState("Idle");
        }

        #endregion

        #region 状态更新和转换

        /// <summary>
        /// 每帧更新状态机
        /// </summary>
        public void Update(double delta)
        {
            if (!_isInitialized) return;
            
            // 更新状态机
            _stateMachine.Update(delta);
            
            // 检查状态变化
            CheckStateTransitions();
        }
        
        /// <summary>
        /// 检查状态转换条件
        /// </summary>
        private void CheckStateTransitions()
        {
            string newState = DetermineNextState();
            
            if (newState != _currentState)
            {
                if (EnableStateLogging)
                {
                    Logger2.Info("PlayerStateManager: 状态转换 {0} -> {1}", _currentState, newState);
                }
                
                _previousState = _currentState;
                _currentState = newState;
                _stateMachine.ChangeState(newState);
                
                // 自动触发动画（如果启用）
                if (EnableAutoAnimation)
                {
                    PlayAnimationForState(newState);
                }
            }
        }
        
        /// <summary>
        /// 确定下一个应该进入的状态
        /// </summary>
        private string DetermineNextState()
        {
            // 检查死亡条件
            if (IsDead())
                return "Dead";
                
            // 检查受伤条件
            if (ShouldBeHurt())
                return "Hurt";
                
            // 检查攻击条件
            if (ShouldAttack())
                return "Attack";
                
            // 检查移动条件
            if (ShouldMove())
                return "Move";
                
            // 默认返回闲置状态
            return "Idle";
        }

        #endregion

        #region 状态条件判断

        /// <summary>
        /// 检查是否应该死亡
        /// </summary>
        private bool IsDead()
        {
            // 检查生命值是否为0或负数
            if (_player != null)
            {
                var currentHealthDef = StatDefDataLoader.Instance.GetStatDefById(StatDefDataLoader.CurrentHealth);
                if (currentHealthDef != null)
                {
                    float health = _player.GetStatContainer().Get(currentHealthDef);
                    return health <= 0;
                }
            }
            return false;
        }
        
        /// <summary>
        /// 检查是否应该进入受伤状态
        /// </summary>
        private bool ShouldBeHurt()
        {
            // TODO: 实现受伤检测逻辑
            // 可以检查是否受到攻击、是否有受伤标记等
            return false;
        }
        
        /// <summary>
        /// 检查是否应该攻击
        /// </summary>
        private bool ShouldAttack()
        {
            // TODO: 从战斗系统获取攻击状态
            // return _player.CombatSystem?.IsAttacking() ?? false;
            return false;
        }
        
        /// <summary>
        /// 检查是否应该移动
        /// </summary>
        private bool ShouldMove()
        {
            // 从移动系统获取移动状态
            return _player?.MovementComponent?.IsMoving ?? false;
        }

        #endregion

        #region 状态控制方法

        /// <summary>
        /// 强制切换到指定状态
        /// </summary>
        public void ForceStateChange(string stateName)
        {
            if (!_isInitialized) return;
            
            if (EnableStateLogging)
            {
                Logger2.Info("PlayerStateManager: 强制状态切换 {0} -> {1}", _currentState, stateName);
            }
            
            _previousState = _currentState;
            _currentState = stateName;
            _stateMachine.ChangeState(stateName);
            
            // 自动触发动画（如果启用）
            if (EnableAutoAnimation)
            {
                PlayAnimationForState(stateName);
            }
        }
        
        /// <summary>
        /// 获取当前状态
        /// </summary>
        public string GetCurrentState()
        {
            return _currentState;
        }
        
        /// <summary>
        /// 获取上一个状态
        /// </summary>
        public string GetPreviousState()
        {
            return _previousState;
        }
        
        /// <summary>
        /// 检查当前是否处于指定状态
        /// </summary>
        public bool IsInState(string stateName)
        {
            return _currentState == stateName;
        }
        
        /// <summary>
        /// 检查是否可以从当前状态转换到目标状态
        /// </summary>
        public bool CanTransitionTo(string targetState)
        {
            // 实现状态转换规则
            // 例如：不能从死亡状态转换到其他状态
            if (_currentState == "Dead" && targetState != "Dead")
                return false;
                
            return true;
        }

        #endregion

        #region 动画系统集成

        /// <summary>
        /// 根据状态播放对应动画
        /// </summary>
        private void PlayAnimationForState(string stateName)
        {
            string animationName = stateName.ToLower() switch
            {
                "idle" => "idle",
                "move" => "walk",
                "attack" => "attack",
                "hurt" => "hurt",
                "dead" => "death",
                _ => "idle"
            };
            
            _player?.AnimationComponent?.Play(animationName);
        }

        #endregion

        #region 自定义状态管理

        /// <summary>
        /// 添加自定义状态
        /// </summary>
        public void AddCustomState(string stateName, Func<State> stateFactory)
        {
            if (!_isInitialized) return;
            
            _stateMachine.Register(stateName, stateFactory);
            Logger2.Info("PlayerStateManager: 添加自定义状态 {0}", stateName);
        }
        
        /// <summary>
        /// 移除自定义状态
        /// </summary>
        public void RemoveCustomState(string stateName)
        {
            // 注意：StateMachine可能需要添加移除状态的方法
            Logger2.Warn("PlayerStateManager: 移除状态功能暂未实现 {0}", stateName);
        }
        
        /// <summary>
        /// 重置状态机
        /// </summary>
        public void ResetStateMachine()
        {
            if (!_isInitialized) return;
            
            _currentState = "Idle";
            _previousState = "";
            _stateMachine.ChangeState("Idle");
            
            // 重置动画
            if (EnableAutoAnimation)
            {
                _player?.AnimationComponent?.Play("idle");
            }
            
            Logger2.Info("PlayerStateManager: 状态机已重置");
        }

        #endregion
    }
}