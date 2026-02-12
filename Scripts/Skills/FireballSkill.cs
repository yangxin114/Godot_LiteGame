using Godot;
using Logs;
using System;

namespace Skills
{
    /// <summary>
    /// 火球术技能示例
    /// 展示如何创建具体的游戏技能
    /// </summary>
    public partial class FireballSkill : BaseSkill
    {
        [Export] public float Damage { get; set; } = 50.0f;
        [Export] public float Range { get; set; } = 300.0f;
        [Export] public float Speed { get; set; } = 400.0f;
        
        // 火球特效预制体路径
        [Export] public string FireballScenePath { get; set; } = "res://scenes/effects/fireball.tscn";

        public override void _Ready()
        {
            // 设置技能基本信息
            SkillId = "fireball_basic";
            SkillName = "火球术";
            Description = "发射一枚火球攻击敌人，造成火焰伤害";
            CooldownTime = 2.0f;
            CastTime = 0.3f;
            ManaCost = 15;
            
            base._Ready();
            
            // 订阅技能事件
            OnSkillStarted += OnFireballStarted;
            OnSkillCompleted += OnFireballCompleted;
            OnSkillCancelled += OnFireballCancelled;
        }

        /// <summary>
        /// 执行火球术的具体效果
        /// </summary>
        protected override void ExecuteSkillEffect()
        {
            Logger2.Info("FireballSkill: 发射火球");
            
            // 创建火球特效
            SpawnFireball();
            
            // 应用伤害（这里需要实现伤害系统）
            ApplyDamage();
            
            // 播放音效
            PlaySound();
        }

        /// <summary>
        /// 生成火球特效
        /// </summary>
        private void SpawnFireball()
        {
            try
            {
                // 加载火球场景
                var fireballScene = GD.Load<PackedScene>(FireballScenePath);
                if (fireballScene != null)
                {
                    var fireball = fireballScene.Instantiate<Node2D>();
                    
                    // 设置火球位置（从施法者位置开始）
                    if (SkillOwner != null)
                    {
                        var ownerNode = SkillOwner.GetNode2DOwner();
                        if (ownerNode != null)
                        {
                            fireball.GlobalPosition = ownerNode.GlobalPosition;
                            
                            // 添加到场景树
                            ownerNode.AddChild(fireball);
                            
                            // 设置火球飞行方向和速度
                            // 这里需要根据玩家面向方向或鼠标位置计算
                            SetupFireballTrajectory(fireball);
                        }
                    }
                }
                else
                {
                    Logger2.Warn("FireballSkill: 无法加载火球场景 {0}", FireballScenePath);
                }
            }
            catch (Exception ex)
            {
                Logger2.Error("FireballSkill.SpawnFireball: 错误 - {0}", ex.Message);
            }
        }

        /// <summary>
        /// 设置火球轨迹
        /// </summary>
        private void SetupFireballTrajectory(Node2D fireball)
        {
            // TODO: 实现火球飞行逻辑
            // 这里应该根据玩家面朝方向或鼠标位置计算飞行方向
            Logger2.Debug("FireballSkill: 设置火球轨迹");
        }

        /// <summary>
        /// 应用伤害
        /// </summary>
        private void ApplyDamage()
        {
            // TODO: 实现伤害系统集成
            Logger2.Info("FireballSkill: 应用 {0} 点火焰伤害", Damage);
        }

        /// <summary>
        /// 播放音效
        /// </summary>
        private void PlaySound()
        {
            // TODO: 实现音效系统集成
            Logger2.Debug("FireballSkill: 播放火球音效");
        }

        /// <summary>
        /// 技能开始事件处理
        /// </summary>
        private void OnFireballStarted(BaseSkill skill)
        {
            Logger2.Info("FireballSkill: 火球术开始施法");
            // 可以在这里添加施法开始的视觉效果
        }

        /// <summary>
        /// 技能完成事件处理
        /// </summary>
        private void OnFireballCompleted(BaseSkill skill)
        {
            Logger2.Info("FireballSkill: 火球术施放完成");
            // 可以在这里添加施法完成的反馈效果
        }

        /// <summary>
        /// 技能取消事件处理
        /// </summary>
        private void OnFireballCancelled(BaseSkill skill)
        {
            Logger2.Info("FireballSkill: 火球术被取消");
            // 可以在这里添加取消时的处理
        }

        /// <summary>
        /// 重写施法状态类，添加火球特有的施法效果
        /// </summary>
        public class FireballCastingState : SkillCastingState
        {
            public FireballCastingState(FireballSkill skill) : base(skill) { }

            protected override void OnCastingStarted()
            {
                base.OnCastingStarted();
                Logger2.Debug("FireballCastingState: 显示火球施法特效");
                // 可以在这里添加火球特有的施法特效
            }
        }

        /// <summary>
        /// 重写激活状态类，添加火球特有的激活效果
        /// </summary>
        public class FireballActiveState : SkillActiveState
        {
            private FireballSkill fireballSkill;

            public FireballActiveState(FireballSkill skill) : base(skill)
            {
                fireballSkill = skill;
            }

            protected override void OnSkillActivated()
            {
                base.OnSkillActivated();
                Logger2.Debug("FireballActiveState: 火球激活特效");
                // 可以在这里添加火球激活时的特效
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public override void _ExitTree()
        {
            // 取消事件订阅
            OnSkillStarted -= OnFireballStarted;
            OnSkillCompleted -= OnFireballCompleted;
            OnSkillCancelled -= OnFireballCancelled;
            
            base._ExitTree();
        }
    }
}