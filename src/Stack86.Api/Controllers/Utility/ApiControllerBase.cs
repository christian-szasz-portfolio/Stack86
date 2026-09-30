namespace Stack86.Api.Controllers.Utility;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Base controller for all API controllers.
/// </summary>
[ApiController]
[Route("api/[controller]/[action]")]
public abstract class ApiControllerBase : ControllerBase
{
}
