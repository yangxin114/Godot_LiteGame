using Godot;
using System.Collections.Generic;

namespace Components
{
    using Entities;
    using Godot;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 2D通用动画组件
    /// 纯粹的动画播放控制器，不包含任何业务逻辑假设
    /// 适用于：所有需要动画的实体（玩家、敌人、投射物、技能效果、UI等）
    /// </summary>
    public class AnimationComponent2D : LogicComponent
    {
        #region 基础配置

        /// <summary>
        /// AnimationPlayer节点路径（相对于实体）
        /// </summary>
        public string AnimationPlayerPath { get; set; } = "AnimationPlayer";

        /// <summary>
        /// Sprite2D节点路径（用于翻转）
        /// </summary>
        public string SpritePath { get; set; } = "Sprite2D";

        /// <summary>
        /// AnimatedSprite2D节点路径
        /// </summary>
        public string AnimatedSpritePath { get; set; } = "AnimatedSprite2D";

        /// <summary>
        /// 动画系统类型
        /// </summary>
        public AnimationSystemType SystemType { get; set; } = AnimationSystemType.AnimationPlayer;

        #endregion

        #region 播放控制

        /// <summary>
        /// 默认播放速度
        /// </summary>
        public float DefaultPlaybackSpeed { get; set; } = 1.0f;

        /// <summary>
        /// 动画混合时间（秒）
        /// </summary>
        public float BlendTime { get; set; } = 0.1f;

        #endregion

        #region 私有字段

        private AnimationPlayer _animationPlayer;
        private AnimatedSprite2D _animatedSprite;
        private Sprite2D _sprite;

        private string _currentAnimationName = "";
        private string _queuedAnimationName = "";
        private float _animationLockTimer = 0f;
        private bool _isAnimationLocked = false;
        private int _loopCount = 0;

        // 动画事件回调
        private Dictionary<string, List<Action>> _animationEventCallbacks = new();

        // 动画完成回调（一次性）
        private Dictionary<string, Action> _oneShotFinishCallbacks = new();

        #endregion

        #region 公共属性

        /// <summary>
        /// 当前播放的动画名称
        /// </summary>
        public string CurrentAnimationName => _currentAnimationName;

        /// <summary>
        /// 是否正在播放动画
        /// </summary>
        public bool IsPlaying
        {
            get
            {
                return SystemType switch
                {
                    AnimationSystemType.AnimationPlayer => _animationPlayer?.IsPlaying() ?? false,
                    AnimationSystemType.AnimatedSprite => _animatedSprite?.IsPlaying() ?? false,
                    _ => false
                };
            }
        }

        /// <summary>
        /// 动画是否被锁定（不能被打断）
        /// </summary>
        public bool IsAnimationLocked => _isAnimationLocked;

        /// <summary>
        /// 当前动画播放进度（0-1）
        /// </summary>
        public float AnimationProgress
        {
            get
            {
                if (SystemType == AnimationSystemType.AnimationPlayer && _animationPlayer != null)
                {
                    var currentAnim = _animationPlayer.CurrentAnimation;
                    if (!string.IsNullOrEmpty(currentAnim))
                    {
                        var animLength = _animationPlayer.GetAnimation(currentAnim).Length;
                        return animLength > 0 ? (float)(_animationPlayer.CurrentAnimationPosition / animLength) : 0f;
                    }
                }
                else if (SystemType == AnimationSystemType.AnimatedSprite && _animatedSprite != null)
                {
                    var frameCount = _animatedSprite.SpriteFrames?.GetFrameCount(_animatedSprite.Animation) ?? 0;
                    return frameCount > 0 ? (float)_animatedSprite.Frame / frameCount : 0f;
                }
                return 0f;
            }
        }

        /// <summary>
        /// 当前动画循环次数
        /// </summary>
        public int LoopCount => _loopCount;

        /// <summary>
        /// Sprite2D引用（用于外部控制翻转等）
        /// </summary>
        public Sprite2D Sprite => _sprite;

        #endregion

        #region 事件

        /// <summary>
        /// 动画开始播放时触发（参数：动画名称）
        /// </summary>
        public event Action<string> OnAnimationStarted;

        /// <summary>
        /// 动画播放结束时触发（参数：动画名称）
        /// </summary>
        public event Action<string> OnAnimationFinished;

        /// <summary>
        /// 动画循环时触发（参数：动画名称，循环次数）
        /// </summary>
        public event Action<string, int> OnAnimationLooped;

        /// <summary>
        /// 动画被打断时触发（参数：被打断的动画，新动画）
        /// </summary>
        public event Action<string, string> OnAnimationInterrupted;

        /// <summary>
        /// 动画锁定状态改变时触发（参数：是否锁定）
        /// </summary>
        public event Action<bool> OnLockStateChanged;

        #endregion

        #region 生命周期

        public override void Initialize(CharacterEntity entity)
        {
            base.Initialize(entity);

            // 根据系统类型获取对应节点
            switch (SystemType)
            {
                case AnimationSystemType.AnimationPlayer:
                    _animationPlayer = entity.GetNodeOrNull<AnimationPlayer>(AnimationPlayerPath);
                    if (_animationPlayer != null)
                    {
                        _animationPlayer.AnimationFinished += OnAnimationPlayerFinished;
                    }
                    break;

                case AnimationSystemType.AnimatedSprite:
                    _animatedSprite = entity.GetNodeOrNull<AnimatedSprite2D>(AnimatedSpritePath);
                    if (_animatedSprite != null)
                    {
                        _animatedSprite.AnimationFinished += OnAnimatedSpriteFinished;
                        _animatedSprite.AnimationLooped += OnAnimatedSpriteLooped;
                    }
                    break;
            }

            // 获取Sprite2D
            if (!string.IsNullOrEmpty(SpritePath))
            {
                _sprite = entity.GetNodeOrNull<Sprite2D>(SpritePath);
            }
        }

        public override void Update(float delta)
        {
            if (!IsEnabled) return;

            // 更新动画锁定计时器
            if (_isAnimationLocked && _animationLockTimer > 0)
            {
                _animationLockTimer -= delta;
                if (_animationLockTimer <= 0)
                {
                    UnlockAnimation();

                    // 如果有排队的动画，播放它
                    if (!string.IsNullOrEmpty(_queuedAnimationName))
                    {
                        Play(_queuedAnimationName);
                        _queuedAnimationName = "";
                    }
                }
            }
        }

        #endregion

        #region 公共方法 - 播放控制

        /// <summary>
        /// 播放动画
        /// </summary>
        /// <param name="animationName">动画名称</param>
        /// <param name="forcePlay">是否强制播放（忽略锁定）</param>
        /// <param name="customSpeed">自定义播放速度（-1使用默认）</param>
        /// <param name="fromBeginning">是否从头播放（即使是相同动画）</param>
        public void Play(string animationName, bool forcePlay = false, float customSpeed = -1f, bool fromBeginning = false)
        {
            if (string.IsNullOrEmpty(animationName)) return;

            // 检查动画锁定
            if (_isAnimationLocked && !forcePlay)
            {
                _queuedAnimationName = animationName;
                return;
            }

            // 检查动画是否存在
            if (!HasAnimation(animationName))
            {
                GD.PrintErr($"动画不存在: {animationName}");
                return;
            }

            // 检查是否是相同动画
            if (_currentAnimationName == animationName && IsPlaying && !fromBeginning)
            {
                return;
            }

            // 记录被打断的动画
            if (IsPlaying && _currentAnimationName != animationName)
            {
                OnAnimationInterrupted?.Invoke(_currentAnimationName, animationName);
            }

            _currentAnimationName = animationName;
            _loopCount = 0;

            // 播放动画
            float speed = customSpeed > 0 ? customSpeed : DefaultPlaybackSpeed;

            switch (SystemType)
            {
                case AnimationSystemType.AnimationPlayer:
                    if (_animationPlayer != null)
                    {
                        _animationPlayer.Play(animationName, BlendTime, speed, fromBeginning);
                    }
                    break;

                case AnimationSystemType.AnimatedSprite:
                    if (_animatedSprite != null)
                    {
                        _animatedSprite.Play(animationName);
                        _animatedSprite.SpeedScale = speed;
                    }
                    break;
            }

            OnAnimationStarted?.Invoke(animationName);
        }

        /// <summary>
        /// 播放动画并在完成后回调（一次性）
        /// </summary>
        public void PlayWithCallback(string animationName, Action onFinished, bool forcePlay = false)
        {
            _oneShotFinishCallbacks[animationName] = onFinished;
            Play(animationName, forcePlay);
        }

        /// <summary>
        /// 停止当前动画
        /// </summary>
        /// <param name="reset">是否重置到第一帧</param>
        public void Stop(bool reset = true)
        {
            switch (SystemType)
            {
                case AnimationSystemType.AnimationPlayer:
                    _animationPlayer?.Stop(reset);
                    break;

                case AnimationSystemType.AnimatedSprite:
                    _animatedSprite?.Stop();
                    if (reset)
                    {
                        _animatedSprite.Frame = 0;
                    }
                    break;
            }

            _currentAnimationName = "";
            _isAnimationLocked = false;
            _animationLockTimer = 0f;
            _loopCount = 0;
        }

        /// <summary>
        /// 暂停当前动画
        /// </summary>
        public void Pause()
        {
            switch (SystemType)
            {
                case AnimationSystemType.AnimationPlayer:
                    if (_animationPlayer != null)
                    {
                        _animationPlayer.Pause();
                    }
                    break;

                case AnimationSystemType.AnimatedSprite:
                    _animatedSprite?.Pause();
                    break;
            }
        }

        /// <summary>
        /// 恢复播放
        /// </summary>
        public void Resume()
        {
            switch (SystemType)
            {
                case AnimationSystemType.AnimationPlayer:
                    if (_animationPlayer != null && _animationPlayer.CurrentAnimation != "")
                    {
                        _animationPlayer.Play();
                    }
                    break;

                case AnimationSystemType.AnimatedSprite:
                    if (_animatedSprite != null && _animatedSprite.Animation != "")
                    {
                        _animatedSprite.Play();
                    }
                    break;
            }
        }

        /// <summary>
        /// 设置动画播放速度
        /// </summary>
        public void SetPlaybackSpeed(float speed)
        {
            switch (SystemType)
            {
                case AnimationSystemType.AnimationPlayer:
                    if (_animationPlayer != null)
                    {
                        _animationPlayer.SpeedScale = speed;
                    }
                    break;

                case AnimationSystemType.AnimatedSprite:
                    if (_animatedSprite != null)
                    {
                        _animatedSprite.SpeedScale = speed;
                    }
                    break;
            }
        }

        /// <summary>
        /// 跳转到动画的指定位置
        /// </summary>
        /// <param name="position">位置（秒）</param>
        public void Seek(float position)
        {
            if (SystemType == AnimationSystemType.AnimationPlayer && _animationPlayer != null)
            {
                _animationPlayer.Seek(position);
            }
        }

        /// <summary>
        /// 跳转到动画的指定进度
        /// </summary>
        /// <param name="progress">进度（0-1）</param>
        public void SeekNormalized(float progress)
        {
            if (SystemType == AnimationSystemType.AnimationPlayer && _animationPlayer != null)
            {
                var currentAnim = _animationPlayer.CurrentAnimation;
                if (!string.IsNullOrEmpty(currentAnim))
                {
                    var animLength = _animationPlayer.GetAnimation(currentAnim).Length;
                    _animationPlayer.Seek(animLength * Mathf.Clamp(progress, 0f, 1f));
                }
            }
            else if (SystemType == AnimationSystemType.AnimatedSprite && _animatedSprite != null)
            {
                var frameCount = _animatedSprite.SpriteFrames?.GetFrameCount(_animatedSprite.Animation) ?? 0;
                _animatedSprite.Frame = Mathf.RoundToInt(frameCount * Mathf.Clamp(progress, 0f, 1f));
            }
        }

        #endregion

        #region 公共方法 - 动画锁定

        /// <summary>
        /// 锁定动画（在指定时间内不能被打断）
        /// </summary>
        /// <param name="duration">锁定时长（秒，0表示永久锁定）</param>
        public void LockAnimation(float duration = 0f)
        {
            bool wasLocked = _isAnimationLocked;
            _isAnimationLocked = true;
            _animationLockTimer = duration;

            if (!wasLocked)
            {
                OnLockStateChanged?.Invoke(true);
            }
        }

        /// <summary>
        /// 解锁动画
        /// </summary>
        public void UnlockAnimation()
        {
            bool wasLocked = _isAnimationLocked;
            _isAnimationLocked = false;
            _animationLockTimer = 0f;
            _queuedAnimationName = "";

            if (wasLocked)
            {
                OnLockStateChanged?.Invoke(false);
            }
        }

        /// <summary>
        /// 播放并锁定动画
        /// </summary>
        public void PlayAndLock(string animationName, float lockDuration = 0f, bool forcePlay = false)
        {
            Play(animationName, forcePlay);
            LockAnimation(lockDuration);
        }

        #endregion

        #region 公共方法 - 查询

        /// <summary>
        /// 检查动画是否存在
        /// </summary>
        public bool HasAnimation(string animationName)
        {
            if (string.IsNullOrEmpty(animationName)) return false;

            return SystemType switch
            {
                AnimationSystemType.AnimationPlayer => _animationPlayer?.HasAnimation(animationName) ?? false,
                AnimationSystemType.AnimatedSprite => _animatedSprite?.SpriteFrames?.HasAnimation(animationName) ?? false,
                _ => false
            };
        }

        /// <summary>
        /// 获取动画时长（秒）
        /// </summary>
        public float GetAnimationLength(string animationName)
        {
            if (SystemType == AnimationSystemType.AnimationPlayer && _animationPlayer != null)
            {
                if (_animationPlayer.HasAnimation(animationName))
                {
                    return (float)_animationPlayer.GetAnimation(animationName).Length;
                }
            }
            else if (SystemType == AnimationSystemType.AnimatedSprite && _animatedSprite != null)
            {
                if (_animatedSprite.SpriteFrames != null && _animatedSprite.SpriteFrames.HasAnimation(animationName))
                {
                    int frameCount = _animatedSprite.SpriteFrames.GetFrameCount(animationName);
                    float fps = (float)_animatedSprite.SpriteFrames.GetAnimationSpeed(animationName);
                    return frameCount / fps;
                }
            }

            return 0f;
        }

        /// <summary>
        /// 获取当前动画剩余时间
        /// </summary>
        public float GetRemainingTime()
        {
            if (SystemType == AnimationSystemType.AnimationPlayer && _animationPlayer != null)
            {
                var currentAnim = _animationPlayer.CurrentAnimation;
                if (!string.IsNullOrEmpty(currentAnim))
                {
                    var animLength = _animationPlayer.GetAnimation(currentAnim).Length;
                    return (float)(animLength - _animationPlayer.CurrentAnimationPosition);
                }
            }

            return 0f;
        }

        /// <summary>
        /// 获取所有可用的动画名称
        /// </summary>
        public List<string> GetAvailableAnimations()
        {
            List<string> animations = new();

            if (SystemType == AnimationSystemType.AnimationPlayer && _animationPlayer != null)
            {
                var animList = _animationPlayer.GetAnimationList();
                foreach (var anim in animList)
                {
                    animations.Add(anim);
                }
            }
            else if (SystemType == AnimationSystemType.AnimatedSprite && _animatedSprite != null)
            {
                if (_animatedSprite.SpriteFrames != null)
                {
                    var animNames = _animatedSprite.SpriteFrames.GetAnimationNames();
                    foreach (var anim in animNames)
                    {
                        animations.Add(anim);
                    }
                }
            }

            return animations;
        }

        #endregion

        #region 公共方法 - 动画事件

        /// <summary>
        /// 注册动画事件回调
        /// （需要在AnimationPlayer的动画轨道中调用 TriggerAnimationEvent）
        /// </summary>
        public void RegisterAnimationEvent(string eventName, Action callback)
        {
            if (!_animationEventCallbacks.ContainsKey(eventName))
            {
                _animationEventCallbacks[eventName] = new List<Action>();
            }

            _animationEventCallbacks[eventName].Add(callback);
        }

        /// <summary>
        /// 取消注册动画事件
        /// </summary>
        public void UnregisterAnimationEvent(string eventName, Action callback)
        {
            if (_animationEventCallbacks.TryGetValue(eventName, out var callbacks))
            {
                callbacks.Remove(callback);
            }
        }

        /// <summary>
        /// 清除所有动画事件回调
        /// </summary>
        public void ClearAnimationEvents()
        {
            _animationEventCallbacks.Clear();
        }

        /// <summary>
        /// 触发动画事件（由AnimationPlayer的动画轨道调用）
        /// 使用方法：在Godot编辑器中，在AnimationPlayer的动画中添加Call Method轨道，
        /// 调用此方法并传入事件名称
        /// </summary>
        public void TriggerAnimationEvent(string eventName)
        {
            if (_animationEventCallbacks.TryGetValue(eventName, out var callbacks))
            {
                foreach (var callback in callbacks)
                {
                    try
                    {
                        callback?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        GD.PrintErr($"动画事件 {eventName} 回调执行失败: {ex.Message}");
                    }
                }
            }
        }

        #endregion

        #region 公共方法 - Sprite控制

        /// <summary>
        /// 设置精灵水平翻转
        /// </summary>
        public void SetSpriteFlipH(bool flip)
        {
            if (_sprite != null)
            {
                _sprite.FlipH = flip;
            }
        }

        /// <summary>
        /// 设置精灵垂直翻转
        /// </summary>
        public void SetSpriteFlipV(bool flip)
        {
            if (_sprite != null)
            {
                _sprite.FlipV = flip;
            }
        }

        /// <summary>
        /// 切换精灵水平翻转
        /// </summary>
        public void ToggleSpriteFlipH()
        {
            if (_sprite != null)
            {
                _sprite.FlipH = !_sprite.FlipH;
            }
        }

        /// <summary>
        /// 获取精灵是否水平翻转
        /// </summary>
        public bool IsSpriteFlippedH()
        {
            return _sprite?.FlipH ?? false;
        }

        /// <summary>
        /// 设置精灵可见性
        /// </summary>
        public void SetSpriteVisible(bool visible)
        {
            if (_sprite != null)
            {
                _sprite.Visible = visible;
            }
            else if (_animatedSprite != null)
            {
                _animatedSprite.Visible = visible;
            }
        }

        #endregion

        #region 事件回调

        private void OnAnimationPlayerFinished(StringName animName)
        {
            string animationName = animName.ToString();
            OnAnimationFinished?.Invoke(animationName);

            // 触发一次性完成回调
            if (_oneShotFinishCallbacks.TryGetValue(animationName, out var callback))
            {
                try
                {
                    callback?.Invoke();
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"动画完成回调执行失败: {ex.Message}");
                }
                _oneShotFinishCallbacks.Remove(animationName);
            }

            // 动画结束后自动解锁（如果锁定时长为0，表示播放完就解锁）
            if (_isAnimationLocked && _animationLockTimer <= 0)
            {
                UnlockAnimation();
            }
        }

        private void OnAnimatedSpriteFinished()
        {
            if (_animatedSprite != null)
            {
                string animationName = _animatedSprite.Animation.ToString();
                OnAnimationFinished?.Invoke(animationName);

                if (_oneShotFinishCallbacks.TryGetValue(animationName, out var callback))
                {
                    try
                    {
                        callback?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        GD.PrintErr($"动画完成回调执行失败: {ex.Message}");
                    }
                    _oneShotFinishCallbacks.Remove(animationName);
                }

                if (_isAnimationLocked && _animationLockTimer <= 0)
                {
                    UnlockAnimation();
                }
            }
        }

        private void OnAnimatedSpriteLooped()
        {
            if (_animatedSprite != null)
            {
                _loopCount++;
                OnAnimationLooped?.Invoke(_animatedSprite.Animation.ToString(), _loopCount);
            }
        }

        #endregion

        public override void Cleanup()
        {
            base.Cleanup();

            if (_animationPlayer != null)
            {
                _animationPlayer.AnimationFinished -= OnAnimationPlayerFinished;
            }

            if (_animatedSprite != null)
            {
                _animatedSprite.AnimationFinished -= OnAnimatedSpriteFinished;
                _animatedSprite.AnimationLooped -= OnAnimatedSpriteLooped;
            }

            _animationEventCallbacks.Clear();
            _oneShotFinishCallbacks.Clear();
        }
    }

    #region 枚举定义

    /// <summary>
    /// 动画系统类型
    /// </summary>
    public enum AnimationSystemType
    {
        /// <summary>
        /// 使用AnimationPlayer
        /// </summary>
        AnimationPlayer,

        /// <summary>
        /// 使用AnimatedSprite2D
        /// </summary>
        AnimatedSprite
    }

    #endregion
}