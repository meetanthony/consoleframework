using System;
using System.Collections.Generic;
using ConsoleFramework.Binding.Adapters;
using ConsoleFramework.Binding.Converters;

namespace ConsoleFramework.Binding;

/// <summary>
/// Contains converters, validators and adapters.
/// </summary>
public class BindingSettingsBase
{
    public static BindingSettingsBase DefaultSettings;

    static BindingSettingsBase()
    {
        DefaultSettings = new BindingSettingsBase();
        DefaultSettings.InitializeDefault();
    }

    private readonly Dictionary<Type, Dictionary<Type, IBindingConverter>> _converters =
        new Dictionary<Type, Dictionary<Type, IBindingConverter>>();

    private readonly Dictionary<Type, IBindingAdapter> _adapters = new Dictionary<Type, IBindingAdapter>();

    /// <summary>
    /// Adds default set of converters and ui adapters.
    /// </summary>
    public void InitializeDefault()
    {
        AddConverter(new StringToIntegerConverter());
    }

    public void AddAdapter(IBindingAdapter adapter)
    {
        Type targetClazz = adapter.TargetType;
        if (_adapters.ContainsKey(targetClazz))
            throw new Exception(String.Format("Adapter for class {0} is already registered.", targetClazz.Name));
        _adapters.Add(targetClazz, adapter);
    }

    public IBindingAdapter GetAdapterFor(Type clazz)
    {
        IBindingAdapter adapter = _adapters[clazz];
        if (null == adapter) throw new Exception(String.Format("Adapter for class {0} not found.", clazz.Name));
        return adapter;
    }

    public void AddConverter(IBindingConverter converter)
    {
        RegisterConverter(converter);
        RegisterConverter(new ReversedConverter(converter));
    }

    private void RegisterConverter(IBindingConverter converter)
    {
        Type first = converter.FirstType;
        Type second = converter.SecondType;
        if (_converters.ContainsKey(first))
        {
            Dictionary<Type, IBindingConverter> firstClassConverters = _converters[first];
            if (firstClassConverters.ContainsKey(second))
            {
                throw new Exception(String.Format("Converter for {0} -> {1} classes is already registered.", first.Name,
                    second.Name));
            }

            firstClassConverters.Add(second, converter);
        }
        else
        {
            Dictionary<Type, IBindingConverter> firstClassConverters = new Dictionary<Type, IBindingConverter>();
            firstClassConverters.Add(second, converter);
            _converters.Add(first, firstClassConverters);
        }
    }

    public IBindingConverter? GetConverterFor(Type first, Type second)
    {
        if (!_converters.ContainsKey(first))
            return null;
        Dictionary<Type, IBindingConverter> firstClassConverters = _converters[first];
        if (!firstClassConverters.ContainsKey(second))
            return null;
        return firstClassConverters[second];
    }
}