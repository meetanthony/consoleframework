using System;

namespace ConsoleFramework.Events;

/// <summary>
/// Тип маршрутизации события.
/// </summary>
public enum RoutingStrategy
{
    /// <summary>
    /// Событие передаётся всем подписчикам, от корневого элемента управления к источнику.
    /// </summary>
    Tunnel,

    /// <summary>
    /// Событие передаётся всем подписчикам, от источника до корневого элемента управления.
    /// </summary>
    Bubble,

    /// <summary>
    /// Событие будет передано только тем подписчикам, которые подписаны на
    /// источник события.
    /// </summary>
    Direct
}

/// <summary>
/// Key for internal usage in routed event management maps.
/// </summary>
public sealed class RoutedEventKey : IEquatable<RoutedEventKey>
{
    public string? Name { get; }

    public Type? OwnerType { get; }

    public RoutedEventKey(string name, Type ownerType)
    {
        this.Name = name;
        this.OwnerType = ownerType;
    }

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <returns>
    /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
    /// </returns>
    /// <param name="other">An object to compare with this object.</param>
    public bool Equals(RoutedEventKey? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Equals(other.Name, Name) && Equals(other.OwnerType, OwnerType);
    }

    /// <summary>
    /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
    /// </summary>
    /// <returns>
    /// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
    /// </returns>
    /// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/>. </param><filterpriority>2</filterpriority>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != typeof(RoutedEventKey)) return false;
        return Equals((RoutedEventKey)obj);
    }

    /// <summary>
    /// Serves as a hash function for a particular type. 
    /// </summary>
    /// <returns>
    /// A hash code for the current <see cref="T:System.Object"/>.
    /// </returns>
    /// <filterpriority>2</filterpriority>
    public override int GetHashCode()
    {
        unchecked
        {
            return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ (OwnerType != null ? OwnerType.GetHashCode() : 0);
        }
    }

    public static bool operator ==(RoutedEventKey left, RoutedEventKey right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(RoutedEventKey left, RoutedEventKey right)
    {
        return !Equals(left, right);
    }
}

/// <summary>
/// Represents event that supports routing through visual tree.
/// </summary>
public sealed class RoutedEvent
{
    public RoutedEvent(Type handlerType, string name, Type ownerType, RoutingStrategy routingStrategy)
    {
        this.HandlerType = handlerType;
        this.Name = name;
        this.OwnerType = ownerType;
        this.RoutingStrategy = routingStrategy;
    }

    /// <summary>
    /// Тип делегата - обработчика события.
    /// </summary>
    public Type HandlerType { get; }

    /// <summary>
    /// Имя события - должно быть уникальным в рамках указанного <see cref="OwnerType"/>.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Тип владельца события.
    /// </summary>
    public Type OwnerType { get; }

    /// <summary>
    /// Стратегия маршрутизации события.
    /// </summary>
    public RoutingStrategy RoutingStrategy { get; }

    public RoutedEventKey Key =>
        // note : mb cache this
        new(Name, OwnerType);
}