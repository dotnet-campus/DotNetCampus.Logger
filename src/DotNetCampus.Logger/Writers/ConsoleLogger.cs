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

        // 使用新的动态标签生成
        var tag = GetTag(logLevel);
        var emptyTag = GetEmptyTag();
        var hasColors = FormatOptions.FastHasFlag(LoggerConsoleFormatOptions.Colors);
        var textColor = hasColors ? logLevel.ToTextColor() : "";

        var firstLineStyle = string.IsNullOrEmpty(tag) ? textColor : $"{tag} {textColor}";
        var otherLineStyle = string.IsNullOrEmpty(emptyTag) ? textColor : $"{emptyTag} {textColor}";

        LogCore(logLevel, exception, message, firstLineStyle, otherLineStyle);
    }

    /// <summary>
    /// 记录日志。在必要的情况下会保证线程安全。
    /// </summary>
    /// <param name="logLevel">日志严重程度。</param>
    /// <param name="exception">如果有异常，则记录异常。</param>
    /// <param name="message">日志的消息内容。</param>
    /// <param name="firstLineStyle">日志的首行样式（如果只有一行，也使用此样式）。</param>
    /// <param name="otherLineStyle">日志的其他行样式。</param>
    private void LogCore(LogLevel logLevel, Exception? exception, string message, string firstLineStyle, string otherLineStyle) => CoreWriter.Do(() =>
    {
        var hasColors = FormatOptions.FastHasFlag(LoggerConsoleFormatOptions.Colors);
        var reset = hasColors ? C.Reset : "";
        var emptyTag = GetEmptyTag();

        if (_repeat.RepeatOrResetLastLog(logLevel, message, exception) is var count and > 1)
        {
            // 仅在控制台输出且启用颜色时才使用光标控制
            if (_isConsoleOutput && hasColors)
            {
                string SingleLineFormatter(string m, int i) => $"{firstLineStyle}{m}{reset}";
                var repeatMessage = hasColors
                    ? $"{reset}{F.BrightBlack}上述日志已重复 {count} 次{reset}"
                    : $"上述日志已重复 {count} 次";
                ConsoleMultilineMessage(repeatMessage, SingleLineFormatter, forceSingleLine: true);
            }
            else
            {
                var repeatSuffix = hasColors
                    ? $" {reset}{F.BrightBlack}(重复 {count} 次){reset}"
                    : $" (重复 {count} 次)";

                string AppendFirstLineFormatter(string m, int i) => $"{firstLineStyle}{m}{repeatSuffix}";

                string OtherLineFormatter(string m, int i) => logLevel is LogLevel.Critical
                    ? $"{otherLineStyle}{m.PadRight(SafeGetSpaceCount(" ", emptyTag))}{reset}"
                    : $"{otherLineStyle}{m}{reset}";

                ConsoleMultilineMessage(message, AppendFirstLineFormatter, OtherLineFormatter);
            }
        }
        else if (exception is null)
        {
            string FirstLineFormatter(string m, int i) => logLevel is LogLevel.Critical
                ? $"{firstLineStyle}{m.PadRight(SafeGetSpaceCount(" ", emptyTag))}{reset}"
                : $"{firstLineStyle}{m}{reset}";

            string OtherLineFormatter(string m, int i) => logLevel is LogLevel.Critical
                ? $"{otherLineStyle}{m.PadRight(SafeGetSpaceCount(" ", emptyTag))}{reset}"
                : $"{otherLineStyle}{m}{reset}";

            ConsoleMultilineMessage(message, FirstLineFormatter, OtherLineFormatter);
        }
        else
        {
            var exceptionTag = logLevel.ToExceptionTag(FormatOptions);
            var emptyExceptionTag = new string(' ', exceptionTag.Length);

            string FirstLineFormatter(string m, int i) => logLevel is LogLevel.Critical
                ? $"{firstLineStyle}{m.PadRight(SafeGetSpaceCount(" ", emptyTag))}{reset}"
                : $"{firstLineStyle}{m}{reset}";

            string OtherLineFormatter(string m, int i) => (logLevel, i) switch
            {
                (LogLevel.Critical, 1) => $"{otherLineStyle}{exceptionTag}{m.PadRight(SafeGetSpaceCount(" ", emptyTag, emptyExceptionTag))}{reset}",
                (LogLevel.Critical, _) => $"{otherLineStyle}{m.PadRight(SafeGetSpaceCount(" ", emptyTag))}{reset}",
                (_, 1) => $"{otherLineStyle}{exceptionTag}{m}{reset}",
                _ => $"{otherLineStyle}{m}{reset}",
            };

            ConsoleMultilineMessage($"""
                {message}
                {exception}
                """, FirstLineFormatter, OtherLineFormatter);
        }
    });

    /// <summary>
    /// 清空当前行并移动光标到上一行。
    /// </summary>
    /// <param name="repeatCount">此移动光标，是因为日志已重复第几次。</param>
    private void ClearAndMoveToLastLine(int repeatCount)
    {
        var hasColors = FormatOptions.FastHasFlag(LoggerConsoleFormatOptions.Colors);

        // 仅在控制台输出且启用颜色时才使用光标控制
        if (!_isConsoleOutput || !hasColors)
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
    /// 记录多行日志。
    /// </summary>
    /// <param name="message">多行日志消息内容。</param>
    /// <param name="firstLineFormatter">首行格式化。</param>
    /// <param name="otherLineFormatter">其他行格式化（如果设置为 <see langword="null"/> 则与首行相同。</param>
    /// <param name="forceSingleLine">将多行消息内容视为单行内容记录。</param>
    private void ConsoleMultilineMessage(string message,
        Func<string, int, string?> firstLineFormatter, Func<string, int, string?>? otherLineFormatter = null, bool forceSingleLine = false)
    {
        if (forceSingleLine || !message.Contains('\n'))
        {
            SafeWriteLine(firstLineFormatter(message, 0));
        }
        else
        {
            var lineIndex = 0;
            using var reader = new StringReader(message);
            while (reader.ReadLine() is { } line)
            {
                var formatter = lineIndex is 0
                    ? firstLineFormatter
                    : otherLineFormatter ?? firstLineFormatter;
                SafeWriteLine(formatter(line, lineIndex));
                lineIndex++;
            }
        }
    }

    /// <summary>
    /// 安全地写入一行日志，避免出现 IO 异常。
    /// </summary>
    /// <param name="message"></param>
    internal void SafeWriteLine(string? message)
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
    /// 安全地获取填充控制台行所需的剩余空白字符数量，避免出现 IO 异常或超出控制台宽度的情况。<br/>
    /// </summary>
    /// <param name="messages">填充剩余字符数量前，该行会输出的字符串。</param>
    /// <returns>填充控制台行所需的剩余字符数量。</returns>
    private int SafeGetSpaceCount(params
#if NET6_0_OR_GREATER
        ReadOnlySpan<string>
#else
        string[]
#endif
        messages)
    {
        var width = SafeGetBufferWidth();
        if (width == 0)
        {
            return 0;
        }

        var paddingWidth = width;

        for (var i = 0; i < messages.Length; i++)
        {
            paddingWidth -= messages[i].Length;
        }

        if (paddingWidth < 0)
        {
            return 0;
        }

        return paddingWidth;
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

    #region 动态标签生成

    /// <summary>
    /// 获取日志标签（包括时间戳、级别前缀、颜色）。
    /// </summary>
    private string GetTag(LogLevel level)
    {
        var hasTimestamp = FormatOptions.FastHasFlag(LoggerConsoleFormatOptions.Timestamp);
        var hasColors = FormatOptions.FastHasFlag(LoggerConsoleFormatOptions.Colors);
        var hasLevelPrefix = FormatOptions.FastHasFlag(LoggerConsoleFormatOptions.LevelPrefix);

        var parts = new List<string>();

        // 时间戳部分
        if (hasTimestamp)
        {
            var timestamp = $"[{DateTime.Now:HH:mm:ss.fff}]";
            parts.Add(hasColors ? $"{level.ToTagColor()}{timestamp}{C.Reset}" : timestamp);
        }

        // 级别前缀部分
        if (hasLevelPrefix)
        {
            var levelPrefix = level.ToPrefixTag(LevelPrefixAlignment);
            parts.Add(levelPrefix);
        }

        return string.Join("", parts);
    }

    /// <summary>
    /// 获取空标签（用于多行日志的对齐）。
    /// </summary>
    private string GetEmptyTag()
    {
        var hasTimestamp = FormatOptions.FastHasFlag(LoggerConsoleFormatOptions.Timestamp);
        var hasColors = FormatOptions.FastHasFlag(LoggerConsoleFormatOptions.Colors);
        var hasLevelPrefix = FormatOptions.FastHasFlag(LoggerConsoleFormatOptions.LevelPrefix);

        var length = 0;

        if (hasTimestamp)
        {
            length += 14; // [HH:mm:ss.fff]
        }

        if (hasLevelPrefix)
        {
            length += LevelPrefixAlignment switch
            {
                LoggerLevelPrefixAlignment.ShortAligned => 5, // [INF]
                LoggerLevelPrefixAlignment.FullAligned => 10, // [CRITICAL]
                _ => 0, // 长度不固定
            };
        }

        if (length == 0)
        {
            return "";
        }

        var emptyTag = new string(' ', length);

        if (hasColors)
        {
            return $"{F.BrightBlack}{emptyTag}{C.Reset}";
        }

        return emptyTag;
    }

    #endregion
}

file static class Extensions
{
    extension(LogLevel level)
    {
        /// <summary>
        /// 将日志级别转换为对应的文本级别前缀。
        /// </summary>
        internal string ToPrefixTag(LoggerLevelPrefixAlignment alignment) => alignment switch
        {
            LoggerLevelPrefixAlignment.ShortAligned => level switch
            {
                LogLevel.Trace /*      */ => "[TRC]",
                LogLevel.Debug /*      */ => "[DBG]",
                LogLevel.Information /**/ => "[INF]",
                LogLevel.Warning /*    */ => "[WAR]",
                LogLevel.Error /*      */ => "[ERR]",
                LogLevel.Critical /*   */ => "[CRT]",
                _ /*                   */ => "[   ]",
            },
            LoggerLevelPrefixAlignment.FullAligned => level switch
            {
                LogLevel.Trace /*      */ => "[TRACE   ]",
                LogLevel.Debug /*      */ => "[DEBUG   ]",
                LogLevel.Information /**/ => "[INFO    ]",
                LogLevel.Warning /*    */ => "[WARNING ]",
                LogLevel.Error /*      */ => "[ERROR   ]",
                LogLevel.Critical /*   */ => "[CRITICAL]",
                _ /*                   */ => "[        ]",
            },
            _ => level switch // NoAlignment
            {
                LogLevel.Trace /*      */ => "[TRACE]",
                LogLevel.Debug /*      */ => "[DEBUG]",
                LogLevel.Information /**/ => "[INFO]",
                LogLevel.Warning /*    */ => "[WARNING]",
                LogLevel.Error /*      */ => "[ERROR]",
                LogLevel.Critical /*   */ => "[CRITICAL]",
                _ /*                   */ => "[]",
            },
        };

        /// <summary>
        /// 获取级别对应的前缀标签的颜色转义字符。
        /// </summary>
        internal string ToTagColor() => level switch
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
        /// 获取级别对应的日志文本本体颜色的转义字符。
        /// </summary>
        internal string ToTextColor() => level switch
        {
            LogLevel.Trace => F.BrightBlack,
            LogLevel.Debug => F.Magenta,
            LogLevel.Information => C.Reset,
            LogLevel.Warning => F.Yellow,
            LogLevel.Error => F.Red,
            LogLevel.Critical => $"{B.Red}{F.Black}",
            _ => C.Reset,
        };

        /// <summary>
        /// 获取异常标签。
        /// </summary>
        internal string ToExceptionTag(LoggerConsoleFormatOptions formatOptions)
        {
            if (!formatOptions.FastHasFlag(LoggerConsoleFormatOptions.Colors))
            {
                return level switch
                {
                    LogLevel.Warning => " ! ",
                    LogLevel.Error => " X ",
                    LogLevel.Critical => " X ",
                    _ => "   ",
                };
            }

            return $"{level.ToTagColor()}{level switch
            {
                LogLevel.Warning => "!",
                LogLevel.Error => "X",
                LogLevel.Critical => "X",
                _ => " ",
            }}{C.Reset}{level.ToTextColor()} ";
        }
    }

    extension(LoggerConsoleFormatOptions options)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool FastHasFlag(LoggerConsoleFormatOptions flag)
        {
            return (options & flag) == flag;
        }
    }
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
    /// 不对齐（紧凑格式）。
    /// 示例：[TRACE], [INFORMATION], [ERROR]
    /// </summary>
    NoAlignment,

    /// <summary>
    /// 短名称对齐（4-5 字符）。
    /// 示例：[TRC ], [INFO], [ERR ], [CRIT]
    /// </summary>
    ShortAligned,

    /// <summary>
    /// 完整名称对齐（11 字符）。
    /// 示例：[TRACE      ], [INFORMATION], [ERROR      ]
    /// </summary>
    FullAligned,
}
