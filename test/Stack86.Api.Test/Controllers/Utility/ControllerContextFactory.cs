namespace Stack86.Api.Test.Controllers.Utility;

using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Factory helpers for constructing <see cref="ControllerContext"/> instances with the
/// HTTP context shape that controller tests need (claims, headers, cookies).
/// </summary>
internal static class ControllerContextFactory
{
    public static ControllerContext Create(
        Guid? userId = null,
        IDictionary<string, string>? headers = null,
        IDictionary<string, string>? cookies = null)
    {
        var httpContext = new DefaultHttpContext();

        if (userId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())],
                    authenticationType: "Test"));
        }

        if (headers is not null)
        {
            foreach (var kvp in headers)
            {
                httpContext.Request.Headers[kvp.Key] = kvp.Value;
            }
        }

        if (cookies is not null && cookies.Count > 0)
        {
            httpContext.Request.Headers["Cookie"] =
                string.Join("; ", cookies.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        }

        return new ControllerContext { HttpContext = httpContext };
    }
}
