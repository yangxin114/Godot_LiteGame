using Godot;
using System;

/// <summary>
/// Godot 4.6 兼容的相机跟随系统
/// - 使用 Position 而非 GlobalPosition
/// - 关闭内置 Smoothing
/// - 使用 MakeCurrent()
/// - 正确使用世界 Limit
/// </summary>
public partial class CameraFollow : Camera2D
{
    [Export] private Node2D _target;

    /// <summary>
    /// 平滑速度（推荐 4~8）
    /// </summary>
    [Export] private float _followSpeed = 6f;

    [Export] private Vector2 _offset = Vector2.Zero;

    [Export] private bool _enableSmooth = true;

    [ExportGroup("World Boundary")]
    [Export] private int _limitLeft = -2000;
    [Export] private int _limitRight = 2000;
    [Export] private int _limitTop = -2000;
    [Export] private int _limitBottom = 2000;

    public override void _Ready()
    {
        // 确保成为当前相机（Godot 4 推荐）
        MakeCurrent();

        // 关闭 Godot 自带平滑，避免双重平滑
        PositionSmoothingEnabled = false;

        ApplyBoundary();

        TeleportToTarget();
    }

    public override void _Process(double delta)
    {
        if (_target == null)
            return;

        Vector2 desired = _target.GlobalPosition + _offset;

        if (_enableSmooth)
        {
            // 指数平滑（帧率无关）
            float t = 1f - Mathf.Exp(-_followSpeed * (float)delta);
            Position = Position.Lerp(desired, t);
        }
        else
        {
            Position = desired;
        }
    }

    /// <summary>
    /// 设置世界边界
    /// </summary>
    private void ApplyBoundary()
    {
        LimitLeft = _limitLeft;
        LimitRight = _limitRight;
        LimitTop = _limitTop;
        LimitBottom = _limitBottom;
    }

    /// <summary>
    /// 外部动态修改边界
    /// </summary>
    public void SetBoundary(int left, int right, int top, int bottom)
    {
        _limitLeft = left;
        _limitRight = right;
        _limitTop = top;
        _limitBottom = bottom;

        ApplyBoundary();
    }

    /// <summary>
    /// 设置目标
    /// </summary>
    public void SetTarget(Node2D target)
    {
        _target = target;
        TeleportToTarget();
    }

    /// <summary>
    /// 立即移动到目标
    /// </summary>
    public void TeleportToTarget()
    {
        if (_target == null)
            return;

        Position = _target.GlobalPosition + _offset;
    }
}
