#nullable enable
using global::System;
using global::System.Diagnostics;

namespace DotNetCampus.Logging.Writers;

/// <summary>
/// 提供仅在 debug 下才会记录的日志。
/// </summary>
public class DebugLogger(ILogger realLogger) : ILogger
{
    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        // 在 debug 模式下，所有日志都是可用的
        return true;
    }

    /// <inheritdoc />
    void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        realLogger.Log(logLevel, eventId, state, exception, formatter);
    }

    /// <summary>
    /// 记录追踪级别的日志。
    /// </summary>
    /// <param name="message">要记录的消息。</param>
    [Conditional("DEBUG")]
    public void Trace(string message)
    {
        realLogger.Log(LogLevel.Trace, default, message, null, (s, ex) => message);
    }

    /// <summary>
    /// 记录调试级别的日志。
    /// </summary>
    /// <param name="message">要记录的消息。</param>
    [Conditional("DEBUG")]
    public void Debug(string message)
    {
        realLogger.Log(LogLevel.Debug, default, message, null, (s, ex) => message);
    }

    /// <summary>
    /// 记录信息日志。
    /// </summary>
    /// <param name="message">要记录的消息。</param>
    [Conditional("DEBUG")]
    public void Info(string message)
    {
        realLogger.Log(LogLevel.Information, default, message, null, (s, ex) => message);
    }

    /// <summary>
    /// 记录警告日志。
    /// </summary>
    /// <param name="message">要记录的消息，形如 [tag] message</param>
    [Conditional("DEBUG")]
    public void Warn(string message)
    {
        realLogger.Log(LogLevel.Warning, default, message, null, (s, ex) => message);
    }

    /// <summary>
    /// 记录错误日志。
    /// </summary>
    /// <param name="message">要记录的消息。</param>
    /// <param name="exception">如果有异常信息，可以传入此参数。</param>
    [Conditional("DEBUG")]
    public void Error(string message, Exception? exception = null)
    {
        realLogger.Log(LogLevel.Warning, default, message, null, (s, ex) => message);
    }

    /// <summary>
    /// 记录崩溃日志。
    /// </summary>
    /// <param name="message">要记录的消息。</param>
    /// <param name="exception">如果有异常信息，可以传入此参数。</param>
    [Conditional("DEBUG")]
    public void Fatal(string message, Exception? exception = null)
    {
        realLogger.Log(LogLevel.Critical, default, message, null, (s, ex) => message);
    }
}
