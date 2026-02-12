using Godot;
using Logs;
using System;

/// <summary>
/// 高级相机跟随系统，带平滑跟随和边界限制
/// </summary>
public partial class CameraFollow : Camera2D
{
    [Export] private Node2D _target;
    [Export] private float _followSmoothness = 0.1f;
    [Export] private Vector2 _offset = Vector2.Zero;  // 相机相对于目标的偏移

    // 边界设置 - 这些是世界坐标中的边界
    [Export] private float _leftBoundary = 0f;
    [Export] private float _rightBoundary = 100f;
    [Export] private float _topBoundary = 0f;
    [Export] private float _bottomBoundary = 100f;

    // 是否启用平滑跟随
    [Export] private bool _smoothFollowEnabled = true;

    // 是否启用边界限制
    [Export] private bool _boundaryLimitEnabled = true;

    public override void _Ready()
    {
        base._Ready();

        if (_target == null)
        {
            GD.PrintErr("Camera target is not set!");
            return;
        }

        // 立即将相机定位到玩家中心位置
        TeleportToTarget();
        
        // 设置相机的内置边界（仅用于启用 Godot 的内置边界系统）
        UpdateCameraLimits();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_target == null) return;

        // 计算期望的相机位置
        Vector2 desiredPosition = _target.GlobalPosition + _offset;

        // 如果启用边界限制，则限制位置
        if (_boundaryLimitEnabled)
        {
            //desiredPosition = ConstrainToBounds(desiredPosition);
        }

        // 根据是否启用平滑跟随决定如何移动相机
        if (_smoothFollowEnabled)
        {
            // 平滑跟随
            GlobalPosition = GlobalPosition.Lerp(desiredPosition, _followSmoothness);
        }
        else
        {
            // 瞬时跟随
            GlobalPosition = desiredPosition;
        }
		Logger2.Info( "Camera Position: {0}", GlobalPosition );
		Logger2.Info( "Camera Target Position: {0}", _target.GlobalPosition );
    }

    /// <summary>
    /// 将位置限制在边界内
    /// </summary>
    private Vector2 ConstrainToBounds(Vector2 position)
    {
        // 直接使用我们定义的世界坐标边界
        return new Vector2(
            Mathf.Clamp(position.X, _leftBoundary, _rightBoundary),
            Mathf.Clamp(position.Y, _topBoundary, _bottomBoundary)
        );
    }

    /// <summary>
    /// 更新相机的内置边界限制（用于 Godot 的内置边界系统）
    /// </summary>
    private void UpdateCameraLimits()
    {
        // 设置 Godot 的内置边界（像素单位），这些值会在相机移动时起作用
        // 注意：这些是相对当前相机位置的偏移，不是世界坐标
        LimitLeft = (int)(_leftBoundary - GlobalPosition.X);
        LimitRight = (int)(_rightBoundary - GlobalPosition.X);
        LimitTop = (int)(_topBoundary - GlobalPosition.Y);
        LimitBottom = (int)(_bottomBoundary - GlobalPosition.Y);
    }

    /// <summary>
    /// 动态设置边界
    /// </summary>
    public void SetBoundaries(float left, float right, float top, float bottom)
    {
        _leftBoundary = left;
        _rightBoundary = right;
        _topBoundary = top;
        _bottomBoundary = bottom;

        // 如果相机已经存在，更新其限制
        if (IsInsideTree())
        {
            UpdateCameraLimits();
        }
    }

    /// <summary>
    /// 设置跟随目标
    /// </summary>
    public void SetTarget(Node2D target)
    {
        _target = target;
    }

    /// <summary>
    /// 设置相机偏移
    /// </summary>
    public void SetOffset(Vector2 offset)
    {
        _offset = offset;
    }

    /// <summary>
    /// 立即移动到目标位置
    /// </summary>
    public void TeleportToTarget()
    {
        if (_target != null)
        {
            Vector2 targetPos = _target.GlobalPosition + _offset;

            if (_boundaryLimitEnabled)
            {
                targetPos = ConstrainToBounds(targetPos);
            }

            GlobalPosition = targetPos;
            
            // 更新内置边界
            UpdateCameraLimits();
        }
    }
    
    /// <summary>
    /// 当相机位置改变时更新边界限制
    /// </summary>
    public override void _Notification(int what)
    {
        if (what == NotificationTransformChanged)
        {
            // 如果启用了边界限制，更新 Godot 内置边界
            if (_boundaryLimitEnabled && IsInsideTree())
            {
                UpdateCameraLimits();
            }
        }
    }
}