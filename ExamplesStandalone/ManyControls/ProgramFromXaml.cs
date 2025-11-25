using ConsoleFramework;
using ConsoleFramework.Controls;
using ConsoleFramework.Xaml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;

namespace ManyControls;

public class ProgramFromXaml : IProgram
{
    private class MyDataContext : INotifyPropertyChanged
    {
        private string? _str;

        public String? Str
        {
            get => _str;
            set
            {
                if (_str != value)
                {
                    _str = value;
                    RaisePropertyChanged(nameof(Str));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void RaisePropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public void Main(string[] args)
    {
        Type type = typeof(Program);
        TypeInfo typeInfo = type.GetTypeInfo();

        var assembly = typeInfo.Assembly;
        var resourceName = "ManyControls.GridTest.xaml";
        Window? createdFromXaml = null;
        using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream != null)
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string result = reader.ReadToEnd();
                    MyDataContext dataContext = new MyDataContext();
                    dataContext.Str = "Введите заголовок";
                    createdFromXaml = XamlParser.CreateFromXaml<Window>(result, dataContext,
                        new List<string>()
                    {
                        "clr-namespace:Xaml;assembly=ConsoleFramework",
                        "clr-namespace:ConsoleFramework.Xaml;assembly=ConsoleFramework",
                        "clr-namespace:ConsoleFramework.Controls;assembly=ConsoleFramework",
                    });
                }
            }
        }
        if (createdFromXaml != null)
            ConsoleApplication.Instance.Run(createdFromXaml);
    }
}