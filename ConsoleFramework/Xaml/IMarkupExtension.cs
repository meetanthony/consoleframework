namespace ConsoleFramework.Xaml;

/// <summary>
/// todo : comment
/// </summary>
public interface IMarkupExtension
{
    /// <summary>
    /// If ProvideValue returns null, it will not be assigned to object property.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    object? ProvideValue(IMarkupExtensionContext? context);
}