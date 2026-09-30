namespace Stack86.Logic.Test.Common;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Stack86.Common.DI;
using Stack86.Common.Exceptions;
using Stack86.Common.Time;
using Stack86.Common.Validation;

[TestClass]
public sealed class CommonMiscTests
{
    [TestMethod]
    public void SystemClock_UtcNow_IsCloseToNow()
    {
        var clock = new SystemClock();
        var diff = (System.DateTimeOffset.UtcNow - clock.UtcNow).Duration();
        Assert.IsTrue(diff < System.TimeSpan.FromSeconds(1));
    }

    [TestMethod]
    public void ValidationException_FromDictionary_HasErrors()
    {
        var errors = new Dictionary<string, string[]> { ["Name"] = ["required"] };
        var ex = new ValidationException(errors);
        Assert.IsTrue(ex.Errors.ContainsKey("Name"));
    }

    [TestMethod]
    public void ValidationException_FromSingleField_PopulatesErrorList()
    {
        var ex = new ValidationException("Email", "bad");
        Assert.AreEqual("bad", ex.Errors["Email"][0]);
    }

    [TestMethod]
    public async Task PropertyInjectionActionFilter_InjectsRegisteredService()
    {
        var sp = new ServiceCollection().AddSingleton<DependencyService>().BuildServiceProvider();
        var controller = new ControllerWithInjectedProperty();
        var ctx = MakeActionExecutingContext(sp, controller);

        var filter = new PropertyInjectionActionFilter();
        await filter.OnActionExecutionAsync(ctx, () => Task.FromResult<ActionExecutedContext>(MakeActionExecutedContext(ctx)));

        Assert.IsNotNull(controller.Service);
    }

    [TestMethod]
    public async Task PropertyInjectionActionFilter_WhenServiceMissing_Throws()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var controller = new ControllerMissingService();
        var ctx = MakeActionExecutingContext(sp, controller);

        var filter = new PropertyInjectionActionFilter();
        await Assert.ThrowsExactlyAsync<DependencyInjectionException>(
            async () => await filter.OnActionExecutionAsync(ctx, () => Task.FromResult<ActionExecutedContext>(MakeActionExecutedContext(ctx))));
    }

    [TestMethod]
    public async Task PropertyInjectionActionFilter_WhenControllerNull_DelegatesNext()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var ctx = MakeActionExecutingContext(sp, controller: null);

        var filter = new PropertyInjectionActionFilter();
        var nextCalled = false;
        await filter.OnActionExecutionAsync(ctx, () =>
        {
            nextCalled = true;
            return Task.FromResult(MakeActionExecutedContext(ctx));
        });
        Assert.IsTrue(nextCalled);
    }

    [TestMethod]
    public void ValidationActionFilter_WhenModelValid_DoesNothing()
    {
        var ctx = MakeActionExecutingContext(new ServiceCollection().BuildServiceProvider(), controller: null);
        new ValidationActionFilter().OnActionExecuting(ctx);
    }

    [TestMethod]
    public void ValidationActionFilter_WhenModelInvalid_ThrowsValidationException()
    {
        var ctx = MakeActionExecutingContext(new ServiceCollection().BuildServiceProvider(), controller: null);
        ctx.ModelState.AddModelError("Field", "is required");
        ctx.ModelState.AddModelError("OtherField", string.Empty);

        var ex = Assert.ThrowsExactly<ValidationException>(() => new ValidationActionFilter().OnActionExecuting(ctx));
        Assert.IsTrue(ex.Errors.ContainsKey("Field"));
        Assert.AreEqual("Invalid value.", ex.Errors["OtherField"][0]);
    }

    [TestMethod]
    public void ValidationActionFilter_OnActionExecuted_DoesNothing()
    {
        var executing = MakeActionExecutingContext(new ServiceCollection().BuildServiceProvider(), controller: null);
        var executed = MakeActionExecutedContext(executing);
        new ValidationActionFilter().OnActionExecuted(executed);
    }

    [TestMethod]
    public void ValidationActionFilter_NullContext_Throws()
    {
        Assert.ThrowsExactly<System.ArgumentNullException>(
            () => new ValidationActionFilter().OnActionExecuting(null!));
    }

    private static ActionExecutingContext MakeActionExecutingContext(IServiceProvider sp, object? controller)
    {
        var http = new DefaultHttpContext { RequestServices = sp };
        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
        return new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), controller!);
    }

    private static ActionExecutedContext MakeActionExecutedContext(ActionExecutingContext ctx)
        => new(ctx, new List<IFilterMetadata>(), ctx.Controller!);

    private sealed class DependencyService
    {
        public string Name { get; set; } = "dep";
    }

    private sealed class ControllerWithInjectedProperty : Controller
    {
        [Inject]
        public DependencyService? Service { get; set; }
    }

    private sealed class ControllerMissingService : Controller
    {
        [Inject]
        public DependencyService? Service { get; set; }
    }
}
