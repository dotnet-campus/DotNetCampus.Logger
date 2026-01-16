#if NET6_0_OR_GREATER
using global::System.Runtime.CompilerServices;

namespace DotNetCampus.Logging;

/// <inheritdoc cref="DefaultInterpolatedStringHandler"/>
[InterpolatedStringHandler]
public ref struct LoggerInterpolatedStringHandler
{
    /// <inheritdoc cref="DefaultInterpolatedStringHandler"/>
    public LoggerInterpolatedStringHandler(int literalLength, int formattedCount)
    {
        _handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount);
    }

    private DefaultInterpolatedStringHandler _handler;

    /// <inheritdoc cref="DefaultInterpolatedStringHandler.AppendLiteral"/>
    public void AppendLiteral(string s)
    {
        ref var handler = ref _handler;
        handler.AppendLiteral(s);
    }

    /// <inheritdoc cref="DefaultInterpolatedStringHandler.AppendFormatted{T}(T)"/>
    public void AppendFormatted<T>(T value)
    {
        ref var handler = ref _handler;
        handler.AppendFormatted(value);
    }

    /// <inheritdoc cref="DefaultInterpolatedStringHandler.AppendFormatted{T}(T,string)"/>
    public void AppendFormatted<T>(T value, string format)
    {
        ref var handler = ref _handler;
        handler.AppendFormatted(value, format);
    }

    /// <inheritdoc cref="DefaultInterpolatedStringHandler.ToStringAndClear"/>
    public string ToStringAndClear()
    {
        ref var handle = ref _handler;
        return handle.ToStringAndClear();
    }

    /// <inheritdoc cref="DefaultInterpolatedStringHandler.ToString"/>
    public override string ToString()
    {
        ref var handler = ref _handler;
        return handler.ToString();
    }

    /// <summary>
    /// 废弃掉此字符串
    /// </summary>
    public void Discard()
    {
        // 这里的 ToStringAndClear 其实只是取其 Clear 的功能
        // 暂时先使用 DefaultInterpolatedStringHandler 提供的能力，后续再考虑是否需要优化
        ref var handler = ref _handler;
        handler.ToStringAndClear();
    }
}

#endif
