using Godot;
using System;
using Numerical;
using Logs;

namespace Characters
{
    /// <summary>
    /// 玩家战斗系统
    /// 负责处理攻击、受击、技能释放等战斗相关逻辑
    /// </summary>
    public partial class PlayerCombatSystem : Node
    {
        private Player _player;
        private bool _isInitialized = false;
        
        // 战斗状态
        private bool _isAttacking = false;
        private double _attackCooldown = 0.0;
        private double _attackTimer = 0.0;
        
        // 战斗配置
        [Export] public float BaseAttackDamage { get; set; } = 10.0f;
        [Export] public float AttackRange { get; set; } = 50.0f;
        [Export] public float AttackCooldownTime { get; set; } = 0.5f;
        [Export] public float KnockbackForce { get; set; } = 200.0f;

        /// <summary>
        /// 初始化战斗系统
        /// </summary>
        public void Initialize(Player player)
        {
            if (_isInitialized)
            {
                Logger2.Warn("PlayerCombatSystem: 已经初始化过了");
                return;
            }

            _player = player ?? throw new ArgumentNullException(nameof(player));
            _isInitialized = true;
            
            Logger2.Info("PlayerCombatSystem: 初始化完成");
        }

        /// <summary>
        /// 每帧更新战斗逻辑
        /// </summary>
        public void Update(double delta)
        {
            if (!_isInitialized) return;
            
            UpdateAttackCooldown(delta);
        }
        
        /// <summary>
        /// 执行攻击动作
        /// </summary>
        public void PerformAttack()
        {
            if (!_isInitialized || _isAttacking || _attackCooldown > 0)
                return;
                
            Logger2.Info("PlayerCombatSystem: 执行攻击");
            
            // 设置攻击状态
            _isAttacking = true;
            _attackTimer = 0.0;
            _attackCooldown = AttackCooldownTime;
            
            // 计算实际伤害
            float damage = CalculateDamage();
            
            // 检测攻击范围内的敌人
            DetectAndDamageEnemies(damage);
            
            // 触发攻击动画和特效
            TriggerAttackEffects();
        }
        
        /// <summary>
        /// 计算攻击伤害
        /// </summary>
        private float CalculateDamage()
        {
            // 基础伤害
            float damage = BaseAttackDamage;
            
            // 从属性系统获取伤害加成
            if (_player != null)
            {
                var physicalDamageDef = StatDefDataLoader.Instance.GetStatDefById(StatDefDataLoader.PhysicalDamage);
                if (physicalDamageDef != null)
                {
                    damage += _player.Stats.Get(physicalDamageDef);
                }
                
                var critChanceDef = StatDefDataLoader.Instance.GetStatDefById(StatDefDataLoader.CriticalChance);
                var critDamageDef = StatDefDataLoader.Instance.GetStatDefById(StatDefDataLoader.CriticalDamage);
                
                if (critChanceDef != null && critDamageDef != null)
                {
                    float critChance = _player.Stats.Get(critChanceDef);
                    if (GD.Randf() < critChance)
                    {
                        damage *= _player.Stats.Get(critDamageDef);
                        Logger2.Info("PlayerCombatSystem: 暴击! 伤害倍数: {0}", _player.Stats.Get(critDamageDef));
                    }
                }
            }
            
            return damage;
        }
        
        /// <summary>
        /// 检测并伤害范围内的敌人
        /// </summary>
        private void DetectAndDamageEnemies(float damage)
        {
            // TODO: 实际的碰撞检测和敌人伤害逻辑
            // 这里只是示例代码
            Logger2.Debug("PlayerCombatSystem: 检测 {0} 范围内的敌人，造成 {1} 伤害", AttackRange, damage);
            
            // 示例：假设有敌人在这个范围内
            // var enemies = GetEnemiesInRange(AttackRange);
            // foreach (var enemy in enemies)
            // {
            //     enemy.TakeDamage(damage, this);
            // }
        }
        
        /// <summary>
        /// 触发攻击特效
        /// </summary>
        private void TriggerAttackEffects()
        {
            // TODO: 播放攻击动画
            // TODO: 播放攻击音效
            // TODO: 产生粒子效果
            Logger2.Debug("PlayerCombatSystem: 触发攻击特效");
        }
        
        /// <summary>
        /// 受到伤害
        /// </summary>
        public void TakeDamage(float damage, Node source = null)
        {
            if (!_isInitialized) return;
            
            Logger2.Info("PlayerCombatSystem: 受到 {0} 点伤害", damage);
            
            // 应用护甲减免
            float reducedDamage = ApplyDamageReduction(damage);
            
            // 从生命值中扣除伤害
            if (_player != null)
            {
                var currentHealthDef = StatDefDataLoader.Instance.GetStatDefById(StatDefDataLoader.CurrentHealth);
                if (currentHealthDef != null)
                {
                    float currentHealth = _player.Stats.Get(currentHealthDef);
                    _player.Stats.SetBase(currentHealthDef, currentHealth - reducedDamage);
                }
            }
            
            // 触发受击效果
            TriggerHitEffects(reducedDamage, source);
        }
        
        /// <summary>
        /// 应用伤害减免
        /// </summary>
        private float ApplyDamageReduction(float damage)
        {
            if (_player == null) return damage;
            
            // 获取护甲值
            var armorDef = StatDefDataLoader.Instance.GetStatDefById(StatDefDataLoader.Armor);
            if (armorDef != null)
            {
                float armor = _player.Stats.Get(armorDef);
                // 简单的伤害减免公式
                float reduction = armor / (armor + 100);
                damage *= (1.0f - reduction);
            }
            
            return damage;
        }
        
        /// <summary>
        /// 触发受击效果
        /// </summary>
        private void TriggerHitEffects(float damage, Node source)
        {
            // TODO: 播放受击动画
            // TODO: 播放受击音效
            // TODO: 产生受击特效
            // TODO: 应用击退效果
            Logger2.Debug("PlayerCombatSystem: 触发受击效果，伤害: {0}", damage);
        }
        
        /// <summary>
        /// 更新攻击冷却时间
        /// </summary>
        private void UpdateAttackCooldown(double delta)
        {
            if (_attackCooldown > 0)
            {
                _attackCooldown -= delta;
                if (_attackCooldown <= 0)
                {
                    _attackCooldown = 0;
                    _isAttacking = false;
                    Logger2.Debug("PlayerCombatSystem: 攻击冷却结束");
                }
            }
            
            // 更新攻击持续时间
            if (_isAttacking)
            {
                _attackTimer += delta;
                // 假设攻击动作持续0.3秒
                if (_attackTimer >= 0.3)
                {
                    _isAttacking = false;
                }
            }
        }
        
        /// <summary>
        /// 检查是否可以攻击
        /// </summary>
        public bool CanAttack()
        {
            return !_isAttacking && _attackCooldown <= 0;
        }
        
        /// <summary>
        /// 获取剩余冷却时间
        /// </summary>
        public double GetRemainingCooldown()
        {
            return _attackCooldown;
        }
        
        /// <summary>
        /// 清理资源
        /// </summary>
        public override void _ExitTree()
        {
            Logger2.Info("PlayerCombatSystem: 资源清理完成");
        }
    }
}