using System;

namespace ConsoleFramework.Binding.Converters;

/// <summary>
/// Represents value conversion result.
/// </summary>
public class ConversionResult
{
    public object? Value { get; }

    public bool Success { get; }

    public string? FailReason { get; }

    public ConversionResult(Object value)
    {
        Value = value;
        Success = true;
    }

    public ConversionResult(bool success, String failReason)
    {
        Success = success;
        FailReason = failReason;
    }
}