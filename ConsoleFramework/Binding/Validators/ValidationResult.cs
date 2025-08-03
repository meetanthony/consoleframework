using System;

namespace ConsoleFramework.Binding.Validators;

/// <summary>
/// Represents the result of data binding validation.
/// </summary>
public class ValidationResult
{
    public bool Valid { get; }

    public string? Message { get; }

    public ValidationResult(bool valid)
    {
        Valid = valid;
    }

    public ValidationResult(bool valid, String message)
    {
        Valid = valid;
        Message = message;
    }
}