using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ConsoleFramework.Xaml;

public interface IMarkupExtensionsResolver
{
    Type Resolve(string name);
}

public class MarkupExtensionsParser
{
    private readonly IMarkupExtensionsResolver _resolver;

    public MarkupExtensionsParser(IMarkupExtensionsResolver resolver, string text)
    {
        _resolver = resolver;
        _text = text;
    }

    private readonly string _text;
    private int _index;

    private bool HasNextChar()
    {
        return _index < _text.Length;
    }

    private char ConsumeChar()
    {
        return _text[_index++];
    }

    private char PeekNextChar()
    {
        return _text[_index];
    }

    public object? ProcessMarkupExtension(IMarkupExtensionContext? context)
    {
        // interpret as markup extension expression
        object? result = ProcessMarkupExtensionCore(context);
        if (result is IFixupToken) return result;

        if (HasNextChar())
        {
            throw new InvalidOperationException(
                $"Syntax error: unexpected characters at {_index}");
        }

        return result;
    }

    /// <summary>
    /// Consumes all whitespace characters. If necessary is true, at least one
    /// whitespace character should be consumed.
    /// </summary>
    private void ProcessWhitespace(bool necessary = true)
    {
        if (necessary)
        {
            // at least one whitespace should be
            if (PeekNextChar() != ' ')
                throw new InvalidOperationException(
                    $"Syntax error: whitespace expected at {_index}.");
        }

        while (PeekNextChar() == ' ') ConsumeChar();
    }

    /// <summary>
    /// Recursive method. Consumes next characters as markup extension definition.
    /// Resolves type, ctor arguments and properties of markup extension,
    /// constructs and initializes it, and returns ProvideValue method result.
    /// </summary>
    /// <param name="context">Context object passed to ProvideValue method.</param>
    private object? ProcessMarkupExtensionCore(IMarkupExtensionContext? context)
    {
        if (ConsumeChar() != '{')
            throw new InvalidOperationException("Syntax error: '{{' token expected at 0.");
        ProcessWhitespace(false);
        string markupExtensionName = ProcessQualifiedName();
        if (markupExtensionName.Length == 0)
            throw new InvalidOperationException("Syntax error: markup extension name is empty.");
        ProcessWhitespace();

        Type type = _resolver.Resolve(markupExtensionName);

        object? obj = null;
        List<object?> ctorArgs = new List<object?>();

        for (;;)
        {
            if (PeekNextChar() == '{')
            {
                // inner markup extension processing

                // syntax error if ctor arg defined after any property
                if (obj != null)
                    throw new InvalidOperationException("Syntax error: constructor argument" +
                                                        " cannot be after property assignment.");

                object? value = ProcessMarkupExtensionCore(context);
                if (value is IFixupToken)
                    return value;
                ctorArgs.Add(value);
            }
            else
            {
                string memberNameOrString = ProcessString();

                if (memberNameOrString.Length == 0)
                    throw new InvalidOperationException(
                        $"Syntax error: member name or string expected at {_index}");

                if (PeekNextChar() == '=')
                {
                    ConsumeChar();
                    object? value = PeekNextChar() == '{'
                        ? ProcessMarkupExtensionCore(context)
                        : ProcessString();

                    if (value is IFixupToken) return value;

                    // construct object if not constructed yet
                    if (obj == null) obj = Construct(type, ctorArgs);

                    // assign value to specified member
                    AssignProperty(type, obj, memberNameOrString, value);
                }
                else if (PeekNextChar() == ',' || PeekNextChar() == '}')
                {
                    // syntax error if ctor arg defined after any property
                    if (obj != null)
                        throw new InvalidOperationException("Syntax error: constructor argument" +
                                                            " cannot be after property assignment.");

                    // store memberNameOrString as string argument of ctor
                    ctorArgs.Add(memberNameOrString);
                }
                else
                {
                    // it is '{' token, throw syntax error
                    throw new InvalidOperationException(
                        $"Syntax error : unexpected '{{' token at {_index}.");
                }
            }

            // after ctor arg or property assignment should be , or }
            if (PeekNextChar() == ',')
            {
                ConsumeChar();
            }
            else if (PeekNextChar() == '}')
            {
                ConsumeChar();

                // construct object
                if (obj == null) obj = Construct(type, ctorArgs);

                // markup extension is finished
                break;
            }
            else
            {
                // it is '{' token (without whitespace), throw syntax error
                throw new InvalidOperationException(
                    $"Syntax error : unexpected '{{' token at {_index}.");
            }

            ProcessWhitespace(false);
        }

        return ((IMarkupExtension)obj).ProvideValue(context);
    }

    private void AssignProperty(Type type, object obj, string propertyName, object? value)
    {
        PropertyInfo? property = type.GetProperty(propertyName);
        property?.SetValue(obj, value, null);
    }

    /// <summary>
    /// Constructs object of specified type using specified ctor arguments list.
    /// </summary>
    private object Construct(Type type, List<object?> ctorArgs)
    {
        ConstructorInfo[] constructors = type.GetConstructors();
        List<ConstructorInfo> constructorInfos =
            constructors.Where(info => info.GetParameters().Length == ctorArgs.Count).ToList();
        if (constructorInfos.Count == 0)
        {
            throw new InvalidOperationException("No suitable constructor");
        }

        if (constructorInfos.Count > 1)
        {
            throw new InvalidOperationException("Ambiguous constructor call");
        }

        ConstructorInfo ctor = constructorInfos[0];
        ParameterInfo[] parameters = ctor.GetParameters();
        object?[] convertedArgs = new object[ctorArgs.Count];
        for (int i = 0; i < parameters.Length; i++)
        {
            convertedArgs[i] = ctorArgs[i];
        }

        return ctor.Invoke(convertedArgs);
    }

    /// <summary>
    /// Возвращает строку, в которой могут содержаться любые символы кроме {},=.
    /// Как только встречается один из этих символов без экранирования обратным слешем,
    /// парсинг прекращается.
    /// </summary>
    private string ProcessString()
    {
        StringBuilder sb = new StringBuilder();
        bool escaping = false;
        for (;;)
        {
            if (!HasNextChar())
            {
                if (escaping) throw new InvalidOperationException("Invalid syntax.");
                break;
            }

            char c = PeekNextChar();
            if (escaping)
            {
                sb.Append(c);
                ConsumeChar();
                escaping = false;
            }
            else
            {
                if (c == '\\')
                {
                    escaping = true;
                    ConsumeChar();
                }
                else
                {
                    if (c == '{' || c == '}' || c == ',' || c == '=')
                    {
                        // break without consuming it
                        break;
                    }
                    else
                    {
                        sb.Append(c);
                        ConsumeChar();
                    }
                }
            }
        }

        return sb.ToString();
    }

    private string ProcessQualifiedName()
    {
        StringBuilder sb = new StringBuilder();
        for (;;)
        {
            char c = PeekNextChar();
            if (c != ':' && !char.IsLetterOrDigit(c))
            {
                break;
            }

            ConsumeChar();
            sb.Append(c);
        }

        return sb.ToString();
    }
}