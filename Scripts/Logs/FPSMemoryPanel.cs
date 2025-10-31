using System;
using Godot;

namespace Logs
{
    /// <summary>
    /// 屏幕性能面板：在游戏运行时显示 FPS 与内存使用情况，方便开发/调试。
    /// 使用方法：将此脚本绑定到一个 Control 节点，或在运行时动态创建并加入到 UI 层（建议加入 CanvasLayer）。
    /// 键位：默认使用 F1 切换可见性。
    /// 注意：本面板为简单调试工具，生产版本应可选编译或通过配置关闭。
    /// </summary>
    public class FPSMemoryPanel : Control
    {
        [Export]
        public bool StartVisible = true;

        // 用于展示文本的 Label（如果场景中已有名为 Label 的子节点会重用它）
        private Label _label;
        // 更新间隔（秒），避免每帧都更新导致开销
        private float _updateInterval = 0.5f;
        private float _acc = 0f;

        public override void _Ready()
        {
            Visible = StartVisible;
            // 如果场景中没有 Label，则动态创建一个
            _label = GetNodeOrNull<Label>("Label");
            if (_label == null)
            {
                _label = new Label();
                AddChild(_label);
            }

            // 简单样式设置：可根据项目 UI 风格替换为自定义字体/背景
            margin_right = 200;
            margin_bottom = 60;
            _label.Align = Label.AlignEnum.Left;
            _label.Valign = Label.VAlign.Top;
            _label.AddColorOverride("font_color", new Color(1,1,1));
        }

        public override void _Process(float delta)
        {
            _acc += delta;
            if (_acc >= _updateInterval)
            {
                var fps = Engine.GetFramesPerSecond();
                var mem = OS.GetStaticMemoryUsage();
                var peak = OS.GetStaticMemoryPeakUsage();
                _label.Text = $"FPS: {fps}\nMem: {mem} bytes\nPeak: {peak} bytes";
                _acc = 0f;
            }

            // 按键切换显示状态（简单去抖动处理）
            if (Input.IsKeyPressed((int)KeyList.F1))
            {
                Visible = !Visible;
                // 注意：这里的去抖处理非常简单，若需要更精确的按键事件处理应使用 Input.IsActionJustPressed
            }
        }
    }
}
