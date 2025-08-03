using System;
using System.ComponentModel;
using ConsoleFramework.Binding;
using Xunit;

namespace Tests.Binding;

public class ValidationTest
{
    class TargetClass : INotifyPropertyChanged
    {
        public String? TargetStr
        {
            get => _targetStr;
            set
            {
                if (_targetStr != value)
                {
                    _targetStr = value;
                    RaisePropertyChanged("TargetStr");
                }
            }
        }

        private string? _targetStr;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler? handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    class SourceClass : INotifyPropertyChanged
    {
        public int SourceInt
        {
            get => _sourceInt;
            set
            {
                if (value != _sourceInt)
                {
                    _sourceInt = value;
                    RaisePropertyChanged("SourceInt");
                }
            }
        }

        private int _sourceInt;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler? handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    [Fact]
    public void TestMethod1()
    {
        SourceClass source = new SourceClass();
        TargetClass target = new TargetClass();
        BindingBase binding = new BindingBase(target, "TargetStr", source, "SourceInt");
        BindingResult? lastResult = null;
        binding.OnBinding += result => { lastResult = result; };
        binding.Bind();
        target.TargetStr = "5";
        Assert.True(source.SourceInt == 5);
        target.TargetStr = "invalid int";
        Assert.True(source.SourceInt == 0);
        Assert.True(lastResult?.HasConversionError);
    }
}