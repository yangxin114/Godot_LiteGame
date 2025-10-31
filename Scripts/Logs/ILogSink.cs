using System;

namespace Logs
{
    /// <summary>
    /// 日志输出端接口（Sink）。
    /// 说明：实现此接口可以将日志发送到不同的目标，例如控制台、文件、远程服务器等。
    /// 方法应尽可能快速返回；如果需要 IO，请考虑内部异步实现以避免阻塞主线程。
    /// </summary>
    public interface ILogSink
    {
        /// <summary>
        /// 写入一条已格式化的日志消息。
        /// 参数：
        /// - <paramref name="level"/>: 日志等级，便于 Sink 做分流或上色显示。
        /// - <paramref name="formattedMessage"/>: 已格式化的消息正文（不包含时间/等级前缀，或视实现决定）。
        /// - <paramref name="timeUtc"/>: 事件时间，UTC 时间戳。
        /// </summary>
        void Write(LogLevel level, string formattedMessage, DateTime timeUtc);
    }
}
