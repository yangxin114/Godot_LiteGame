using Godot;
using StateMachine;
using System;
using Logs;
using Numerical;

namespace Skills
{
    /// <summary>
    /// 技能基类 - 所有技能的基类
    /// 集成了状态机系统，支持复杂的技能释放流程
    /// </summary>
    public partial class BaseSkill : Node2D, ISkillable
    {
        // 技能基本信息
        [Export] public string SkillId { get; set; } = "";
        [Export] public string SkillName { get; set; } = "Unknown Skill";
        [Export] public string Description { get; set; } = "";
        [Export] public float CooldownTime { get; set; } = 1.0f;
        [Export] public float CastTime { get; set; } = 0.5f;
        [Export] public int ManaCost { get; set; } = 10;
        
        // 技能所有者
        public ISkillOwner SkillOwner { get; set; }
        
        // 状态机相关
        private StateMachine.StateMachine _skillStateMachine;
        private string _currentState = "Ready";
        private double _cooldownTimer = 0.0;
        private double _castTimer = 0.0;
        private bool _isOnCooldown = false;
        
        // 技能属性容器
        private StatContainer _skillStats;
        
        // 事件
        public event Action<BaseSkill> OnSkillStarted;
        public event Action<BaseSkill> OnSkillCompleted;
        public event Action<BaseSkill> OnSkillCancelled;
        public event Action<BaseSkill> OnCooldownStarted;
        public event Action<BaseSkill> OnCooldownFinished;

        public override void _Ready()
        {
            InitializeSkill();
        }

        /// <summary>
        /// 初始化技能
        /// </summary>
        private void InitializeSkill()
        {
            // 初始化状态机
            _skillStateMachine = new StateMachine.StateMachine(this);
            SetupSkillStates();
            
            // 初始化属性容器
            _skillStats = new StatContainer();
            
            // 设置初始状态
            _skillStateMachine.ChangeState("Ready");
            
            Logger2.Info("BaseSkill: 技能 {0} 初始化完成", SkillName);
        }

        /// <summary>
        /// 设置技能状态
        /// </summary>
        private void SetupSkillStates()
        {
            _skillStateMachine.Register("Ready", () => new SkillReadyState(this));
            _skillStateMachine.Register("Casting", () => new SkillCastingState(this));
            _skillStateMachine.Register("Active", () => new SkillActiveState(this));
            _skillStateMachine.Register("Cooldown", () => new SkillCooldownState(this));
            _skillStateMachine.Register("Disabled", () => new SkillDisabledState(this));
        }

        /// <summary>
        /// 尝试使用技能
        /// </summary>
        public virtual bool TryUseSkill()
        {
            if (!CanUseSkill())
            {
                Logger2.Warn("BaseSkill: 技能 {0} 无法使用", SkillName);
                return false;
            }

            // 开始施法过程
            StartCasting();
            return true;
        }

        /// <summary>
        /// 检查技能是否可以使用
        /// </summary>
        public virtual bool CanUseSkill()
        {
            // 检查冷却时间
            if (_isOnCooldown)
            {
                Logger2.Debug("BaseSkill: 技能 {0} 正在冷却中", SkillName);
                return false;
            }

            // 检查施法状态
            if (_currentState == "Casting" || _currentState == "Active")
            {
                Logger2.Debug("BaseSkill: 技能 {0} 正在施放中", SkillName);
                return false;
            }

            // 检查魔力消耗（如果有魔力系统）
            if (!CheckManaCost())
            {
                Logger2.Debug("BaseSkill: 魔力不足，无法使用技能 {0}", SkillName);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 开始施法
        /// </summary>
        private void StartCasting()
        {
            if (CastTime <= 0)
            {
                // 瞬发技能，直接激活
                ActivateSkill();
            }
            else
            {
                // 需要施法时间
                _castTimer = 0.0;
                _skillStateMachine.ChangeState("Casting");
                OnSkillStarted?.Invoke(this);
                Logger2.Info("BaseSkill: 开始施放技能 {0}", SkillName);
            }
        }

        /// <summary>
        /// 激活技能效果
        /// </summary>
        public virtual void ActivateSkill()
        {
            _skillStateMachine.ChangeState("Active");
            ExecuteSkillEffect();
            StartCooldown();
            OnSkillCompleted?.Invoke(this);
            Logger2.Info("BaseSkill: 技能 {0} 激活", SkillName);
        }

        /// <summary>
        /// 执行技能效果（子类重写）
        /// </summary>
        protected virtual void ExecuteSkillEffect()
        {
            // 基础技能不执行具体效果
            // 子类应该重写此方法实现具体技能逻辑
            Logger2.Debug("BaseSkill: 执行技能效果 - {0}", SkillName);
        }

        /// <summary>
        /// 开始冷却（公共方法，供SkillManager调用）
        /// </summary>
        public void StartCooldown()
        {
            if (!_isOnCooldown)
            {
                StartCooldownInternal();
            }
        }

        /// <summary>
        /// 内部开始冷却方法
        /// </summary>
        private void StartCooldownInternal()
        {
            if (CooldownTime > 0)
            {
                _cooldownTimer = 0.0;
                _isOnCooldown = true;
                _skillStateMachine.ChangeState("Cooldown");
                OnCooldownStarted?.Invoke(this);
                Logger2.Info("BaseSkill: 技能 {0} 开始冷却", SkillName);
            }
        }

        /// <summary>
        /// 重置冷却时间
        /// </summary>
        public void ResetCooldown()
        {
            _isOnCooldown = false;
            _cooldownTimer = 0.0;
            if (_currentState == "Cooldown")
            {
                _skillStateMachine.ChangeState("Ready");
            }
            OnCooldownFinished?.Invoke(this);
            Logger2.Info("BaseSkill: 技能 {0} 冷却已重置", SkillName);
        }

        /// <summary>
        /// 取消技能
        /// </summary>
        public virtual void CancelSkill()
        {
            _skillStateMachine.ChangeState("Ready");
            _castTimer = 0.0;
            OnSkillCancelled?.Invoke(this);
            Logger2.Info("BaseSkill: 技能 {0} 已取消", SkillName);
        }

        /// <summary>
        /// 禁用技能
        /// </summary>
        public virtual void DisableSkill()
        {
            _skillStateMachine.ChangeState("Disabled");
            Logger2.Info("BaseSkill: 技能 {0} 已禁用", SkillName);
        }

        /// <summary>
        /// 启用技能
        /// </summary>
        public virtual void EnableSkill()
        {
            if (_currentState == "Disabled")
            {
                _skillStateMachine.ChangeState("Ready");
                Logger2.Info("BaseSkill: 技能 {0} 已启用", SkillName);
            }
        }

        /// <summary>
        /// 检查魔力消耗
        /// </summary>
        private bool CheckManaCost()
        {
            // TODO: 实现魔力系统集成
            // 这里暂时返回true，实际项目中需要检查玩家魔力
            return true;
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        public override void _Process(double delta)
        {
            // 更新状态机
            _skillStateMachine?.Update(delta);
            
            // 更新冷却计时器
            UpdateCooldown(delta);
            
            // 更新施法计时器
            UpdateCastTimer(delta);
        }

        /// <summary>
        /// 更新冷却时间
        /// </summary>
        private void UpdateCooldown(double delta)
        {
            if (_isOnCooldown)
            {
                _cooldownTimer += delta;
                if (_cooldownTimer >= CooldownTime)
                {
                    FinishCooldown();
                }
            }
        }

        /// <summary>
        /// 更新施法时间
        /// </summary>
        private void UpdateCastTimer(double delta)
        {
            if (_currentState == "Casting")
            {
                _castTimer += delta;
                if (_castTimer >= CastTime)
                {
                    ActivateSkill();
                }
            }
        }

        /// <summary>
        /// 结束冷却
        /// </summary>
        private void FinishCooldown()
        {
            _isOnCooldown = false;
            _cooldownTimer = 0.0;
            _skillStateMachine.ChangeState("Ready");
            OnCooldownFinished?.Invoke(this);
            Logger2.Info("BaseSkill: 技能 {0} 冷却结束", SkillName);
        }

        /// <summary>
        /// 获取剩余冷却时间
        /// </summary>
        public float GetRemainingCooldown()
        {
            if (!_isOnCooldown) return 0.0f;
            return (float)Math.Max(0, CooldownTime - _cooldownTimer);
        }

        /// <summary>
        /// 获取施法进度 (0-1)
        /// </summary>
        public float GetCastProgress()
        {
            if (_currentState != "Casting") return 0.0f;
            return (float)(_castTimer / CastTime);
        }

        /// <summary>
        /// 获取当前状态
        /// </summary>
        public string GetCurrentState()
        {
            return _currentState;
        }

        /// <summary>
        /// 检查是否正在冷却
        /// </summary>
        public bool IsOnCooldown()
        {
            return _isOnCooldown;
        }

        /// <summary>
        /// 检查是否正在施法
        /// </summary>
        public bool IsCasting()
        {
            return _currentState == "Casting";
        }

        /// <summary>
        /// 检查技能是否可用
        /// </summary>
        public bool IsAvailable()
        {
            return _currentState == "Ready" && !_isOnCooldown;
        }

        /// <summary>
        /// 获取技能属性容器
        /// </summary>
        public StatContainer GetSkillStats()
        {
            return _skillStats;
        }

        /// <summary>
        /// 设置技能所有者
        /// </summary>
        public void SetSkillOwner(ISkillOwner owner)
        {
            SkillOwner = owner;
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public override void _ExitTree()
        {
            _skillStateMachine = null;
            _skillStats = null;
            Logger2.Info("BaseSkill: 技能 {0} 资源已清理", SkillName);
        }
    }
}