# 文件日志功能设计方案 (Rev 2.0)

## 1. 设计理念：约定优于配置

在早期设计中，我们试图构建一个“万能”的 `FileLogger`，但这导致了配置极其繁琐。新的设计理念转向 **“日志范式” (Logging Paradigms)**。

我们不再要求开发者思考“如何配置滚动策略、命名规则、缓冲区大小”，而是提供几种开箱即用的标准范式。同时，通过提取抽象基类，将最复杂的异步写入与线程安全逻辑封装在底层，降低具体范式的实现难度。

## 2. 核心架构设计

### 2.1 抽象基类：`AsyncTextFileLogger`

为了解决“多线程写入文本文件”的复杂性，我们引入一个抽象基类。它实现了 `ILogger` 和 `IFlushable` 接口，并负责处理**双缓冲 (Double Buffering)** 和 **线程切换**。

```csharp
/// <summary>
/// 为文本文件日志提供异步写入队列、线程安全和缓冲区刷新机制的基类。
/// 具体的写入行为（如打开哪个文件、是否滚动）由子类决定。
/// </summary>
public abstract class AsyncTextFileLogger : ILogger, IFlushable
{
    // 1. 实现 ILogger 接口：将日志转换为纯文本字符串，放入 Channel
    public void Log<TState>(...) 
    {
        var text = FormatLogEntry(...);
        _channel.Writer.TryWrite(text);
    }

    // 2. 实现 IFlushable 接口：等待队列清空
    public Task FlushAsync() { ... }

    // 3. 后台线程循环 (Background Loop)
    private async Task ProcessQueueAsync()
    {
        // 从 Channel 读取批次数据
        // 调用抽象方法写入磁盘
        await WriteBatchAsync(batchLogs);
    }

    // [核心扩展点] 子类需实现：将一批日志写入目标（具体的 Stream 管理在此处实现）
    protected abstract Task WriteBatchAsync(StringBuilder batchContent);
}
```

*   **组合 vs 继承**：考虑到文件日志天然需要持有 `Channel` 和 `Task`，且不同范式的核心差异仅在于 `IoStream` 的获取方式（是追加、还是换新文件），继承 `AsyncTextFileLogger` 是最自然且理解成本最低的实现方式。

### 2.2 内置日志范式 (Built-in Paradigms)

基于上述基类，库将内置三种标准范式，覆盖 95% 的使用场景。

#### 2.2.1 简单日志 (Single File Style)
*   **类名**：`SingleFileLogger`
*   **行为**：永远只向同一个文件追加日志。不滚动、不改名、不清理。
*   **适用**：极其简单的工具、测试 demo。
*   **API**：`.AddSimpleFile("app.log")`

#### 2.2.2 滚动日志 (Rolling Style)
*   **类名**：`RollingFileLogger`
*   **行为**：
    *   支持按**文件大小**或**时间跨度**滚动。
    *   内置安全的文件清理逻辑。
*   **适用**：长期运行的服务、复杂的桌面应用。
*   **API**：`.AddRollingFile("logs/app.log", size: 10MB, interval: Infinite)`

#### 2.2.3 团队标准范式 (Dual File Style / "All & Last")
*   **行为**：这是本团队推荐的最佳实践，一次性注册两个 Logger 实例，实现“双轨”记录：
    1.  **Last Log**（独占模式）：`logs/Last_Run.log`。每次启动时覆盖，记录本次运行的完整生命周期，用于调试最近一次崩溃。
    2.  **All Log**（滚动模式）：`logs/All.log`。长期追加，按大小滚动，保留最近 N 天，用于追溯历史问题。
*   **API**：`.AddDualFileLog()` (零配置，全部使用默认最佳实践路径)

## 3. 详细详细设计

### 3.1 滚动在何处发生？

滚动逻辑不再由外部 Options 控制，而是内聚在 `RollingFileLogger.WriteBatchAsync` 中：

1.  **Check**：在写入一批日志前，检查当前文件流 `Length` 是否超过阈值，或当前时间是否跨越了 `Interval`。
2.  **Roll**：如果满足条件，关闭当前 Stream，重命名文件（例如 `app.log` -> `app.2026.bak`），重新创建 `app.log`。
3.  **Write**：写入日志。

### 3.2 自动清理的安全性挑战

针对“业务随随便便命名”的文件清理难题，我们采取**极其保守的白名单策略**：

*   **原则**：库只清理**绝对能确定的、由本库生成的**文件。
*   **实现**：
    *   对于 `RollingFileLogger`，开发者不需要指定复杂的通配符。
    *   在发生滚动时，Logger 知道自己生成了 `app.log.1`, `app.log.2` 等。
    *   清理逻辑仅扫描 `Directory` 下完全匹配 `"{OriginalName}.{Pattern}"` 的文件。
    *   **不处理**：如果用户改了文件名，或者把日志放在了 `C:\Windows` 下，库**绝不**尝试删除父文件夹。

### 3.3 格式化不需要 Json

*   移除 `ILogFormatter`。所有文件日志默认采用类似于 `[yyyy-MM-dd HH:mm:ss.fff] [Level] Message` 的紧凑标准纯文本格式。
*   如果业务方需要记录 Json，应在调用 `Log.Info(jsonString)` 时自行序列化好。

## 4. API 设计：从配置地狱到一键接入

### 4.1 传统方式（已废弃）
*   ~~`.AddFile(opt => opt.Rolling = ... opt.Path = ...)`~~ (太繁琐，太容易出错)

### 4.2 全新 Builder 扩展

```csharp
public static class FileLoggerExtensions
{
    // 范式 1: 简单只写一个文件
    public static LoggerBuilder AddSimpleFile(this LoggerBuilder builder, string filePath);

    // 范式 2: 标准滚动日志 (仅暴露最核心参数)
    public static LoggerBuilder AddRollingFile(this LoggerBuilder builder, 
        string filePath, 
        FileSize maxFileSize = default, // 默认 10MB
        int maxFileCount = 20,          // 默认 20 个
        LogLevel minLevel = LogLevel.Trace);

    // 范式 3: 团队标准双文件策略 (强烈推荐)
    // 自动配置 Last_Run.log 和 All.log，无需任何参数，也可以传入 logDirectory 修改根目录
    public static LoggerBuilder AddDualFileLog(this LoggerBuilder builder, string logDirectory = "logs");
}
```

### 4.3 最终用户代码示例

```csharp
// Scenario: 真实项目接入
var logger = new LoggerBuilder()
    .AddConsole()
    // 一句话接入团队标准范式
    // 自动会有:
    // 1. logs/Last_Run.log (本次运行全量日志)
    // 2. logs/All.log (历史滚动归档)
    .AddDualFileLog() 
    .Build();

// Scenario: 写个小工具
var quickLogger = new LoggerBuilder()
    .AddConsole()
    .AddSimpleFile("output.log")
    .Build();
```

## 5. 待解决与风险点

1.  **TextFileLoggerWriter 命名**：建议直接简化为 `AsyncTextFileLogger`，表明它也是一个 Logger，可以直接被 Builder 使用。
2.  **清理逻辑的边界**：虽然我们采用了保守策略，但如果业务程序并发运行（多实例），多个进程同时试图滚动物理上的同一个 `app.log`，可能会导致文件占用异常。
    *   *规避方案*：在 `DualFileLog` 范式中，`Last_Run.log` 可以加上 PID 后缀，或者内部处理文件占用（退避策略）。`All.log` 使用追加模式，尽量减少滚动频率（仅按大小滚动，不按时间），并接受多进程下文件大小可能略微超标的事实。

