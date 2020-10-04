﻿using System;
using System.IO;
using System.Linq;

namespace dotnetCampus.Logging.IO
{
    /// <summary>
    /// 为 <see cref="TextFileLogger"/> 和其子类提供扩展方法。
    /// </summary>
    public static class TextFileLoggerExtensions
    {
        /// <summary>
        /// 在开始向日志文件中写入日志的时候，检查日志文件是否过大。如果过大（字节），则清空日志。
        /// </summary>
        /// <param name="logger">日志实例。</param>
        /// <param name="maxFileSize">大于此大小（字节）时清空日志。</param>
        /// <returns>构造器模式。</returns>
        public static TTextFileLogger WithMaxFileSize<TTextFileLogger>(this TTextFileLogger logger, long maxFileSize)
            where TTextFileLogger : TextFileLogger
        {
            if (maxFileSize <= 0)
            {
                throw new ArgumentException("日志文件限制的大小必须是正整数。", nameof(maxFileSize));
            }

            logger.AddInitializeInterceptor((file, _) =>
            {
                file = new FileInfo(file.FullName);
                if (file.Exists && file.Length > maxFileSize)
                {
                    File.WriteAllText(file.FullName, "");
                }
            });

            return logger;
        }

        /// <summary>
        /// 在开始向日志文件中写入日志的时候，检查日志文件行数是否过多。如果过多，则清空前面的行，保留最后的 <paramref name="newLineCountAfterLimitReached"/> 行。
        /// </summary>
        /// <param name="logger">日志实例。</param>
        /// <param name="maxLineCount">大于此行数时清空日志的前面行。</param>
        /// <param name="newLineCountAfterLimitReached">清空后应该保留行数。默认为完全不保留。</param>
        /// <returns>构造器模式。</returns>
        public static TTextFileLogger WithMaxLineCount<TTextFileLogger>(this TTextFileLogger logger, int maxLineCount, int newLineCountAfterLimitReached = 0)
            where TTextFileLogger : TextFileLogger
        {
            if (maxLineCount <= 0)
            {
                throw new ArgumentException("日志文件限制的行数必须是正整数。", nameof(maxLineCount));
            }

            if (newLineCountAfterLimitReached < 0)
            {
                throw new ArgumentException("日志文件清空后的行数必须是非负整数。", nameof(newLineCountAfterLimitReached));
            }

            if (newLineCountAfterLimitReached > maxLineCount)
            {
                throw new ArgumentException("日志文件清空后的行数不能大于最大限制行数。", nameof(newLineCountAfterLimitReached));
            }

            logger.AddInitializeInterceptor((file, _) =>
            {
                if (file.Exists)
                {
                    var lines = File.ReadAllLines(file.FullName);
                    if (lines.Length > maxLineCount)
                    {
                        if (newLineCountAfterLimitReached == 0)
                        {
                            File.WriteAllText(file.FullName, "");
                        }
                        else
                        {
                            File.WriteAllLines(file.FullName, lines.Skip(lines.Length - newLineCountAfterLimitReached));
                        }
                    }
                }
            });

            return logger;
        }

        /// <summary>
        /// 在开始向日志文件中写入日志的时候，是否覆盖曾经在文件内写入过的日志。
        /// </summary>
        /// <param name="logger">日志实例。</param>
        /// <param name="override">如果需要覆盖，请设置为 true。</param>
        /// <returns>构造器模式。</returns>
        public static TTextFileLogger WithWholeFileOverride<TTextFileLogger>(this TTextFileLogger logger, bool @override = true)
            where TTextFileLogger : TextFileLogger
        {
            if (@override)
            {
                logger.AddInitializeInterceptor((file, _) => File.Delete(file.FullName));
            }

            return logger;
        }

        /// <summary>
        /// 在开始向日志文件中写入日志的时候，是否覆盖曾经在文件内写入过的日志。
        /// </summary>
        /// <param name="logger">日志实例。</param>
        /// <param name="overrideForInfo">如果你希望首次写入信息日志时覆盖原来日志的整个文件，则设为 true；如果希望保留之前的日志而追加，则设为 false。</param>
        /// <param name="overrideForError">如果你希望首次写入错误日志时覆盖原来日志的整个文件，则设为 true；如果希望保留之前的日志而追加，则设为 false。</param>
        /// <returns>构造器模式。</returns>
        public static TTextFileLogger WithWholeFileOverride<TTextFileLogger>(this TTextFileLogger logger, bool overrideForInfo, bool overrideForError)
            where TTextFileLogger : TextFileLogger
        {
            if (overrideForInfo || overrideForError)
            {
                logger.AddInitializeInterceptor((file, level) =>
                {
                    if (File.Exists(file.FullName))
                    {
                        if (level == LogLevel.Fatal)
                        {
                            if (overrideForError)
                            {
                                File.WriteAllText(file.FullName, "");
                            }
                        }
                        else if (level == LogLevel.Warning)
                        {
                            if (overrideForInfo)
                            {
                                File.WriteAllText(file.FullName, "");
                            }
                        }
                    }
                });
            }

            return logger;
        }
    }
}
