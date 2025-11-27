#nullable enable
using global::System;
using global::System.Diagnostics;

namespace DotNetCampus.Logging.Writers;

/// <summary>
/// 提供仅在开启了 TRACE 条件编译符下才会记录的日志。
/// </summary>
public class TraceLogger(ILogger realLogger) : ILogger
{
    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        realLogger.Log(logLevel, eventId, state, exception, formatter);
    }

    /// <summary>
    /// 记录追踪级别的日志。
    /// </summary>
    /// <param name="message">要记录的消息。</param>
    [Conditional("TRACE")]
    public void Trace(string message)
    {
        realLogger.Log(LogLevel.Trace, default, message, null, (s, ex) => message);
    }

    /// <summary>
    /// 记录调试级别的日志。
    /// </summary>
    /// <param name="message">要记录的消息。</param>
    [Conditional("TRACE")]
    public void Debug(string message)
    {
        realLogger.Log(LogLevel.Debug, default, message, null, (s, ex) => message);
    }

    /// <summary>
    /// 记录信息日志。
    /// </summary>
    /// <param name="message">要记录的消息。</param>
    [Conditional("TRACE")]
    public void Info(string message)
    {
        realLogger.Log(LogLevel.Information, default, message, null, (s, ex) => message);
    }

    /// <summary>
    /// 记录警告日志。
    /// </summary>
    /// <param name="message">要记录的消息，形如 [tag] message</param>
    [Conditional("TRACE")]
    public void Warn(string message)
    {
        realLogger.Log(LogLevel.Warning, default, message, null, (s, ex) => message);
    }

    /// <summary>
    /// 记录错误日志。
    /// </summary>
    /// <param name="message">要记录的消息。</param>
    /// <param name="exception">如果有异常信息，可以传入此参数。</param>
    [Conditional("TRACE")]
    public void Error(string message, Exception? exception = null)
    {
        realLogger.Log(LogLevel.Warning, default, message, null, (s, ex) => message);
    }

    /// <summary>
    /// 记录崩溃日志。
    /// </summary>
    /// <param name="message">要记录的消息。</param>
    /// <param name="exception">如果有异常信息，可以传入此参数。</param>
    [Conditional("TRACE")]
    public void Fatal(string message, Exception? exception = null)
    {
        realLogger.Log(LogLevel.Critical, default, message, null, (s, ex) => message);
    }
}
