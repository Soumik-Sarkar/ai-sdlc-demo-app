using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace InvoiceApp.Api.Auth;

/// <summary>Allows the action only for the listed roles; everyone else gets 403 with a reason.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireRoleAttribute : ActionFilterAttribute
{
    private readonly string[] _roles;

    public RequireRoleAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Items[HttpContextExtensions.SessionKey] as UserSession;

        if (session is null || !_roles.Contains(session.Role))
        {
            var roleName = session?.Role ?? "unknown";
            context.Result = new ObjectResult(new { error = $"Role '{roleName}' is not allowed to do this." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
