using System;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using ConsoleFramework.Binding.Adapters;
using ConsoleFramework.Binding.Converters;
using ConsoleFramework.Binding.Observables;
using ConsoleFramework.Binding.Validators;
using ListChangedEventArgs = ConsoleFramework.Binding.Observables.ListChangedEventArgs;

namespace ConsoleFramework.Binding;

/// <summary>
/// Handler of binding operation when data is transferred from Target to Source.
/// </summary>
public delegate void OnBindingHandler(BindingResult result);

/// <summary>
/// Provides data sync connection between two objects - source and target. Both source and target can be just objects,
/// but if you want to bind to object that does not implement <see cref="INotifyPropertyChanged"/>,
/// you should use it as target and use appropriate adapter (<see cref="IBindingAdapter"/> implementation). One Binding instance connects
/// one source property and one target property.
/// </summary>
public class BindingBase
{
    protected Object Target;
    private readonly String _targetProperty;
    protected INotifyPropertyChanged Source;
    private readonly String _sourceProperty;
    private bool _bound;
    private readonly BindingMode _mode;
    protected BindingMode RealMode;
    private readonly BindingSettingsBase _settings;

    // This may be initialized using true in inherited classes for specialized binding
    protected bool NeedAdapterAnyway = false;

    private IBindingAdapter? _adapter;
    private PropertyInfo? _targetPropertyInfo;
    private PropertyInfo? _sourcePropertyInfo;

    // Converts target to source and back
    private IBindingConverter? _converter;

    // Used instead targetListener if target does not implement INotifyPropertyChanged
    protected Object? TargetListenerWrapper;

    // Flags used to avoid infinite recursive loop
    private bool _ignoreSourceListener;
    protected bool IgnoreTargetListener;

    // Collections synchronization support
    private bool _sourceIsObservable;
    private IList? _targetList;

    private bool _targetIsObservable;
    private IList? _sourceList;

    private IBindingValidator? _validator;

    /// <summary>
    /// If target value conversion or validation fails, the source property will be set to null
    /// if this flag is set to true. Otherwise the source property setter won't be called.
    /// Default value is true
    /// </summary>
    public bool UpdateSourceIfBindingFails { get; set; } = true;

    /// <summary>
    /// Event will be invoked when data goes from Target to Source.
    /// </summary>
    public event OnBindingHandler? OnBinding;

    /// <summary>
    /// Validator triggered when data flows from Target to Source.
    /// </summary>
    public IBindingValidator? Validator
    {
        get => _validator;
        set
        {
            if (_bound) throw new InvalidOperationException("Cannot change validator when binding is active.");
            _validator = value;
        }
    }

    /// <summary>
    /// BindingAdapter used as bridge to Target if Target doesn't
    /// implement INotifyPropertyChanged.
    /// </summary>
    public IBindingAdapter? Adapter
    {
        get => _adapter;
        set
        {
            if (_bound) throw new InvalidOperationException("Cannot change adapter when binding is active.");
            _adapter = value;
        }
    }

    /// <summary>
    /// Converter used for values conversion between Source and Target.
    /// </summary>
    public IBindingConverter? Converter
    {
        get => _converter;
        set
        {
            if (_bound) throw new InvalidOperationException("Cannot change converter when binding is active.");
            _converter = value;
        }
    }

    public BindingBase(Object target, String targetProperty, INotifyPropertyChanged source, String sourceProperty) :
        this(target, targetProperty, source, sourceProperty, BindingMode.Default)
    {
    }

    public BindingBase(Object target, String targetProperty, INotifyPropertyChanged source,
        String sourceProperty, BindingMode mode) :
        this(target, targetProperty, source, sourceProperty, mode, BindingSettingsBase.DefaultSettings)
    {
    }

    public BindingBase(Object target, String targetProperty, INotifyPropertyChanged source,
        String sourceProperty, BindingMode mode, BindingSettingsBase settings)
    {
        if (null == target) throw new ArgumentNullException("target");
        if (string.IsNullOrEmpty(targetProperty)) throw new ArgumentException("targetProperty is null or empty");
        if (null == source) throw new ArgumentNullException("source");
        if (string.IsNullOrEmpty(sourceProperty)) throw new ArgumentException("sourceProperty is null or empty");
        //
        Target = target;
        _targetProperty = targetProperty;
        Source = source;
        _sourceProperty = sourceProperty;
        _mode = mode;
        _bound = false;
        _settings = settings;
    }

    /// <summary>
    /// Forces a data transfer from the binding source property to the binding target property.
    /// </summary>
    public void UpdateTarget()
    {
        if (RealMode != BindingMode.OneTime && RealMode != BindingMode.OneWay && RealMode != BindingMode.TwoWay)
            throw new Exception(String.Format("Cannot update target in {0} binding mode.", RealMode));
        IgnoreTargetListener = true;
        try
        {
            Object? sourceValue = _sourcePropertyInfo?.GetGetMethod()?.Invoke(Source, null);
            if (_sourceIsObservable)
            {
                // work with observable list
                // We should take target list and initialize it using source items
                IList? targetListNow;
                if (_adapter == null)
                {
                    targetListNow = (IList?)_targetPropertyInfo?.GetGetMethod()?.Invoke(Target, null);
                }
                else
                {
                    targetListNow = (IList)_adapter.GetValue(Target, _targetProperty);
                }

                if (sourceValue == null)
                {
                    if (null != targetListNow)
                        targetListNow.Clear();
                }
                else
                {
                    if (null != targetListNow)
                    {
                        targetListNow.Clear();
                        foreach (Object x in ((IEnumerable)sourceValue))
                        {
                            targetListNow.Add(x);
                        }

                        // Subscribe
                        if (_sourceList != null)
                        {
                            ((IObservableList)_sourceList).ListChanged -= SourceListChanged;
                        }

                        _sourceList = (IList)sourceValue;
                        _targetList = targetListNow;
                        ((IObservableList)_sourceList).ListChanged += SourceListChanged;
                    }
                    else
                    {
                        // Nothing to do : target list is null, ignoring sync operation
                    }
                }
            }
            else
            {
                // Work with usual property
                Object? converted = sourceValue;
                // Convert back if need
                if (null != _converter && sourceValue != null)
                {
                    ConversionResult result = _converter.ConvertBack(sourceValue);
                    if (!result.Success)
                    {
                        return;
                    }

                    converted = result.Value;
                }

                //
                if (_adapter == null)
                    _targetPropertyInfo?.GetSetMethod()?.Invoke(Target, [converted]);
                else if (converted != null)
                    _adapter.SetValue(Target, _targetProperty, converted);
            }
        }
        finally
        {
            IgnoreTargetListener = false;
        }
    }

    /// <summary>
    /// Synchronizes changes of srcList, applying them to destList.
    /// Changes are described in args.
    /// </summary>
    public static void ApplyChanges(IList destList, IList srcList, ListChangedEventArgs args)
    {
        switch (args.Type)
        {
            case ListChangedEventType.ItemsInserted:
            {
                for (int i = 0; i < args.Count; i++)
                {
                    destList.Insert(args.Index + i, srcList[args.Index + i]);
                }

                break;
            }
            case ListChangedEventType.ItemsRemoved:
                for (int i = 0; i < args.Count; i++)
                    destList.RemoveAt(args.Index);
                break;
            case ListChangedEventType.ItemReplaced:
            {
                destList[args.Index] = srcList[args.Index];
                break;
            }
        }
    }

    private void SourceListChanged(object sender, ListChangedEventArgs args)
    {
        // To avoid side effects from old listeners
        // (can be reproduced if call raisePropertyChanged inside another ObservableList handler)
        // propertyChanged will cause re-subscription to ListChanged, but
        // old list still can call ListChanged when enumerates event handlers
        if (!ReferenceEquals(sender, _sourceList)) return;

        IgnoreTargetListener = true;
        try
        {
            if (_targetList != null)
                ApplyChanges(_targetList, _sourceList, args);
        }
        finally
        {
            IgnoreTargetListener = false;
        }
    }

    private void TargetListChanged(object sender, ListChangedEventArgs args)
    {
        // To avoid side effects from old listeners
        // (can be reproduced if call raisePropertyChanged inside another ObservableList handler)
        // propertyChanged will cause re-subscription to ListChanged, but
        // old list still can call ListChanged when enumerates event handlers
        if (!ReferenceEquals(sender, _targetList)) return;

        _ignoreSourceListener = true;
        try
        {
            if (_sourceList != null)
                ApplyChanges(_sourceList, _targetList, args);
        }
        finally
        {
            _ignoreSourceListener = false;
        }
    }

    /// <summary>
    /// Sends the current binding target value to the binding source property in TwoWay or OneWayToSource bindings.
    /// </summary>
    public void UpdateSource()
    {
        if (RealMode != BindingMode.OneWayToSource && RealMode != BindingMode.TwoWay)
            throw new Exception(String.Format("Cannot update source in {0} binding mode.", RealMode));
        _ignoreSourceListener = true;
        try
        {
            Object? targetValue;
            if (null == _adapter)
                targetValue = _targetPropertyInfo?.GetGetMethod()?.Invoke(Target, null);
            else
            {
                targetValue = _adapter.GetValue(Target, _targetProperty);
            }

            //
            if (_targetIsObservable)
            {
                // Work with collection
                IList? sourceListNow = (IList?)_sourcePropertyInfo?.GetGetMethod()?.Invoke(Source, null);
                if (targetValue == null)
                {
                    if (null != sourceListNow) sourceListNow.Clear();
                }
                else
                {
                    if (null != sourceListNow)
                    {
                        sourceListNow.Clear();
                        foreach (object item in (IEnumerable)targetValue)
                        {
                            sourceListNow.Add(item);
                        }

                        // Subscribe
                        if (_targetList != null)
                        {
                            ((IObservableList)_targetList).ListChanged -= TargetListChanged;
                        }

                        _targetList = (IList)targetValue;
                        _sourceList = sourceListNow;
                        ((IObservableList)_targetList).ListChanged += TargetListChanged;
                    }
                    else
                    {
                        // Nothing to do : source list is null, ignoring sync operation
                    }
                }
            }
            else
            {
                // Work with usual property
                Object? convertedValue = targetValue;
                // Convert if need
                if (null != _converter)
                {
                    ConversionResult result =
                        targetValue == null
                            ? new ConversionResult(false, $"{nameof(targetValue)} is null")
                            : _converter.Convert(targetValue);
                    if (!result.Success)
                    {
                        if (null != OnBinding)
                            OnBinding.Invoke(new BindingResult(
                                true, false, result.FailReason ?? ""));
                        if (UpdateSourceIfBindingFails)
                        {
                            // Will update source using null or default(T) if T is primitive
                            _sourcePropertyInfo?.GetSetMethod()?.Invoke(Source, [null]);
                        }

                        return;
                    }

                    convertedValue = result.Value;
                }

                // Validate if need
                if (null != Validator)
                {
                    ValidationResult validationResult = Validator.Validate(convertedValue);
                    if (!validationResult.Valid)
                    {
                        if (null != OnBinding)
                            OnBinding.Invoke(new BindingResult(
                                false, true, validationResult.Message ?? string.Empty));
                        if (UpdateSourceIfBindingFails)
                        {
                            // Will update source using null or default(T) if T is primitive
                            _sourcePropertyInfo?.GetSetMethod()?.Invoke(Source, [null]);
                        }

                        return;
                    }
                }

                _sourcePropertyInfo?.GetSetMethod()?.Invoke(Source, [convertedValue]);
                if (null != OnBinding)
                    OnBinding.Invoke(new BindingResult(false));
                //
            }
        }
        finally
        {
            _ignoreSourceListener = false;
        }
    }

    /// <summary>
    /// Connects Source and Target objects.
    /// </summary>
    public void Bind()
    {
        // Resolve binding mode and search converter if need
        if (NeedAdapterAnyway)
        {
            _adapter ??= _settings.GetAdapterFor(Target.GetType());
            RealMode = _mode == BindingMode.Default ? _adapter.DefaultMode : _mode;
        }
        else
        {
            RealMode = _mode == BindingMode.Default ? BindingMode.TwoWay : _mode;
            if (RealMode == BindingMode.TwoWay || RealMode == BindingMode.OneWayToSource)
            {
                if (Target is not INotifyPropertyChanged)
                    _adapter ??= _settings.GetAdapterFor(Target.GetType());
            }
        }

        // Get properties info and check if they are collections
        _sourcePropertyInfo = Source.GetType().GetProperty(_sourceProperty);
        if (null == _adapter)
            _targetPropertyInfo = Target.GetType().GetProperty(_targetProperty);

        Type? targetPropertyClass = null;
        if (null == _adapter)
                targetPropertyClass = _targetPropertyInfo?.PropertyType;
        else
            _adapter.GetTargetPropertyClass(_targetProperty);

        Type? sourcePropertyClass = _sourcePropertyInfo?.PropertyType;
        
        _sourceIsObservable = typeof(IObservableList).IsAssignableFrom(sourcePropertyClass);
        _targetIsObservable = typeof(IObservableList).IsAssignableFrom(targetPropertyClass);

        // We need converter if data will flow from non-observable property to property of another class
        if (targetPropertyClass != sourcePropertyClass)
        {
            bool needConverter = false;
            if (RealMode == BindingMode.OneTime
                || RealMode == BindingMode.OneWay
                || RealMode == BindingMode.TwoWay)
            {
                if (!_sourceIsObservable)
                {
                    if (targetPropertyClass == null)
                        needConverter = true;
                    else
                        needConverter |= !targetPropertyClass.IsAssignableFrom(_sourcePropertyInfo?.PropertyType);
                }
            }

            if (RealMode == BindingMode.OneWayToSource
                || RealMode == BindingMode.TwoWay)
            {
                if (!_targetIsObservable)
                {
                    if (_sourcePropertyInfo == null)
                        needConverter = true;
                    else
                        needConverter |= !_sourcePropertyInfo.PropertyType.IsAssignableFrom(targetPropertyClass);
                }
            }

            if (needConverter)
            {
                if (_converter == null)
                {
                    if (targetPropertyClass != null && _sourcePropertyInfo != null)
                        _converter = _settings.GetConverterFor(targetPropertyClass, _sourcePropertyInfo.PropertyType);
                }
                else
                {
                    // Check if converter must be reversed
                    if (_converter.FirstType.IsAssignableFrom(targetPropertyClass) &&
                        _converter.SecondType.IsAssignableFrom(_sourcePropertyInfo?.PropertyType))
                    {
                        // Nothing to do, it's ok
                    }
                    else if (_converter.SecondType.IsAssignableFrom(targetPropertyClass) &&
                             _converter.FirstType.IsAssignableFrom(_sourcePropertyInfo?.PropertyType))
                    {
                        // Should be reversed
                        _converter = new ReversedConverter(_converter);
                    }
                    else
                    {
                        throw new Exception("Provided converter doesn't support conversion between " +
                                            "specified properties.");
                    }
                }

                if (_converter == null)
                    throw new Exception(String.Format("Converter for {0} -> {1} classes not found.",
                        targetPropertyClass?.Name, _sourcePropertyInfo?.PropertyType.Name));
            }
        }

        // Verify properties getters and setters for specified binding mode
        if (RealMode == BindingMode.OneTime || RealMode == BindingMode.OneWay || RealMode == BindingMode.TwoWay)
        {
            if (_sourcePropertyInfo?.GetGetMethod() == null)
                throw new Exception("Source property getter not found");
            if (_sourceIsObservable)
            {
                if (null == _adapter && _targetPropertyInfo?.GetGetMethod() == null)
                    throw new Exception("Target property getter not found");
                if (!typeof(IList).IsAssignableFrom(targetPropertyClass))
                    throw new Exception("Target property class have to implement IList");
            }
            else
            {
                if (null == _adapter && _targetPropertyInfo?.GetSetMethod() == null)
                    throw new Exception("Target property setter not found");
            }
        }

        if (RealMode == BindingMode.OneWayToSource || RealMode == BindingMode.TwoWay)
        {
            if (null == _adapter && _targetPropertyInfo?.GetGetMethod() == null)
                throw new Exception("Target property getter not found");
            if (_targetIsObservable)
            {
                if (_sourcePropertyInfo?.GetGetMethod() == null) 
                    throw new Exception("Source property getter not found");
                if (!typeof(IList).IsAssignableFrom(_sourcePropertyInfo.PropertyType))
                    throw new Exception("Source property class have to implement IList");
            }
            else
            {
                if (_sourcePropertyInfo?.GetSetMethod() == null)
                    throw new Exception("Source property setter not found");
            }
        }

        // Subscribe to listeners
        ConnectSourceAndTarget();

        // Initial flush values
        if (RealMode == BindingMode.OneTime || RealMode == BindingMode.OneWay || RealMode == BindingMode.TwoWay)
            UpdateTarget();
        if (RealMode == BindingMode.OneWayToSource || RealMode == BindingMode.TwoWay)
            UpdateSource();

        _bound = true;
    }

    protected void ConnectSourceAndTarget()
    {
        switch (RealMode)
        {
            case BindingMode.OneTime:
                break;
            case BindingMode.OneWay:
                Source.PropertyChanged += SourceListener;
                break;
            case BindingMode.OneWayToSource:
                if (null == _adapter)
                {
                    ((INotifyPropertyChanged)Target).PropertyChanged += TargetListener;
                }
                else
                {
                    TargetListenerWrapper = _adapter.AddPropertyChangedListener(Target, TargetListener);
                }

                break;
            case BindingMode.TwoWay:
                Source.PropertyChanged += SourceListener;
                //
                if (null == _adapter)
                {
                    ((INotifyPropertyChanged)Target).PropertyChanged += TargetListener;
                }
                else
                {
                    TargetListenerWrapper = _adapter.AddPropertyChangedListener(Target, TargetListener);
                }

                break;
        }
    }

    private void TargetListener(object? sender, PropertyChangedEventArgs args)
    {
        if (!IgnoreTargetListener && args.PropertyName == _targetProperty)
            UpdateSource();
    }

    private void SourceListener(object? sender, PropertyChangedEventArgs args)
    {
        if (!_ignoreSourceListener && args.PropertyName == _sourceProperty)
            UpdateTarget();
    }

    /// <summary>
    /// Disconnects Source and Target objects.
    /// </summary>
    public void Unbind()
    {
        if (!_bound) return;

        DisconnectSourceAndTarget();

        _sourcePropertyInfo = null;
        _targetPropertyInfo = null;

        _bound = false;
    }

    protected void DisconnectSourceAndTarget()
    {
        if (RealMode == BindingMode.OneWay || RealMode == BindingMode.TwoWay)
        {
            // Remove source listener
            Source.PropertyChanged -= SourceListener;
        }

        if (RealMode == BindingMode.OneWayToSource || RealMode == BindingMode.TwoWay)
        {
            // Remove target listener
            if (_adapter == null)
            {
                ((INotifyPropertyChanged)Target).PropertyChanged -= TargetListener;
            }
            else
            {
                if (TargetListenerWrapper != null)
                {
                    _adapter.RemovePropertyChangedListener(Target, TargetListenerWrapper);
                    TargetListenerWrapper = null;
                }
            }
        }

        if (_sourceList != null && _sourceIsObservable)
        {
            ((IObservableList)_sourceList).ListChanged -= SourceListChanged;
            _sourceList = null;
        }

        if (_targetList != null && _targetIsObservable)
        {
            ((IObservableList)_targetList).ListChanged -= TargetListChanged;
            _targetList = null;
        }
    }

    /// <summary>
    /// Changes the binding Source object. If current binding state is bound,
    /// the <see cref="Unbind"/> and <see cref="Bind"/> methods will be called automatically.
    /// <param name="source">New Source object</param>
    /// </summary>
    public void SetSource(INotifyPropertyChanged source)
    {
        if (null == source) throw new ArgumentNullException(nameof(source));
        if (_bound)
        {
            Unbind();
            Source = source;
            Bind();
        }
        else
        {
            Source = source;
        }
    }

    /// <summary>
    /// Changes the binding Target object. If current binding state is bound,
    /// the <see cref="Unbind"/> and <see cref="Bind"/> methods will be called automatically.
    /// @param target New Target object
    /// </summary>
    public void SetTarget(Object target)
    {
        if (null == target) throw new ArgumentNullException(nameof(target));
        if (_bound)
        {
            Unbind();
            Target = target;
            Bind();
        }
        else
        {
            Target = target;
        }
    }
}