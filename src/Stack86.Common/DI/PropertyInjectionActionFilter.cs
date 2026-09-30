namespace Stack86.Common.DI;

using System.Reflection;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Stack86.Common.Exceptions;

/// <summary>
/// MVC action filter that performs property injection on controller instances
/// for properties decorated with <see cref="InjectAttribute"/>.
/// </summary>
public sealed class PropertyInjectionActionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var controller = context.Controller;
        if (controller is not null)
        {
            var services = context.HttpContext.RequestServices;
            var controllerType = controller.GetType();

            foreach (var prop in controllerType
                         .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                         .Where(p => p.CanWrite && p.GetCustomAttributes(typeof(InjectAttribute), inherit: true).Length != 0))
            {
                var value = services.GetService(prop.PropertyType);
                if (value is null)
                {
                    throw new DependencyInjectionException(
                        $"Cannot provide value for property: '{prop.Name}' on controller '{controllerType.FullName}'. No service for type '{prop.PropertyType.FullName}' has been registered.");
                }

                prop.SetValue(controller, value);
            }
        }

        await next();
    }
}
