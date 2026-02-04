using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Logs
{
    /// <summary>
    /// 异步文件 Sink 实现。
    /// 说明：为了避免在主线程做磁盘 IO 导致卡顿，该实现使用内部队列和后台 Task 将日志异步写入文件。
    /// 使用建议：1) 将日志文件放在可写路径（如 Godot 的 user://）；2) 在程序退出时调用 <see cref="Dispose"/> 或 <see cref="LogManager.Shutdown"/> 以确保缓冲区数据被 flush。
    /// 注意事项：此实现尝试吞掉写入线程的异常以避免崩溃；如果需要更严格的错误处理，可扩展出错回调或上报逻辑。
    /// </summary>
    public class FileSink : ILogSink, IDisposable
    {
        // 目标文件路径（绝对或相对于运行目录）
        private readonly string _filePath;
        // 内部线程安全队列用于缓存待写入的日志行
        private readonly ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();
        // 信号量（当前实现保留，可能用于扩展通知）
        private readonly AutoResetEvent _signal = new AutoResetEvent(false);
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private Task _writerTask;

        // 添加计数器和上次刷新时间字段
        private long _linesSinceLastFlush = 0;
        private const long FLUSH_INTERVAL_LINES = 10;  // 每10行刷新一次
        private DateTime _lastFlushTime = DateTime.UtcNow;
        private const double FLUSH_INTERVAL_SECONDS = 1.0;  // 每1秒强制刷新一次

        /// <summary>
        /// 构造函数，传入目标文件路径。会创建目录并启动后台写入任务。
        /// </summary>
        public FileSink(string filePath)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath) ?? ".");
            Godot.GD.Print($"[FileSink] Log file path: {_filePath}");
            StartWriter();
        }

        /// <summary>
        /// 启动后台写入任务（基于 Task）。
        /// 实现要点：
        /// - 循环读取队列并写入文件；
        /// - 使用短延迟减少 CPU 占用；
        /// - 退出时 flush 剩余队列。
        /// </summary>
        private void StartWriter()
        {
            _writerTask = Task.Run(async () =>
            {
                try
                {
                    using (var stream = new FileStream(_filePath, FileMode.Append, FileAccess.Write, FileShare.Read))
                    using (var writer = new StreamWriter(stream, Encoding.UTF8))
                    {
                        while (!_cts.Token.IsCancellationRequested)
                        {
                            int processedLines = 0;
                            while (_queue.TryDequeue(out var line))
                            {
                                await writer.WriteLineAsync(line).ConfigureAwait(false);
                                Interlocked.Increment(ref _linesSinceLastFlush);
                                processedLines++;
                                
                                // 如果处理了太多行，暂停一下，让其他操作有机会执行
                                if (processedLines >= 100)
                                    break;
                            }

                            // 检查是否需要刷新：按行数或时间间隔
                            if (Interlocked.Read(ref _linesSinceLastFlush) >= FLUSH_INTERVAL_LINES || 
                                (DateTime.UtcNow - _lastFlushTime).TotalSeconds >= FLUSH_INTERVAL_SECONDS)
                            {
                                await writer.FlushAsync().ConfigureAwait(false);
                                Interlocked.Exchange(ref _linesSinceLastFlush, 0);
                                _lastFlushTime = DateTime.UtcNow;
                            }

                            // 当没有数据时短暂睡眠，避免 busy-loop
                            await Task.Delay(50, _cts.Token).ConfigureAwait(false);
                        }

                        // 程序关闭时尝试将剩余队列写入并 flush
                        while (_queue.TryDequeue(out var rest))
                        {
                            await writer.WriteLineAsync(rest).ConfigureAwait(false);
                        }

                        await writer.FlushAsync().ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    // 正常取消，不做额外处理
                }
                catch
                {
                    // 为保持稳定性，这里吞掉异常。可在未来增加回调以通知上层。
                }
            }, _cts.Token);
        }

        /// <summary>
        /// 将已格式化消息入队列，实际写入由后台任务负责。
        /// 请注意：此方法非常快速且线程安全。
        /// </summary>
        public void Write(LogLevel level, string formattedMessage, DateTime timeUtc)
        {
            var line = $"[{timeUtc:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {formattedMessage}";
            _queue.Enqueue(line);
            _signal.Set();
        }

        /// <summary>
        /// 释放资源：取消后台任务并等待短时间的退出。如果需要保证所有日志已写入，建议在应用退出前调用 <see cref="LogManager.Shutdown"/>。
        /// </summary>
        public void Dispose()
        {
            _cts.Cancel();
            try
            {
                _writerTask?.Wait(1000);
            }
            catch { }
            _cts.Dispose();
            _signal.Dispose();
        }
    }
}
