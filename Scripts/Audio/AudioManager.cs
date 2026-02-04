using System;
using System.Collections.Generic;
using Godot;
using Logs;

namespace Audio
{
    /// <summary>
    /// 简易的音频管理器（Godot + C#）。
    /// 功能：
    /// - 管理背景音乐（BGM）播放、淡入/淡出、切换；
    /// - 管理音效（SFX）播放，并使用池化复用 AudioStreamPlayer 以减少节点创建开销；
    /// - 支持按组（Bus）控制音量（BGM/SFX/Master）；
    /// - 支持资源预加载与释放；
    /// - 提供全局静音/恢复接口。
    /// 
    /// 使用方式：将此脚本作为 Autoload（单例）或在 Start 中创建并加入场景树。
    /// </summary>
    public partial class AudioManager : Node
    {
        /// <summary>
        /// 单例实例引用（当此节点被加入到场景树时自动设置）。
        /// 建议将此节点作为 Autoload 或由 `Start` 在启动时创建并加入到根节点。
        /// 使用时通过 `Audio.AudioManager.Instance` 访问。
        /// </summary>
        public static AudioManager Instance { get; private set; }

        // 音效播放器池（可增长）
        private readonly Queue<AudioStreamPlayer> _sfxPool = new Queue<AudioStreamPlayer>();
        private readonly List<SfxInstance> _activeSfx = new List<SfxInstance>();

        // 双 BGM 播放器用于交叉淡入淡出
        private AudioStreamPlayer _bgmA;
        private AudioStreamPlayer _bgmB;
        private bool _bgmAActive = true; // 指示当前主播放器

        // 资源缓存
        private readonly Dictionary<string, AudioStream> _cache = new Dictionary<string, AudioStream>();

        // 池大小配置
        [Export]
        public int InitialPoolSize = 6;

        [Export]
        public int MaxPoolSize = 16;

        // 构造/初始化
        public override void _Ready()
        {
            // 如果已有实例，记录警告（避免不经意创建多个实例）
            if (Instance != null && Instance != this)
            {
                Logs.Logger2.Warn("AudioManager: 已存在另一个实例。新的实例将覆盖全局 Instance 引用。");
            }
            Instance = this;

            // 创建双 BGM 播放器
            _bgmA = new AudioStreamPlayer { Name = "BGM_A", Bus = "BGM" };
            _bgmB = new AudioStreamPlayer { Name = "BGM_B", Bus = "BGM" };
            AddChild(_bgmA);
            AddChild(_bgmB);

            // 初始化 SFX 池
            for (int i = 0; i < InitialPoolSize; i++)
            {
                var p = CreateSfxPlayer(i);
                _sfxPool.Enqueue(p);
            }
        }

        /// <summary>
        /// 播放 BGM，并支持交叉淡入淡出（如果已有音乐在播放）。
        /// 将在备用播放器上启动新音乐并交叉淡入。
        /// </summary>
        public void CrossfadeBGM(object pathOrStream, float duration = 1.0f, bool loop = true)
        {
            var stream = ResolveStream(pathOrStream);
            if (stream == null) return;

            var target = _bgmAActive ? _bgmB : _bgmA;
            var source = _bgmAActive ? _bgmA : _bgmB;

            target.Stream = stream;
            //target.Stream.Loop = loop;
            target.VolumeDb = -80f;
            target.Play();

            // 交叉淡入淡出：target 从 -80 -> 0，source 从 current -> -80，然后停止 source
            TweenFade(target, 0f, duration);
            TweenFade(source, -80f, duration, stopAfterFade: true);

            _bgmAActive = !_bgmAActive;
        }

        /// <summary>
        /// 直接停止所有 BGM（可带淡出）。
        /// </summary>
        public void StopAllBGM(float fadeOut = 1.0f)
        {
            TweenFade(_bgmA, -80f, fadeOut, stopAfterFade: true);
            TweenFade(_bgmB, -80f, fadeOut, stopAfterFade: true);
        }

        /// <summary>
        /// SFX 实例信息，用于记录优先级与对应播放器。
        /// </summary>
        private class SfxInstance
        {
            public AudioStreamPlayer Player;
            public int Priority;
        }

        /// <summary>
        /// 播放音效，支持优先级与池扩展策略。
        /// priority：数值越大优先级越高。
        /// 如果没有可用播放器且已达 MaxPoolSize，则会根据优先级决定是否中断最低优先级音效。
        /// </summary>
        public void PlaySFX(object pathOrStream, float volumeDb = 0f, int priority = 0)
        {
            var stream = ResolveStream(pathOrStream);
            if (stream == null) return;

            AudioStreamPlayer player = null;

            if (_sfxPool.Count > 0)
            {
                player = _sfxPool.Dequeue();
            }
            else
            {
                // 池空：若未达到 MaxPoolSize，则创建新播放器；否则尝试抢占最低优先级
                var total = _sfxPool.Count + _activeSfx.Count;
                if (total < MaxPoolSize)
                {
                    player = CreateSfxPlayer(total);
                }
                else
                {
                    // 找到最低优先级
                    SfxInstance lowest = null;
                    foreach (var inst in _activeSfx)
                    {
                        if (lowest == null || inst.Priority < lowest.Priority) lowest = inst;
                    }

                    if (lowest != null && lowest.Priority < priority)
                    {
                        // 中断低优先级音效并复用播放器
                        lowest.Player.Stop();
                        player = lowest.Player;
                        _activeSfx.Remove(lowest);
                    }
                    else
                    {
                        // 无可用播放器且优先级不够，跳过播放
                        return;
                    }
                }
            }

            player.Stream = stream;
            player.VolumeDb = volumeDb;
            player.Bus = "SFX";
            player.Play();

            var instNew = new SfxInstance { Player = player, Priority = priority };
            _activeSfx.Add(instNew);
            // 绑定完成回调（通过 bind 传入 player 引用）
            player.Connect("finished", Callable.From(() => OnSfxFinished(player)));
        }

        private void OnSfxFinished(AudioStreamPlayer player)
        {
            // 查找对应的实例并回收
            SfxInstance found = null;
            foreach (var inst in _activeSfx)
            {
                if (inst.Player == player)
                {
                    found = inst;
                    break;
                }
            }
            if (found != null)
            {
                _activeSfx.Remove(found);
                if (_sfxPool.Count < InitialPoolSize)
                {
                    // 回收到初始池
                    _sfxPool.Enqueue(player);
                }
                else
                {
                    // 若初始池已满，则检查池总量是否小于 MaxPoolSize，再决定保留或释放
                    var total = _sfxPool.Count + _activeSfx.Count;
                    if (total < MaxPoolSize)
                    {
                        _sfxPool.Enqueue(player);
                    }
                    else
                    {
                        player.QueueFree();
                    }
                }
            }
            else
            {
                // 未找到对应项（可能是临时创建的），直接尝试回收
                if (_sfxPool.Count + _activeSfx.Count < MaxPoolSize)
                {
                    _sfxPool.Enqueue(player);
                }
                else
                {
                    player.QueueFree();
                }
            }
        }

        /// <summary>
        /// 创建并初始化一个新的 AudioStreamPlayer 用于 SFX。
        /// </summary>
        private AudioStreamPlayer CreateSfxPlayer(int index)
        {
            var p = new AudioStreamPlayer();
            p.Name = "SFXPlayer" + index;
            p.Bus = "SFX";
            AddChild(p);
            return p;
        }

        /// <summary>
        /// 将路径或已加载流解析为 AudioStream，并缓存已加载资源以便复用。
        /// 支持 string（路径）或 AudioStream 直接传入。
        /// </summary>
        private AudioStream ResolveStream(object pathOrStream)
        {
            if (pathOrStream == null) return null;
            if (pathOrStream is AudioStream s) return s;
            if (pathOrStream is string path)
            {
                if (_cache.TryGetValue(path, out var cached)) return cached;
                var res = GD.Load<AudioStream>(path);
                if (res != null)
                {
                    _cache[path] = res;
                }
                else
                {
                    GD.PrintErr($"AudioManager: 无法加载音频资源：{path}");
                }
                return res;
            }
            GD.PrintErr("AudioManager: 不支持的音频资源类型");
            return null;
        }

        /// <summary>
        /// 简单的淡入/淡出实现，使用 Tween 对 VolumeDb 做线性插值。
        /// </summary>
        private void TweenFade(AudioStreamPlayer player, float targetDb, float duration, bool stopAfterFade = false)
        {
            if (player == null) return;
            var tween = GetTree().CreateTween();
            float from = player.VolumeDb;
            tween.TweenProperty(player, "volume_db", targetDb, duration);
            tween.TweenCallback(Callable.From(player.QueueFree));

        }
        
        /// <summary>
        /// 设置总线音量（以分贝为单位）。
        /// busName 示例："Master", "SFX", "BGM"。请确保在 Project -> Audio -> Buses 中已创建这些 Bus。
        /// </summary>
        public void SetBusVolume(string busName, float volumeDb)
        {
            var idx = AudioServer.GetBusIndex(busName);
            if (idx < 0)
            {
                GD.PrintErr($"AudioManager: 找不到音频总线 {busName}");
                return;
            }
            AudioServer.SetBusVolumeDb(idx, volumeDb);
        }

        /// <summary>
        /// 全局静音开关（切换 Master 总线静音状态）。
        /// </summary>
        public void SetMute(bool mute)
        {
            var idx = AudioServer.GetBusIndex("Master");
            if (idx < 0) return;
            AudioServer.SetBusMute(idx, mute);
        }

        /// <summary>
        /// 释放缓存的音频资源（可在场景切换或内存紧张时调用）。
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
            // 资源会在没有引用时由 Godot 回收
        }
    }
}
