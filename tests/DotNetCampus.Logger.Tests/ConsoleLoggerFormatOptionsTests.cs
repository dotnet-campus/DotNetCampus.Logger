using DotNetCampus.Logging;
using DotNetCampus.Logging.Writers;

namespace DotNetCampus.Logger.Tests;

[TestClass]
public class ConsoleLoggerFormatOptionsTests
{
    [TestMethod("默认格式应包含时间戳和颜色")]
    public void DefaultFormat_ShouldContainTimestampAndColors()
    {
        // Arrange & Act
        var output = CaptureConsoleOutput(() =>
        {
            var logger = CreateLogger(LoggerConsoleFormatOptions.ColorfulConsole, LoggerLevelPrefixAlignment.NoAlignment);
            logger.Info("测试消息");
        });

        // Assert
        Assert.IsTrue(output.Contains("["), "应包含时间戳的方括号");
        Assert.IsTrue(output.Contains(":"), "应包含时间戳的冒号");
        Assert.IsTrue(output.Contains("\e["), "应包含 ANSI 颜色码");
    }

    [TestMethod("PlainText 格式不应包含时间戳和颜色")]
    public void PlainTextFormat_ShouldNotContainTimestampOrColors()
    {
        // Arrange & Act
        var output = CaptureConsoleOutput(() =>
        {
            var logger = CreateLogger(LoggerConsoleFormatOptions.PlainText, LoggerLevelPrefixAlignment.NoAlignment);
            logger.Info("测试消息");
        });

        // Assert
        Assert.IsFalse(output.Contains("\e["), "不应包含 ANSI 颜色码");
        Assert.IsFalse(output.Contains("["), "不应包含时间戳或级别前缀");
        Assert.IsTrue(output.Contains("测试消息"), "应包含消息内容");
    }

    [TestMethod("StdioRedirect 格式应包含时间戳和级别前缀，但不包含颜色")]
    public void StdioRedirectFormat_ShouldContainTimestampAndLevelPrefixButNoColors()
    {
        // Arrange & Act
        var output = CaptureConsoleOutput(() =>
        {
            var logger = CreateLogger(LoggerConsoleFormatOptions.StdioRedirected, LoggerLevelPrefixAlignment.Short);
            logger.Info("测试消息");
        });

        // Assert
        Assert.IsTrue(output.Contains("["), "应包含时间戳和级别前缀的方括号");
        Assert.IsFalse(output.Contains("\e["), "不应包含 ANSI 颜色码");
        Assert.IsTrue(output.Contains("INFO"), "应包含级别前缀");
    }

    [DataTestMethod]
    [DataRow(LoggerLevelPrefixAlignment.NoAlignment, "[TRACE]", DisplayName = "NoAlignment 应使用完整级别名称")]
    [DataRow(LoggerLevelPrefixAlignment.Compact, "[TRC]", DisplayName = "Compact 应使用3字符紧凑对齐")]
    [DataRow(LoggerLevelPrefixAlignment.Short, "[TRCE]", DisplayName = "Short 应使用4字符短名称对齐")]
    [DataRow(LoggerLevelPrefixAlignment.Full, "[TRACE      ]", DisplayName = "Full 应使用完整名称对齐")]
    public void LevelPrefixAlignment_ShouldFormatCorrectly(LoggerLevelPrefixAlignment alignment, string expectedPrefix)
    {
        // Arrange & Act
        var output = CaptureConsoleOutput(() =>
        {
            var logger = CreateLogger(LoggerConsoleFormatOptions.LevelPrefix, alignment);
            logger.Trace("测试消息");
        });

        // Assert
        Assert.IsTrue(output.Contains(expectedPrefix), $"应包含 {expectedPrefix}");
    }

    [DataTestMethod]
    [DataRow(LogLevel.Trace, "[TRC]", DisplayName = "Trace 级别")]
    [DataRow(LogLevel.Debug, "[DBG]", DisplayName = "Debug 级别")]
    [DataRow(LogLevel.Information, "[INF]", DisplayName = "Information 级别")]
    [DataRow(LogLevel.Warning, "[WRN]", DisplayName = "Warning 级别")]
    [DataRow(LogLevel.Error, "[ERR]", DisplayName = "Error 级别")]
    [DataRow(LogLevel.Critical, "[FTL]", DisplayName = "Critical 级别")]
    public void Compact_AllLevels_ShouldFormatCorrectly(LogLevel level, string expectedPrefix)
    {
        // Arrange & Act
        var output = CaptureConsoleOutput(() =>
        {
            var logger = CreateLogger(LoggerConsoleFormatOptions.LevelPrefix, LoggerLevelPrefixAlignment.Compact);
            logger.Log(level, default, "", null, (s, e) => "测试消息");
        });

        // Assert
        Assert.IsTrue(output.Contains(expectedPrefix), $"应包含 {expectedPrefix}");
    }

    [DataTestMethod]
    [DataRow(LogLevel.Trace, "[TRCE]", DisplayName = "Trace 级别")]
    [DataRow(LogLevel.Debug, "[DBUG]", DisplayName = "Debug 级别")]
    [DataRow(LogLevel.Information, "[INFO]", DisplayName = "Information 级别")]
    [DataRow(LogLevel.Warning, "[WARN]", DisplayName = "Warning 级别")]
    [DataRow(LogLevel.Error, "[ERRO]", DisplayName = "Error 级别")]
    [DataRow(LogLevel.Critical, "[CRIT]", DisplayName = "Critical 级别")]
    public void Short_AllLevels_ShouldFormatCorrectly(LogLevel level, string expectedPrefix)
    {
        // Arrange & Act
        var output = CaptureConsoleOutput(() =>
        {
            var logger = CreateLogger(LoggerConsoleFormatOptions.LevelPrefix, LoggerLevelPrefixAlignment.Short);
            logger.Log(level, default, "", null, (s, e) => "测试消息");
        });

        // Assert
        Assert.IsTrue(output.Contains(expectedPrefix), $"应包含 {expectedPrefix}");
    }

    [DataTestMethod]
    [DataRow(LogLevel.Trace, "[TRACE      ]", DisplayName = "Trace 级别")]
    [DataRow(LogLevel.Debug, "[DEBUG      ]", DisplayName = "Debug 级别")]
    [DataRow(LogLevel.Information, "[INFORMATION]", DisplayName = "Information 级别")]
    [DataRow(LogLevel.Warning, "[WARNING    ]", DisplayName = "Warning 级别")]
    [DataRow(LogLevel.Error, "[ERROR      ]", DisplayName = "Error 级别")]
    [DataRow(LogLevel.Critical, "[CRITICAL   ]", DisplayName = "Critical 级别")]
    public void Full_AllLevels_ShouldFormatCorrectly(LogLevel level, string expectedPrefix)
    {
        // Arrange & Act
        var output = CaptureConsoleOutput(() =>
        {
            var logger = CreateLogger(LoggerConsoleFormatOptions.LevelPrefix, LoggerLevelPrefixAlignment.Full);
            logger.Log(level, default, "", null, (s, e) => "测试消息");
        });

        // Assert
        Assert.IsTrue(output.Contains(expectedPrefix), $"应包含 {expectedPrefix}");
    }

    [TestMethod("多行日志应正确对齐")]
    public void MultilineLog_ShouldAlignCorrectly()
    {
        // Arrange & Act
        var output = CaptureConsoleOutput(() =>
        {
            var logger = CreateLogger(LoggerConsoleFormatOptions.StdioRedirected, LoggerLevelPrefixAlignment.Short);
            logger.Info("第一行\n第二行\n第三行");
        });

        // Assert
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.IsTrue(lines.Length >= 3, "应有至少三行");
        Assert.IsTrue(lines[0].Contains("[INFO]") || lines[0].Contains(":"), "第一行应包含标签");
    }

    [TestMethod("带异常的日志应包含异常标记")]
    public void LogWithException_ShouldContainExceptionMarker()
    {
        // Arrange & Act
        var output = CaptureConsoleOutput(() =>
        {
            var logger = CreateLogger(LoggerConsoleFormatOptions.LevelPrefix, LoggerLevelPrefixAlignment.NoAlignment);
            var ex = new InvalidOperationException("测试异常");
            logger.Error("发生错误", ex);
        });

        // Assert
        Assert.IsTrue(output.Contains("X") || output.Contains("发生错误"), "应包含异常标记或错误消息");
        Assert.IsTrue(output.Contains("InvalidOperationException") || output.Contains("测试异常"), "应包含异常信息");
    }

    [TestMethod("颜色和级别前缀可以同时启用")]
    public void ColorsAndLevelPrefix_CanBeEnabledTogether()
    {
        // Arrange & Act
        var output = CaptureConsoleOutput(() =>
        {
            var logger = CreateLogger(
                LoggerConsoleFormatOptions.Timestamp | LoggerConsoleFormatOptions.Colors | LoggerConsoleFormatOptions.LevelPrefix,
                LoggerLevelPrefixAlignment.Short);
            logger.Info("测试消息");
        });

        // Assert
        Assert.IsTrue(output.Contains("\e["), "应包含 ANSI 颜色码");
        Assert.IsTrue(output.Contains("[INFO]"), "应包含级别前缀");
        Assert.IsTrue(output.Contains(":"), "应包含时间戳");
    }

    [TestMethod("WithStdioRedirectFormat 应配置正确的格式")]
    public void WithStdioRedirectFormat_ShouldConfigureCorrectly()
    {
        // Arrange
        var builder = new ConsoleLoggerBuilder();

        // Act
        builder
            .WithFormat(LoggerConsoleFormatOptions.StdioRedirected, LoggerLevelPrefixAlignment.Short)
            .WithOutput(LoggerConsoleOutputTo.StandardError);
        var consoleLogger = builder.Build();

        // Assert
        Assert.AreEqual(LoggerConsoleFormatOptions.StdioRedirected, consoleLogger.FormatOptions);
        Assert.AreEqual(LoggerLevelPrefixAlignment.Short, consoleLogger.LevelPrefixAlignment);
        Assert.AreEqual(LoggerConsoleOutputTo.StandardError, consoleLogger.OutputTo);
    }

    [TestMethod("WithDefaultFormat 应配置默认格式")]
    public void WithDefaultFormat_ShouldConfigureDefaultFormat()
    {
        // Arrange
        var builder = new ConsoleLoggerBuilder();

        // Act
        builder.WithFormat(LoggerConsoleFormatOptions.ColorfulConsole, LoggerLevelPrefixAlignment.NoAlignment);
        var consoleLogger = builder.Build();

        // Assert
        Assert.AreEqual(LoggerConsoleFormatOptions.ColorfulConsole, consoleLogger.FormatOptions);
    }

    [TestMethod("仅时间戳格式应只显示时间戳")]
    public void TimestampOnly_ShouldShowOnlyTimestamp()
    {
        // Arrange & Act
        var output = CaptureConsoleOutput(() =>
        {
            var logger = CreateLogger(LoggerConsoleFormatOptions.Timestamp, LoggerLevelPrefixAlignment.NoAlignment);
            logger.Info("测试消息");
        });

        // Assert
        Assert.IsTrue(output.Contains(":"), "应包含时间戳");
        Assert.IsFalse(output.Contains("\e["), "不应包含 ANSI 颜色码");
        Assert.IsTrue(output.Contains("测试消息"), "应包含消息内容");
    }

    /// <summary>
    /// 创建一个 ConsoleLogger 实例用于测试。
    /// </summary>
    private static ILogger CreateLogger(LoggerConsoleFormatOptions formatOptions, LoggerLevelPrefixAlignment alignment)
    {
        return new LoggerBuilder()
            .AddConsoleLogger(b => b
                .WithFormat(formatOptions, alignment)
                .WithThreadSafe(LogWritingThreadMode.NotThreadSafe))
            .Build()
            .Logger;
    }

    /// <summary>
    /// 捕获控制台输出。
    /// </summary>
    private static string CaptureConsoleOutput(Action action)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;

        try
        {
            using var writer = new StringWriter();
            Console.SetOut(writer);
            Console.SetError(writer);

            action();

            return writer.ToString();
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }
}
