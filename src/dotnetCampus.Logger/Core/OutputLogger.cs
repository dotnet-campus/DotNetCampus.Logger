﻿using System;
using System.Runtime.CompilerServices;

namespace dotnetCampus.Logging.Core
{
    /// <summary>
    /// 为同步的日志记录提供公共基类。
    /// </summary>
    public abstract class OutputLogger : ILogger
    {
        private readonly object _locker = new object();
        private bool _isInitialized;

        /// <summary>
        /// 获取或设置日志的记录等级。
        /// 你可以在日志记录的过程当中随时修改日志等级，修改后会立刻生效。
        /// 默认是所有调用日志记录的方法都全部记录。
        /// </summary>
        public virtual LogLevel Level { get; set; } = LogLevel.Message;

        /// <inheritdoc />
        public void Trace(string? text, [CallerMemberName] string? callerMemberName = null)
            => LogCore(text, LogLevel.Detail, null, callerMemberName);

        /// <inheritdoc />
        public void Message(string? text, [CallerMemberName] string? callerMemberName = null)
            => LogCore(text, LogLevel.Message, null, callerMemberName);

        /// <inheritdoc />
        public void Warning(string? text, [CallerMemberName] string? callerMemberName = null)
            => LogCore(text, LogLevel.Warning, null, callerMemberName);

        /// <inheritdoc />
        public void Error(string text, Exception? exception = null, [CallerMemberName] string? callerMemberName = null)
            => LogCore(text, LogLevel.Error, exception?.ToString(), callerMemberName);

        /// <inheritdoc />
        public void Error(Exception exception, string? text = null, [CallerMemberName] string? callerMemberName = null)
            => LogCore(text, LogLevel.Error, exception.ToString(), callerMemberName);

        /// <inheritdoc />
        public void Fatal(Exception exception, string? text, [CallerMemberName] string? callerMemberName = null)
            => LogCore(text, LogLevel.Error, exception.ToString(), callerMemberName);

        /// <summary>
        /// 使用底层的日志记录方法来异步记录日志。
        /// </summary>
        /// <param name="text">要记录的日志的文本。</param>
        /// <param name="currentLevel">要记录的当条日志等级。</param>
        /// <param name="extraInfo">如果此条日志包含额外的信息，则在此传入额外的信息。</param>
        /// <param name="callerMemberName">此参数由编译器自动生成，请勿传入。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogCore(string? text, LogLevel currentLevel,
            string? extraInfo, [CallerMemberName] string? callerMemberName = null)
        {
            if (callerMemberName is null)
            {
                throw new ArgumentNullException(nameof(callerMemberName), "不允许显式将 CallerMemberName 指定成 null。");
            }

            if (string.IsNullOrWhiteSpace(callerMemberName))
            {
                throw new ArgumentException("不允许显式将 CallerMemberName 指定成空字符串。", nameof(callerMemberName));
            }

            if (Level < currentLevel)
            {
                return;
            }

            LogCore(new LogContext(DateTimeOffset.Now, callerMemberName, text ?? "", extraInfo, currentLevel));
        }

        /// <summary>
        /// 使用底层的日志记录方法来异步记录日志。
        /// </summary>
        /// <param name="context">当条日志上下文。</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal void LogCore(in LogContext context)
        {
            if (string.IsNullOrWhiteSpace(context.CallerMemberName))
            {
                throw new ArgumentException("不允许显式将 CallerMemberName 指定成 null 或空字符串。", nameof(LogContext.CallerMemberName));
            }

            if (Level < context.CurrentLevel)
            {
                return;
            }

            if (!_isInitialized)
            {
                lock (_locker)
                {
                    if (!_isInitialized)
                    {
                        _isInitialized = true;
                        OnInitialized();
                    }
                }
            }

            lock (_locker)
            {
                OnLogReceived(context);
            }
        }

        /// <summary>
        /// 派生类重写此方法时，可以在收到第一条日志的时候执行一些初始化操作。
        /// </summary>
        protected abstract void OnInitialized();

        /// <summary>
        /// 派生类重写此方法时，将日志输出。
        /// </summary>
        /// <param name="context">包含一条日志的所有上下文信息。</param>
        protected abstract void OnLogReceived(in LogContext context);
    }
}
