using System.Collections.Generic;

namespace ConsoleFramework.Xaml;

/// <summary>
/// Контекст, доступный расширению разметки.
/// </summary>
public interface IMarkupExtensionContext
{
    /// <summary>
    /// Имя свойства, которое определяется при помощи расширения разметки.
    /// </summary>
    string PropertyName { get; }

    /// <summary>
    /// Ссылка на конфигурируемый объект.
    /// </summary>
    object Object { get; }

    /// <summary>
    /// Возвращает активный для конфигурируемого объекта DataContext.
    /// Если у текущего конфигурируемого объекта нет собственного DataContext'a,
    /// будет взят контекст объекта выше по иерархии контролов, и так до главного элемента
    /// дерева контролов.
    /// </summary>
    object DataContext { get; }

    /// <summary>
    /// Returns already created object with specified x:Id attribute value or null if object with
    /// this x:Id is not constructed yet. To resolve forward references use fixup tokens mechanism.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    object? GetObjectById(string id);

    /// <summary>
    /// Gets a value that determines whether calling GetFixupToken is available
    /// in order to resolve a name into a token for forward resolution.
    /// </summary>
    bool IsFixupTokenAvailable { get; }

    /// <summary>
    /// Returns an object that can correct for certain markup patterns that produce forward references.
    /// </summary>
    /// <param name="ids">A collection of ids that are possible forward references.</param>
    /// <returns>An object that provides a token for lookup behavior to be evaluated later.</returns>
    IFixupToken GetFixupToken(IEnumerable<string> ids);
}