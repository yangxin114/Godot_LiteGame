using Godot;
using System;
using System.Collections.Generic;
using Entities;
using Logs;

namespace Components
{
    /// <summary>
    /// 技能音效组件
    /// 负责管理技能相关的音频播放，包括施法音效、飞行音效、命中音效等
    /// </summary>
    public partial class SkillAudioComponent : LogicComponent
    {
        #region 字段和属性

        /// <summary>
        /// 音效音量
        /// </summary>
        public float Volume { get; set; } = 0.0f; // dB

        /// <summary>
        /// 音效播放速度
        /// </summary>
        public float PitchScale { get; set; } = 1.0f;

        /// <summary>
        /// 音效衰减距离
        /// </summary>
        public float AttenuationDistance { get; set; } = 1000.0f;

        /// <summary>
        /// 是否启用3D音效
        /// </summary>
        public bool Enable3DAudio { get; set; } = true;

        /// <summary>
        /// 音效淡入时间（秒）
        /// </summary>
        public float FadeInDuration { get; set; } = 0.1f;

        /// <summary>
        /// 音效淡出时间（秒）
        /// </summary>
        public float FadeOutDuration { get; set; } = 0.2f;

        #endregion

        #region 私有字段

        private Dictionary<string, AudioStreamPlayer2D> _audioPlayers = new();
        private Dictionary<string, AudioStreamPlayer3D> _audioPlayers3D = new();
        private Dictionary<string, AudioTemplate> _audioTemplates = new();
        private List<AudioInstanceBase> _activeAudioInstances = new();
        private AudioStreamPlayer2D _ambientPlayer;

        #endregion

        #region 事件

        /// <summary>
        /// 音效开始播放事件
        /// </summary>
        public event Action<AudioEventArgs> OnAudioStarted;

        /// <summary>
        /// 音效播放完成事件
        /// </summary>
        public event Action<AudioEventArgs> OnAudioFinished;

        /// <summary>
        /// 音效被中断事件
        /// </summary>
        public event Action<AudioEventArgs> OnAudioInterrupted;

        #endregion

        #region 公共接口

        /// <summary>
        /// 添加到活动音频列表
        /// </summary>
        private void AddToActiveAudio(AudioInstanceBase instance, string templateId)
        {
            instance.TemplateId = templateId;
            _activeAudioInstances.Add(instance);
        }

        /// <summary>
        /// 启动淡入效果
        /// </summary>
        private void StartFadeIn(AudioStreamPlayer2D player, float targetVolume)
        {
            var tween = Entity.CreateTween();
            tween.TweenProperty(player, "volume_db", targetVolume, FadeInDuration);
        }

        /// <summary>
        /// 启动淡入效果（3D）
        /// </summary>
        private void StartFadeIn(AudioStreamPlayer3D player, float targetVolume)
        {
            var tween = Entity.CreateTween();
            tween.TweenProperty(player, "volume_db", targetVolume, FadeInDuration);
        }

        /// <summary>
        /// 启动淡出效果
        /// </summary>
        private void StartFadeOut(AudioStreamPlayer2D player, AudioInstanceBase instance)
        {
            var tween = Entity.CreateTween();
            tween.TweenProperty(player, "volume_db", -80.0f, FadeOutDuration);
            tween.Finished += () =>
            {
                FinishAudioInstance(instance);
            };
        }

        /// <summary>
        /// 启动淡出效果（3D）
        /// </summary>
        private void StartFadeOut(AudioStreamPlayer3D player, AudioInstanceBase instance)
        {
            var tween = Entity.CreateTween();
            tween.TweenProperty(player, "volume_db", -80.0f, FadeOutDuration);
            tween.Finished += () =>
            {
                FinishAudioInstance(instance);
            };
        }

        /// <summary>
        /// 完成音频实例
        /// </summary>
        private void FinishAudioInstance(AudioInstanceBase instance)
        {
            instance.Stop();
            
            OnAudioFinished?.Invoke(new AudioEventArgs
            {
                TemplateId = instance.TemplateId,
                Instance = instance,
                Position = instance.GetPosition()
            });

            Logger2.Debug($"SkillAudioComponent: 音频完成 {instance.TemplateId}");
        }

        /// <summary>
        /// 中断音频实例
        /// </summary>
        private void InterruptAudioInstance(AudioInstanceBase instance)
        {
            if (FadeOutDuration > 0)
            {
                if (instance is AudioInstance2D instance2D)
                {
                    StartFadeOut(instance2D.Player, instance);
                }
                else if (instance is AudioInstance3D instance3D)
                {
                    StartFadeOut(instance3D.Player, instance);
                }
            }
            else
            {
                instance.Stop();
                
                OnAudioInterrupted?.Invoke(new AudioEventArgs
                {
                    TemplateId = instance.TemplateId,
                    Instance = instance,
                    Position = instance.GetPosition()
                });
            }

            Logger2.Debug($"SkillAudioComponent: 音频被中断 {instance.TemplateId}");
        }

        /// <summary>
        /// 注册音频模板
        /// </summary>
        public void RegisterAudioTemplates(Dictionary<string, AudioTemplate> templates)
        {
            foreach (var kvp in templates)
            {
                _audioTemplates[kvp.Key] = kvp.Value;
                Logger2.Debug($"SkillAudioComponent: 注册音频模板 {kvp.Key}");
            }
        }

        /// <summary>
        /// 播放技能施法音效
        /// </summary>
        public bool PlayCastAudio(string skillId, Vector2 position)
        {
            return PlayAudio($"{skillId}_cast", position);
        }

        /// <summary>
        /// 播放技能飞行音效
        /// </summary>
        public bool PlayFlightAudio(string skillId, Vector2 position, float duration)
        {
            var templateId = $"{skillId}_flight";
            if (!_audioTemplates.ContainsKey(templateId))
            {
                Logger2.Warn($"SkillAudioComponent: 未找到飞行音效模板 {templateId}");
                return false;
            }

            var template = _audioTemplates[templateId];
            var instance = CreateAudioInstance(template, position);
            if (instance == null) return false;

            // 设置持续时间
            instance.IsContinuous = true;
            instance.Duration = duration;

            AddToActiveAudio(instance, templateId);
            Logger2.Debug($"SkillAudioComponent: 播放飞行音效 {templateId}");
            return true;
        }

        /// <summary>
        /// 播放技能命中音效
        /// </summary>
        public bool PlayImpactAudio(string skillId, Vector2 position)
        {
            return PlayAudio($"{skillId}_impact", position);
        }

        /// <summary>
        /// 播放技能区域音效
        /// </summary>
        public bool PlayAreaAudio(string skillId, Vector2 center, float radius)
        {
            return PlayAudio($"{skillId}_area", center);
        }

        /// <summary>
        /// 播放通用音效
        /// </summary>
        public bool PlayAudio(string templateId, Vector2 position)
        {
            if (!_audioTemplates.ContainsKey(templateId))
            {
                Logger2.Warn($"SkillAudioComponent: 未找到音效模板 {templateId}");
                return false;
            }

            var template = _audioTemplates[templateId];
            var instance = CreateAudioInstance(template, position);
            if (instance == null) return false;

            AddToActiveAudio(instance, templateId);
            
            OnAudioStarted?.Invoke(new AudioEventArgs
            {
                TemplateId = templateId,
                Instance = instance,
                Position = position
            });

            Logger2.Debug($"SkillAudioComponent: 播放音效 {templateId} 位置:{position}");
            return true;
        }

        /// <summary>
        /// 播放环境音效
        /// </summary>
        public bool PlayAmbientAudio(string audioPath, float volumeDb = 0.0f, bool loop = false)
        {
            try
            {
                var stream = GD.Load<AudioStream>(audioPath);
                if (stream == null)
                {
                    Logger2.Error($"SkillAudioComponent: 无法加载环境音效 {audioPath}");
                    return false;
                }

                _ambientPlayer.Stream = stream;
                _ambientPlayer.VolumeDb = volumeDb;
                _ambientPlayer.Play();
                
                if (loop)
                {
                    // 循环播放需要特殊处理
                    _ambientPlayer.Finished += () => _ambientPlayer.Play();
                }

                Logger2.Debug($"SkillAudioComponent: 播放环境音效 {audioPath}");
                return true;
            }
            catch (Exception ex)
            {
                Logger2.Error($"SkillAudioComponent: 播放环境音效失败 - {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 停止指定音效
        /// </summary>
        public void StopAudio(string templateId)
        {
            for (int i = _activeAudioInstances.Count - 1; i >= 0; i--)
            {
                var instance = _activeAudioInstances[i];
                if (instance.TemplateId == templateId)
                {
                    InterruptAudioInstance(instance);
                    _activeAudioInstances.RemoveAt(i);
                }
            }
            Logger2.Debug($"SkillAudioComponent: 停止音效 {templateId}");
        }

        /// <summary>
        /// 停止所有音效
        /// </summary>
        public void StopAllAudio()
        {
            foreach (var instance in _activeAudioInstances)
            {
                InterruptAudioInstance(instance);
            }
            _activeAudioInstances.Clear();
            
            _ambientPlayer?.Stop();
            
            Logger2.Debug("SkillAudioComponent: 停止所有音效");
        }

        /// <summary>
        /// 获取活动音效数量
        /// </summary>
        public int GetActiveAudioCount()
        {
            return _activeAudioInstances.Count;
        }

        #endregion

        #region 音频管理

        /// <summary>
        /// 创建音频实例
        /// </summary>
        private AudioInstanceBase CreateAudioInstance(AudioTemplate template, Vector2 position)
        {
            try
            {
                // 加载音频流
                var stream = GD.Load<AudioStream>(template.AudioPath);
                if (stream == null)
                {
                    Logger2.Error($"SkillAudioComponent: 无法加载音频文件 {template.AudioPath}");
                    return null;
                }

                // 选择播放器类型
                if (Enable3DAudio && template.Use3DAudio)
                {
                    var player3D = GetOrCreate3DPlayer(template.TemplateId);
                    player3D.Stream = stream;
                    // 3D音频使用Vector3位置
                    player3D.Position = new Vector3(position.X, position.Y, 0);
                    player3D.VolumeDb = template.Volume + Volume;
                    player3D.PitchScale = template.PitchScale * PitchScale;
                    player3D.MaxDistance = AttenuationDistance;

                    // 启动淡入效果
                    if (FadeInDuration > 0)
                    {
                        StartFadeIn(player3D, template.Volume + Volume);
                    }

                    player3D.Play();

                    var instance = new AudioInstance3D
                    {
                        Player = player3D,
                        TemplateId = template.TemplateId,
                        StartTime = Time.GetTicksMsec(),
                        Duration = template.Duration,
                        IsLooping = template.AutoLoop
                    };

                    return instance;
                }
                else
                {
                    var player2D = GetOrCreate2DPlayer(template.TemplateId);
                    player2D.Stream = stream;
                    player2D.Position = position;
                    player2D.VolumeDb = template.Volume + Volume;
                    player2D.PitchScale = template.PitchScale * PitchScale;

                    // 启动淡入效果
                    if (FadeInDuration > 0)
                    {
                        StartFadeIn(player2D, template.Volume + Volume);
                    }

                    player2D.Play();

                    var instance = new AudioInstance2D
                    {
                        Player = player2D,
                        TemplateId = template.TemplateId,
                        StartTime = Time.GetTicksMsec(),
                        Duration = template.Duration,
                        IsLooping = template.AutoLoop
                    };

                    return instance;
                }
            }
            catch (Exception ex)
            {
                Logger2.Error($"SkillAudioComponent: 创建音频实例失败 - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取或创建2D音频播放器
        /// </summary>
        private AudioStreamPlayer2D GetOrCreate2DPlayer(string templateId)
        {
            if (!_audioPlayers.ContainsKey(templateId))
            {
                var player = new AudioStreamPlayer2D();
                player.Name = $"AudioPlayer_{templateId}";
                player.Bus = "SFX";
                Entity.AddChild(player);
                
                // 连接完成事件
                player.Finished += () => OnAudioPlayerFinished(templateId);
                
                _audioPlayers[templateId] = player;
            }
            
            return _audioPlayers[templateId];
        }

        /// <summary>
        /// 获取或创建3D音频播放器
        /// </summary>
        private AudioStreamPlayer3D GetOrCreate3DPlayer(string templateId)
        {
            if (!_audioPlayers3D.ContainsKey(templateId))
            {
                var player = new AudioStreamPlayer3D();
                player.Name = $"AudioPlayer3D_{templateId}";
                player.Bus = "SFX";
                Entity.AddChild(player);
                
                // 连接完成事件
                player.Finished += () => OnAudioPlayerFinished(templateId);
                
                _audioPlayers3D[templateId] = player;
            }
            
            return _audioPlayers3D[templateId];
        }

        /// <summary>
        /// 音频播放器完成回调
        /// </summary>
        private void OnAudioPlayerFinished(string templateId)
        {
            // 查找对应的实例并移除
            for (int i = _activeAudioInstances.Count - 1; i >= 0; i--)
            {
                var instance = _activeAudioInstances[i];
                if (instance.TemplateId == templateId && !instance.IsLooping && !instance.IsContinuous)
                {
                    _activeAudioInstances.RemoveAt(i);
                    break;
                }
            }
        }

        #endregion

        #region 更新逻辑

        /// <summary>
        /// 更新活动音频
        /// </summary>
        private void UpdateActiveAudio(float delta)
        {
            for (int i = _activeAudioInstances.Count - 1; i >= 0; i--)
            {
                var instance = _activeAudioInstances[i];

                // 检查生命周期
                if (!instance.IsLooping && !instance.IsContinuous)
                {
                    ulong elapsed = Time.GetTicksMsec() - instance.StartTime;
                    if (elapsed >= instance.Duration * 1000)
                    {
                        if (FadeOutDuration > 0)
                        {
                            if (instance is AudioInstance2D instance2D)
                            {
                                StartFadeOut(instance2D.Player, instance);
                            }
                            else if (instance is AudioInstance3D instance3D)
                            {
                                StartFadeOut(instance3D.Player, instance);
                            }
                        }
                        else
                        {
                            FinishAudioInstance(instance);
                        }
                        _activeAudioInstances.RemoveAt(i);
                    }
                }
            }
        }

        #endregion
    }

    #region 数据结构

    /// <summary>
    /// 音频模板
    /// </summary>
    public class AudioTemplate
    {
        public string TemplateId { get; set; }
        public string AudioPath { get; set; }
        public float Volume { get; set; } = 0.0f; // dB
        public float PitchScale { get; set; } = 1.0f;
        public float Duration { get; set; } = 1.0f;
        public bool AutoLoop { get; set; } = false;
        public bool Use3DAudio { get; set; } = false;
        public AudioCategory Category { get; set; } = AudioCategory.SFX;
    }

    /// <summary>
    /// 音频实例基类
    /// </summary>
    public abstract class AudioInstanceBase
    {
        public string TemplateId { get; set; }
        public ulong StartTime { get; set; }
        public float Duration { get; set; }
        public bool IsLooping { get; set; } = false;
        public bool IsContinuous { get; set; } = false;
        
        public abstract Vector2 GetPosition();
        public abstract void Stop();
        public abstract void SetVolume(float volumeDb);
    }

    /// <summary>
    /// 2D音频实例
    /// </summary>
    public class AudioInstance2D : AudioInstanceBase
    {
        public AudioStreamPlayer2D Player { get; set; }
        
        public override Vector2 GetPosition() => Player?.Position ?? Vector2.Zero;
        public override void Stop() => Player?.Stop();
        public override void SetVolume(float volumeDb) 
        {
            if (Player != null) Player.VolumeDb = volumeDb;
        }
    }

    /// <summary>
    /// 3D音频实例
    /// </summary>
    public class AudioInstance3D : AudioInstanceBase
    {
        public AudioStreamPlayer3D Player { get; set; }
        
        public override Vector2 GetPosition() => Player != null ? new Vector2(Player.Position.X, Player.Position.Y) : Vector2.Zero;
        public override void Stop() => Player?.Stop();
        public override void SetVolume(float volumeDb) 
        {
            if (Player != null) Player.VolumeDb = volumeDb;
        }
    }

    /// <summary>
    /// 音频事件参数
    /// </summary>
    public class AudioEventArgs
    {
        public string TemplateId { get; set; }
        public AudioInstanceBase Instance { get; set; }
        public Vector2 Position { get; set; }
    }

    #endregion

    #region 枚举定义

    /// <summary>
    /// 音频类别
    /// </summary>
    public enum AudioCategory
    {
        SFX,        // 音效
        Voice,      // 语音
        Music,      // 音乐
        Ambient     // 环境音
    }

    #endregion
}