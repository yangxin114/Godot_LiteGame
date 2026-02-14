using Godot;
using System;
using Logs;

namespace Characters
{
    /// <summary>
    /// 简单的翻转功能测试脚本
    /// 用于验证玩家左右移动时的精灵翻转是否正常工作
    /// </summary>
    public partial class FlipTest : Node
    {
        private Player _player;
        private Label _statusLabel;
        
        public override void _Ready()
        {
            // 查找场景中的Player节点
            _player = GetTree().Root.GetNodeOrNull<Player>("Main/Player");
            
            if (_player == null)
            {
                Logger2.Warn("FlipTest: 未找到Player节点");
                return;
            }
            
            Logger2.Info("FlipTest: 找到Player节点，开始测试翻转功能");
            
            // 创建状态显示标签
            CreateStatusLabel();
            
            // 开始测试
            TestFlipFunctionality();
        }
        
        private void CreateStatusLabel()
        {
            _statusLabel = new Label();
            _statusLabel.Position = new Vector2(10, 10);
            _statusLabel.Size = new Vector2(400, 150);
            _statusLabel.AddThemeFontSizeOverride("font_size", 14);
            _statusLabel.AddThemeColorOverride("font_color", Colors.White);
            _statusLabel.AddThemeConstantOverride("outline_size", 2);
            _statusLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
            AddChild(_statusLabel);
        }
        
        public override void _Process(double delta)
        {
            if (_player == null || _statusLabel == null) return;
            
            // 实时显示玩家状态
            UpdateStatusDisplay();
        }
        
        private void UpdateStatusDisplay()
        {
            if (_player?.InputComponent == null || _player?.AnimationComponent == null) return;
            
            Vector2 moveDirection = _player.InputComponent.MoveDirection;
            bool isFlipped = _player.AnimationComponent.IsSpriteFlippedH();
            string currentState = _player.GetCurrentState() ?? "Unknown";
            
            string statusText = $"状态: {currentState}\n" +
                               $"移动方向: ({moveDirection.X:F2}, {moveDirection.Y:F2})\n" +
                               $"水平翻转: {isFlipped}\n" +
                               $"正在移动: {_player.IsMoving()}\n" +
                               $"当前动画: {_player.AnimationComponent.CurrentAnimationName}";
            
            _statusLabel.Text = statusText;
        }
        
        /// <summary>
        /// 测试翻转功能
        /// </summary>
        private async void TestFlipFunctionality()
        {
            if (_player == null) return;
            
            Logger2.Info("FlipTest: 开始自动翻转测试");
            
            // 测试向右移动
            Logger2.Debug("FlipTest: 测试向右移动");
            SimulateMovement(new Vector2(1, 0));
            await ToSignal(GetTree().CreateTimer(1.0), "timeout");
            
            // 测试向左移动
            Logger2.Debug("FlipTest: 测试向左移动");
            SimulateMovement(new Vector2(-1, 0));
            await ToSignal(GetTree().CreateTimer(1.0), "timeout");
            
            // 测试停止移动
            Logger2.Debug("FlipTest: 测试停止移动");
            SimulateMovement(Vector2.Zero);
            await ToSignal(GetTree().CreateTimer(1.0), "timeout");
            
            Logger2.Info("FlipTest: 自动测试完成，请手动测试键盘控制");
        }
        
        /// <summary>
        /// 模拟移动输入
        /// </summary>
        private void SimulateMovement(Vector2 direction)
        {
            if (_player?.InputComponent == null) return;
            
            // 使用输入组件的模拟方法
            _player.InputComponent.SimulateMovement(direction);
        }
    }
}