using System;

namespace Logs
{
    /// <summary>
    /// 日志门面（便捷方法）。
    /// 说明：代码中请优先使用 <see cref="Logger2.Trace/Debug/Info/..."/> 这类方法来记录日志，方便统一替换和格式化。\
    /// 使用Logger2名字，因为Logger与Godot下Logger名字冲突
    /// </summary>
    public static class Logger2
    {
        /// <summary>
        /// 直接写入已生成的消息。
        /// </summary>
        public static void Log(LogLevel level, string message)
        {
            LogManager.Emit(level, message);
        }

        /// <summary>
        /// 使用格式化字符串写入日志（类似 string.Format）。
        /// </summary>
        public static void Log(LogLevel level, string format, params object[] args)
        {
            var msg = args == null || args.Length == 0 ? format : string.Format(format, args);
            LogManager.Emit(level, msg);
        }

        // 各等级的快捷方法，便于在调用处直接使用
        public static void Trace(string format, params object[] args) => Log(LogLevel.Trace, format, args);
        public static void Debug(string format, params object[] args) => Log(LogLevel.Debug, format, args);
        public static void Info(string format, params object[] args) => Log(LogLevel.Info, format, args);
        public static void Warn(string format, params object[] args) => Log(LogLevel.Warn, format, args);
        public static void Error(string format, params object[] args) => Log(LogLevel.Error, format, args);
        public static void Fatal(string format, params object[] args) => Log(LogLevel.Fatal, format, args);
    }
}
