using System;

// 日志级别定义文件（中文注释）
// 说明：级别数值越小表示越详细的输出（Trace 最详细，Fatal 最严重）。
// 建议用法：开发阶段可设为 Trace/Debug；发布阶段设为 Info 或 Warn 以减少 IO 开销。
namespace Logs
{
    /// <summary>
    /// 日志严重性等级枚举。
    /// Trace/Debug/Info/Warn/Error/Fatal（从最详细到最严重）。
    /// 请在运行时通过 <see cref="LogManager.SetMinLevel"/> 或 <see cref="LogManager.Initialize"/> 来设置最小输出等级。
    /// </summary>
    public enum LogLevel
    {
        /// <summary>跟踪信息，最详细（通常用于函数入口/退出、状态变化）</summary>
        Trace = 0,
        /// <summary>调试信息（用于调试流程和变量）</summary>
        Debug = 1,
        /// <summary>普通信息（运行时普通日志，如事件、状态）</summary>
        Info = 2,
        /// <summary>警告（可能的问题或不严重的异常）</summary>
        Warn = 3,
        /// <summary>错误（需要关注的问题，可能影响功能）</summary>
        Error = 4,
        /// <summary>严重错误（致命问题，可能需要终止或上报）</summary>
        Fatal = 5
    }
}
