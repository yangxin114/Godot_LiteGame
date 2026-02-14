using Godot;
using System.Threading.Tasks;

namespace Skills
{
    /// <summary>
    /// 单个技能接口
    /// 定义一个具体技能实例所需实现的基本功能和事件
    /// 
    /// 设计理念：
    /// - 专注于单个技能的核心功能实现
    /// - 与技能使用者、技能管理器等其他组件分离职责
    /// - 提供完整的技能生命周期管理能力
    /// - 通过事件系统实现与其他系统的解耦
    /// 
    /// 核心职责：
    /// 1. 技能施放和状态管理
    /// 2. 资源消耗和验证
    /// 3. 效果应用和管理
    /// 4. 生命周期事件通知
    /// </summary>
    public partial interface ISkill
    {

        #region 技能施放操作

        /// <summary>
        /// 施放技能的核心方法
        /// 执行完整的技能施放流程
        /// </summary>
        /// <param name="target">技能施放的目标实体（可选）</param>
        /// <returns>施放是否成功</returns>
        Task<bool> CastSkill(ISkillTarget target = null);

        /// <summary>
        /// 检查技能是否可以施放
        /// 验证所有施放前置条件是否满足
        /// </summary>
        /// <returns>是否可以施放</returns>
        bool CanCastSkill();

        /// <summary>
        /// 中断正在进行的技能施法
        /// 通常由外部事件触发（如受到攻击、移动等）
        /// </summary>
        void InterruptSkill();

        /// <summary>
        /// 取消技能（根据当前状态执行不同的取消逻辑）
        /// 处理施法中断或效果取消等不同场景
        /// </summary>
        void CancelSkill();

        #endregion

        #region 状态查询

        /// <summary>
        /// 获取技能当前状态
        /// </summary>
        /// <returns>当前技能状态枚举值</returns>
        SkillState GetSkillState();

        /// <summary>
        /// 检查技能是否处于冷却状态
        /// </summary>
        /// <returns>是否在冷却中</returns>
        bool IsSkillOnCooldown();

        /// <summary>
        /// 获取技能剩余冷却时间
        /// </summary>
        /// <returns>剩余冷却时间（秒），如果不在冷却中则返回0</returns>
        float GetSkillCooldownRemaining();

        /// <summary>
        /// 检查是否正在施法过程中
        /// </summary>
        /// <returns>是否正在施法</returns>
        bool IsCastingSkill();

        #endregion

        #region 资源管理

        /// <summary>
        /// 检查是否有足够的资源施放技能
        /// 验证所有类型的资源消耗需求
        /// </summary>
        /// <returns>资源是否充足</returns>
        bool HasEnoughResources();

        /// <summary>
        /// 获取技能的资源消耗详情
        /// </summary>
        /// <returns>包含资源类型、数量和是否可负担的信息</returns>
        SkillResourceCost GetSkillResourceCost();

        /// <summary>
        /// 检查特定类型资源是否充足
        /// </summary>
        /// <param name="resourceType">资源类型</param>
        /// <param name="amount">需要的数量</param>
        /// <returns>资源是否充足</returns>
        bool HasResource(ResourceType resourceType, float amount);

        /// <summary>
        /// 消耗指定类型的资源
        /// </summary>
        /// <param name="resourceType">资源类型</param>
        /// <param name="amount">消耗数量</param>
        /// <returns>消耗是否成功</returns>
        bool ConsumeResource(ResourceType resourceType, float amount);

        #endregion

        #region 技能管理

        /// <summary>
        /// 升级技能
        /// 增加技能等级并更新相关属性
        /// </summary>
        /// <returns>升级是否成功</returns>
        bool UpgradeSkill();

        #endregion

        #region 效果处理

        /// <summary>
        /// 应用技能效果
        /// 处理效果的添加、计时和事件触发
        /// </summary>
        /// <param name="effect">要应用的技能效果数据</param>
        void ApplySkillEffect(SkillEffect effect);

        /// <summary>
        /// 移除指定的技能效果
        /// 清理效果相关的计时器和数据
        /// </summary>
        /// <param name="effectId">要移除的效果ID</param>
        void RemoveSkillEffect(string effectId);

        /// <summary>
        /// 检查是否拥有指定效果
        /// </summary>
        /// <param name="effectId">效果ID</param>
        /// <returns>是否拥有该效果</returns>
        bool HasEffect(string effectId);

        /// <summary>
        /// 获取当前激活的所有效果
        /// </summary>
        /// <returns>激活效果数组</returns>
        ActiveEffect[] GetActiveEffects();

        #endregion

        #region 事件通知

        /// <summary>
        /// 技能开始施法时触发
        /// </summary>
        event System.Action<SkillEventData> OnSkillCastStart;

        /// <summary>
        /// 技能施法完成时触发
        /// </summary>
        event System.Action<SkillEventData> OnSkillCastComplete;

        /// <summary>
        /// 技能激活时触发
        /// </summary>
        event System.Action<SkillEventData> OnSkillActivated;

        /// <summary>
        /// 技能结束时触发
        /// </summary>
        event System.Action<SkillEventData> OnSkillEnded;

        /// <summary>
        /// 技能被中断时触发
        /// </summary>
        event System.Action<SkillInterruptEventData> OnSkillInterrupted;

        /// <summary>
        /// 技能进入冷却时触发
        /// </summary>
        event System.Action<SkillCooldownEventData> OnSkillCooldownStart;

        /// <summary>
        /// 技能冷却结束时触发
        /// </summary>
        event System.Action<SkillCooldownEventData> OnSkillCooldownEnd;

        /// <summary>
        /// 资源不足时触发
        /// </summary>
        event System.Action<ResourceInsufficientEventData> OnResourceInsufficient;

        /// <summary>
        /// 技能效果应用时触发
        /// </summary>
        event System.Action<SkillEffectEventData> OnSkillEffectApplied;

        /// <summary>
        /// 技能效果移除时触发
        /// </summary>
        event System.Action<SkillEffectEventData> OnSkillEffectRemoved;

        #endregion
    }

    #region 技能相关数据结构

    /// <summary>
    /// 技能状态枚举
    /// 定义技能在其生命周期中可能处于的各种状态
    /// </summary>
    public enum SkillState
    {
        /// <summary>
        /// 就绪状态 - 技能可以被施放
        /// </summary>
        Ready,

        /// <summary>
        /// 施法状态 - 技能正在施放过程中
        /// </summary>
        Casting,

        /// <summary>
        /// 激活状态 - 技能效果已激活
        /// </summary>
        Active,

        /// <summary>
        /// 冷却状态 - 技能正在冷却中
        /// </summary>
        Cooldown,

        /// <summary>
        /// 禁用状态 - 技能被暂时禁用
        /// </summary>
        Disabled,

        /// <summary>
        /// 锁定状态 - 技能不能使用（通常因为未解锁）
        /// </summary>
        Locked
    }

    /// <summary>
    /// 资源类型枚举
    /// 定义游戏中可用的各种资源类型
    /// </summary>
    public enum ResourceType
    {
        /// <summary>
        /// 法力值 - 用于施放魔法技能
        /// </summary>
        Mana,

        /// <summary>
        /// 体力值 - 用于物理攻击和移动
        /// </summary>
        Stamina,

        /// <summary>
        /// 怒气值 - 通过战斗积累，用于强力技能
        /// </summary>
        Rage,

        /// <summary>
        /// 生命值 - 角色的生命储备
        /// </summary>
        Health,

        /// <summary>
        /// 能量值 - 特殊职业使用的资源
        /// </summary>
        Energy,

        /// <summary>
        /// 专注值 - 用于需要集中注意力的技能
        /// </summary>
        Focus
    }

    /// <summary>
    /// 技能资源消耗信息
    /// 描述施放技能所需的资源详情
    /// </summary>
    public struct SkillResourceCost
    {
        /// <summary>
        /// 资源类型
        /// </summary>
        public ResourceType Type;

        /// <summary>
        /// 所需资源数量
        /// </summary>
        public float Amount;

        /// <summary>
        /// 是否能够负担此次消耗
        /// </summary>
        public bool CanAfford;
    }

    /// <summary>
    /// 技能效果数据
    /// 定义技能产生的具体效果
    /// </summary>
    public partial class SkillEffect : Resource
    {
        [Export] public string EffectId { get; set; } = "";
        [Export] public string Name { get; set; } = "";
        [Export] public EffectType Type { get; set; }
        [Export] public float Value { get; set; }
        [Export] public float Duration { get; set; }
        [Export] public bool IsDebuff { get; set; }
        [Export] public string[] Tags { get; set; } = new string[0];
    }

    /// <summary>
    /// 效果类型枚举
    /// 定义技能效果的各种类型
    /// </summary>
    public enum EffectType
    {
        /// <summary>
        /// 直接伤害 - 立即造成伤害
        /// </summary>
        Damage,

        /// <summary>
        /// 治疗效果 - 恢复生命值
        /// </summary>
        Heal,

        /// <summary>
        /// 增益效果 - 提升属性或能力
        /// </summary>
        Buff,

        /// <summary>
        /// 减益效果 - 降低属性或造成持续伤害
        /// </summary>
        Debuff,

        /// <summary>
        /// 属性修饰 - 临时改变角色属性
        /// </summary>
        StatModifier,

        /// <summary>
        /// 控制效果 - 限制角色行动
        /// </summary>
        CrowdControl,

        /// <summary>
        /// 召唤效果 - 召唤单位或物体
        /// </summary>
        Summon
    }

    /// <summary>
    /// 激活中的效果实例
    /// 表示当前正在生效的技能效果
    /// </summary>
    public class ActiveEffect
    {
        /// <summary>
        /// 效果唯一标识符
        /// </summary>
        public string EffectId { get; set; }

        /// <summary>
        /// 来源技能ID
        /// </summary>
        public string SourceSkillId { get; set; }

        /// <summary>
        /// 效果类型
        /// </summary>
        public EffectType Type { get; set; }

        /// <summary>
        /// 效果数值
        /// </summary>
        public float Value { get; set; }

        /// <summary>
        /// 剩余持续时间（秒）
        /// </summary>
        public float RemainingDuration { get; set; }

        /// <summary>
        /// 是否为减益效果
        /// </summary>
        public bool IsDebuff { get; set; }
    }

    #endregion

    #region 技能事件数据结构

    /// <summary>
    /// 技能事件基础数据
    /// 包含技能事件的通用信息
    /// </summary>
    public class SkillEventData
    {
        /// <summary>
        /// 时间戳（秒）
        /// </summary>
        public float Timestamp { get; set; }
    }

    /// <summary>
    /// 技能中断事件数据
    /// 包含技能被中断时的详细信息
    /// </summary>
    public class SkillInterruptEventData : SkillEventData
    {
        /// <summary>
        /// 中断来源
        /// </summary>
        public string InterruptSource { get; set; }

        /// <summary>
        /// 中断原因
        /// </summary>
        public InterruptReason Reason { get; set; }
    }

    /// <summary>
    /// 中断原因枚举
    /// 定义技能被中断的各种可能原因
    /// </summary>
    public enum InterruptReason
    {
        /// <summary>
        /// 受到伤害 - 因受到攻击而中断
        /// </summary>
        Damage,

        /// <summary>
        /// 眩晕 - 因眩晕状态而中断
        /// </summary>
        Stun,

        /// <summary>
        /// 沉默 - 因沉默状态而中断
        /// </summary>
        Silence,

        /// <summary>
        /// 死亡 - 因角色死亡而中断
        /// </summary>
        Death,

        /// <summary>
        /// 手动取消 - 由玩家主动取消
        /// </summary>
        Manual,

        /// <summary>
        /// 超出范围 - 因目标超出施法范围而中断
        /// </summary>
        OutOfRange
    }

    /// <summary>
    /// 技能冷却事件数据
    /// 包含技能冷却相关的信息
    /// </summary>
    public class SkillCooldownEventData : SkillEventData
    {
        /// <summary>
        /// 冷却总时长（秒）
        /// </summary>
        public float CooldownDuration { get; set; }

        /// <summary>
        /// 剩余时间（秒）
        /// </summary>
        public float RemainingTime { get; set; }
    }

    /// <summary>
    /// 资源不足事件数据
    /// 当资源不足以施放技能时触发
    /// </summary>
    public class ResourceInsufficientEventData : SkillEventData
    {
        /// <summary>
        /// 资源类型
        /// </summary>
        public ResourceType ResourceType { get; set; }

        /// <summary>
        /// 所需资源数量
        /// </summary>
        public float RequiredAmount { get; set; }

        /// <summary>
        /// 当前拥有的资源数量
        /// </summary>
        public float CurrentAmount { get; set; }
    }

    /// <summary>
    /// 技能效果事件数据
    /// 包含技能效果应用或移除时的信息
    /// </summary>
    public class SkillEffectEventData : SkillEventData
    {
        /// <summary>
        /// 效果ID
        /// </summary>
        public string EffectId { get; set; }

        /// <summary>
        /// 效果类型
        /// </summary>
        public EffectType EffectType { get; set; }

        /// <summary>
        /// 效果数值
        /// </summary>
        public float Value { get; set; }

        /// <summary>
        /// 效果持续时间
        /// </summary>
        public float Duration { get; set; }
    }

    #endregion
}