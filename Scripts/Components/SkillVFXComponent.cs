using Godot;
using System;
using System.Collections.Generic;
using Entities;
using Logs;

namespace Components
{
    /// <summary>
    /// 技能特效组件
    /// 负责管理技能相关的视觉特效，包括施法前摇、飞行轨迹、命中效果等
    /// </summary>
    public partial class SkillVFXComponent : LogicComponent
    {
        #region 字段和属性

        /// <summary>
        /// 特效播放模式
        /// </summary>
        public VFXPlayMode PlayMode { get; set; } = VFXPlayMode.Sequential;

        /// <summary>
        /// 特效缩放因子
        /// </summary>
        public float ScaleFactor { get; set; } = 1.0f;

        /// <summary>
        /// 特效播放速度
        /// </summary>
        public float PlaybackSpeed { get; set; } = 1.0f;

        /// <summary>
        /// 是否循环播放
        /// </summary>
        public bool LoopEnabled { get; set; } = false;

        /// <summary>
        /// 特效淡入时间（秒）
        /// </summary>
        public float FadeInDuration { get; set; } = 0.2f;

        /// <summary>
        /// 特效淡出时间（秒）
        /// </summary>
        public float FadeOutDuration { get; set; } = 0.3f;

        #endregion

        #region 私有字段

        private Dictionary<string, List<VFXInstance>> _activeVFXInstances = new();
        private Dictionary<string, VFXTemplate> _vfxTemplates = new();
        private List<VFXInstance> _pendingCleanup = new();

        #endregion

        #region 事件

        /// <summary>
        /// 特效开始播放事件
        /// </summary>
        public event Action<VFXEventArgs> OnVFXStarted;

        /// <summary>
        /// 特效播放完成事件
        /// </summary>
        public event Action<VFXEventArgs> OnVFXFinished;

        /// <summary>
        /// 特效被中断事件
        /// </summary>
        public event Action<VFXEventArgs> OnVFXInterrupted;

        #endregion

        #region 生命周期

        public override void Initialize(CharacterEntity entity)
        {
            base.Initialize(entity);
            Logger2.Info("SkillVFXComponent: 技能特效组件初始化完成");
        }

        public override void Update(float delta)
        {
            if (!IsEnabled) return;
            
            UpdateActiveVFX(delta);
            CleanupFinishedVFX();
        }

        public override void Cleanup()
        {
            base.Cleanup();
            StopAllVFX();
            _activeVFXInstances.Clear();
            _vfxTemplates.Clear();
            _pendingCleanup.Clear();
            Logger2.Info("SkillVFXComponent: 特效组件清理完成");
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 注册特效模板
        /// </summary>
        public void RegisterVFXTemplates(Dictionary<string, VFXTemplate> templates)
        {
            foreach (var kvp in templates)
            {
                _vfxTemplates[kvp.Key] = kvp.Value;
                Logger2.Debug($"SkillVFXComponent: 注册特效模板 {kvp.Key}");
            }
        }

        /// <summary>
        /// 播放技能施法特效
        /// </summary>
        public bool PlayCastVFX(string skillId, Vector2 position, Vector2 direction)
        {
            return PlayVFX($"{skillId}_cast", position, direction);
        }

        /// <summary>
        /// 播放技能飞行特效
        /// </summary>
        public bool PlayFlightVFX(string skillId, Vector2 startPosition, Vector2 endPosition, float duration)
        {
            var templateId = $"{skillId}_flight";
            if (!_vfxTemplates.ContainsKey(templateId))
            {
                Logger2.Warn($"SkillVFXComponent: 未找到飞行特效模板 {templateId}");
                return false;
            }

            var template = _vfxTemplates[templateId];
            var instance = CreateVFXInstance(template, startPosition);
            if (instance == null) return false;

            // 设置飞行轨迹
            instance.IsFlying = true;
            instance.StartPosition = startPosition;
            instance.EndPosition = endPosition;
            instance.FlightDuration = duration;
            instance.FlightTimer = 0f;

            // 添加到活动列表
            AddToActiveVFX(templateId, instance);

            Logger2.Debug($"SkillVFXComponent: 播放飞行特效 {templateId}");
            return true;
        }

        /// <summary>
        /// 播放技能命中特效
        /// </summary>
        public bool PlayImpactVFX(string skillId, Vector2 position, Vector2 normal)
        {
            return PlayVFX($"{skillId}_impact", position, normal);
        }

        /// <summary>
        /// 播放技能区域特效
        /// </summary>
        public bool PlayAreaVFX(string skillId, Vector2 center, float radius)
        {
            var templateId = $"{skillId}_area";
            if (!_vfxTemplates.ContainsKey(templateId))
            {
                Logger2.Warn($"SkillVFXComponent: 未找到区域特效模板 {templateId}");
                return false;
            }

            var template = _vfxTemplates[templateId];
            var instance = CreateVFXInstance(template, center);
            if (instance == null) return false;

            // 设置区域参数
            instance.IsAreaEffect = true;
            instance.AreaCenter = center;
            instance.AreaRadius = radius;

            AddToActiveVFX(templateId, instance);
            Logger2.Debug($"SkillVFXComponent: 播放区域特效 {templateId}");
            return true;
        }

        /// <summary>
        /// 播放通用特效
        /// </summary>
        public bool PlayVFX(string templateId, Vector2 position, Vector2 direction)
        {
            if (!_vfxTemplates.ContainsKey(templateId))
            {
                Logger2.Warn($"SkillVFXComponent: 未找到特效模板 {templateId}");
                return false;
            }

            var template = _vfxTemplates[templateId];
            var instance = CreateVFXInstance(template, position);
            if (instance == null) return false;

            // 设置朝向
            if (direction != Vector2.Zero)
            {
                instance.Node.Rotation = direction.Angle();
            }

            AddToActiveVFX(templateId, instance);
            
            OnVFXStarted?.Invoke(new VFXEventArgs
            {
                TemplateId = templateId,
                Instance = instance,
                Position = position
            });

            Logger2.Debug($"SkillVFXComponent: 播放特效 {templateId} 位置:{position}");
            return true;
        }

        /// <summary>
        /// 停止指定特效
        /// </summary>
        public void StopVFX(string templateId)
        {
            if (_activeVFXInstances.ContainsKey(templateId))
            {
                var instances = _activeVFXInstances[templateId];
                foreach (var instance in instances)
                {
                    InterruptVFX(instance, templateId);
                }
                instances.Clear();
                Logger2.Debug($"SkillVFXComponent: 停止特效 {templateId}");
            }
        }

        /// <summary>
        /// 停止所有特效
        /// </summary>
        public void StopAllVFX()
        {
            foreach (var kvp in _activeVFXInstances)
            {
                foreach (var instance in kvp.Value)
                {
                    InterruptVFX(instance, kvp.Key);
                }
            }
            _activeVFXInstances.Clear();
            Logger2.Debug("SkillVFXComponent: 停止所有特效");
        }

        /// <summary>
        /// 获取活动特效数量
        /// </summary>
        public int GetActiveVFXCount()
        {
            int count = 0;
            foreach (var instances in _activeVFXInstances.Values)
            {
                count += instances.Count;
            }
            return count;
        }

        #endregion

        #region 特效管理

        /// <summary>
        /// 创建特效实例
        /// </summary>
        private VFXInstance CreateVFXInstance(VFXTemplate template, Vector2 position)
        {
            try
            {
                // 加载预制体
                var packedScene = GD.Load<PackedScene>(template.ScenePath);
                if (packedScene == null)
                {
                    Logger2.Error($"SkillVFXComponent: 无法加载特效预制体 {template.ScenePath}");
                    return null;
                }

                var node = packedScene.Instantiate<Node2D>();
                if (node == null)
                {
                    Logger2.Error($"SkillVFXComponent: 无法实例化特效节点 {template.ScenePath}");
                    return null;
                }

                // 设置基本属性
                node.Position = position;
                node.Scale = Vector2.One * template.Scale * ScaleFactor;
                node.Visible = false;

                // 添加到场景树
                Entity.AddChild(node);

                var instance = new VFXInstance
                {
                    Node = node,
                    Template = template,
                    StartTime = Time.GetTicksMsec(),
                    Duration = template.Duration,
                    IsLooping = template.AutoLoop && LoopEnabled
                };

                // 启动淡入效果
                if (FadeInDuration > 0)
                {
                    StartFadeIn(instance);
                }
                else
                {
                    node.Visible = true;
                    StartVFXAnimation(instance);
                }

                return instance;
            }
            catch (Exception ex)
            {
                Logger2.Error($"SkillVFXComponent: 创建特效实例失败 - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 添加到活动特效列表
        /// </summary>
        private void AddToActiveVFX(string templateId, VFXInstance instance)
        {
            if (!_activeVFXInstances.ContainsKey(templateId))
            {
                _activeVFXInstances[templateId] = new List<VFXInstance>();
            }
            _activeVFXInstances[templateId].Add(instance);
        }

        /// <summary>
        /// 启动特效动画
        /// </summary>
        private void StartVFXAnimation(VFXInstance instance)
        {
            if (instance.Node is AnimatedSprite2D animatedSprite)
            {
                if (animatedSprite.SpriteFrames != null)
                {
                    animatedSprite.Play(instance.Template.AnimationName);
                    animatedSprite.SpeedScale = PlaybackSpeed;
                }
            }
            else
            {
                // 尝试获取AnimationPlayer组件
                var animationPlayer = instance.Node.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
                if (animationPlayer != null && animationPlayer.HasAnimation(instance.Template.AnimationName))
                {
                    animationPlayer.Play(instance.Template.AnimationName);
                    animationPlayer.SpeedScale = PlaybackSpeed;
                }
            }
        }

        /// <summary>
        /// 启动淡入效果
        /// </summary>
        private void StartFadeIn(VFXInstance instance)
        {
            var tween = Entity.CreateTween();
            tween.TweenProperty(instance.Node, "modulate:a", 1.0f, FadeInDuration);
            tween.Finished += () =>
            {
                instance.Node.Visible = true;
                StartVFXAnimation(instance);
            };
        }

        /// <summary>
        /// 启动淡出效果
        /// </summary>
        private void StartFadeOut(VFXInstance instance, string templateId)
        {
            var tween = Entity.CreateTween();
            tween.TweenProperty(instance.Node, "modulate:a", 0.0f, FadeOutDuration);
            tween.Finished += () =>
            {
                FinishVFX(instance, templateId);
            };
        }

        /// <summary>
        /// 完成特效
        /// </summary>
        private void FinishVFX(VFXInstance instance, string templateId)
        {
            instance.Node.QueueFree();
            
            if (_activeVFXInstances.ContainsKey(templateId))
            {
                _activeVFXInstances[templateId].Remove(instance);
            }

            OnVFXFinished?.Invoke(new VFXEventArgs
            {
                TemplateId = templateId,
                Instance = instance,
                Position = instance.Node.Position
            });

            Logger2.Debug($"SkillVFXComponent: 特效完成 {templateId}");
        }

        /// <summary>
        /// 中断特效
        /// </summary>
        private void InterruptVFX(VFXInstance instance, string templateId)
        {
            instance.Node.QueueFree();
            
            if (_activeVFXInstances.ContainsKey(templateId))
            {
                _activeVFXInstances[templateId].Remove(instance);
            }

            OnVFXInterrupted?.Invoke(new VFXEventArgs
            {
                TemplateId = templateId,
                Instance = instance,
                Position = instance.Node.Position
            });

            Logger2.Debug($"SkillVFXComponent: 特效被中断 {templateId}");
        }

        #endregion

        #region 更新逻辑

        /// <summary>
        /// 更新活动特效
        /// </summary>
        private void UpdateActiveVFX(float delta)
        {
            foreach (var kvp in _activeVFXInstances)
            {
                var templateId = kvp.Key;
                var instances = kvp.Value;

                for (int i = instances.Count - 1; i >= 0; i--)
                {
                    var instance = instances[i];
                    
                    // 更新飞行特效
                    if (instance.IsFlying)
                    {
                        UpdateFlightVFX(instance, delta);
                    }

                    // 检查生命周期
                    if (!instance.IsLooping)
                    {
                        ulong elapsed = Time.GetTicksMsec() - instance.StartTime;
                        if (elapsed >= instance.Duration * 1000)
                        {
                            if (FadeOutDuration > 0)
                            {
                                StartFadeOut(instance, templateId);
                            }
                            else
                            {
                                FinishVFX(instance, templateId);
                            }
                            instances.RemoveAt(i);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 更新飞行特效
        /// </summary>
        private void UpdateFlightVFX(VFXInstance instance, float delta)
        {
            instance.FlightTimer += delta;
            float progress = Mathf.Min(1.0f, instance.FlightTimer / instance.FlightDuration);
            
            // 线性插值计算位置
            var newPosition = instance.StartPosition.Lerp(instance.EndPosition, progress);
            instance.Node.Position = newPosition;

            // 完成飞行
            if (progress >= 1.0f)
            {
                instance.IsFlying = false;
            }
        }

        /// <summary>
        /// 清理已完成的特效
        /// </summary>
        private void CleanupFinishedVFX()
        {
            foreach (var instance in _pendingCleanup)
            {
                instance.Node.QueueFree();
            }
            _pendingCleanup.Clear();
        }

        #endregion
    }

    #region 数据结构

    /// <summary>
    /// 特效模板
    /// </summary>
    public class VFXTemplate
    {
        public string ScenePath { get; set; }
        public string AnimationName { get; set; } = "default";
        public float Duration { get; set; } = 1.0f;
        public float Scale { get; set; } = 1.0f;
        public bool AutoLoop { get; set; } = false;
        public VFXLayer Layer { get; set; } = VFXLayer.Normal;
        public bool WorldSpace { get; set; } = true;
    }

    /// <summary>
    /// 特效实例
    /// </summary>
    public class VFXInstance
    {
        public Node2D Node { get; set; }
        public VFXTemplate Template { get; set; }
        public ulong StartTime { get; set; }
        public float Duration { get; set; }
        public bool IsLooping { get; set; }
        
        // 飞行特效参数
        public bool IsFlying { get; set; } = false;
        public Vector2 StartPosition { get; set; }
        public Vector2 EndPosition { get; set; }
        public float FlightDuration { get; set; }
        public float FlightTimer { get; set; }
        
        // 区域特效参数
        public bool IsAreaEffect { get; set; } = false;
        public Vector2 AreaCenter { get; set; }
        public float AreaRadius { get; set; }
    }

    /// <summary>
    /// 特效事件参数
    /// </summary>
    public class VFXEventArgs
    {
        public string TemplateId { get; set; }
        public VFXInstance Instance { get; set; }
        public Vector2 Position { get; set; }
    }

    #endregion

    #region 枚举定义

    /// <summary>
    /// 特效播放模式
    /// </summary>
    public enum VFXPlayMode
    {
        Sequential,  // 顺序播放
        Parallel,    // 并行播放
        Override     // 覆盖播放
    }

    /// <summary>
    /// 特效层级
    /// </summary>
    public enum VFXLayer
    {
        Background,  // 背景层
        Normal,      // 普通层
        Foreground,  // 前景层
        UI           // UI层
    }

    #endregion
}