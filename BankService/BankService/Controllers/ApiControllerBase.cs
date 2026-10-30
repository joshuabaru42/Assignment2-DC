using Microsoft.AspNetCore.Mvc;

namespace BankService.Controllers
{
    // =====================================================
    // MEMBER 3 - PART A: STRUCTURED API ERROR RESPONSES
    // =====================================================
    // Returns consistent structured JSON errors for business
    // validation and missing-resource failures.
    //
    // References:
    // https://learn.microsoft.com/aspnet/core/web-api/handle-errors
    // https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.mvc.controllerbase.problem
    public abstract class ApiControllerBase : ControllerBase
    {
        protected ObjectResult ApiProblem(int statusCode, string title, string? detail = null)
        {
            return Problem(statusCode: statusCode, title: title, detail: detail);
        }
    }
}
