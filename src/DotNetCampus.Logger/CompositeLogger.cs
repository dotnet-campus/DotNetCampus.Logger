using System;
using System.Collections.Generic;
using DotNetCampus.Logging.Configurations;

namespace DotNetCampus.Logging;

/// <summary>
/// 一个聚合多个日志记录器的综合记录器，通常作为应用程序的主要日志记录器。
/// </summary>
public class CompositeLogger : ILogger
{
    internal CompositeLogger(LogOptions options)
    {
        Configuration = new InheritedConfiguration<LogOptions>(options);
    }

    private InheritedConfiguration<LogOptions> Configuration { get; }

    /// <summary>
    /// 高于或等于此级别的日志才会被记录。
    /// </summary>
    public LogLevel Level
    {
        get => Configuration.GetValue(o => o.LogLevel);
        set => Configuration.SetValue(o => o.LogLevel = value);
    }

    /// <summary>
    /// 当前所有的日志记录器。
    /// </summary>
    public required IReadOnlyList<ILogger> Writers { get; init; }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        if (logLevel < Level)
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (logLevel < Level)
        {
            return;
        }

        foreach (var writer in Writers)
        {
            writer.Log(logLevel, eventId, state, exception, formatter);
        }
    }
}
