using System;
using System.Collections.Generic;
using Logs;

namespace Numerical
{
    /// <summary>
    /// StatContainer 是某个实体的“数值大脑”
    ///
    /// 玩家 / 敌人 / 子弹 / 召唤物
    /// 全部使用同一套逻辑
    /// </summary>
    public class StatContainer
    {
        private readonly Dictionary<string, StatInstance> _stats = new();

        public IStatOwner Owner;

        /// <summary>
        /// 属性变更事件：(StatDef, oldValue, newValue)
        /// </summary>
        public event Action<StatDef, float, float> ValueChanged;

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
        public float Get(StatDef def, StatContext ctx = null)
        {
            var stat = EnsureStat(def);

            // 先清理过期的修饰器，避免影响计算
            CleanExpiredModifiers(stat);

            var old = stat.FinalValue;

            if (stat.Dirty)
            {
                StatCalculator.Recalculate(stat, ctx);
                if (Math.Abs(old - stat.FinalValue) > 1e-6f)
                {
                    ValueChanged?.Invoke(def, old, stat.FinalValue);
                    Logger2.Info("AttributeContainer", "ValueChanged {0} {1} -> {2}", def.Id, old, stat.FinalValue);
                }
            }

            return stat.FinalValue;
        }

        /// <summary>
        /// 设置基础值
        /// </summary>
        public void SetBase(StatDef def, float value)
        {
            var attr = EnsureStat(def);
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
                Logger2.Warn("StatContainer", "Attempt to add invalid modifier");
                return;
            }

            var attr = EnsureStat(mod.Def.Target);

            // 设置应用时间
            mod.ApplyTime = (float)System.DateTime.UtcNow.Subtract(System.DateTime.UnixEpoch).TotalSeconds;

            // 处理叠加/唯一性规则
            if (mod.Def.StackMode == ModifierStackMode.UniquePerSource && mod.Source != null)
            {
                var existing = attr.Modifiers.Find(m => m.Def == mod.Def && m.Source == mod.Source);
                if (existing != null)
                {
                    Logger2.Debug("StatContainer", "Modifier unique per source exists, ignoring add (or refresh). Def={0}", mod.Def.Target.Id);
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
                    Logger2.Debug("StatContainer", "Modifier unique global exists, ignoring add. Def={0}", mod.Def.Target.Id);
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
            Logger2.Info("StatContainer", "ModifierAdded {0} from {1}", mod.Def.Target.Id, mod.Source?.ToString() ?? "null");
        }

        /// <summary>
        /// 移除修饰器
        /// </summary>
        public void RemoveModifier(ModifierInstance mod)
        {
            if (mod == null || mod.Def == null || mod.Def.Target == null) return;
            if (!_stats.TryGetValue(mod.Def.Target.Id, out var attr)) return;
            if (attr.Modifiers.Remove(mod))
            {
                attr.Dirty = true;
                ModifierRemoved?.Invoke(mod);
                Logger2.Info("StatContainer", "ModifierRemoved {0}", mod.Def.Target.Id);
            }
        }

        /// <summary>
        /// 清理过期的修饰器
        /// </summary>
        private void CleanExpiredModifiers(StatInstance stat)
        {
            bool removed = false;
            for (int i = stat.Modifiers.Count - 1; i >= 0; i--)
            {
                var m = stat.Modifiers[i];
                if (m.IsExpired())
                {
                    stat.Modifiers.RemoveAt(i);
                    removed = true;
                    ModifierRemoved?.Invoke(m);
                    Logger2.Debug("StatContainer", "Auto-removed expired modifier on {0}", stat.Def?.Id ?? "?");
                }
            }
            if (removed) stat.Dirty = true;
        }

        /// <summary>
        /// 确保属性存在（从定义生成实例）
        /// </summary>
        public StatInstance EnsureStat(StatDef def)
        {
            if (!_stats.TryGetValue(def.Id, out var stat))
            {
                stat = new StatInstance
                {
                    Def = def,
                    BaseValue = def.DefaultValue,
                    FinalValue = def.DefaultValue,
                    Dirty = true
                };
                _stats[def.Id] = stat;
                Logger2.Info("StatContainer", "EnsureStat created {0}", def.Id);
            }
            return stat;
        }

        /// <summary>
        /// 从一组 StatDef 初始化容器
        /// </summary>
        public void InitializeFromDefs(IEnumerable<StatDef> defs)
        {
            foreach (var d in defs)
            {
                EnsureStat(d);
            }
        }
    }



}