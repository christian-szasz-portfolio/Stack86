#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stack86.Common.Exceptions;

/// <summary>
/// Provides extension methods for registering services with property injection support.
/// </summary>
public static class AddPropertyInjectedServicesExtension
{
    private static readonly string ServiceImplementationTypeProp = "serviceImplementationType";

    /// <summary>
    /// Enables property injection for services registered in the specified service collection.
    /// </summary>
    /// <param name="services">The IServiceCollection to scan and update.</param>
    /// <returns>The same IServiceCollection instance with property injection enabled.</returns>
    public static IServiceCollection AddPropertyInjectedServices(this IServiceCollection services)
    {
        var servicesList = services.ToArray();

        foreach (var service in servicesList)
        {
            var implementationFactory = service?.ImplementationFactory;
            var implementationInstance = service?.ImplementationInstance;

            var implementationType = service?.ImplementationType ??
                implementationFactory?.GetType()
                    ?.GenericTypeArguments
                    ?.LastOrDefault() ??
                implementationInstance?.GetType();

            if (implementationType == typeof(object))
            {
                implementationType = implementationFactory?.Target
                    ?.GetType()
                    ?.GetField(ServiceImplementationTypeProp)
                    ?.GetValue(implementationFactory?.Target) as Type;
            }

            if (implementationType is null)
            {
                continue;
            }

            var injectableProperties = implementationType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.CanWrite && x.GetCustomAttributes(typeof(InjectAttribute), false).Length != 0)
                .ToList();

            if (injectableProperties.Count == 0)
            {
                continue;
            }

            Func<IServiceProvider, object> createInstanceFunc;

            if (implementationFactory != null)
            {
                createInstanceFunc = (services) =>
                {
                    var serviceInstance = implementationFactory.Invoke(services);
                    return serviceInstance;
                };
            }
            else if (implementationInstance != null)
            {
                createInstanceFunc = (services) =>
                {
                    return implementationInstance;
                };
            }
            else
            {
                createInstanceFunc = (services) =>
                {
                    var ctorParameters = implementationType.GetConstructors()
                        .First()
                        .GetParameters()
                        .Select(x => services.GetRequiredService(x.ParameterType))
                        .ToArray();

                    var serviceInstance = Activator.CreateInstance(implementationType, ctorParameters)!;
                    return serviceInstance;
                };
            }

            var serviceType = service?.ServiceType ?? implementationType;
            var serviceLifeTime = service!.Lifetime;

            services.Replace(new ServiceDescriptor(
                serviceType,
                (services) =>
                {
                    var serviceInstance = createInstanceFunc(services);

                    foreach (var injectableProperty in injectableProperties)
                    {
                        var dependencyInstance = services.GetService(injectableProperty.PropertyType);

                        if (dependencyInstance is null)
                        {
                            throw new DependencyInjectionException(
                                $"Cannot provide value for property: '{injectableProperty.Name}' on type '{serviceInstance?.GetType()?.FullName}'. No service for type '{injectableProperty.PropertyType.FullName}' has been registered.");
                        }

                        injectableProperty.SetValue(serviceInstance, dependencyInstance);
                    }

                    return serviceInstance;
                },
                serviceLifeTime));
        }

        return services;
    }
}
