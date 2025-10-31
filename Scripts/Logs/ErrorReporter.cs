using System;
using System.Text;

namespace Logs
{
    /// <summary>
    /// 错误/异常上报器。
    /// 功能：将异常格式化并写入日志；可注册远程上报回调将异常发送到崩溃收集端点（例如 Sentry、自建服务等）。
    /// 注意：远程上报回调需要自行保证线程安全和网络发送策略（可做异步/重试）。
    /// </summary>
    public static class ErrorReporter
    {
        // 用户注册的远程上报委托（接收最终 payload 字符串）
        private static Action<string> _remoteReporter;

        /// <summary>
        /// 注册远程上报委托。该委托会在发生异常时被调用，传入格式化后的异常信息。
        /// 注意：回调环境与调用环境相同（可能在主线程），请勿在回调中直接操作 Godot 对象，或在回调中进行长时间阻塞操作。
        /// </summary>
        public static void RegisterRemoteReporter(Action<string> reporter)
        {
            _remoteReporter = reporter;
        }

        /// <summary>
        /// 报告异常：会将异常构造成一个可读 payload，写到日志，并尝试调用远程上报委托。
        /// - context: 可选的上下文信息，帮助定位问题（例如场景名、玩家 ID 等）。
        /// - fatal: 标记为致命异常时会使用 Fatal 等级写入日志。
        /// </summary>
        public static void Report(Exception ex, string context = null, bool fatal = false)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Exception Report");
            if (!string.IsNullOrEmpty(context)) sb.AppendLine("Context: " + context);
            sb.AppendLine("Type: " + ex.GetType().FullName);
            sb.AppendLine("Message: " + ex.Message);
            sb.AppendLine("StackTrace:");
            sb.AppendLine(ex.StackTrace);

            var payload = sb.ToString();

            // 写入本地日志（根据 fatal 标记选择等级）
            if (fatal)
                Logger.Fatal(payload);
            else
                Logger.Error(payload);

            // 若配置了远程上报，则尝试转发（吞掉回调异常以保证稳定性）
            try
            {
                _remoteReporter?.Invoke(payload);
            }
            catch
            {
                // 远程上报失败不应影响主流程
            }
        }
    }
}
