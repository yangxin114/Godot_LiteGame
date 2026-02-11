using System.Collections.Generic;
using Godot;

namespace Numerical
{
    /// <summary>
    /// StatDefDataLoader，包含所有预定义的StatDef内容
    /// 采用单例模式，使用缓存机制优化性能
    /// </summary>
    public partial class StatDefDataLoader
    {
        private static StatDefDataLoader _instance;
        public static StatDefDataLoader Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new StatDefDataLoader();
                }
                return _instance;
            }
        }

        // 缓存所有StatDef对象
        private List<StatDef> _cachedStats;
        // 通过ID快速查找的字典索引
        private Dictionary<string, StatDef> _statIndex;
        // 标记是否已初始化缓存
        private bool _isInitialized = false;

        // 基础属性
        public static string CurrentHealth = "current_health";
        public static string MaxHealth = "max_health";
        public static string CurrentMana = "current_mana";
        public static string MaxMana = "max_mana";
        public static string CurrentStamina = "current_stamina";
        public static string MaxStamina = "max_stamina";
        public static string MoveSpeed = "move_speed";
        public static string AttackSpeed = "attack_speed";
        public static string CastSpeed = "cast_speed";

        // 战斗属性
        public static string PhysicalDamage = "physical_damage";
        public static string MagicalDamage = "magical_damage";
        public static string CriticalChance = "critical_chance";
        public static string CriticalDamage = "critical_damage";
        public static string Armor = "armor";
        public static string MagicResistance = "magic_resistance";
        public static string DodgeChance = "dodge_chance";
        public static string BlockChance = "block_chance";
        public static string BlockAmount = "block_amount";

        // 生存属性
        public static string LifeRegeneration = "life_regeneration";
        public static string ManaRegeneration = "mana_regeneration";
        public static string StaminaRegeneration = "stamina_regeneration";
        public static string LifeLeech = "life_leech";
        public static string ManaLeech = "mana_leech";
        public static string StaminaLeech = "stamina_leech";

        // 元素属性
        public static string FireDamage = "fire_damage";
        public static string ColdDamage = "cold_damage";
        public static string LightningDamage = "lightning_damage";
        public static string PoisonDamage = "poison_damage";
        public static string FireResistance = "fire_resistance";
        public static string ColdResistance = "cold_resistance";
        public static string LightningResistance = "lightning_resistance";
        public static string PoisonResistance = "poison_resistance";

        // 技能属性
        public static string SkillCooldownReduction = "skill_cooldown_reduction";
        public static string AreaOfEffect = "area_of_effect";
        public static string ProjectileSpeed = "projectile_speed";
        public static string TrapThrowingSpeed = "trap_throwing_speed";
        public static string MineLayingSpeed = "mine_laying_speed";

        // 资源获取
        public static string GoldFind = "gold_find";
        public static string MagicFind = "magic_find";
        public static string ExperienceGain = "experience_gain";
        public static string FlaskChargesGained = "flask_charges_gained";
        public static string FlaskDuration = "flask_duration";

        // 特殊属性
        public static string ChanceToFreeze = "chance_to_freeze";
        public static string ChanceToShock = "chance_to_shock";
        public static string ChanceToIgnite = "chance_to_ignite";
        public static string ChanceToPoison = "chance_to_poison";
        public static string KnockbackDistance = "knockback_distance";
        public static string StunDuration = "stun_duration";
        public static string BlindDuration = "blind_duration";

        private StatDefDataLoader() 
        {
            InitializeCache();
        }

        /// <summary>
        /// 初始化缓存数据
        /// </summary>
        private void InitializeCache()
        {
            if (_isInitialized) return;

            _cachedStats = new List<StatDef>();
            _statIndex = new Dictionary<string, StatDef>();
            
            // 基础属性
            AddStatDef(_cachedStats, CurrentHealth, "当前生命值", StatCategory.Survival);
            AddStatDef(_cachedStats, MaxHealth, "最大生命值", StatCategory.Survival);
            AddStatDef(_cachedStats, CurrentMana, "当前法力值", StatCategory.Survival);
            AddStatDef(_cachedStats, MaxMana, "最大法力值", StatCategory.Survival);
            AddStatDef(_cachedStats, CurrentStamina, "当前体力值", StatCategory.Survival);
            AddStatDef(_cachedStats, MaxStamina, "最大体力值", StatCategory.Survival);
            AddStatDef(_cachedStats, MoveSpeed, "移动速度", StatCategory.Movement);
            AddStatDef(_cachedStats, AttackSpeed, "攻击速度", StatCategory.Combat);
            AddStatDef(_cachedStats, CastSpeed, "施法速度", StatCategory.Combat);

            // 战斗属性
            AddStatDef(_cachedStats, PhysicalDamage, "物理伤害", StatCategory.Combat);
            AddStatDef(_cachedStats, MagicalDamage, "魔法伤害", StatCategory.Combat);
            AddStatDef(_cachedStats, CriticalChance, "暴击率", StatCategory.Combat);
            AddStatDef(_cachedStats, CriticalDamage, "暴击伤害", StatCategory.Combat);
            AddStatDef(_cachedStats, Armor, "护甲", StatCategory.Defense);
            AddStatDef(_cachedStats, MagicResistance, "魔法抗性", StatCategory.Defense);
            AddStatDef(_cachedStats, DodgeChance, "闪避率", StatCategory.Defense);
            AddStatDef(_cachedStats, BlockChance, "格挡率", StatCategory.Defense);
            AddStatDef(_cachedStats, BlockAmount, "格挡值", StatCategory.Defense);

            // 生存属性
            AddStatDef(_cachedStats, LifeRegeneration, "生命回复", StatCategory.Survival);
            AddStatDef(_cachedStats, ManaRegeneration, "法力回复", StatCategory.Survival);
            AddStatDef(_cachedStats, StaminaRegeneration, "体力回复", StatCategory.Survival);
            AddStatDef(_cachedStats, LifeLeech, "生命偷取", StatCategory.Survival);
            AddStatDef(_cachedStats, ManaLeech, "法力偷取", StatCategory.Survival);
            AddStatDef(_cachedStats, StaminaLeech, "体力偷取", StatCategory.Survival);

            // 元素属性
            AddStatDef(_cachedStats, FireDamage, "火焰伤害", StatCategory.Elemental);
            AddStatDef(_cachedStats, ColdDamage, "冰霜伤害", StatCategory.Elemental);
            AddStatDef(_cachedStats, LightningDamage, "闪电伤害", StatCategory.Elemental);
            AddStatDef(_cachedStats, PoisonDamage, "毒素伤害", StatCategory.Elemental);
            AddStatDef(_cachedStats, FireResistance, "火焰抗性", StatCategory.Elemental);
            AddStatDef(_cachedStats, ColdResistance, "冰霜抗性", StatCategory.Elemental);
            AddStatDef(_cachedStats, LightningResistance, "闪电抗性", StatCategory.Elemental);
            AddStatDef(_cachedStats, PoisonResistance, "毒素抗性", StatCategory.Elemental);

            // 技能属性
            AddStatDef(_cachedStats, SkillCooldownReduction, "技能冷却减少", StatCategory.Skill);
            AddStatDef(_cachedStats, AreaOfEffect, "效果范围", StatCategory.Skill);
            AddStatDef(_cachedStats, ProjectileSpeed, "投射物速度", StatCategory.Skill);
            AddStatDef(_cachedStats, TrapThrowingSpeed, "陷阱投掷速度", StatCategory.Skill);
            AddStatDef(_cachedStats, MineLayingSpeed, "地雷布置速度", StatCategory.Skill);

            // 资源获取
            AddStatDef(_cachedStats, GoldFind, "金币获取", StatCategory.Wealth);
            AddStatDef(_cachedStats, MagicFind, "魔法物品获取", StatCategory.Wealth);
            AddStatDef(_cachedStats, ExperienceGain, "经验获取", StatCategory.Wealth);
            AddStatDef(_cachedStats, FlaskChargesGained, "药剂充能获取", StatCategory.Wealth);
            AddStatDef(_cachedStats, FlaskDuration, "药剂持续时间", StatCategory.Wealth);

            // 特殊属性
            AddStatDef(_cachedStats, ChanceToFreeze, "冻结几率", StatCategory.Status);
            AddStatDef(_cachedStats, ChanceToShock, "感电几率", StatCategory.Status);
            AddStatDef(_cachedStats, ChanceToIgnite, "点燃几率", StatCategory.Status);
            AddStatDef(_cachedStats, ChanceToPoison, "中毒几率", StatCategory.Status);
            AddStatDef(_cachedStats, KnockbackDistance, "击退距离", StatCategory.Status);
            AddStatDef(_cachedStats, StunDuration, "眩晕持续时间", StatCategory.Status);
            AddStatDef(_cachedStats, BlindDuration, "致盲持续时间", StatCategory.Status);

            // 构建ID索引字典
            foreach (var stat in _cachedStats)
            {
                _statIndex[stat.Id] = stat;
            }

            _isInitialized = true;
        }

        /// <summary>
        /// 获取所有StatDef列表（返回缓存的副本）
        /// </summary>
        public List<StatDef> LoadAll()
        {
            if (!_isInitialized)
            {
                InitializeCache();
            }
            // 返回副本以防止外部修改缓存数据
            return new List<StatDef>(_cachedStats);
        }

        /// <summary>
        /// 根据ID获取单个StatDef（O(1)时间复杂度）
        /// </summary>
        /// <param name="id">StatDef的ID</param>
        /// <returns>对应的StatDef对象，如果未找到则返回null</returns>
        public StatDef GetStatDefById(string id)
        {
            if (!_isInitialized)
            {
                InitializeCache();
            }
            
            if (_statIndex.TryGetValue(id, out StatDef stat))
            {
                return stat;
            }
            return null;
        }

        /// <summary>
        /// 检查指定ID的StatDef是否存在
        /// </summary>
        /// <param name="id">要检查的StatDef ID</param>
        /// <returns>存在返回true，否则返回false</returns>
        public bool ContainsStatDef(string id)
        {
            if (!_isInitialized)
            {
                InitializeCache();
            }
            return _statIndex.ContainsKey(id);
        }

        /// <summary>
        /// 获取缓存中的StatDef数量
        /// </summary>
        public int GetStatDefCount()
        {
            if (!_isInitialized)
            {
                InitializeCache();
            }
            return _cachedStats.Count;
        }

        /// <summary>
        /// 清除缓存（用于热重载等场景）
        /// </summary>
        public void ClearCache()
        {
            _cachedStats?.Clear();
            _statIndex?.Clear();
            _isInitialized = false;
        }

        private void AddStatDef(List<StatDef> list, string id, string displayName, StatCategory category)
        {
            var statDef = new StatDef();
            statDef.Id = id;
            statDef.DisplayName = displayName;
            statDef.Category = category;
            list.Add(statDef);
        }
    }
}