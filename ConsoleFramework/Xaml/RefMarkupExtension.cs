using System;

namespace ConsoleFramework.Xaml;

/// <summary>
/// Returns an object that is referenced in expression. Example:
/// {Ref myObject} will return object with x:Id="myObject".
/// Forward-references are supported too.
/// </summary>
[MarkupExtension("Ref")]
class RefMarkupExtension : IMarkupExtension
{
    public RefMarkupExtension()
    {
    }

    public RefMarkupExtension(string @ref)
    {
        Ref = @ref;
    }

    /// <summary>
    /// String reference to ID of object to be used.
    /// </summary>
    public string? Ref { get; set; }

    public object? ProvideValue(IMarkupExtensionContext? context)
    {
        if (string.IsNullOrEmpty(Ref))
            throw new InvalidOperationException("Ref is null or empty string.");

        if (context == null)
            return null;

        object? obj = context.GetObjectById(Ref);
        if (null == obj)
        {
            if (context.IsFixupTokenAvailable)
            {
                return context.GetFixupToken(new string[] { Ref });
            }

            throw new InvalidOperationException($"Object with Id={Ref} not found.");
        }

        return obj;
    }
}