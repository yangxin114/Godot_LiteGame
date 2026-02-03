using System.Threading.Tasks;
using Godot;

namespace Scenes
{
    /// <summary>
    /// 简单的场景切换过渡控件（全屏遮罩淡入/淡出）。
    /// 使用方法：将此节点挂到最顶层 CanvasLayer，调用 FadeIn/FadeOut 并等待完成。
    /// </summary>
    public partial class SceneTransition : Control
    {
        private ColorRect _mask;

        public override void _Ready()
        {
            // 创建一个全屏 ColorRect 作为遮罩
            _mask = GetNodeOrNull<ColorRect>("Mask");
            if (_mask == null)
            {
                _mask = new ColorRect();
                _mask.Name = "Mask";
                _mask.Color = new Color(0, 0, 0, 0);
                _mask.AnchorLeft = 0;
                _mask.AnchorTop = 0;
                _mask.AnchorRight = 1;
                _mask.AnchorBottom = 1;
                AddChild(_mask);
            }
            // 初始不可见
            Visible = false;
        }

        /// <summary>
        /// 淡入（遮罩透明 -> 不透明）。
        /// </summary>
        public async Task FadeIn(float duration = 0.5f)
        {
            Visible = true;
            var tween = GetTree().CreateTween();
            tween.TweenProperty(_mask, "color:a", 1.0f, duration);
            await ToSignal(tween, "tween_all_completed");
            tween.Kill();
        }

        /// <summary>
        /// 淡出（遮罩不透明 -> 透明）。完成后隐藏节点。
        /// </summary>
        public async Task FadeOut(float duration = 0.5f)
        {
            var tween = GetTree().CreateTween();
            tween.TweenProperty(_mask, "color:a", 0.0f, duration);
            await ToSignal(tween, "tween_all_completed");
            tween.Kill(); // 修复：使用Kill()而不是QueueFree()
            Visible = false;
        }
    }
}
