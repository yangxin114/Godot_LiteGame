using Godot;
using System;
using System.Threading.Tasks;
using Logs;

/// <summary>
/// 场景过渡效果管理器
/// 提供淡入淡出等视觉过渡效果
/// </summary>
public partial class SceneTransition : CanvasLayer
{
    // 过渡颜色（默认黑色）
    private Color _transitionColor = Colors.Black;
    
    // 过渡遮罩节点
    private ColorRect _transitionOverlay;
    
    // Tween动画控制器
    private Tween _currentTween;
    
    // 过渡状态标志
    private bool _isTransitioning = false;
    
    // 最小过渡时间（防止过快闪烁）
    private const float MIN_TRANSITION_TIME = 0.1f;

    public override void _Ready()
    {
        try
        {
            InitializeTransitionOverlay();
            Logger2.Info("SceneTransition: 初始化完成");
        }
        catch (Exception ex)
        {
            Logger2.Error($"SceneTransition: 初始化失败 - {ex.Message}");
            GD.PrintErr($"SceneTransition initialization failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 初始化过渡遮罩
    /// </summary>
    private void InitializeTransitionOverlay()
    {
        // 创建全屏遮罩
        _transitionOverlay = new ColorRect
        {
            Name = "TransitionOverlay",
            Color = _transitionColor,
            AnchorRight = 1,
            AnchorBottom = 1,
            Visible = false
        };
        
        AddChild(_transitionOverlay);
        
        // 设置为顶层显示
        Set("z_index", 1000);
    }

    /// <summary>
    /// 淡入效果（遮罩逐渐变 opaque）
    /// </summary>
    /// <param name="duration">过渡持续时间</param>
    public async Task FadeIn(float duration = 0.5f)
    {
        if (_isTransitioning)
        {
            Logger2.Warn("SceneTransition: 淡入操作已在进行中，跳过");
            return;
        }

        try
        {
            _isTransitioning = true;
            duration = Math.Max(duration, MIN_TRANSITION_TIME);
            
            Logger2.Debug($"SceneTransition: 开始淡入，持续时间 {duration}s");
            
            // 确保遮罩可见并重置透明度
            _transitionOverlay.Visible = true;
            _transitionOverlay.Color = new Color(_transitionColor, 0);
            
            // 停止之前的Tween
            if (_currentTween != null)
            {
                _currentTween.Kill();
                _currentTween = null;
            }
            
            // 创建新的Tween
            _currentTween = CreateTween();
            _currentTween.SetParallel(true);
            
            // 执行淡入动画
            _currentTween.TweenProperty(_transitionOverlay, "color:a", 1.0f, duration)
                      .SetTrans(Tween.TransitionType.Sine)
                      .SetEase(Tween.EaseType.Out);
            
            // 等待动画完成
            await ToSignal(_currentTween, "finished");
            
            Logger2.Debug("SceneTransition: 淡入完成");
        }
        catch (Exception ex)
        {
            Logger2.Error($"SceneTransition: 淡入失败 - {ex.Message}");
            GD.PrintErr($"FadeIn failed: {ex.Message}");
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    /// <summary>
    /// 淡出效果（遮罩逐渐变 transparent）
    /// </summary>
    /// <param name="duration">过渡持续时间</param>
    public async Task FadeOut(float duration = 0.5f)
    {
        if (_isTransitioning)
        {
            Logger2.Warn("SceneTransition: 淡出操作已在进行中，跳过");
            return;
        }

        try
        {
            _isTransitioning = true;
            duration = Math.Max(duration, MIN_TRANSITION_TIME);
            
            Logger2.Debug($"SceneTransition: 开始淡出，持续时间 {duration}s");
            
            // 确保遮罩可见
            _transitionOverlay.Visible = true;
            _transitionOverlay.Color = new Color(_transitionColor, 1.0f);
            
            // 停止之前的Tween
            if (_currentTween != null)
            {
                _currentTween.Kill();
                _currentTween = null;
            }
            
            // 创建新的Tween
            _currentTween = CreateTween();
            _currentTween.SetParallel(true);
            
            // 执行淡出动画
            _currentTween.TweenProperty(_transitionOverlay, "color:a", 0.0f, duration)
                      .SetTrans(Tween.TransitionType.Sine)
                      .SetEase(Tween.EaseType.In);
            
            // 等待动画完成
            await ToSignal(_currentTween, "finished");
            
            // 隐藏遮罩
            _transitionOverlay.Visible = false;
            
            Logger2.Debug("SceneTransition: 淡出完成");
        }
        catch (Exception ex)
        {
            Logger2.Error($"SceneTransition: 淡出失败 - {ex.Message}");
            GD.PrintErr($"FadeOut failed: {ex.Message}");
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    /// <summary>
    /// 自定义颜色的淡入效果
    /// </summary>
    /// <param name="color">过渡颜色</param>
    /// <param name="duration">过渡持续时间</param>
    public async Task FadeInWithColor(Color color, float duration = 0.5f)
    {
        _transitionColor = color;
        await FadeIn(duration);
    }

    /// <summary>
    /// 自定义颜色的淡出效果
    /// </summary>
    /// <param name="color">过渡颜色</param>
    /// <param name="duration">过渡持续时间</param>
    public async Task FadeOutWithColor(Color color, float duration = 0.5f)
    {
        _transitionColor = color;
        await FadeOut(duration);
    }

    /// <summary>
    /// 立即隐藏过渡遮罩
    /// </summary>
    public void HideImmediately()
    {
        try
        {
            // 停止当前Tween
            if (_currentTween != null)
            {
                _currentTween.Kill();
                _currentTween = null;
            }
            
            _transitionOverlay.Visible = false;
            _transitionOverlay.Color = new Color(_transitionColor, 0);
            _isTransitioning = false;
            
            Logger2.Debug("SceneTransition: 立即隐藏遮罩");
        }
        catch (Exception ex)
        {
            Logger2.Error($"SceneTransition: 立即隐藏失败 - {ex.Message}");
        }
    }

    /// <summary>
    /// 立即显示过渡遮罩
    /// </summary>
    /// <param name="alpha">透明度 (0-1)</param>
    public void ShowImmediately(float alpha = 1.0f)
    {
        try
        {
            // 停止当前Tween
            if (_currentTween != null)
            {
                _currentTween.Kill();
                _currentTween = null;
            }
            
            _transitionOverlay.Visible = true;
            _transitionOverlay.Color = new Color(_transitionColor, Mathf.Clamp(alpha, 0, 1));
            _isTransitioning = false;
            
            Logger2.Debug($"SceneTransition: 立即显示遮罩，透明度 {alpha}");
        }
        catch (Exception ex)
        {
            Logger2.Error($"SceneTransition: 立即显示失败 - {ex.Message}");
        }
    }

    /// <summary>
    /// 检查是否正在进行过渡
    /// </summary>
    public bool IsTransitioning => _isTransitioning;

    /// <summary>
    /// 获取当前过渡颜色
    /// </summary>
    public Color TransitionColor => _transitionColor;

    /// <summary>
    /// 设置过渡颜色
    /// </summary>
    /// <param name="color">新的过渡颜色</param>
    public void SetTransitionColor(Color color)
    {
        _transitionColor = color;
        if (_transitionOverlay.Visible)
        {
            var currentAlpha = _transitionOverlay.Color.A;
            _transitionOverlay.Color = new Color(color, currentAlpha);
        }
    }

    public override void _ExitTree()
    {
        try
        {
            // 清理Tween资源
            if (_currentTween != null)
            {
                _currentTween.Kill();
                _currentTween = null;
            }
            
            Logger2.Info("SceneTransition: 资源清理完成");
        }
        catch (Exception ex)
        {
            Logger2.Error($"SceneTransition: 清理失败 - {ex.Message}");
        }
    }
}