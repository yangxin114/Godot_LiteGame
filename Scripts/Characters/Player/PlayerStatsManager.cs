using Godot;
using System;
using Numerical;
using Logs;

namespace Characters
{
    /// <summary>
    /// Player属性管理器
    /// 专门负责Player的数值系统初始化、管理和业务逻辑处理
    /// </summary>
    public partial class PlayerStatsManager : Node
    {
        private Player _player;
        private bool _isInitialized = false;

        public StatContainer statContainer = new ();

        /// <summary>
        /// 初始化属性管理器
        /// </summary>
        public void Initialize(Player player)
        {
            if (_isInitialized)
            {
                Logger2.Warn("PlayerStatsManager: 已经初始化过了");
                return;
            }

            _player = player ?? throw new ArgumentNullException(nameof(player));
            _isInitialized = true;
            
            Logger2.Info("PlayerStatsManager: 初始化完成");
        }

        /// <summary>
        /// 初始化玩家属性系统
        /// </summary>
        public void InitStats()
        {
            if (!_isInitialized)
            {
                Logger2.Error("PlayerStatsManager: 请先调用Initialize方法");
                return;
            }

            Logger2.Info("PlayerStatsManager.InitStats: 开始初始化玩家属性系统");
            
            try
            {
                // 确保Stats容器已正确关联
                statContainer.Owner = _player;
                
                // 从PlayerData加载基础属性
                LoadBaseStatsFromPlayerData();
                
                // 设置属性变化监听器
                SetupStatListeners();
                
                Logger2.Info("PlayerStatsManager.InitStats: 玩家属性系统初始化完成");
            }
            catch (Exception ex)
            {
                Logger2.Error("PlayerStatsManager.InitStats: 初始化过程中发生错误 - {0}", ex.Message);
                ErrorReporter.Report(ex, "Player属性初始化");
            }
        }
        
        /// <summary>
        /// 从PlayerData加载基础属性值
        /// </summary>
        private void LoadBaseStatsFromPlayerData()
        {
            // 获取必要的属性定义
            var currentHealthDef = StatDefDataLoader.Instance.GetStatDefById(StatDefDataLoader.CurrentHealth);
            var maxHealthDef = StatDefDataLoader.Instance.GetStatDefById(StatDefDataLoader.MaxHealth);
            var moveSpeedDef = StatDefDataLoader.Instance.GetStatDefById(StatDefDataLoader.MoveSpeed);
            
            // 验证属性定义是否存在
            if (currentHealthDef == null || maxHealthDef == null || moveSpeedDef == null)
            {
                Logger2.Error("PlayerStatsManager.LoadBaseStatsFromPlayerData: 无法找到必要的属性定义");
                return;
            }
            
            // 确保属性实例存在
            _player.GetStatContainer().EnsureStat(currentHealthDef);
            _player.GetStatContainer().EnsureStat(maxHealthDef);
            _player.GetStatContainer().EnsureStat(moveSpeedDef);
            
            // 从PlayerData设置初始值（如果有配置的话）
            if (_player.PlayerData != null)
            {
                // 设置生命值
                if (_player.PlayerData.MaxHealth > 0)
                {
                    _player.GetStatContainer().SetBase(maxHealthDef, _player.PlayerData.MaxHealth);
                    _player.GetStatContainer().SetBase(currentHealthDef, 
                        _player.PlayerData.CurrentHealth > 0 ? _player.PlayerData.CurrentHealth : _player.PlayerData.MaxHealth);
                }
                
                // 设置移动速度
                if (_player.PlayerData.MoveSpeed > 0)
                {
                    _player.GetStatContainer().SetBase(moveSpeedDef, _player.PlayerData.MoveSpeed);
                }
                
                Logger2.Info("PlayerStatsManager.LoadBaseStatsFromPlayerData: 从PlayerData加载配置 - Health:{0}/{1}, Speed:{2}", 
                    _player.PlayerData.CurrentHealth, _player.PlayerData.MaxHealth, _player.PlayerData.MoveSpeed);
            }
            else
            {
                // 使用合理的默认值而不是StatDef的默认值
                const float DEFAULT_MAX_HEALTH = 100f;
                const float DEFAULT_MOVE_SPEED = 100f;
                
                _player.GetStatContainer().SetBase(maxHealthDef, DEFAULT_MAX_HEALTH);
                _player.GetStatContainer().SetBase(currentHealthDef, DEFAULT_MAX_HEALTH); // 当前生命等于最大生命
                _player.GetStatContainer().SetBase(moveSpeedDef, DEFAULT_MOVE_SPEED);
                
                Logger2.Info("PlayerStatsManager.LoadBaseStatsFromPlayerData: 使用默认属性值初始化 - Health:100/100, Speed:200");
            }
        }
        
        /// <summary>
        /// 设置属性变化监听器
        /// </summary>
        private void SetupStatListeners()
        {
            // 监听生命值变化
            _player.GetStatContainer().ValueChanged += OnStatValueChanged;
            
            // 监听修饰器变化
            _player.GetStatContainer().ModifierAdded += OnModifierAdded;
            _player.GetStatContainer().ModifierRemoved += OnModifierRemoved;
            
            Logger2.Debug("PlayerStatsManager.SetupStatListeners: 属性监听器设置完成");
        }
        
        /// <summary>
        /// 属性值变化回调
        /// </summary>
        private void OnStatValueChanged(StatDef def, float oldValue, float newValue)
        {
            Logger2.Debug("PlayerStatsManager.OnStatValueChanged: {0} 从 {1} 变为 {2}", def.Id, oldValue, newValue);
            
            // 特定属性的业务逻辑处理
            if (def.Id == StatDefDataLoader.CurrentHealth)
            {
                HandleHealthChange(oldValue, newValue);
            }
            else if (def.Id == StatDefDataLoader.MaxHealth)
            {
                HandleMaxHealthChange(oldValue, newValue);
            }
        }
        
        /// <summary>
        /// 处理生命值变化
        /// </summary>
        private void HandleHealthChange(float oldValue, float newValue)
        {
            // 检查死亡条件
            if (newValue <= 0)
            {
                Logger2.Info("PlayerStatsManager.HandleHealthChange: 玩家死亡");
                _player.SetState("Dead");
            }
            // TODO: 更新UI血条等
        }
        
        /// <summary>
        /// 处理最大生命值变化
        /// </summary>
        private void HandleMaxHealthChange(float oldValue, float newValue)
        {
            // 确保当前生命值不超过最大生命值
            var currentHealthDef = StatDefDataLoader.Instance.GetStatDefById(StatDefDataLoader.CurrentHealth);
            var currentHealth = _player.GetStatContainer().Get(currentHealthDef);
            
            if (currentHealth > newValue)
            {
                _player.GetStatContainer().SetBase(currentHealthDef, newValue);
            }
            
            // TODO: 更新UI最大血条显示
        }
        
        /// <summary>
        /// 修饰器添加回调
        /// </summary>
        private void OnModifierAdded(ModifierInstance modifier)
        {
            Logger2.Info("PlayerStatsManager.OnModifierAdded: 添加修饰器 {0}", modifier.Def?.Target?.Id ?? "Unknown");
            // TODO: 处理Buff/Debuff添加的视觉反馈
        }
        
        /// <summary>
        /// 修饰器移除回调
        /// </summary>
        private void OnModifierRemoved(ModifierInstance modifier)
        {
            Logger2.Info("PlayerStatsManager.OnModifierRemoved: 移除修饰器 {0}", modifier.Def?.Target?.Id ?? "Unknown");
            // TODO: 处理Buff/Debuff移除的视觉反馈
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public override void _ExitTree()
        {
            // 移除事件监听器
            if (_player?.GetStatContainer() != null)
            {
                _player.GetStatContainer().ValueChanged -= OnStatValueChanged;
                _player.GetStatContainer().ModifierAdded -= OnModifierAdded;
                _player.GetStatContainer().ModifierRemoved -= OnModifierRemoved;
            }
            
            Logger2.Info("PlayerStatsManager: 资源清理完成");
        }
    }
}