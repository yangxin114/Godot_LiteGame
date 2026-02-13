using Godot;
using System;
using Logs;
using Entities;
using Components;

namespace Test
{
    /// <summary>
    /// InputHandlerComponent测试场景
    /// 验证输入组件的各项功能
    /// </summary>
    public partial class InputComponentTest : Node2D
    {
        private CharacterEntity _testEntity;
        private InputHandlerComponent _inputComponent;
        private Label _infoLabel;
        private Timer _testTimer;

        public override void _Ready()
        {
            SetupTestEnvironment();
            CreateTestEntity();
            SetupUITest();
            StartTests();
        }

        private void SetupTestEnvironment()
        {
            Logger2.Info("InputComponentTest: 开始测试InputHandlerComponent");
            
            // 创建测试计时器
            _testTimer = new Timer();
            _testTimer.WaitTime = 1.0;
            _testTimer.OneShot = false;
            _testTimer.Timeout += OnTestTimerTimeout;
            AddChild(_testTimer);
        }

        private void CreateTestEntity()
        {
            // 创建测试实体
            _testEntity = new CharacterEntity
            {
                Name = "TestEntity"
            };
            AddChild(_testEntity);
            _testEntity.Position = new Vector2(400, 300);

            // 添加输入组件
            _inputComponent = _testEntity.AddComponent<InputHandlerComponent>();
            _inputComponent.InputEnabled = true;

            // 订阅事件
            _inputComponent.OnMoveDirectionChanged += OnMoveDirectionChanged;
            _inputComponent.OnAttackPressed += OnAttackPressed;
            _inputComponent.OnDashPressed += OnDashPressed;
            _inputComponent.OnRunStateChanged += OnRunStateChanged;

            Logger2.Info("InputComponentTest: 测试实体创建完成");
        }

        private void SetupUITest()
        {
            // 创建信息显示标签
            _infoLabel = new Label
            {
                Text = "InputHandlerComponent测试中...",
                Position = new Vector2(10, 10),
                ThemeTypeVariation = "Normal"
            };
            AddChild(_infoLabel);
        }

        private void StartTests()
        {
            _testTimer.Start();
            UpdateInfoDisplay();
        }

        private void OnTestTimerTimeout()
        {
            UpdateInfoDisplay();
        }

        private void OnMoveDirectionChanged(Vector2 direction)
        {
            Logger2.Info("Test: 移动方向改变 - ({0:F2}, {1:F2})", direction.X, direction.Y);
        }

        private void OnAttackPressed()
        {
            Logger2.Info("Test: 攻击按钮按下");
        }

        private void OnDashPressed(Vector2 direction)
        {
            Logger2.Info("Test: 冲刺按钮按下 - 方向({0:F2}, {1:F2})", direction.X, direction.Y);
        }

        private void OnRunStateChanged(bool isRunning)
        {
            Logger2.Info("Test: 奔跑状态改变 - {0}", isRunning);
        }

        private void UpdateInfoDisplay()
        {
            if (_infoLabel == null || _inputComponent == null) return;

            string infoText = $"InputHandlerComponent测试\n" +
                             $"========================\n" +
                             $"移动方向: ({_inputComponent.MoveDirection.X:F2}, {_inputComponent.MoveDirection.Y:F2})\n" +
                             $"最后方向: ({_inputComponent.LastMoveDirection.X:F2}, {_inputComponent.LastMoveDirection.Y:F2})\n" +
                             $"正在移动: {_inputComponent.IsMoving}\n" +
                             $"正在攻击: {_inputComponent.IsAttacking}\n" +
                             $"正在奔跑: {_inputComponent.IsRunning}\n" +
                             $"正在跳跃: {_inputComponent.IsJumping}\n" +
                             $"正在冲刺: {_inputComponent.IsDashing}\n" +
                             $"方向索引: {_inputComponent.GetDirectionIndex()}\n" +
                             $"输入启用: {_inputComponent.InputEnabled}\n" +
                             $"组件启用: {_inputComponent.IsEnabled}\n" +
                             $"FPS: {Engine.GetFramesPerSecond()}";

            _infoLabel.Text = infoText;
        }

        public override void _Process(double delta)
        {
            // 实时更新显示
            UpdateInfoDisplay();
        }

        public override void _ExitTree()
        {
            _testTimer?.Stop();
            Logger2.Info("InputComponentTest: 测试结束");
        }
    }
}