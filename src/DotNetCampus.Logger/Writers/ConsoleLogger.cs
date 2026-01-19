using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using DotNetCampus.Logging.Writers.ConsoleLoggerHelpers;
using DotNetCampus.Logging.Writers.Helpers;
using C = DotNetCampus.Logging.Writers.ConsoleLoggerHelpers.ConsoleColors;
using B = DotNetCampus.Logging.Writers.ConsoleLoggerHelpers.ConsoleColors.Background;
using D = DotNetCampus.Logging.Writers.ConsoleLoggerHelpers.ConsoleColors.Decoration;
using F = DotNetCampus.Logging.Writers.ConsoleLoggerHelpers.ConsoleColors.Foreground;

namespace DotNetCampus.Logging.Writers;

/// <summary>
/// 在控制台输出日志的日志记录器。
/// </summary>
public class ConsoleLogger : ILogger
{
    private readonly Lazy<TextWriter> _outLazy;

    /// <summary>
    /// 获取用于输出日志的文本写入流。
    /// </summary>
    private TextWriter Out => _outLazy.Value;

    /// <summary>
    /// 用于处理重复的日志，避免重复日志污染控制台输出内容。
    /// </summary>
    private readonly RepeatLoggerDetector _repeat;

    /// <summary>
    /// 判断当前是否在控制台输出日志（否则可能在普通的标准输入流，被其他应用程序对接）。
    /// </summary>
    /// <remarks>
    /// 如果在控制台输出，则可以使用 ASCII 控制码来修改颜色、控制光标移动。也可以用来获取控制台窗口信息。
    /// </remarks>
    private readonly bool _isConsoleOutput;

    /// <summary>
    /// 创建一个 <see cref="ConsoleLogger"/> 的新实例。
    /// </summary>
    /// <param name="threadMode">指定控制台日志的线程安全模式。</param>
    /// <param name="mainArgs">Main 方法的参数。</param>
    public ConsoleLogger(LogWritingThreadMode threadMode = LogWritingThreadMode.NotThreadSafe, IReadOnlyList<string>? mainArgs = null)
    {
        _repeat = new RepeatLoggerDetector(ClearAndMoveToLastLine);
        CoreWriter = threadMode.CreateCoreLogWriter(SafeWriteLine);
        TagManager = TagFilterManager.FromCommandLineArgs(mainArgs ?? []);
        _isConsoleOutput = ConsoleInitializer.Initialize();
        _outLazy = new Lazy<TextWriter>(GetStandardOutputWriter, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>
    /// 高于或等于此级别的日志才会被记录。
    /// </summary>
    public LogLevel Level { get; init; }

    /// <summary>
    /// 指定控制台日志输出的目标。
    /// </summary>
    public LoggerConsoleOutputTo OutputTo { get; init; } = LoggerConsoleOutputTo.StandardOutput;

    /// <summary>
    /// 控制台日志格式化选项。
    /// </summary>
    public LoggerConsoleFormatOptions FormatOptions { get; init; } = LoggerConsoleFormatOptions.ColorfulConsole;

    /// <summary>
    /// 级别前缀对齐方式（仅在启用 <see cref="LoggerConsoleFormatOptions.LevelPrefix"/> 时生效）。
    /// </summary>
    public LoggerLevelPrefixAlignment LevelPrefixAlignment { get; init; } = LoggerLevelPrefixAlignment.NoAlignment;

    /// <summary>
    /// 最终日志写入器。
    /// </summary>
    private ICoreLogWriter CoreWriter { get; }

    /// <summary>
    /// 管理控制台日志的标签过滤。
    /// </summary>
    private TagFilterManager? TagManager { get; }

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

        var message = formatter(state, exception);
        if (TagManager?.IsTagEnabled(message) is false)
        {
            return;
        }

        LogCore(logLevel, exception, message);
    }

    /// <summary>
    /// 记录日志核心逻辑。在必要的情况下会保证线程安全。
    /// </summary>
    private void LogCore(LogLevel logLevel, Exception? exception, string message) => CoreWriter.Do(() =>
    {
        var builder = new ConsoleLogTextBuilder(FormatOptions);

        // 处理重复日志
        if (_repeat.RepeatOrResetLastLog(logLevel, message, exception) is var count and > 1)
        {
            builder.AppendTimestamp(logLevel);
            builder.AppendLevelPrefix(logLevel, LevelPrefixAlignment);

            string output;
            if (_isConsoleOutput)
            {
                // 控制台模式：覆盖上一行并显示重复信息
                output = builder.ToLoggerString(logLevel, $"上述日志已重复 {count} 次");
            }
            else
            {
                // 非控制台模式：正常消息 + 重复次数
                output = builder.ToLoggerString(logLevel, $"{message} (重复 {count} 次)");
            }

            SafeWriteLine(output);
            return;
        }

        // 正常日志 - 支持多行
        builder.AppendTimestamp(logLevel);
        builder.AppendLevelPrefix(logLevel, LevelPrefixAlignment);

        string logOutput;
        if (exception is not null)
        {
            builder.AppendExceptionMarker(logLevel);
            logOutput = builder.ToLoggerString(logLevel, message, exception.ToString());
        }
        else
        {
            logOutput = builder.ToLoggerString(logLevel, message);
        }

        SafeWriteLine(logOutput);
    });

    /// <summary>
    /// 清空当前行并移动光标到上一行。
    /// </summary>
    /// <param name="repeatCount">此移动光标，是因为日志已重复第几次。</param>
    private void ClearAndMoveToLastLine(int repeatCount)
    {
        if (!_isConsoleOutput)
        {
            return;
        }

        var width = SafeGetBufferWidth();
        const string cursorPreviousLine = "\e[1F";
        const string cursorHorizontalAbsolute0 = "\e[0G";
#if NETCOREAPP3_1_OR_GREATER
        Span<char> builder = stackalloc char[cursorPreviousLine.Length + width + cursorHorizontalAbsolute0.Length];
        cursorPreviousLine.CopyTo(builder);
        for (var i = cursorPreviousLine.Length; i < cursorPreviousLine.Length + width; i++)
        {
            builder[cursorPreviousLine.Length + i] = ' ';
        }
        cursorHorizontalAbsolute0.CopyTo(builder.Slice(cursorPreviousLine.Length + width));
        Out.Write(builder.ToString());
#else
        Out.Write($"{cursorPreviousLine}{new string(' ', width)}{cursorHorizontalAbsolute0}");
#endif
    }

    /// <summary>
    /// 安全地写入一行日志，避免出现 IO 异常。
    /// </summary>
    /// <param name="message"></param>
    private void SafeWriteLine(string? message)
    {
        try
        {
            Out.WriteLine(message);
        }
        catch (IOException)
        {
            // 如果写入日志时发生 IO 异常，已经没有办法救活了。
            // 这是日志系统的内部实现，所以日志也记不了了，汗……
        }
    }

    /// <summary>
    /// 安全地获取控制台的宽度，避免出现 IO 异常。
    /// </summary>
    /// <returns>控制台的宽度。如果不支持获取宽度，则返回 0。</returns>
    private int SafeGetBufferWidth()
    {
        try
        {
            return _isConsoleOutput ? Console.WindowWidth : 0;
        }
        catch (IOException)
        {
            // _isConsoleOutput 的条件已经非常苛刻了，足以确保 Console.WindowWidth 的获取不会出现异常。
            // 这只是保险性捕获。
            return 0;
        }
    }

    /// <summary>
    /// 获取标准输出的写入流。<br/>
    /// 如果当前在控制台中输出，则使用控制台的输出流；否则创建一个 UTF-8 编码的标准输出流。
    /// </summary>
    /// <returns>文本写入流。</returns>
    private TextWriter GetStandardOutputWriter()
    {
        if (Console.OutputEncoding.CodePage is not 0)
        {
            return OutputTo switch
            {
                LoggerConsoleOutputTo.StandardError => Console.Error,
                _ => Console.Out,
            };
        }

        var standardOutput = OutputTo switch
        {
            LoggerConsoleOutputTo.StandardError => Console.OpenStandardError(),
            _ => Console.OpenStandardOutput(),
        };
        var writer = new StreamWriter(standardOutput, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            AutoFlush = true,
        };
        return writer;
    }
}

/// <summary>
/// 控制台日志文本构建器，用于高效构建带颜色格式的日志文本。
/// </summary>
internal struct ConsoleLogTextBuilder(LoggerConsoleFormatOptions formatOptions)
{
    private readonly StringBuilder _builder = new(64);
    private readonly bool _hasTimestamp = formatOptions.FastHasFlag(LoggerConsoleFormatOptions.Timestamp);
    private readonly bool _hasColors = formatOptions.FastHasFlag(LoggerConsoleFormatOptions.Colors);
    private readonly bool _hasLevelPrefix = formatOptions.FastHasFlag(LoggerConsoleFormatOptions.LevelPrefix);

    /// <summary>
    /// 获取可见字符长度（不包含 ANSI 转义码）。
    /// </summary>
    public int VisibleLength { get; private set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendTimestamp(LogLevel level)
    {
        if (!_hasTimestamp)
        {
            return;
        }

        if (_hasColors)
        {
            _builder.Append(GetTagFormat(level));
        }
        _builder.Append('[').Append(DateTime.Now.ToString("HH:mm:ss.fff")).Append(']');
        VisibleLength += 14; // [HH:mm:ss.fff]
        if (_hasColors)
        {
            _builder.Append(C.Reset);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLevelPrefix(LogLevel level, LoggerLevelPrefixAlignment alignment)
    {
        if (!_hasLevelPrefix)
        {
            return;
        }

        var levelPrefix = GetLevelPrefixText(level, alignment);
        if (_hasColors)
        {
            _builder.Append(GetTextFormat(level));
        }
        _builder.Append(levelPrefix);
        VisibleLength += levelPrefix.Length;
        if (_hasColors)
        {
            _builder.Append(C.Reset);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendExceptionMarker(LogLevel level)
    {
        var marker = GetExceptionMarkerText(level);
        if (_hasColors)
        {
            _builder.Append(GetTagFormat(level));
        }
        _builder.Append(marker);
        VisibleLength += marker.Length;
        if (_hasColors)
        {
            _builder.Append(C.Reset);
        }
    }

    /// <summary>
    /// 完整创建日志字符串，并返回。此方法调用完成后，<see cref="ConsoleLogTextBuilder"/> 的状态并不会发生变化，因此可以反复调用用来创建不同的日志文本。
    /// </summary>
    /// <param name="level">日志级别。</param>
    /// <param name="messages">日志字符串（可能有多段，如异常日志的消息和异常文本；每一段可能是一个多行文本）。</param>
    /// <returns>
    /// 完整的可直接写入到控制台的日志字符串。
    /// <code>
    /// [WARN] This is a multi-line
    ///        logger message.
    /// </code>
    /// <code>
    /// [WARN] message 1/2: This is a multi-line
    ///        logger message.
    ///        message 2/2: This is a multi-line
    ///        logger message.
    /// </code>
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ToLoggerString(LogLevel level, params
#if NET6_0_OR_GREATER
        ReadOnlySpan<string>
#else
        string[]
#endif
        messages)
    {
        var tagLength = VisibleLength;
        var result = new StringBuilder(256);
        var textColor = _hasColors ? GetTextFormat(level) : "";
        var reset = _hasColors ? C.Reset : "";
        var isFirstLine = true;

        foreach (var message in messages)
        {
#if NET6_0_OR_GREATER
            var messageSpan = message.AsSpan();
            while (messageSpan.Length > 0)
            {
                var newlineIndex = messageSpan.IndexOf('\n');
                ReadOnlySpan<char> line;

                if (newlineIndex >= 0)
                {
                    line = messageSpan[..newlineIndex];
                    messageSpan = messageSpan[(newlineIndex + 1)..];
                }
                else
                {
                    line = messageSpan;
                    messageSpan = ReadOnlySpan<char>.Empty;
                }

                if (line.Length > 0 && line[^1] == '\r')
                {
                    line = line[..^1];
                }
#else
            foreach (var rawLine in message.Split('\n'))
            {
                var line = rawLine.TrimEnd('\r');
#endif

                if (isFirstLine)
                {
                    // 首行：标签 + 空格 + 消息
                    result.Append(_builder).Append(' ');
                    if (!string.IsNullOrEmpty(textColor))
                    {
                        result.Append(textColor);
                    }
                    result.Append(line);
                    if (!string.IsNullOrEmpty(reset))
                    {
                        result.Append(reset);
                    }

                    result.Append('\n');
                    isFirstLine = false;
                }
                else
                {
                    // 后续行：对齐空格 + 消息
                    result.Append(' ', tagLength + 1);
                    if (!string.IsNullOrEmpty(textColor))
                    {
                        result.Append(textColor);
                    }
                    result.Append(line);
                    if (!string.IsNullOrEmpty(reset))
                    {
                        result.Append(reset);
                    }

                    result.Append('\n');
                }
            }
        }

        // 移除最后一个换行符
        if (result.Length > 0 && result[^1] == '\n')
        {
            result.Length--;
        }

        return result.ToString();
    }

    /// <summary>
    /// 获取对应日志级别的级别前缀字符串（不包含颜色码）。
    /// </summary>
    /// <param name="level">日志级别。</param>
    /// <param name="alignment">对齐方式。</param>
    /// <returns>级别前缀字符串（不包含颜色码）。</returns>
    private string GetLevelPrefixText(LogLevel level, LoggerLevelPrefixAlignment alignment) => alignment switch
    {
        LoggerLevelPrefixAlignment.Compact => level switch
        {
            LogLevel.Trace /*      */ => "[TRC]",
            LogLevel.Debug /*      */ => "[DBG]",
            LogLevel.Information /**/ => "[INF]",
            LogLevel.Warning /*    */ => "[WRN]",
            LogLevel.Error /*      */ => "[ERR]",
            LogLevel.Critical /*   */ => "[FTL]",
            _ /*                   */ => "[   ]",
        },
        LoggerLevelPrefixAlignment.Short => level switch
        {
            LogLevel.Trace /*      */ => "[TRCE]",
            LogLevel.Debug /*      */ => "[DBUG]",
            LogLevel.Information /**/ => "[INFO]",
            LogLevel.Warning /*    */ => "[WARN]",
            LogLevel.Error /*      */ => "[ERRO]",
            LogLevel.Critical /*   */ => "[CRIT]",
            _ /*                   */ => "[    ]",
        },
        LoggerLevelPrefixAlignment.Full => level switch
        {
            LogLevel.Trace /*      */ => "[TRACE      ]",
            LogLevel.Debug /*      */ => "[DEBUG      ]",
            LogLevel.Information /**/ => "[INFORMATION]",
            LogLevel.Warning /*    */ => "[WARNING    ]",
            LogLevel.Error /*      */ => "[ERROR      ]",
            LogLevel.Critical /*   */ => "[CRITICAL   ]",
            _ /*                   */ => "[           ]",
        },
        _ => level switch // NoAlignment
        {
            LogLevel.Trace /*      */ => "[TRACE]",
            LogLevel.Debug /*      */ => "[DEBUG]",
            LogLevel.Information /**/ => "[INFORMATION]",
            LogLevel.Warning /*    */ => "[WARNING]",
            LogLevel.Error /*      */ => "[ERROR]",
            LogLevel.Critical /*   */ => "[CRITICAL]",
            _ /*                   */ => "[]",
        },
    };

    /// <summary>
    /// 获取对应日志级别的标签格式字符串转义字符。
    /// </summary>
    private string GetTagFormat(LogLevel level) => level switch
    {
        LogLevel.Trace => F.BrightBlack,
        LogLevel.Debug => F.Magenta,
        LogLevel.Information => $"{B.Green}{F.Black}",
        LogLevel.Warning => $"{B.Yellow}{F.Black}",
        LogLevel.Error => $"{B.Red}{F.Black}",
        LogLevel.Critical => $"{B.Black}{D.Bold}{F.Red}",
        _ => C.Reset,
    };

    /// <summary>
    /// 获取对应日志级别的文本格式字符串转义字符。
    /// </summary>
    private string GetTextFormat(LogLevel level) => level switch
    {
        LogLevel.Trace => F.BrightBlack,
        LogLevel.Debug => F.Magenta,
        LogLevel.Information => "",
        LogLevel.Warning => F.Yellow,
        LogLevel.Error => F.Red,
        LogLevel.Critical => $"{B.Red}{F.Black}",
        _ => "",
    };

    /// <summary>
    /// 获取异常标签。
    /// </summary>
    private string GetExceptionMarkerText(LogLevel level) => level switch
    {
        LogLevel.Warning => " ! ",
        LogLevel.Error => " X ",
        LogLevel.Critical => " X ",
        _ => "   ",
    };

    /// <summary>
    /// 转换为字符串。
    /// </summary>
    public readonly override string ToString() => _builder.ToString();
}

/// <summary>
/// 指定控制台日志输出的目标。
/// </summary>
public enum LoggerConsoleOutputTo
{
    /// <summary>
    /// 输出到标准输出流。
    /// </summary>
    StandardOutput,

    /// <summary>
    /// 输出到标准错误流。
    /// </summary>
    /// <remarks>
    /// 对于某些标准输出流有特殊用途的程序（例如 MCP 协议，或约定了输入输出格式的工具链），会考虑使用标准错误流来输出日志。
    /// </remarks>
    StandardError,
}

/// <summary>
/// 控制台日志格式化选项。
/// </summary>
[Flags]
public enum LoggerConsoleFormatOptions
{
    /// <summary>
    /// 纯文本格式（无时间戳，无颜色，无级别前缀）。
    /// </summary>
    PlainText = 0,

    /// <summary>
    /// 包含时间戳。
    /// </summary>
    Timestamp = 1 << 0,

    /// <summary>
    /// 包含 ANSI 颜色代码。
    /// </summary>
    Colors = 1 << 1,

    /// <summary>
    /// 包含文本级别标识符（如 [INFO], [ERROR]）。
    /// </summary>
    LevelPrefix = 1 << 2,

    /// <summary>
    /// 彩色控制台格式（时间戳 + 颜色），当控制台可用时默认使用此格式。
    /// </summary>
    ColorfulConsole = Timestamp | Colors,

    /// <summary>
    /// STDIO 重定向格式（时间戳 + 级别前缀，无颜色），当控制台不可用时默认使用此格式。
    /// </summary>
    StdioRedirected = Timestamp | LevelPrefix,
}

/// <summary>
/// 控制台日志级别前缀对齐方式。
/// </summary>
public enum LoggerLevelPrefixAlignment
{
    /// <summary>
    /// 不对齐。
    /// <code>
    /// [TRACE] = Trace
    /// [DEBUG] = Debug
    /// [INFORMATION] = Information
    /// [WARNING] = Warning
    /// [ERROR] = Error
    /// [CRITICAL] = Critical
    /// []
    /// </code>
    /// </summary>
    NoAlignment,

    /// <summary>
    /// 紧凑对齐（级别缩写 3 字符，含括号共 5 字符）。
    /// <code>
    /// [TRC] = Trace
    /// [DBG] = Debug
    /// [INF] = Information
    /// [WRN] = Warning
    /// [ERR] = Error
    /// [FTL] = Critical
    /// [   ]
    /// </code>
    /// </summary>
    Compact,

    /// <summary>
    /// 短名称对齐（级别缩写 4 字符，含括号共 6 字符）。
    /// <code>
    /// [TRCE] = Trace
    /// [DBUG] = Debug
    /// [INFO] = Information
    /// [WARN] = Warning
    /// [ERRO] = Error
    /// [CRIT] = Critical
    /// [    ]
    /// </code>
    /// </summary>
    Short,

    /// <summary>
    /// 完整名称对齐（级别缩写 11 字符，含括号共 13 字符）。
    /// <code>
    /// [TRACE      ] = Trace
    /// [DEBUG      ] = Debug
    /// [INFORMATION] = Information
    /// [WARNING    ] = Warning
    /// [ERROR      ] = Error
    /// [CRITICAL   ] = Critical
    /// [           ]
    /// </code>
    /// </summary>
    Full,
}

file static class Extensions
{
    extension(LoggerConsoleFormatOptions options)
    {
        /// <summary>
        /// 快速检查是否包含指定的标志位。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool FastHasFlag(LoggerConsoleFormatOptions flag) => (options & flag) == flag;
    }
}
