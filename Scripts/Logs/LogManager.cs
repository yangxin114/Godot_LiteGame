using System;
using System.Collections.Generic;
using System.IO;
using Godot;

namespace Logs
{
    /// <summary>
    /// 日志管理中心：负责管理全局最小日志等级、输出 Sink 列表，以及初始化/关机操作。
    /// 使用说明：在游戏启动（例如 Start.cs._Ready 或 Autoload 初始化）时调用 <see cref="Initialize"/>。
    /// </summary>
    public static class LogManager
    {
        // 注册的输出目标（sink），日志将依次发送到这些 sink
        private static readonly List<ILogSink> _sinks = new List<ILogSink>();
        // 当前全局最小输出等级，低于该等级的日志将被丢弃
        private static LogLevel _minLevel = LogLevel.Debug;
        // 记录文件 sink 的引用以便在 Shutdown 时释放
        private static FileSink _fileSink;

        /// <summary>
        /// 当前最小日志等级（读取属性）。
        /// </summary>
        public static LogLevel MinLevel => _minLevel;

        /// <summary>
        /// 初始化日志系统。
        /// 参数：
        /// - minLevel: 设置最小输出等级，低于该等级的日志不会被发送到 sink。
        /// - filePath: 可选的日志文件路径。如果为 null/空则不启用文件输出。
        /// - enableConsole: 是否加入 ConsoleSink（GD.Print），通常开发时开启。
        /// </summary>
        public static void Initialize(LogLevel minLevel = LogLevel.Debug, string filePath = null, bool enableConsole = true)
        {
            _minLevel = minLevel;
            _sinks.Clear();

            if (enableConsole)
            {
                _sinks.Add(new ConsoleSink());
            }

            if (!string.IsNullOrEmpty(filePath))
            {
                // 确保目录存在
                var dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                _fileSink = new FileSink(filePath);
                _sinks.Add(_fileSink);
            }
        }

        /// <summary>
        /// 运行时动态修改最小日志等级。
        /// </summary>
        public static void SetMinLevel(LogLevel level)
        {
            _minLevel = level;
        }

        /// <summary>
        /// 添加自定义 Sink（例如远程上传、数据库等）。
        /// </summary>
        public static void AddSink(ILogSink sink)
        {
            if (sink == null) return;
            _sinks.Add(sink);
        }

        /// <summary>
        /// 移除已注册的 Sink。
        /// </summary>
        public static void RemoveSink(ILogSink sink)
        {
            if (sink == null) return;
            _sinks.Remove(sink);
        }

        /// <summary>
        /// 向所有已注册 sink 发送日志（内部方法）。
        /// - 会根据最小等级过滤。
        /// - 对单个 sink 的写入异常会被捕获并吞掉，保证日志系统不会引发主流程崩溃。
        /// </summary>
        internal static void Emit(LogLevel level, string message)
        {
            if (level < _minLevel) return;
            var now = DateTime.Now;
            foreach (var s in _sinks)
            {
                try
                {
                    s.Write(level, message, now);
                }
                catch
                {
                    // 单个 sink 出错不应导致应用崩溃；在需要时可扩展为上报异常
                }
            }
        }

        /// <summary>
        /// 关闭日志系统并释放资源（例如文件句柄）。建议在程序退出路径调用此方法。
        /// </summary>
        public static void Shutdown()
        {
            // 释放文件 sink
            try
            {
                _fileSink?.Dispose();
            }
            catch { }
            _sinks.Clear();
        }
    }
}
