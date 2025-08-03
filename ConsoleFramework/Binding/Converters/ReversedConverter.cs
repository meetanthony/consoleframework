using System;

namespace ConsoleFramework.Binding.Converters;

public class ReversedConverter : IBindingConverter {
    readonly IBindingConverter _converter;

    public ReversedConverter(IBindingConverter converter) {
        _converter = converter;
    }

    public Type FirstType => _converter.SecondType;

    public Type SecondType => _converter.FirstType;

    public ConversionResult Convert(object tFirst) {
        return _converter.ConvertBack(tFirst);
    }

    public ConversionResult ConvertBack(object tSecond) {
        return _converter.Convert(tSecond);
    }
}