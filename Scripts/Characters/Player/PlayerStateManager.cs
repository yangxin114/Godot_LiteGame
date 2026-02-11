using Godot;
using System;
using StateMachine;
using Logs;
using Numerical;

namespace Characters
{
    /// <summary>
    /// 玩家状态管理器
    /// 负责管理玩家的各种状态（闲置、移动、攻击、受伤、死亡等）
    /// </summary>
    public partial class PlayerStateManager : Node
    {
        private Player _player;
        private StateMachine.StateMachine _stateMachine;
        private bool _isInitialized = false;
        
        // 当前状态
        private string _currentState = "Idle";
        private string _previousState = "";
        
        // 状态配置
        [Export] public bool EnableStateLogging { get; set; } = true;

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
        /// 设置所有状态
        /// </summary>
        private void SetupStates()
        {
            // 注册各种状态（使用匿名状态实现）
            _stateMachine.Register("Idle", () => CreateSimpleState("Idle"));
            _stateMachine.Register("Move", () => CreateSimpleState("Move"));
            _stateMachine.Register("Attack", () => CreateSimpleState("Attack"));
            _stateMachine.Register("Hurt", () => CreateSimpleState("Hurt"));
            _stateMachine.Register("Dead", () => CreateSimpleState("Dead"));
            
            // 设置初始状态
            _stateMachine.ChangeState("Idle");
        }
        
        /// <summary>
        /// 创建简单状态实例
        /// </summary>
        private State CreateSimpleState(string name)
        {
            return new SimpleState(_player, name);
        }

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
                    float health = _player.Stats.Get(currentHealthDef);
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
            // TODO: 从移动系统获取移动状态
            // return _player.MovementSystem?.IsMoving() ?? false;
            return false;
        }
        
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
            // TODO: 实现状态转换规则
            // 例如：不能从死亡状态转换到其他状态
            if (_currentState == "Dead" && targetState != "Dead")
                return false;
                
            return true;
        }
        
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
            
            Logger2.Info("PlayerStateManager: 状态机已重置");
        }
        
        /// <summary>
        /// 清理资源
        /// </summary>
        public override void _ExitTree()
        {
            _stateMachine = null;
            Logger2.Info("PlayerStateManager: 资源清理完成");
        }
    }
    
    /// <summary>
    /// 简单状态实现类
    /// </summary>
    public class SimpleState : State
    {
        private string _name;
        
        public SimpleState(Node owner, string name) : base(owner)
        {
            _name = name;
        }
        
        public override void Enter()
        {
            Logger2.Debug("SimpleState.Enter: 进入状态 {0}", _name);
        }
        
        public override void Exit()
        {
            Logger2.Debug("SimpleState.Exit: 离开状态 {0}", _name);
        }
        
        public override void Update(double delta)
        {
            // 简单状态不需要每帧更新逻辑
        }
    }
}