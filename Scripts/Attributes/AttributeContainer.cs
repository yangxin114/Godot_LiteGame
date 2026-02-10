using System;
using System.Collections.Generic;
using Logs;

namespace Attributes
{
    /// <summary>
    /// AttributeContainer 是某个实体的“数值大脑”
    ///
    /// 玩家 / 敌人 / 子弹 / 召唤物
    /// 全部使用同一套逻辑
    /// </summary>
    public class AttributeContainer
    {
        private readonly Dictionary<string, AttributeInstance> _attributes = new();

        public AttributesOwner Owner;

        /// <summary>
        /// 属性变更事件：(AttributeDef, oldValue, newValue)
        /// </summary>
        public event Action<AttributeDef, float, float> ValueChanged;

        /// <summary>
        /// 修饰器添加事件
        /// </summary>
        public event Action<ModifierInstance> ModifierAdded;

        /// <summary>
        /// 修饰器移除事件
        /// </summary>
        public event Action<ModifierInstance> ModifierRemoved;

        /// <summary>
        /// 获取属性最终值
        /// </summary>
        public float Get(AttributeDef def, AttributeContext ctx = null)
        {
            var attr = EnsureAttribute(def);

            // 先清理过期的修饰器，避免影响计算
            CleanExpiredModifiers(attr);

            var old = attr.FinalValue;

            if (attr.Dirty)
            {
                AttributeCalculator.Recalculate(attr, ctx);
                if (Math.Abs(old - attr.FinalValue) > 1e-6f)
                {
                    ValueChanged?.Invoke(def, old, attr.FinalValue);
                    Logger2.Info("AttributeContainer", "ValueChanged {0} {1} -> {2}", def.Id, old, attr.FinalValue);
                }
            }

            return attr.FinalValue;
        }

        /// <summary>
        /// 设置基础值
        /// </summary>
        public void SetBase(AttributeDef def, float value)
        {
            var attr = EnsureAttribute(def);
            attr.BaseValue = value;
            attr.Dirty = true;
        }

        /// <summary>
        /// 添加 Modifier
        /// </summary>
        public void AddModifier(ModifierInstance mod)
        {
            if (mod == null || mod.Def == null || mod.Def.Target == null)
            {
                Logger2.Warn("AttributeContainer", "Attempt to add invalid modifier");
                return;
            }

            var attr = EnsureAttribute(mod.Def.Target);

            // 设置应用时间
            mod.ApplyTime = (float)System.DateTime.UtcNow.Subtract(System.DateTime.UnixEpoch).TotalSeconds;

            // 处理叠加/唯一性规则
            if (mod.Def.StackMode == ModifierStackMode.UniquePerSource && mod.Source != null)
            {
                var existing = attr.Modifiers.Find(m => m.Def == mod.Def && m.Source == mod.Source);
                if (existing != null)
                {
                    Logger2.Debug("AttributeContainer", "Modifier unique per source exists, ignoring add (or refresh). Def={0}", mod.Def.Target.Id);
                    // 如果是 RefreshDuration，则刷新时间
                    if (mod.Def.StackMode == ModifierStackMode.RefreshDuration)
                    {
                        existing.ApplyTime = mod.ApplyTime;
                    }
                    return;
                }
            }

            if (mod.Def.StackMode == ModifierStackMode.UniqueGlobal)
            {
                var existing = attr.Modifiers.Find(m => m.Def == mod.Def);
                if (existing != null)
                {
                    Logger2.Debug("AttributeContainer", "Modifier unique global exists, ignoring add. Def={0}", mod.Def.Target.Id);
                    if (mod.Def.StackMode == ModifierStackMode.RefreshDuration)
                    {
                        existing.ApplyTime = mod.ApplyTime;
                    }
                    return;
                }
            }

            // 默认可堆叠
            attr.Modifiers.Add(mod);
            attr.Dirty = true;
            ModifierAdded?.Invoke(mod);
            Logger2.Info("AttributeContainer", "ModifierAdded {0} from {1}", mod.Def.Target.Id, mod.Source?.Name ?? "<anon>");
        }

        /// <summary>
        /// 移除修饰器
        /// </summary>
        public void RemoveModifier(ModifierInstance mod)
        {
            if (mod == null || mod.Def == null || mod.Def.Target == null) return;
            if (!_attributes.TryGetValue(mod.Def.Target.Id, out var attr)) return;
            if (attr.Modifiers.Remove(mod))
            {
                attr.Dirty = true;
                ModifierRemoved?.Invoke(mod);
                Logger2.Info("AttributeContainer", "ModifierRemoved {0}", mod.Def.Target.Id);
            }
        }

        /// <summary>
        /// 清理过期的修饰器
        /// </summary>
        private void CleanExpiredModifiers(AttributeInstance attr)
        {
            bool removed = false;
            for (int i = attr.Modifiers.Count - 1; i >= 0; i--)
            {
                var m = attr.Modifiers[i];
                if (m.IsExpired())
                {
                    attr.Modifiers.RemoveAt(i);
                    removed = true;
                    ModifierRemoved?.Invoke(m);
                    Logger2.Debug("AttributeContainer", "Auto-removed expired modifier on {0}", attr.Def?.Id ?? "?");
                }
            }
            if (removed) attr.Dirty = true;
        }

        /// <summary>
        /// 确保属性存在（从定义生成实例）
        /// </summary>
        public AttributeInstance EnsureAttribute(AttributeDef def)
        {
            if (!_attributes.TryGetValue(def.Id, out var attr))
            {
                attr = new AttributeInstance
                {
                    Def = def,
                    BaseValue = def.DefaultValue,
                    FinalValue = def.DefaultValue,
                    Dirty = true
                };
                _attributes[def.Id] = attr;
                Logger2.Info("AttributeContainer", "EnsureAttribute created {0}", def.Id);
            }
            return attr;
        }

        /// <summary>
        /// 从一组 AttributeDef 初始化容器
        /// </summary>
        public void InitializeFromDefs(IEnumerable<AttributeDef> defs)
        {
            foreach (var d in defs)
            {
                EnsureAttribute(d);
            }
        }
    }



}