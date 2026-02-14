using Godot;
using System;

namespace Projectiles
{
    /// <summary>
    /// 投射物数据定义类
    /// 定义投射物的各项属性和行为配置
    /// </summary>
    [GlobalClass]
    public partial class ProjectileData : Resource
    {
        #region 基础信息

        [ExportGroup("🎯 基础信息")]
        
        /// <summary>
        /// 投射物唯一标识符 - 用于在游戏中唯一识别此投射物类型的字符串ID
        /// </summary>
        [Export] 
        public string ProjectileId { get; set; } = "";

        /// <summary>
        /// 投射物显示名称 - 在UI界面中向玩家显示的友好名称
        /// </summary>
        [Export] 
        public string Name { get; set; } = "";

        /// <summary>
        /// 投射物详细描述 - 对投射物功能和特性的文字说明
        /// </summary>
        [Export(PropertyHint.MultilineText)] 
        public string Description { get; set; } = "";

        /// <summary>
        /// 投射物预制体资源路径 - 指向包含投射物视觉表现的场景文件路径
        /// </summary>
        [Export(PropertyHint.File, "*.tscn,*.scn")] 
        public string PrefabPath { get; set; } = "";

        /// <summary>
        /// 投射物图标资源路径 - 用于UI界面显示的小图标文件路径
        /// </summary>
        [Export(PropertyHint.File, "*.png,*.jpg,*.svg")] 
        public string IconPath { get; set; } = "";

        [ExportSubgroup("游戏副本属性")]
        
        /// <summary>
        /// 投射物类型分类 - 决定投射物的基本行为模式和功能特性
        /// </summary>
        [Export(PropertyHint.Enum, "Normal:普通投射物,Homing:追踪投射物,Boomerang:回旋镖,Mine:地雷,Beam:激光束")] 
        public ProjectileType Type { get; set; } = ProjectileType.Normal;

        /// <summary>
        /// 投射物稀有度等级 - 影响投射物的获取难度和属性强度
        /// </summary>
        [Export(PropertyHint.Enum, "Common:普通,Rare:稀有,Epic:史诗,Legendary:传说")] 
        public ProjectileRarity Rarity { get; set; } = ProjectileRarity.Common;

        #endregion

        #region 物理属性

        [ExportGroup("⚙️ 物理属性")]

        [ExportSubgroup("运动参数")]
        
        /// <summary>
        /// 投射物初始发射速度 - 投射物被发射时的起始移动速度，单位：像素/秒
        /// </summary>
        [Export] 
        public float InitialSpeed { get; set; } = 300f;

        /// <summary>
        /// 投射物最大移动速度 - 投射物能够达到的最高速度限制，单位：像素/秒
        /// </summary>
        [Export] 
        public float MaxSpeed { get; set; } = 500f;

        /// <summary>
        /// 投射物加速度 - 投射物在飞行过程中速度的变化率，正值加速负值减速，单位：像素/秒²
        /// </summary>
        [Export] 
        public float Acceleration { get; set; } = 0f;

        [ExportSubgroup("环境影响")]
        
        /// <summary>
        /// 重力影响系数 - 控制投射物受重力影响的程度，1.0为标准重力，0为无重力
        /// </summary>
        [Export] 
        public float GravityScale { get; set; } = 1.0f;

        /// <summary>
        /// 投射物质量 - 影响投射物的物理行为和与其他物体的交互
        /// </summary>
        [Export] 
        public float Mass { get; set; } = 1.0f;

        /// <summary>
        /// 空气阻力系数 - 控制投射物在空气中受到的阻力大小，影响飞行距离
        /// </summary>
        [Export] 
        public float DragCoefficient { get; set; } = 0.1f;

        #endregion

        #region 生命周期

        [ExportGroup("⏱️ 生命周期管理")]

        [ExportSubgroup("存活控制")]
        
        /// <summary>
        /// 投射物最大存活时间 - 投射物从发射到自动销毁的最大存在时间，单位：秒
        /// </summary>
        [Export] 
        public float MaxLifetime { get; set; } = 5.0f;

        [ExportSubgroup("穿透设置")]
        
        /// <summary>
        /// 是否允许穿透目标 - 启用后投射物可以穿过多个目标而不立即销毁
        /// </summary>
        [Export] 
        public bool IsPenetrating { get; set; } = false;

        /// <summary>
        /// 最大穿透目标数量 - 当启用穿透时，投射物最多可以穿透的目标数目
        /// </summary>
        [Export] 
        public int MaxPenetrationCount { get; set; } = 1;

        [ExportSubgroup("环境交互")]
        
        /// <summary>
        /// 是否受环境因素影响 - 控制投射物是否会受到风力、重力等环境因素影响
        /// </summary>
        [Export] 
        public bool AffectedByEnvironment { get; set; } = true;

        /// <summary>
        /// 是否跟随发射者移动 - 启用后投射物会相对于发射者的移动而调整位置
        /// </summary>
        [Export] 
        public bool FollowCaster { get; set; } = false;

        #endregion

        #region 轨迹与瞄准

        [ExportGroup("🎯 轨迹与瞄准系统")]

        [ExportSubgroup("飞行轨迹")]
        
        /// <summary>
        /// 投射物飞行轨迹类型 - 决定投射物的飞行路径模式
        /// </summary>
        [Export(PropertyHint.Enum, "Linear:直线飞行,Parabolic:抛物线飞行,Sine:正弦波飞行,Spiral:螺旋飞行,Homing:追踪目标")] 
        public TrajectoryType Trajectory { get; set; } = TrajectoryType.Linear;

        /// <summary>
        /// 抛物线轨迹弧高 - 当使用抛物线轨迹时，控制抛物线的最高点高度
        /// </summary>
        [Export] 
        public float ArcHeight { get; set; } = 0f;

        [ExportSubgroup("瞄准精度")]
        
        /// <summary>
        /// 是否启用自动瞄准 - 启用后投射物会自动调整方向朝向最近的有效目标
        /// </summary>
        [Export] 
        public bool AutoAim { get; set; } = false;

        /// <summary>
        /// 瞄准误差角度 - 自动瞄准时的随机偏差角度，增加游戏的不确定性和挑战性
        /// </summary>
        [Export] 
        public float AimErrorAngle { get; set; } = 0f;

        /// <summary>
        /// 是否预测目标位置 - 启用后投射物会计算目标的移动趋势并提前瞄准
        /// </summary>
        [Export] 
        public bool PredictTargetPosition { get; set; } = false;

        #endregion

        #region 发射模式

        [ExportGroup("🔫 发射模式配置")]

        [ExportSubgroup("发射基础设置")]
        
        /// <summary>
        /// 投射物发射模式 - 决定一次技能释放时投射物的发射方式
        /// </summary>
        [Export(PropertyHint.Enum, "Single:单发射击,Spread:散射模式,Circle:圆形发射,Cone:锥形发射,Volley:齐射模式,Rain:雨点式发射")] 
        public SpawnPattern Pattern { get; set; } = SpawnPattern.Single;

        /// <summary>
        /// 单次发射的投射物数量 - 一次技能释放产生的投射物总数
        /// </summary>
        [Export] 
        public int SpawnCount { get; set; } = 1;

        /// <summary>
        /// 投射物发射间隔 - 多个投射物之间的时间间隔，用于控制连续发射的节奏
        /// </summary>
        [Export] 
        public float SpawnInterval { get; set; } = 0.1f;

        [ExportSubgroup("散布控制")]
        
        /// <summary>
        /// 发射角度散布范围 - 控制多个投射物发射时的角度分散程度
        /// </summary>
        [Export] 
        public float SpreadAngle { get; set; } = 0f;

        /// <summary>
        /// 圆形发射半径 - 当使用圆形发射模式时，控制投射物的分布半径
        /// </summary>
        [Export] 
        public float CircleRadius { get; set; } = 50f;

        #endregion

        #region 碰撞与伤害

        [ExportGroup("💥 碰撞与伤害系统")]

        [ExportSubgroup("碰撞检测")]
        
        /// <summary>
        /// 投射物碰撞形状 - 决定投射物用于碰撞检测的几何形状
        /// </summary>
        [Export(PropertyHint.Enum, "Circle:圆形碰撞,Rectangle:矩形碰撞,Polygon:多边形碰撞")] 
        public CollisionShape Shape { get; set; } = CollisionShape.Circle;

        /// <summary>
        /// 碰撞检测半径 - 投射物碰撞体积的大小，影响命中判定的精确度
        /// </summary>
        [Export] 
        public float CollisionRadius { get; set; } = 10f;

        [ExportSubgroup("伤害计算")]
        
        /// <summary>
        /// 投射物基础伤害值 - 投射物命中目标时造成的初始伤害点数
        /// </summary>
        [Export] 
        public float BaseDamage { get; set; } = 10f;

        /// <summary>
        /// 伤害类型分类 - 决定伤害的性质和目标的抗性计算方式
        /// </summary>
        [Export(PropertyHint.Enum, "Physical:物理伤害,Magical:魔法伤害,True:真实伤害,Poison:毒素伤害,Fire:火焰伤害,Ice:冰霜伤害,Lightning:雷电伤害")] 
        public DamageType DamageType { get; set; } = DamageType.Physical;

        [ExportSubgroup("范围伤害")]
        
        /// <summary>
        /// 是否造成范围伤害 - 启用后投射物命中时会对周围目标造成溅射伤害
        /// </summary>
        [Export] 
        public bool AoEDamage { get; set; } = false;

        /// <summary>
        /// 范围伤害半径 - 范围伤害影响的圆形区域半径
        /// </summary>
        [Export] 
        public float AoERadius { get; set; } = 0f;

        #endregion

        #region 特殊效果

        [ExportGroup("✨ 特殊效果系统")]

        [ExportSubgroup("爆炸效果")]
        
        /// <summary>
        /// 是否具有爆炸效果 - 启用后投射物会在特定条件下产生爆炸
        /// </summary>
        [Export] 
        public bool HasExplosion { get; set; } = false;

        /// <summary>
        /// 爆炸延迟时间 - 从投射物生成到爆炸触发的时间间隔
        /// </summary>
        [Export] 
        public float ExplosionDelay { get; set; } = 0f;

        /// <summary>
        /// 爆炸影响半径 - 爆炸伤害作用的圆形范围
        /// </summary>
        [Export] 
        public float ExplosionRadius { get; set; } = 50f;

        /// <summary>
        /// 爆炸基础伤害 - 爆炸对范围内目标造成的伤害值
        /// </summary>
        [Export] 
        public float ExplosionDamage { get; set; } = 20f;

        [ExportSubgroup("控制效果")]
        
        /// <summary>
        /// 击退力度 - 投射物命中目标时施加的推力强度
        /// </summary>
        [Export] 
        public float KnockbackForce { get; set; } = 100f;

        [ExportSubgroup("状态效果")]
        
        /// <summary>
        /// 状态效果列表 - 投射物命中目标时可能附加的各种状态效果
        /// </summary>
        [Export] 
        public Godot.Collections.Array<StatusEffectData> StatusEffects { get; set; } = new();

        #endregion

        #region 视觉效果

        [ExportGroup("🎨 视觉与音效系统")]

        [ExportSubgroup("视觉特效")]
        
        /// <summary>
        /// 飞行过程特效路径 - 投射物在飞行过程中显示的粒子特效资源路径
        /// </summary>
        [Export(PropertyHint.File, "*.tscn,*.scn")] 
        public string FlightEffectPath { get; set; } = "";

        /// <summary>
        /// 命中爆炸特效路径 - 投射物命中目标时播放的特效资源路径
        /// </summary>
        [Export(PropertyHint.File, "*.tscn,*.scn")] 
        public string ImpactEffectPath { get; set; } = "";

        /// <summary>
        /// 轨迹拖尾特效路径 - 投射物飞行时留下的轨迹特效资源路径
        /// </summary>
        [Export(PropertyHint.File, "*.tscn,*.scn")] 
        public string TrailEffectPath { get; set; } = "";

        [ExportSubgroup("音频效果")]
        
        /// <summary>
        /// 飞行音效路径 - 投射物飞行过程中播放的音频文件路径
        /// </summary>
        [Export(PropertyHint.File, "*.wav,*.ogg,*.mp3")] 
        public string FlightSoundPath { get; set; } = "";

        /// <summary>
        /// 命中音效路径 - 投射物命中目标时播放的音频文件路径
        /// </summary>
        [Export(PropertyHint.File, "*.wav,*.ogg,*.mp3")] 
        public string ImpactSoundPath { get; set; } = "";

        #endregion

        #region 辅助方法

        /// <summary>
        /// 计算实际伤害（考虑等级加成）
        /// 根据投射物等级计算最终伤害值，每级增加10%伤害
        /// </summary>
        /// <param name="level">投射物等级</param>
        /// <returns>考虑等级加成后的实际伤害值</returns>
        public float GetActualDamage(int level = 1)
        {
            // 简单的线性成长公式，可根据需要调整
            return BaseDamage * (1 + (level - 1) * 0.1f);
        }

        /// <summary>
        /// 计算实际爆炸伤害
        /// 根据投射物等级计算爆炸伤害值，每级增加10%伤害
        /// </summary>
        /// <param name="level">投射物等级</param>
        /// <returns>考虑等级加成后的实际爆炸伤害值</returns>
        public float GetActualExplosionDamage(int level = 1)
        {
            return ExplosionDamage * (1 + (level - 1) * 0.1f);
        }

        /// <summary>
        /// 获取随机散布角度（弧度）
        /// 生成符合设定散布角度的随机偏移角度
        /// </summary>
        /// <returns>随机散布角度（弧度）</returns>
        public float GetRandomSpread()
        {
            if (SpreadAngle <= 0) return 0f;
            
            var random = new Random();
            var spreadDegrees = (float)(random.NextDouble() * SpreadAngle - SpreadAngle / 2);
            return Mathf.DegToRad(spreadDegrees);
        }

        /// <summary>
        /// 验证数据完整性
        /// 检查投射物数据是否包含必要的基本信息
        /// </summary>
        /// <returns>数据是否有效的布尔值</returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(ProjectileId) && 
                   !string.IsNullOrEmpty(Name) && 
                   !string.IsNullOrEmpty(PrefabPath) &&
                   InitialSpeed > 0 && 
                   MaxLifetime > 0;
        }

        #endregion
    }

    #region 枚举定义

    /// <summary>
    /// 投射物类型枚举
    /// </summary>
    public enum ProjectileType
    {
        Normal,         // 普通投射物
        Homing,         // 追踪投射物
        Boomerang,      // 回旋镖
        Mine,           // 地雷
        Beam            // 激光束
    }

    /// <summary>
    /// 投射物稀有度枚举
    /// </summary>
    public enum ProjectileRarity
    {
        Common,         // 普通
        Rare,           // 稀有
        Epic,           // 史诗
        Legendary       // 传说
    }

    /// <summary>
    /// 轨迹类型枚举
    /// </summary>
    public enum TrajectoryType
    {
        Linear,         // 直线
        Parabolic,      // 抛物线
        Sine,           // 正弦波
        Spiral,         // 螺旋
        Homing          // 追踪
    }

    /// <summary>
    /// 发射模式枚举
    /// </summary>
    public enum SpawnPattern
    {
        Single,         // 单发
        Spread,         // 散射
        Circle,         // 圆形
        Cone,           // 锥形
        Volley,         // 齐射
        Rain            // 雨点式
    }

    /// <summary>
    /// 碰撞形状枚举
    /// </summary>
    public enum CollisionShape
    {
        Circle,         // 圆形
        Rectangle,      // 矩形
        Polygon         // 多边形
    }

    /// <summary>
    /// 伤害类型枚举
    /// </summary>
    public enum DamageType
    {
        Physical,       // 物理伤害
        Magical,        // 魔法伤害
        True,           // 真实伤害
        Poison,         // 毒素伤害
        Fire,           // 火焰伤害
        Ice,            // 冰霜伤害
        Lightning       // 雷电伤害
    }

    /// <summary>
    /// 状态效果数据（与技能系统共享）
    /// 定义投射物命中目标时可能附加的各种状态效果
    /// </summary>
    [GlobalClass]
    public partial class StatusEffectData : Resource
    {
        [ExportGroup("🏷️ 状态效果基础信息")]
        
        /// <summary>
        /// 状态效果唯一标识符 - 用于在程序中唯一识别此状态效果的字符串ID
        /// </summary>
        [Export] 
        public string EffectId { get; set; } = "";

        /// <summary>
        /// 状态效果显示名称 - 在UI界面中向玩家显示的友好名称
        /// </summary>
        [Export] 
        public string EffectName { get; set; } = "";

        [ExportGroup("🔧 效果配置")]
        
        /// <summary>
        /// 状态效果类型 - 决定效果的基本性质和作用机制
        /// </summary>
        [Export(PropertyHint.Enum, "Buff:增益效果,Debuff:减益效果,DamageOverTime:持续伤害,HealOverTime:持续治疗,StatModifier:属性修改,Immunity:免疫效果")] 
        public StatusEffectType EffectType { get; set; }

        /// <summary>
        /// 状态效果持续时间 - 效果在目标身上维持的时间长度，0表示永久效果
        /// </summary>
        [Export] 
        public float Duration { get; set; } = 0f;

        /// <summary>
        /// 状态效果强度 - 控制效果的强度等级，具体含义根据效果类型而定
        /// </summary>
        [Export] 
        public float Power { get; set; } = 0f;

        /// <summary>
        /// 是否为负面效果 - 标识此效果是否对目标产生负面影响
        /// </summary>
        [Export] 
        public bool IsDebuff { get; set; } = false;
    }

    /// <summary>
    /// 状态效果类型枚举
    /// </summary>
    public enum StatusEffectType
    {
        Buff,               // 增益效果
        Debuff,             // 减益效果
        DamageOverTime,     // 持续伤害
        HealOverTime,       // 持续治疗
        StatModifier,       // 属性修改
        Immunity            // 免疫效果
    }

    #endregion
}