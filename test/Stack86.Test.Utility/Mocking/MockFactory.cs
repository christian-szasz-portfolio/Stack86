namespace Stack86.Test.Utility.Mocking;

using System;
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Moq;

/// <summary>
/// A utility factory for creating and managing Moq mocks with support for property injection.
/// Mocks are retrieved via <see cref="GetMock{T}"/>, configured with Moq's fluent API, and consumed
/// via <see cref="GetObject{T}"/>. Use <see cref="Create{T}"/> to instantiate a system under test
/// with all constructor and <c>[Inject]</c> properties wired to mocks.
/// </summary>
public class MockFactory : IServiceProvider
{
    private readonly ConcurrentDictionary<Type, object> mocks = new();

    /// <summary>Gets or creates a mock for the specified interface or class type.</summary>
    public Mock<T> GetMock<T>()
        where T : class
    {
        var mock = (Mock<T>)this.mocks.GetOrAdd(typeof(T), _ => new Mock<T>());
        return mock;
    }

    /// <summary>Gets the mocked object instance for the specified type.</summary>
    public T GetObject<T>()
        where T : class
    {
        return this.GetMock<T>().Object;
    }

    /// <summary>
    /// Creates an instance of <typeparamref name="T"/> with constructor dependencies resolved from
    /// mocks and properties marked with <see cref="InjectAttribute"/> populated with mock instances.
    /// </summary>
    public T Create<T>()
        where T : class
    {
        return (T)this.Create(typeof(T));
    }

    /// <summary>
    /// Creates an instance of the specified type with constructor dependencies resolved from mocks
    /// and properties marked with <see cref="InjectAttribute"/> populated with mock instances.
    /// </summary>
    public object Create(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var constructor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault() ?? throw new InvalidOperationException(
                $"Type '{type.FullName}' has no public constructors.");

        var parameters = constructor.GetParameters()
            .Select(p => this.GetService(p.ParameterType))
            .ToArray();

        var instance = constructor.Invoke(parameters);
        this.InjectProperties(instance);
        return instance;
    }

    /// <summary>Injects mock instances into properties marked with <see cref="InjectAttribute"/>.</summary>
    public void InjectProperties(object instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var instanceType = instance.GetType();
        var injectableProperties = instanceType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && p.GetCustomAttributes(typeof(InjectAttribute), inherit: true).Length != 0);

        foreach (var property in injectableProperties)
        {
            var mockObject = this.GetService(property.PropertyType);
            property.SetValue(instance, mockObject);
        }
    }

    /// <inheritdoc/>
    public object? GetService(Type? serviceType)
    {
        if (serviceType == null)
        {
            return null;
        }

        if (this.mocks.TryGetValue(serviceType, out var existingMock))
        {
            return GetObjectFromMock(existingMock);
        }

        if (serviceType.IsInterface || (serviceType.IsClass && !serviceType.IsSealed))
        {
            var mockType = typeof(Mock<>).MakeGenericType(serviceType);
            var mock = Activator.CreateInstance(mockType)!;
            this.mocks.TryAdd(serviceType, mock);
            return GetObjectFromMock(mock);
        }

        return null;
    }

    /// <summary>Verifies all expectations on all mocks created by this factory.</summary>
    public void VerifyAll()
    {
        foreach (var mock in this.mocks.Values)
        {
            if (mock is Mock mockBase)
            {
                mockBase.VerifyAll();
            }
        }
    }

    /// <summary>Verifies all verifiable expectations on all mocks have been met.</summary>
    public void Verify()
    {
        foreach (var mock in this.mocks.Values)
        {
            if (mock is Mock mockBase)
            {
                mockBase.Verify();
            }
        }
    }

    /// <summary>Resets all mocks created by this factory.</summary>
    public void Reset()
    {
        foreach (var mock in this.mocks.Values)
        {
            if (mock is Mock mockBase)
            {
                mockBase.Reset();
            }
        }
    }

    /// <summary>Clears all mocks from the factory.</summary>
    public void Clear()
    {
        this.mocks.Clear();
    }

    private static object GetObjectFromMock(object mock)
    {
        var objectProperty = mock.GetType().GetProperty("Object", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;
        return objectProperty.GetValue(mock)!;
    }
}
