﻿using System;

namespace dotnetCampus.Logger.Analyzer.Utils;

/// <summary>
/// 嵌入的文本资源的数据。
/// </summary>
/// <param name="EmbeddedName">文件在嵌入的资源中的名称。</param>
/// <param name="Content">文件的文本内容。</param>
internal readonly record struct EmbeddedSourceFile(string EmbeddedName, string Content)
{
    /// <summary>
    /// 根据资源名称猜测文件的无扩展名的名称。
    /// </summary>
    /// <returns>无扩展名的文件名。</returns>
    public string GuessFileNameWithoutExtension()
    {
        var span = EmbeddedName.AsSpan();
        var secondLastDotIndex = 0;
        var lastDotIndex = 0;
        for (var i = 0; i < span.Length; i++)
        {
            var c = span[i];
            if (c is '.')
            {
                secondLastDotIndex = lastDotIndex;
                lastDotIndex = i;
            }
        }
        var guessedName = lastDotIndex is 0
            ? span
            : span.Slice(secondLastDotIndex + 1, lastDotIndex - secondLastDotIndex - 1);
        return guessedName.ToString();
    }
}
