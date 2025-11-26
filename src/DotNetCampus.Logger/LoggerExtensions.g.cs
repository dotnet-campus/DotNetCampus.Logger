#nullable enable
using global::System;

namespace DotNetCampus.Logging;

/// <summary>
/// <see cref="ILogger"/> 的常见场景的扩展方法。
/// </summary>
public static class LoggerExtensions
{
#pragma warning disable CS0419 // 因为在不同框架中 Trace 方法的重载不同，所以注释难以保持一致
    /// <summary>
    /// 记录追踪级别的日志。
    /// </summary>
    /// <param name="logger">记录日志所使用的记录器。</param>
    /// <param name="message">要记录的消息。</param>
    /// <remarks>
    /// 请注意，这里的 <see cref="Trace"/> 仅代表追踪级别；如果配置输出 trace 级别的日志，即便编译时未定义 TRACE 条件编译符也会输出。<br/>
    /// 如果希望仅在定义了 TRACE 条件编译符时输出日志，请使用 <see cref="Log"/>.<see cref="Log.Trace"/>。
    /// </remarks>
#pragma warning restore CS0419
    public static void Trace(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Trace, default, message, null, (s, ex) => message);
    }

#pragma warning disable CS0419 // 因为在不同框架中 Debug 方法的重载不同，所以注释难以保持一致
    /// <summary>
    /// 记录调试级别的日志。
    /// </summary>
    /// <param name="logger">记录日志所使用的记录器。</param>
    /// <param name="message">要记录的消息。</param>
    /// <remarks>
    /// 请注意，这里的 <see cref="Debug"/> 仅代表调试级别；如果配置了输出 debug 级别的日志，即便是 release 编译也会输出。<br/>
    /// 如果希望仅在 debug 配置下输出日志，请使用 <see cref="Log"/>.<see cref="Log.Debug"/>。
    /// </remarks>
#pragma warning restore CS0419
    public static void Debug(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Debug, default, message, null, (s, ex) => message);
    }

    /// <summary>
    /// 记录信息日志。
    /// </summary>
    /// <param name="logger">记录日志所使用的记录器。</param>
    /// <param name="message">要记录的消息。</param>
    public static void Info(this ILogger logger, string message)
    {
        logger.Log(LogLevel.Information, default, message, null, (s, ex) => message);
    }

    /// <summary>
    /// 记录警告日志。
    /// </summary>
    /// <param name="logger">记录日志所使用的记录器。</param>
    /// <param name="message">要记录的消息。</param>
    /// <param name="exception">如果有异常信息，可以传入此参数。</param>
    public static void Warn(this ILogger logger, string message, Exception? exception = null)
    {
        logger.Log(LogLevel.Warning, default, message, exception, (s, ex) => message);
    }

    /// <summary>
    /// 记录错误日志。
    /// </summary>
    /// <param name="logger">记录日志所使用的记录器。</param>
    /// <param name="message">要记录的消息。</param>
    /// <param name="exception">如果有异常信息，可以传入此参数。</param>
    public static void Error(this ILogger logger, string message, Exception? exception = null)
    {
        logger.Log(LogLevel.Error, default, message, exception, (s, ex) => message);
    }

    /// <summary>
    /// 记录崩溃日志。
    /// </summary>
    /// <param name="logger">记录日志所使用的记录器。</param>
    /// <param name="message">要记录的消息。</param>
    /// <param name="exception">如果有异常信息，可以传入此参数。</param>
    public static void Fatal(this ILogger logger, string message, Exception? exception = null)
    {
        logger.Log(LogLevel.Critical, default, message, exception, (s, ex) => message);
    }
}
