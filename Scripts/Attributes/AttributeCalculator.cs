using System.Collections.Generic;
using Godot;

namespace Attributes
{
    /// <summary>
    /// AttributeCalculator 是整个系统
    /// 唯一允许“做数学计算”的地方
    ///
    /// ⚠️ 非常重要：
    /// 所有数值问题都只能改这里
    /// </summary>
    public static class AttributeCalculator
    {
        public static void Recalculate(
            AttributeInstance attr,
            AttributeContext ctx
        )
        {
            float value = attr.BaseValue;

            ApplyPhase(attr, ModifierPhase.Base, ref value, ctx);
            ApplyPhase(attr, ModifierPhase.Increase, ref value, ctx);
            ApplyPhase(attr, ModifierPhase.More, ref value, ctx);
            ApplyPhase(attr, ModifierPhase.Override, ref value, ctx);
            ApplyPhase(attr, ModifierPhase.Clamp, ref value, ctx);

            if (attr.Def.ClampEnabled)
            {
                value = Mathf.Clamp(
                    value,
                    attr.Def.MinValue,
                    attr.Def.MaxValue
                );
            }

            attr.FinalValue = value;
            attr.Dirty = false;
        }

        private static void ApplyPhase(
            AttributeInstance attr,
            ModifierPhase phase,
            ref float value,
            AttributeContext ctx
        )
        {
            // 按 Priority 排序，优先级高的先应用
            var mods = new List<ModifierInstance>(attr.Modifiers);
            mods.Sort((a, b) => b.Def.Priority.CompareTo(a.Def.Priority));

            foreach (var mod in mods)
            {
                if (mod == null) continue;
                if (mod.IsExpired())
                    continue;

                if (mod.Def == null) continue;
                if (mod.Def.Phase != phase)
                    continue;

                if (mod.Def.UseContext)
                {
                    if (ctx == null || !ctx.Tags.Contains(mod.Def.RequiredTag))
                        continue;
                }

                float v = mod.ResolveValue(null);

                switch (mod.Def.Operation)
                {
                    case ModifierOp.Flat:
                        value += v;
                        break;

                    case ModifierOp.Increased:
                        value *= 1f + v;
                        break;

                    case ModifierOp.More:
                        value *= v;
                        break;

                    case ModifierOp.Override:
                        value = v;
                        break;
                }
            }
        }
    }


}