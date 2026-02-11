using Godot;
using System;
using Logs;

namespace Characters
{
    /// <summary>
    /// 测试Player移动翻转功能的脚本
    /// 用于验证修复后的翻转逻辑是否正常工作
    /// </summary>
    public partial class TestFlipFunctionality : Node
    {
        private Player _player;
        private PlayerAnimationSystem _animationSystem;
        private Label _debugLabel;
        
        public override void _Ready()
        {
            // 查找场景中的Player节点
            _player = GetTree().Root.GetNode<Player>("Main/Player");
            if (_player != null)
            {
                _animationSystem = _player.AnimationSystem;
                Logger2.Info("TestFlipFunctionality: 找到Player节点");
            }
            else
            {
                Logger2.Warn("TestFlipFunctionality: 未找到Player节点");
            }
            
            // 创建调试标签
            _debugLabel = new Label();
            _debugLabel.Position = new Vector2(10, 10);
            _debugLabel.Size = new Vector2(400, 200);
            _debugLabel.AddThemeFontSizeOverride("font_size", 14);
            AddChild(_debugLabel);
        }
        
        public override void _Process(double delta)
        {
            if (_player == null || _animationSystem == null) return;
            
            // 获取当前状态信息
            string currentState = _player.GetCurrentState();
            Vector2 moveDirection = _player.InputHandler?.MoveDirection ?? Vector2.Zero;
            bool flipH = false;
            bool flipV = false;
            
            // 尝试获取AnimatedSprite2D的翻转状态
            var animatedSprite = _player.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
            if (animatedSprite != null)
            {
                flipH = animatedSprite.FlipH;
                flipV = animatedSprite.FlipV;
            }
            
            // 更新调试信息
            string debugInfo = $"状态: {currentState}\n" +
                              $"移动方向: ({moveDirection.X:F2}, {moveDirection.Y:F2})\n" +
                              $"水平翻转: {flipH}\n" +
                              $"垂直翻转: {flipV}\n" +
                              $"当前动画: {_animationSystem.CurrentAnimation}\n" +
                              $"动画播放中: {_animationSystem.IsPlaying}\n" +
                              $"系统架构: AnimationSystem负责核心更新逻辑";
            
            _debugLabel.Text = debugInfo;
        }
        
        /// <summary>
        /// 手动测试翻转功能
        /// </summary>
        public void TestManualFlip()
        {
            if (_animationSystem == null) return;
            
            Logger2.Info("TestFlipFunctionality: 开始手动翻转测试");
            
            // 测试不同的翻转组合
            var testDirections = new[]
            {
                new Vector2(1, 0),   // 右
                new Vector2(-1, 0),  // 左
                new Vector2(0, 1),   // 下
                new Vector2(0, -1),  // 上
                new Vector2(1, 1),   // 右下
                new Vector2(-1, -1)  // 左上
            };
            
            foreach (var direction in testDirections)
            {
                _animationSystem.AutoFlip(direction);
                Logger2.Debug("TestFlipFunctionality: 测试方向 ({0:F1}, {1:F1}) - FlipH: {2}, FlipV: {3}",
                    direction.X, direction.Y, 
                    _player.GetNode<AnimatedSprite2D>("AnimatedSprite2D").FlipH,
                    _player.GetNode<AnimatedSprite2D>("AnimatedSprite2D").FlipV);
            }
        }
        
        /// <summary>
        /// 测试AnimationSystem的UpdateAnimation方法
        /// </summary>
        public void TestAnimationSystemUpdate()
        {
            if (_animationSystem == null || _player == null) return;
            
            Logger2.Info("TestFlipFunctionality: 测试AnimationSystem.UpdateAnimation方法");
            
            // 模拟不同的状态和移动方向组合
            var testCases = new[]
            {
                new { State = "idle", Direction = Vector2.Zero },
                new { State = "move", Direction = new Vector2(1, 0) },
                new { State = "move", Direction = new Vector2(0, 1) },
                new { State = "move", Direction = new Vector2(-1, -1) },
                new { State = "attack", Direction = Vector2.Zero }
            };
            
            foreach (var testCase in testCases)
            {
                _animationSystem.UpdateAnimation(testCase.State, testCase.Direction);
                Logger2.Debug("TestFlipFunctionality: 状态'{0}' 方向({1:F1},{2:F1}) -> 动画'{3}'", 
                    testCase.State, testCase.Direction.X, testCase.Direction.Y,
                    _animationSystem.CurrentAnimation);
            }
        }
    }
}