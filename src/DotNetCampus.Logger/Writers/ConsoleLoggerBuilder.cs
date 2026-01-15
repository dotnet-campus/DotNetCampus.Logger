using System;
using System.Collections.Generic;

namespace DotNetCampus.Logging.Writers;

/// <summary>
/// 辅助创建控制台日志记录器的构建器。
/// </summary>
public sealed class ConsoleLoggerBuilder
{
    private LogWritingThreadMode _threadMode = LogWritingThreadMode.NotThreadSafe;
    private LoggerConsoleOutputTo _outputTo = LoggerConsoleOutputTo.StandardOutput;
    private LoggerConsoleFormatOptions _formatOptions = LoggerConsoleFormatOptions.ColorfulConsole;
    private LoggerLevelPrefixAlignment _levelPrefixAlignment = LoggerLevelPrefixAlignment.NoAlignment;
    private IReadOnlyList<string>? _mainArgs;

    /// <summary>
    /// 高于或等于此级别的日志才会被记录。
    /// </summary>
    public LogLevel Level { get; set; }

    /// <summary>
    /// 高于或等于此级别的日志才会被记录。
    /// </summary>
    public ConsoleLoggerBuilder WithLevel(LogLevel level)
    {
        Level = level;
        return this;
    }

    /// <summary>
    /// 指定控制台日志的输出位置。
    /// </summary>
    /// <param name="outputTo">输出位置。</param>
    /// <returns>用于链式调用的构建器。</returns>
    public ConsoleLoggerBuilder WithOutput(LoggerConsoleOutputTo outputTo)
    {
        _outputTo = outputTo;
        return this;
    }

    /// <summary>
    /// 指定控制台日志的格式化选项。
    /// </summary>
    /// <param name="options">格式化选项。</param>
    /// <returns>用于链式调用的构建器。</returns>
    public ConsoleLoggerBuilder WithFormat(LoggerConsoleFormatOptions options)
    {
        _formatOptions = options;
        return this;
    }

    /// <summary>
    /// 使用默认格式（时间戳 + 颜色）。
    /// </summary>
    /// <returns>用于链式调用的构建器。</returns>
    public ConsoleLoggerBuilder WithColorfulConsoleFormat()
    {
        _formatOptions = LoggerConsoleFormatOptions.ColorfulConsole;
        return this;
    }

    /// <summary>
    /// 使用 STDIO 重定向格式（时间戳 + 级别前缀，无颜色，输出到 StandardError）。
    /// 适用于需要将日志输出到标准错误流的场景（如 STDIO 协议服务器）。
    /// </summary>
    /// <returns>用于链式调用的构建器。</returns>
    public ConsoleLoggerBuilder WithStdioRedirectedFormat()
    {
        _formatOptions = LoggerConsoleFormatOptions.StdioRedirected;
        _outputTo = LoggerConsoleOutputTo.StandardError;
        _levelPrefixAlignment = LoggerLevelPrefixAlignment.Short;
        return this;
    }

    /// <summary>
    /// 指定级别前缀的对齐方式（仅在启用 LevelPrefix 时生效）。
    /// </summary>
    /// <param name="alignment">对齐方式。</param>
    /// <returns>用于链式调用的构建器。</returns>
    public ConsoleLoggerBuilder WithLevelPrefixAlignment(LoggerLevelPrefixAlignment alignment)
    {
        _levelPrefixAlignment = alignment;
        return this;
    }

    /// <summary>
    /// 指定控制台日志的线程安全模式。
    /// </summary>
    /// <param name="threadMode">线程安全模式。</param>
    /// <returns>用于链式调用的构建器。</returns>
    /// <exception cref="ArgumentOutOfRangeException">线程安全模式不支持。</exception>
    public ConsoleLoggerBuilder WithThreadSafe(LogWritingThreadMode threadMode)
    {
        _threadMode = threadMode;
        return this;
    }

    /// <summary>
    /// 从命令行参数中提取过滤标签，使得控制台日志支持过滤标签行为。
    /// </summary>
    /// <param name="args">命令行参数。</param>
    /// <returns>用于链式调用的构建器。</returns>
    public ConsoleLoggerBuilder FilterConsoleTagsFromCommandLineArgs(IReadOnlyList<string> args)
    {
        _mainArgs = args;
        return this;
    }

    /// <summary>
    /// 创建控制台日志记录器。
    /// </summary>
    /// <returns>控制台日志记录器。</returns>
    internal ConsoleLogger Build() => new(_threadMode, _mainArgs)
    {
        Level = Level,
        OutputTo = _outputTo,
        FormatOptions = _formatOptions,
        LevelPrefixAlignment = _levelPrefixAlignment,
    };
}

/// <summary>
/// 辅助创建控制台日志记录器。
/// </summary>
public static class ConsoleLoggerBuilderExtensions
{
    /// <summary>
    /// 添加控制台日志记录器。
    /// </summary>
    /// <param name="builder">日志构建器。</param>
    /// <param name="configure">配置控制台日志记录器。</param>
    /// <returns>用于链式调用的构建器。</returns>
    public static LoggerBuilder AddConsoleLogger(this LoggerBuilder builder, Action<ConsoleLoggerBuilder> configure)
    {
        var consoleLoggerBuilder = new ConsoleLoggerBuilder();
        configure(consoleLoggerBuilder);
        return builder.AddWriter(consoleLoggerBuilder.Build());
    }
}
