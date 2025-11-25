using System;
using System.Reflection;
using ConsoleFramework.Binding.Converters;
using ConsoleFramework.Xaml;

namespace ConsoleFramework.XamlIntegration;

/// <summary>
/// Converts Value to property type using specified converter.
/// </summary>
[MarkupExtension("Convert")]
public class ConvertMarkupExtension : IMarkupExtension
{
    /// <summary>
    /// Converter to be used.
    /// </summary>
    public IBindingConverter? Converter { get; set; }

    /// <summary>
    /// Value to convert. String or any object (if created using nested markup extension).
    /// </summary>
    public object? Value { get; set; }

    public object? ProvideValue(IMarkupExtensionContext? context)
    {
        if (null == Converter)
            throw new InvalidOperationException("Converter is null");

        if (null == Value)
            return null;

        if (null == context)
            throw new InvalidOperationException("Markup extension context is null");

        PropertyInfo? propertyInfo = context.Object.GetType().GetProperty(context.PropertyName);
        if (null == propertyInfo)
            throw new InvalidOperationException(
                $"Cannot find property {context.PropertyName} on type {context.Object.GetType().Name}");

        Type propertyType = propertyInfo.PropertyType;
        Type valueType = Value.GetType();
        Type firstType = Converter.FirstType;
        Type secondType = Converter.SecondType;
        if (firstType.IsAssignableFrom(propertyType) &&
            secondType.IsAssignableFrom(valueType))
        {
            ConversionResult conversionResult = Converter.ConvertBack(Value);
            if (!conversionResult.Success)
                throw new InvalidOperationException($"Cannot convert value : {conversionResult.FailReason}");
            return conversionResult.Value;
        }

        if (firstType.IsAssignableFrom(valueType)
            && secondType.IsAssignableFrom(propertyType))
        {
            ConversionResult conversionResult = Converter.Convert(Value);
            if (!conversionResult.Success)
                throw new InvalidOperationException($"Cannot convert value : {conversionResult.FailReason}");
            return conversionResult.Value;
        }

        throw new InvalidOperationException(
            string.Format("Cannot use specified converter to convert {0} to {1}",
                valueType.Name, propertyType.Name));
    }
}