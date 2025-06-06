﻿using DotNetCampus.Logger.Properties;
using Microsoft.CodeAnalysis;
using static DotNetCampus.Logger.Properties.Localizations;

// ReSharper disable InconsistentNaming

namespace DotNetCampus.Logger;

/// <summary>
/// 包含日志库中的所有诊断。
/// </summary>
public class Diagnostics
{
    public static DiagnosticDescriptor DL0000_UnknownError { get; } = new(
        nameof(DL0000),
        Localize(nameof(DL0000)),
        Localize(nameof(DL0000_Message)),
        Categories.Useless,
        DiagnosticSeverity.Error,
        true);

    public static DiagnosticDescriptor DL0101_ProgramIsRecommendedToBePartial { get; } = new(
        nameof(DL1001),
        Localize(nameof(DL1001)),
        Localize(nameof(DL1001_Message)),
        Categories.Performance,
        DiagnosticSeverity.Info,
        true,
        description: Localize(DL1001_Description));

    private static class Categories
    {
        /// <summary>
        /// 可能产生 bug，则报告此诊断。
        /// </summary>
        public const string AvoidBugs = "DotNetCampus.AvoidBugs";

        /// <summary>
        /// 为了提供代码生成能力，则报告此诊断。
        /// </summary>
        public const string CodeFixOnly = "DotNetCampus.CodeFixOnly";

        /// <summary>
        /// 因编译要求而必须满足的条件没有满足，则报告此诊断。
        /// </summary>
        public const string Compiler = "DotNetCampus.Compiler";

        /// <summary>
        /// 因库内的机制限制，必须满足此要求后库才可正常工作，则报告此诊断。
        /// </summary>
        public const string Mechanism = "DotNetCampus.Mechanism";

        /// <summary>
        /// 为了代码可读性，使之更易于理解、方便调试，则报告此诊断。
        /// </summary>
        public const string Readable = "DotNetCampus.Readable";

        /// <summary>
        /// 为了提升性能，或避免性能问题，则报告此诊断。
        /// </summary>
        public const string Performance = "DotNetCampus.Performance";

        /// <summary>
        /// 能写得出来正常编译，但会引发运行时异常，则报告此诊断。
        /// </summary>
        public const string RuntimeException = "DotNetCampus.RuntimeException";

        /// <summary>
        /// 编写了无法生效的代码，则报告此诊断。
        /// </summary>
        public const string Useless = "DotNetCampus.Useless";
    }

    private static LocalizableString Localize(string key) => new LocalizableResourceString(key, Localizations.ResourceManager, typeof(Localizations));
}
