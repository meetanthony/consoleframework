using System;
using System.Globalization;

namespace ConsoleFramework.Binding.Converters;

/// <summary>
/// Converter between String and Integer.
/// </summary>
public class StringToIntegerConverter : IBindingConverter {
    public Type FirstType => typeof(String);

    public Type SecondType => typeof(int);

    public ConversionResult Convert(Object s) {
        try {
            int value = int.Parse(( string ) s);
            return new ConversionResult(value);
        } catch (FormatException e) {
            return new ConversionResult(false, "Incorrect number");
        }
    }

    public ConversionResult ConvertBack(Object integer) {
        return new ConversionResult(((int) integer).ToString(CultureInfo.InvariantCulture));
    }
}