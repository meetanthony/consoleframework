using System;
using System.ComponentModel;
using System.Reflection;
using ConsoleFramework.Binding;
using ConsoleFramework.Binding.Converters;
using ConsoleFramework.Xaml;

namespace ConsoleFramework.XamlIntegration;

[MarkupExtension("Binding")]
class BindingMarkupExtension : IMarkupExtension
{
    public BindingMarkupExtension()
    {
    }

    public BindingMarkupExtension(string path)
    {
        Path = path;
    }

    public string? Path { get; set; }

    public string? Mode { get; set; }

    public object? Source { get; set; }

    /// <summary>
    /// Converter to be used.
    /// </summary>
    public IBindingConverter? Converter { get; set; }

    public object? ProvideValue(IMarkupExtensionContext? context)
    {
        if (context == null)
            return null;

        object realSource = Source ?? context.DataContext;
        if (null != realSource && !(realSource is INotifyPropertyChanged))
        {
            throw new ArgumentException("Source must be INotifyPropertyChanged to use bindings");
        }

        if (null != realSource)
        {
            BindingMode mode = BindingMode.Default;
            if (Path != null)
            {
                Type enumType = typeof(BindingMode);
                string[] enumNames = enumType.GetTypeInfo().GetEnumNames();
                for (int i = 0, len = enumNames.Length; i < len; i++)
                {
                    if (enumNames[i] == Mode)
                    {
                        var enumValue = enumType.GetTypeInfo().GetEnumValues().GetValue(i);
                        if (enumValue != null)
                            mode = (BindingMode)Enum.ToObject(enumType, enumValue);
                        break;
                    }
                }
            }

            BindingBase binding = new BindingBase(context.Object, context.PropertyName,
                (INotifyPropertyChanged)realSource, Path, mode);
            if (Converter != null)
                binding.Converter = Converter;
            binding.Bind();
            // mb return actual property value ?
            return null;
        }

        return null;
    }
}