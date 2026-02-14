using Godot;
using System;
using Components;
using Projectiles;
using Logs;
using Characters;

namespace Projectiles
{
    /// <summary>
    /// 投射物使用示例
    /// 演示如何在实际游戏中使用投射物发射组件
    /// </summary>
    public partial class ProjectileUsageExample : Node2D
    {
        #region 字段和属性

        private Player _player;
        private ProjectileSpawnerComponent _spawner;
        private Label _statusLabel;
        private Timer _autoFireTimer;

        #endregion

        #region 生命周期

        public override void _Ready()
        {
            SetupPlayer();
            SetupSpawner();
            SetupUI();
            SetupAutoFire();
            
            Logger2.Info("ProjectileUsageExample: 示例场景初始化完成");
        }

        public override void _Process(double delta)
        {
            UpdateStatusDisplay();
            HandleInput();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 设置玩家
        /// </summary>
        private void SetupPlayer()
        {
            // 创建一个简单的玩家实体用于演示
            _player = new Player();
            _player.Name = "Player";
            AddChild(_player);
            
            Logger2.Debug("ProjectileUsageExample: 玩家实体创建完成");
        }

        /// <summary>
        /// 设置投射物发射器
        /// </summary>
        private void SetupSpawner()
        {
            _spawner = new ProjectileSpawnerComponent();
            _spawner.PoolSize = 30;
            _spawner.UseEntityFacing = true;
            _spawner.SpawnOffset = new Vector2(20, 0);
            
            // 创建示例投射物数据
            var fireballData = CreateFireballData();
            var arrowData = CreateArrowData();
            var magicMissileData = CreateMagicMissileData();
            
            // 使用System.Collections.Generic.List
            var dataList = new System.Collections.Generic.List<ProjectileData>();
            dataList.Add(fireballData);
            dataList.Add(arrowData);
            dataList.Add(magicMissileData);
            _spawner.ProjectileDatas = dataList;
            
            _player.AddComponent(_spawner);
            _spawner.Initialize(_player);
            
            // 订阅事件
            _spawner.OnProjectileSpawned += OnProjectileSpawned;
            _spawner.OnSpawnComplete += OnSpawnComplete;
            
            // 设置默认投射物
            _spawner.SetCurrentProjectile("fireball");
            
            Logger2.Debug("ProjectileUsageExample: 投射物发射器设置完成");
        }

        /// <summary>
        /// 设置UI
        /// </summary>
        private void SetupUI()
        {
            // 创建状态显示标签
            _statusLabel = new Label();
            _statusLabel.Position = new Vector2(10, 10);
            _statusLabel.Size = new Vector2(400, 200);
            _statusLabel.AddThemeFontSizeOverride("font_size", 16);
            AddChild(_statusLabel);
            
            // 创建控制说明
            var instructionLabel = new Label();
            instructionLabel.Text = "控制说明:\n" +
                                  "1-3: 切换投射物类型\n" +
                                  "鼠标左键: 发射投射物\n" +
                                  "空格: 自动连续发射\n" +
                                  "R: 重置场景";
            instructionLabel.Position = new Vector2(10, 220);
            instructionLabel.AddThemeFontSizeOverride("font_size", 14);
            instructionLabel.AddThemeColorOverride("font_color", Colors.Yellow);
            AddChild(instructionLabel);
        }

        /// <summary>
        /// 设置自动发射
        /// </summary>
        private void SetupAutoFire()
        {
            _autoFireTimer = new Timer();
            _autoFireTimer.WaitTime = 0.5f;
            _autoFireTimer.OneShot = false;
            _autoFireTimer.Timeout += OnAutoFireTimeout;
            AddChild(_autoFireTimer);
        }

        #endregion

        #region 投射物数据创建

        /// <summary>
        /// 创建火球数据
        /// </summary>
        private ProjectileData CreateFireballData()
        {
            return new ProjectileData
            {
                ProjectileId = "fireball",
                Name = "火球术",
                Description = "发射一枚爆炸火球",
                PrefabPath = "res://Prefabs/Projectiles/Fireball.tscn",
                Type = ProjectileType.Normal,
                Rarity = ProjectileRarity.Rare,
                InitialSpeed = 400f,
                MaxSpeed = 600f,
                GravityScale = 0.5f,
                MaxLifetime = 3.0f,
                Trajectory = TrajectoryType.Parabolic,
                ArcHeight = 100f,
                Pattern = SpawnPattern.Single,
                CollisionRadius = 15f,
                BaseDamage = 25f,
                DamageType = DamageType.Fire,
                HasExplosion = true,
                ExplosionDelay = 0f,
                ExplosionRadius = 80f,
                ExplosionDamage = 15f,
                KnockbackForce = 200f,
                FlightEffectPath = "res://Effects/FireballTrail.tscn",
                ImpactEffectPath = "res://Effects/FireballExplosion.tscn"
            };
        }

        /// <summary>
        /// 创建箭矢数据
        /// </summary>
        private ProjectileData CreateArrowData()
        {
            return new ProjectileData
            {
                ProjectileId = "arrow",
                Name = "精准射击",
                Description = "高速穿刺箭矢",
                PrefabPath = "res://Prefabs/Projectiles/Arrow.tscn",
                Type = ProjectileType.Normal,
                Rarity = ProjectileRarity.Common,
                InitialSpeed = 800f,
                MaxSpeed = 1000f,
                GravityScale = 0.2f,
                MaxLifetime = 2.0f,
                Trajectory = TrajectoryType.Linear,
                Pattern = SpawnPattern.Single,
                IsPenetrating = true,
                MaxPenetrationCount = 3,
                CollisionRadius = 5f,
                BaseDamage = 20f,
                DamageType = DamageType.Physical,
                KnockbackForce = 150f,
                FlightEffectPath = "res://Effects/ArrowTrail.tscn"
            };
        }

        /// <summary>
        /// 创建魔法飞弹数据
        /// </summary>
        private ProjectileData CreateMagicMissileData()
        {
            return new ProjectileData
            {
                ProjectileId = "magic_missile",
                Name = "_magic飞弹",
                Description = "追踪型魔法飞弹",
                PrefabPath = "res://Prefabs/Projectiles/MagicMissile.tscn",
                Type = ProjectileType.Homing,
                Rarity = ProjectileRarity.Epic,
                InitialSpeed = 300f,
                MaxSpeed = 500f,
                GravityScale = 0f,
                MaxLifetime = 4.0f,
                Trajectory = TrajectoryType.Homing,
                Pattern = SpawnPattern.Volley,
                SpawnCount = 5,
                SpawnInterval = 0.1f,
                SpreadAngle = 30f,
                CollisionRadius = 10f,
                BaseDamage = 15f,
                DamageType = DamageType.Magical,
                StatusEffects = new Godot.Collections.Array<StatusEffectData>
                {
                    new StatusEffectData
                    {
                        EffectId = "slow_effect",
                        EffectName = "减速",
                        EffectType = StatusEffectType.Debuff,
                        Duration = 3.0f,
                        Power = 0.5f,
                        IsDebuff = true
                    }
                },
                FlightEffectPath = "res://Effects/MagicTrail.tscn",
                ImpactEffectPath = "res://Effects/MagicImpact.tscn"
            };
        }

        #endregion

        #region 输入处理

        /// <summary>
        /// 处理输入
        /// </summary>
        private void HandleInput()
        {
            // 切换投射物类型
            if (Input.IsKeyPressed(Key.Key1))
            {
                _spawner.SetCurrentProjectile("fireball");
                Logger2.Info("ProjectileUsageExample: 切换到火球术");
            }
            else if (Input.IsKeyPressed(Key.Key2))
            {
                _spawner.SetCurrentProjectile("arrow");
                Logger2.Info("ProjectileUsageExample: 切换到精准射击");
            }
            else if (Input.IsKeyPressed(Key.Key3))
            {
                _spawner.SetCurrentProjectile("magic_missile");
                Logger2.Info("ProjectileUsageExample: 切换到魔法飞弹");
            }

            // 发射投射物
            if (Input.IsMouseButtonPressed(MouseButton.Left))
            {
                var mousePos = GetGlobalMousePosition();
                _spawner.SpawnProjectile(mousePos);
            }

            // 自动连续发射切换
            if (Input.IsKeyPressed(Key.Space))
            {
                if (_autoFireTimer.IsStopped())
                {
                    _autoFireTimer.Start();
                    Logger2.Info("ProjectileUsageExample: 开始自动发射");
                }
                else
                {
                    _autoFireTimer.Stop();
                    Logger2.Info("ProjectileUsageExample: 停止自动发射");
                }
            }

            // 重置场景
            if (Input.IsKeyPressed(Key.R))
            {
                GetTree().ReloadCurrentScene();
            }
        }

        /// <summary>
        /// 自动发射超时回调
        /// </summary>
        private void OnAutoFireTimeout()
        {
            var random = new Random();
            var targetPos = new Vector2(
                (float)(random.NextDouble() * 400 - 200),
                (float)(random.NextDouble() * 300 - 150)
            );
            
            _spawner.SpawnProjectile(Position + targetPos);
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 投射物发射事件处理
        /// </summary>
        private void OnProjectileSpawned(ProjectileSpawnEventArgs args)
        {
            Logger2.Debug($"ProjectileUsageExample: 投射物发射 - {args.Data.Name} 位置: {args.Position}");
        }

        /// <summary>
        /// 发射完成事件处理
        /// </summary>
        private void OnSpawnComplete()
        {
            Logger2.Debug("ProjectileUsageExample: 发射序列完成");
        }

        #endregion

        #region UI更新

        /// <summary>
        /// 更新状态显示
        /// </summary>
        private void UpdateStatusDisplay()
        {
            if (_statusLabel == null || _spawner == null) return;
            
            var currentData = _spawner.CurrentProjectileData;
            var statusText = $"当前投射物: {currentData?.Name ?? "无"}\n" +
                           $"投射物类型: {currentData?.Type}\n" +
                           $"稀有度: {currentData?.Rarity}\n" +
                           $"基础伤害: {currentData?.BaseDamage}\n" +
                           $"发射模式: {currentData?.Pattern}\n" +
                           $"是否穿透: {currentData?.IsPenetrating}\n" +
                           $"爆炸伤害: {currentData?.ExplosionDamage}\n" +
                           $"正在发射: {_spawner.IsSpawning}\n" +
                           $"活跃投射物: {_spawner.GetType().GetField("_activeProjectiles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_spawner)}";
            
            _statusLabel.Text = statusText;
        }

        #endregion

        #region 清理

        public override void _ExitTree()
        {
            // 清理事件订阅
            if (_spawner != null)
            {
                _spawner.OnProjectileSpawned -= OnProjectileSpawned;
                _spawner.OnSpawnComplete -= OnSpawnComplete;
            }
            
            Logger2.Info("ProjectileUsageExample: 示例场景清理完成");
        }

        #endregion
    }
}