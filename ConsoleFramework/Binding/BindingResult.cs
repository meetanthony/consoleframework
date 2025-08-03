using System;

namespace ConsoleFramework.Binding;

/// <summary>
/// Represents result of one synchronization operation from Target to Source.
/// If hasConversionError is true, message will represent conversion error message.
/// If hasValidationError is true, message will represent validation error message.
/// Both hasConversionError and hasValidationError cannot be set to true.
/// </summary>
public class BindingResult
{
    public bool HasError;
    public bool HasConversionError;
    public bool HasValidationError;
    public String? Message;

    public BindingResult(bool hasError)
    {
        HasError = hasError;
    }

    public BindingResult(bool hasConversionError, bool hasValidationError, String message)
    {
        HasConversionError = hasConversionError;
        HasValidationError = hasValidationError;
        HasError = hasConversionError || hasValidationError;
        Message = message;
    }
}