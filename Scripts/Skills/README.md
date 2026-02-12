# 技能系统 (Skill System)

## 系统概述

这是一个完整的技能系统，基于状态机架构设计，支持复杂的技能释放流程、冷却管理、输入处理等功能。

## 核心组件

### 1. BaseSkill (技能基类)
所有技能的基类，集成了状态机系统。

**主要特性:**
- 状态机管理（就绪、施法、激活、冷却、禁用）
- 冷却时间管理
- 施法时间支持
- 魔力消耗检查
- 事件系统支持

**使用方法:**
```csharp
public partial class MySkill : BaseSkill
{
    public override void _Ready()
    {
        SkillId = "my_skill";
        SkillName = "我的技能";
        CooldownTime = 3.0f;
        CastTime = 0.5f;
        ManaCost = 20;
        
        base._Ready();
    }

    protected override void ExecuteSkillEffect()
    {
        // 实现具体的技能效果
        Logger2.Info("MySkill: 执行技能效果");
    }
}
```

### 2. 技能状态类
各种技能状态的具体实现：

- **SkillReadyState**: 技能就绪状态
- **SkillCastingState**: 技能施法状态  
- **SkillActiveState**: 技能激活状态
- **SkillCooldownState**: 技能冷却状态
- **SkillDisabledState**: 技能禁用状态

### 3. SkillManager (技能管理器)
管理角色的所有技能，支持技能组合和冷却组功能。

**主要功能:**
- 技能添加/移除
- 技能使用管理
- 冷却组管理
- 技能状态查询

### 4. SkillInputHandler (输入处理器)
处理技能快捷键输入，将按键映射到具体技能。

**主要功能:**
- 按键绑定管理
- 输入事件处理
- 技能使用失败反馈

## 系统架构

```
SkillSystem
├── BaseSkill (技能基类)
│   ├── SkillState (状态基类)
│   │   ├── SkillReadyState
│   │   ├── SkillCastingState
│   │   ├── SkillActiveState
│   │   ├── SkillCooldownState
│   │   └── SkillDisabledState
│   └── FireballSkill (具体技能示例)
├── SkillManager (技能管理器)
├── SkillInputHandler (输入处理器)
└── SkillSystemDemo (使用示例)
```

## 状态转换流程

```
Ready → Casting → Active → Cooldown → Ready
   ↓                                    ↑
Disabled ←───────────────────────────────┘
```

## 使用示例

### 1. 在Player中集成技能系统

```csharp
public partial class Player : CombatEntity, ISkillOwner
{
    private SkillManager _skillManager;
    private SkillInputHandler _skillInputHandler;

    public override void _Ready()
    {
        base._Ready();
        
        // 初始化技能系统
        InitializeSkillSystem();
    }

    private void InitializeSkillSystem()
    {
        // 创建技能管理器
        _skillManager = new SkillManager();
        AddChild(_skillManager);
        _skillManager.Initialize(this);

        // 创建输入处理器
        _skillInputHandler = new SkillInputHandler();
        AddChild(_skillInputHandler);
        _skillInputHandler.Initialize(_skillManager);

        // 添加技能
        var fireball = new FireballSkill();
        _skillManager.AddSkill(fireball);
        
        // 绑定按键
        _skillInputHandler.BindKeyToSkill(Key.Key1, fireball.SkillId);
    }

    public Node2D GetNode2DOwner()
    {
        return this;
    }
}
```

### 2. 创建自定义技能

```csharp
public partial class HealSkill : BaseSkill
{
    [Export] public float HealAmount { get; set; } = 100.0f;

    public override void _Ready()
    {
        SkillId = "heal";
        SkillName = "治疗术";
        Description = "恢复生命值";
        CooldownTime = 5.0f;
        CastTime = 1.0f;
        ManaCost = 30;
        
        base._Ready();
    }

    protected override void ExecuteSkillEffect()
    {
        // 实现治疗逻辑
        Logger2.Info("HealSkill: 恢复 {0} 点生命值", HealAmount);
        // TODO: 实际的生命值恢复逻辑
    }
}
```

### 3. 使用冷却组

```csharp
// 将多个技能加入同一个冷却组
_skillManager.AddSkillToCooldownGroup("combat_skills", "fireball");
_skillManager.AddSkillToCooldownGroup("combat_skills", "frostbolt");
_skillManager.AddSkillToCooldownGroup("combat_skills", "lightning");

// 当使用组中任一技能时，同组其他技能也会进入冷却
```

## 事件系统

技能系统提供了丰富的事件支持：

```csharp
// 订阅技能事件
skill.OnSkillStarted += OnSkillStarted;
skill.OnSkillCompleted += OnSkillCompleted;
skill.OnCooldownStarted += OnCooldownStarted;

// 技能管理器事件
_skillManager.OnAnySkillUsed += OnAnySkillUsed;
_skillManager.OnCooldownGroupTriggered += OnCooldownGroupTriggered;
```

## 扩展性

### 1. 自定义技能状态
```csharp
public class CustomSkillState : SkillState
{
    public CustomSkillState(BaseSkill skill) : base(skill) { }

    public override void Enter()
    {
        // 自定义进入逻辑
    }

    public override void StateUpdate(double delta)
    {
        // 自定义更新逻辑
    }
}
```

### 2. 技能效果系统
可以通过继承`SkillActiveState`来实现持续性技能效果：

```csharp
public class ChannelingState : SkillActiveState
{
    protected override double GetSkillDuration()
    {
        return 3.0; // 持续3秒
    }

    protected override void UpdateSkillEffect(double delta)
    {
        // 每帧执行的效果
        Logger2.Debug("Channeling: 持续施法中...");
    }
}
```

## 最佳实践

1. **技能平衡**: 合理设置冷却时间和施法时间
2. **用户体验**: 提供清晰的技能状态反馈
3. **性能优化**: 避免在技能更新中进行heavy计算
4. **错误处理**: 妥善处理技能使用失败的情况
5. **配置化**: 将技能参数外部化，便于调整平衡性

## 待办事项

- [ ] 添加技能等级系统
- [ ] 实现技能学习/遗忘功能
- [ ] 添加技能特效系统集成
- [ ] 实现技能连击系统
- [ ] 添加技能AI使用逻辑
- [ ] 实现技能数据持久化