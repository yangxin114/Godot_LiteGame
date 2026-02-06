using Godot;
using System;
using PlayerStateMachine;
using PlayerStateMachine.PlayerStates;

namespace Characters
{
    /// <summary>
    /// BasePlayer：基础玩家节点，实现了 IPlayer 接口并内置一个轻量状态机。
    /// 作用：作为玩家的默认运行时实现，负责加载外观（Sprite / ColorRect）、
    /// 管理基本属性（Name/Speed/Health），并注册基础状态（Idle / Move）。
    ///
    /// 可扩展点：
    /// - 通过继承 BasePlayer 来实现具体的移动、攻击、技能逻辑。
    /// - 在子类的 _Ready 中注册更多状态（例如 Attack、Hurt、Dead）。
    /// </summary>
    public partial class BasePlayer : Node2D, IPlayer
    {
        /// <summary>
        /// 内部状态机实例
        /// </summary>
        private StateMachine stateMachine;

        // 可配置属性（通过 PlayerData 初始化）
        public string PlayerName { get; private set; }
        public float Speed { get; private set; } = 200f;
        public int Health { get; private set; } = 10;

        public override void _Ready()
        {
            // 初始化状态机并注册基础状态
            stateMachine = new StateMachine(this);
            stateMachine.Register("Idle", () => new IdleState(this));
            stateMachine.Register("Move", () => new MoveState(this));
            // 注册扩展状态：攻击、受伤、死亡
            stateMachine.Register("Attack", () => new PlayerStateMachine.PlayerStates.AttackState(this));
            stateMachine.Register("Hurt", () => new PlayerStateMachine.PlayerStates.HurtState(this));
            stateMachine.Register("Dead", () => new PlayerStateMachine.PlayerStates.DeadState(this));
            stateMachine.ChangeState("Idle");
        }

        /// <summary>
        /// 根据 PlayerData 初始化玩家属性与外观。可以在创建后或动态替换时调用。
        /// </summary>
        /// <param name="data">播放器定义数据</param>
        public void Init(PlayerData data)
        {
            if (data == null)
                return;

            PlayerName = data.Name ?? PlayerName;
            Speed = data.Speed <= 0 ? Speed : data.Speed;
            Health = data.Health > 0 ? data.Health : Health;

            // 如果提供了 Sprite 路径，尝试加载贴图并创建 Sprite2D 显示
            if (!string.IsNullOrEmpty(data.SpritePath))
            {
                var tex = GD.Load<Texture2D>(data.SpritePath);
                if (tex != null)
                {
                    var sprite = new Sprite2D();
                    sprite.Texture = tex;
                    AddChild(sprite);
                }
            }
            else
            {
                // 否则创建一个简单的 ColorRect 作为占位显示（便于调试）
                var rect = new ColorRect();
                rect.Color = data.Color;
                rect.Size = new Vector2(32, 32);
                AddChild(rect);
            }
        }

        /// <summary>
        /// 每帧由外部包装器调用，推进状态机逻辑。
        /// </summary>
        public void Process(double delta)
        {
            stateMachine?.Update(delta);
        }

        /// <summary>
        /// 外部接口：按名称切换状态。
        /// </summary>
        public void SetState(string stateName)
        {
            stateMachine?.ChangeState(stateName);
        }

        /// <summary>
        /// 受伤处理：减少生命值并根据剩余生命决定进入 Hurt 或 Dead 状态。
        /// 此方法可由外部（例如敌人攻击或测试）调用。
        /// </summary>
        /// <param name="amount">受伤数值（正数）</param>
        public void TakeDamage(int amount)
        {
            if (amount <= 0)
                return;
            Health -= amount;
            if (Health <= 0)
            {
                SetState("Dead");
            }
            else
            {
                SetState("Hurt");
            }
        }

        /// <summary>
        /// 暴露当前状态名称，便于测试与调试。
        /// </summary>
        public string CurrentStateName => stateMachine?.CurrentName;
    }
}
