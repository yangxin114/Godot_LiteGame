using System;
using Godot;

namespace Logs
{
    /// <summary>
    /// 性能监控节点：定期采样 FPS 与内存并将统计信息写入日志。
    /// 使用方式：将此 Node 添加到 SceneTree（例如作为 Autoload 或在 Start.cs 中动态添加）。
    /// 参数：LogInterval 控制统计间隔（秒）。
    /// 注意：此实现使用 Engine.GetFramesPerSecond() 与 OS 内存接口，依赖 Godot 提供的 API。
    /// </summary>
    public partial class PerformanceMonitor : Node
    {
        [Export]
        public double LogInterval = 5.0f; // 采样并记录到日志的时间间隔（秒）

        // 内部累积变量用于计算平均 FPS
        private double _accum = 0.0;
        private int _frames = 0;
        private double _time = 0.0;

        public override void _Ready()
        {
            // 可在此处初始化更多采样指标或启动自定义计时器
        }

        /// <summary>
        /// 在 _Process 中累积帧数和时间，到达间隔时写入一条性能日志。
        /// 日志格式示例：Perf: avg_fps=60.0, frames=300, elapsed=5.00s, mem=12345678 bytes, peak=23456789 bytes
        /// </summary>
        public override void _Process(double delta)
        {
            _accum += delta;
            _frames++;
            _time += delta;

            if (_accum >= LogInterval)
            {
                var avgFps = _frames / _accum;
                var staticMem = OS.GetStaticMemoryUsage();
                var peakMem = OS.GetStaticMemoryPeakUsage();
                Logger2.Info("Perf: avg_fps={0:F1}, frames={1}, elapsed={2:F2}s, mem={3} bytes, peak={4} bytes", avgFps, _frames, _accum, staticMem, peakMem);

                // 重置采样
                _accum = 0.0;
                _frames = 0;
            }
        }
    }
}
