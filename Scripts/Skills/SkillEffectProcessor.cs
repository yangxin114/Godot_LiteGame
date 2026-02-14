using Godot;
using System;
using System.Collections.Generic;
using Logs;

namespace Skills
{
    /// <summary>
    /// 技能效果处理器
    /// 专门负责处理各种技能效果的执行和管理
    /// </summary>
    public partial class SkillEffectProcessor : Node
    {
        #region 字段和属性

        /// <summary>
        /// 效果处理器字典
        /// </summary>
        private Dictionary<EffectType, IEffectHandler> _effectHandlers = new();

        /// <summary>
        /// 激活中的效果列表
        /// </summary>
        private List<ActiveEffectInstance> _activeEffects = new();

        #endregion

        #region 生命周期

        public override void _Ready()
        {
            InitializeEffectHandlers();
        }

        public override void _Process(double delta)
        {
            UpdateEffects((float)delta);
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化效果处理器
        /// </summary>
        private void InitializeEffectHandlers()
        {
            // 注册各种效果处理器
            RegisterEffectHandler(EffectType.Damage, new DamageEffectHandler());
            RegisterEffectHandler(EffectType.Heal, new HealEffectHandler());
            RegisterEffectHandler(EffectType.Buff, new BuffEffectHandler());
            RegisterEffectHandler(EffectType.Debuff, new DebuffEffectHandler());
            RegisterEffectHandler(EffectType.StatModifier, new StatModifierEffectHandler());
            RegisterEffectHandler(EffectType.CrowdControl, new CrowdControlEffectHandler());
            RegisterEffectHandler(EffectType.Summon, new SummonEffectHandler());

            Logger2.Info("SkillEffectProcessor: 效果处理器初始化完成");
        }

        /// <summary>
        /// 注册效果处理器
        /// </summary>
        private void RegisterEffectHandler(EffectType effectType, IEffectHandler handler)
        {
            _effectHandlers[effectType] = handler;
            Logger2.Debug($"SkillEffectProcessor: 注册效果处理器 {effectType}");
        }

        #endregion

        #region 效果处理

        /// <summary>
        /// 应用技能效果
        /// </summary>
        public void ApplyEffect(SkillEffect effect, ISkill caster, ISkill target, string sourceSkillId)
        {
            if (effect == null || target == null)
            {
                Logger2.Warn("SkillEffectProcessor: 无效的效果或目标");
                return;
            }

            // 检查是否有对应的效果处理器
            if (!_effectHandlers.TryGetValue(effect.Type, out var handler))
            {
                Logger2.Warn($"SkillEffectProcessor: 未找到效果处理器 {effect.Type}");
                return;
            }

            try
            {
                // 执行效果应用
                handler.ApplyEffect(effect, caster, target, sourceSkillId);

                // 如果是持续效果，添加到激活列表
                if (effect.Duration > 0)
                {
                    var activeEffect = new ActiveEffectInstance
                    {
                        Effect = effect,
                        Caster = caster,
                        Target = target,
                        SourceSkillId = sourceSkillId,
                        RemainingDuration = effect.Duration,
                        Handler = handler
                    };

                    _activeEffects.Add(activeEffect);
                    
                    Logger2.Debug($"SkillEffectProcessor: 应用持续效果 {effect.Name}, 时长: {effect.Duration}s");
                }
                else
                {
                    Logger2.Debug($"SkillEffectProcessor: 应用即时效果 {effect.Name}");
                }
            }
            catch (Exception ex)
            {
                Logger2.Error($"SkillEffectProcessor: 应用效果失败 - {ex.Message}");
            }
        }

        /// <summary>
        /// 移除效果
        /// </summary>
        public void RemoveEffect(string effectId, ISkill target, string sourceSkillId = null)
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var activeEffect = _activeEffects[i];
                
                if (activeEffect.Effect.EffectId == effectId && 
                    activeEffect.Target == target &&
                    (sourceSkillId == null || activeEffect.SourceSkillId == sourceSkillId))
                {
                    try
                    {
                        // 执行效果移除
                        activeEffect.Handler.RemoveEffect(activeEffect.Effect, activeEffect.Caster, activeEffect.Target, activeEffect.SourceSkillId);
                        
                        _activeEffects.RemoveAt(i);
                        
                        Logger2.Debug($"SkillEffectProcessor: 移除效果 {effectId}");
                    }
                    catch (Exception ex)
                    {
                        Logger2.Error($"SkillEffectProcessor: 移除效果失败 - {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 移除目标的所有效果
        /// </summary>
        public void RemoveAllEffects(ISkill target)
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var activeEffect = _activeEffects[i];
                
                if (activeEffect.Target == target)
                {
                    try
                    {
                        activeEffect.Handler.RemoveEffect(activeEffect.Effect, activeEffect.Caster, activeEffect.Target, activeEffect.SourceSkillId);
                        _activeEffects.RemoveAt(i);
                    }
                    catch (Exception ex)
                    {
                        Logger2.Error($"SkillEffectProcessor: 移除效果失败 - {ex.Message}");
                    }
                }
            }
            
            Logger2.Debug($"SkillEffectProcessor: 移除目标 {target} 的所有效果");
        }

        #endregion

        #region 更新逻辑

        /// <summary>
        /// 更新激活效果
        /// </summary>
        private void UpdateEffects(float deltaTime)
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var activeEffect = _activeEffects[i];
                
                // 更新剩余时间
                activeEffect.RemainingDuration -= deltaTime;
                
                try
                {
                    // 执行周期性更新（如果处理器支持）
                    if (activeEffect.Handler is IPeriodicEffectHandler periodicHandler)
                    {
                        periodicHandler.UpdateEffect(activeEffect.Effect, activeEffect.Caster, activeEffect.Target, activeEffect.SourceSkillId, deltaTime);
                    }
                    
                    // 检查效果是否结束
                    if (activeEffect.RemainingDuration <= 0)
                    {
                        // 执行效果移除
                        activeEffect.Handler.RemoveEffect(activeEffect.Effect, activeEffect.Caster, activeEffect.Target, activeEffect.SourceSkillId);
                        _activeEffects.RemoveAt(i);
                        
                        Logger2.Debug($"SkillEffectProcessor: 效果 {activeEffect.Effect.Name} 结束");
                    }
                }
                catch (Exception ex)
                {
                    Logger2.Error($"SkillEffectProcessor: 更新效果失败 - {ex.Message}");
                    _activeEffects.RemoveAt(i);
                }
            }
        }

        #endregion
    }

    #region 效果实例和接口定义

    /// <summary>
    /// 激活中的效果实例
    /// </summary>
    public class ActiveEffectInstance
    {
        public SkillEffect Effect { get; set; }
        public ISkill Caster { get; set; }
        public ISkill Target { get; set; }
        public string SourceSkillId { get; set; }
        public float RemainingDuration { get; set; }
        public IEffectHandler Handler { get; set; }
    }

    /// <summary>
    /// 效果处理器接口
    /// </summary>
    public interface IEffectHandler
    {
        void ApplyEffect(SkillEffect effect, ISkill caster, ISkill target, string sourceSkillId);
        void RemoveEffect(SkillEffect effect, ISkill caster, ISkill target, string sourceSkillId);
    }

    /// <summary>
    /// 周期性效果处理器接口
    /// </summary>
    public interface IPeriodicEffectHandler : IEffectHandler
    {
        void UpdateEffect(SkillEffect effect, ISkill caster, ISkill target, string sourceSkillId, float deltaTime);
    }

    #endregion

    #region 具体效果处理器实现

    /// <summary>
    /// 伤害效果处理器
    /// </summary>
    public class DamageEffectHandler : IEffectHandler
    {
        public void ApplyEffect(SkillEffect effect, ISkill caster, ISkill target, string sourceSkillId)
        {
            // 这里应该调用实际的伤害系统
            Logger2.Debug($"DamageEffectHandler: 对 {target} 造成 {effect.Value} 点伤害");
        }

        public void RemoveEffect(SkillEffect effect, ISkill caster, ISkill target, string sourceSkillId)
        {
            // 伤害效果通常是即时的，不需要特殊移除逻辑
        }
    }

    /// <summary>
    /// 治疗效果处理器
    /// </summary>
    public class HealEffectHandler : IEffectHandler
    {
        public void ApplyEffect(SkillEffect effect, ISkill caster, ISkill target, string sourceSkillId)
        {
            // 这里应该调用实际的治疗系统
            Logger2.Debug($"HealEffectHandler: 为 {target} 恢复 {effect.Value} 点生命");
        }

        public void RemoveEffect(SkillEffect effect, ISkill caster, ISkill target, string sourceSkillId)
        {
            // 治疗效果通常是即时的，不需要特殊移除逻辑
        }
    }

    /// <summary>
    /// 增益效果处理器
    /// </summary>
    public class BuffEffectHandler : IEffectHandler
    {
        public void ApplyEffect(SkillEffect effect, ISkill caster, ISkill target, string sourceSkillId)
        {
            Logger2.Debug($"BuffEffectHandler: 为 {target} 应用增益 {effect.Name}");
            // 这里应该应用实际的增益效果
        }

        public void RemoveEffect(SkillEffect effect, ISkill caster, ISkill target, string sourceSkillId)
        {
            Logger2.Debug($"BuffEffectHandler: 从 {target} 移除增益 {effect.Name}");
            // 这里应该移除实际的增益效果
        }
    }

    /// <summary>
    /// 减益效果处理器
    /// </summary>
    public class DebuffEffectHandler : IEffectHandler
    {
        public void ApplyEffect(SkillEffect effect, ISkill caster, ISkill target, string sourceSkillId)
        {
            Logger2.Debug($"DebuffEffectHandler: 为 {target} 应用减益 {effect.Name}");
            // 这里应该应用实际的减益效果
        }

        public void RemoveEffect(SkillEffect effect, ISkill caster, ISkill target, string sourceSkillId)
        {
            Logger2.Debug($"DebuffEffectHandler: 从 {target} 移除减益 {effect.Name}");
            // 这里应该移除实际的减益效果
        }
    }

    /// <summary>
    /// 属性修改效果处理器
    /// </summary>
    public class StatModifierEffectHandler : IEffectHandler
    {
        public void ApplyEffect(SkillEffect effect, ISkill caster, ISkill target, string sourceSkillId)
        {
            Logger2.Debug($"StatModifierEffectHandler: 修改 {target} 的属性");
            // 这里应该调用属性系统修改目标属性
        }

        public void RemoveEffect(SkillEffect effect, ISkill caster, ISkill target, string sourceSkillId)
        {
            Logger2.Debug($"StatModifierEffectHandler: 恢复 {target} 的属性");
            // 这里应该恢复目标属性
        }
    }

    /// <summary>
    /// 控制效果处理器
    /// </summary>
    public class CrowdControlEffectHandler : IEffectHandler
    {
        public void ApplyEffect(SkillEffect effect, ISkill caster, ISkill target, string sourceSkillId)
        {
            Logger2.Debug($"CrowdControlEffectHandler: 对 {target} 应用控制效果");
            // 这里应该应用眩晕、减速等控制效果
        }

        public void RemoveEffect(SkillEffect effect, ISkill caster, ISkill target, string sourceSkillId)
        {
            Logger2.Debug($"CrowdControlEffectHandler: 移除 {target} 的控制效果");
            // 这里应该移除控制效果
        }
    }

    /// <summary>
    /// 召唤效果处理器
    /// </summary>
    public class SummonEffectHandler : IEffectHandler
    {
        public void ApplyEffect(SkillEffect effect, ISkill caster, ISkill target, string sourceSkillId)
        {
            Logger2.Debug($"SummonEffectHandler: 召唤单位");
            // 这里应该处理召唤逻辑
        }

        public void RemoveEffect(SkillEffect effect, ISkill caster, ISkill target, string sourceSkillId)
        {
            Logger2.Debug($"SummonEffectHandler: 移除召唤单位");
            // 这里应该处理召唤单位的移除
        }
    }

    #endregion
}