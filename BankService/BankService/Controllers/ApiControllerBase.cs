using Microsoft.AspNetCore.Mvc;

namespace BankService.Controllers
{
    // Member 3: Centralizes RFC 9457 error responses for API controllers.
    public abstract class ApiControllerBase : ControllerBase
    {
        protected ObjectResult ApiProblem(int statusCode, string title, string? detail = null)
        {
            return Problem(statusCode: statusCode, title: title, detail: detail);
        }
    }
}
