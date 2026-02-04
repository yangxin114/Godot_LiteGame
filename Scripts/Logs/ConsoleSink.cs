using System;
using Godot;

namespace Logs
{
    /// <summary>
    /// 控制台输出 Sink（将日志写入 Godot 的控制台）。
    /// 说明：用于开发时在编辑器或运行时控制台快速查看日志。
    /// 实现会在消息前加上时间与等级前缀，随后调用 GD.Print / GD.PrintWarning / GD.PrintErr。
    /// </summary>
    public class ConsoleSink : ILogSink
    {
        /// <summary>
        /// 将日志写入 Godot 控制台。不同等级使用不同的输出 API，便于在编辑器中区分。
        /// 注意：Godot 的打印函数是线程不安全的，应在主线程使用；本实现假设由主线程调用 LogManager.Emit。
        /// </summary>
        public void Write(LogLevel level, string formattedMessage, DateTime timeUtc)
        {
            // 构造统一的消息前缀，方便搜索与按时间排序
            var msg = $"[{timeUtc:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {formattedMessage}";
            switch (level)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                case LogLevel.Info:
                    // 普通输出
                    GD.Print(msg);
                    break;
                case LogLevel.Warn:
                    // 警告输出，带上下文 tag
                    GD.PushWarning(msg);
                    GD.Print(msg);
                    break;
                case LogLevel.Error:
                case LogLevel.Fatal:
                    // 错误/致命错误输出到错误通道
                    GD.PushError(msg);
                    GD.Print(msg);
                    break;
                default:
                    GD.Print(msg);
                    break;
            }
        }
    }
}
