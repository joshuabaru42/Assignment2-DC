using Microsoft.AspNetCore.Mvc;

namespace BankService.Controllers
{
    // Member 3: Centralizes structured JSON errors for API business validation
    // and missing-resource failures so controllers return consistent responses.
    public abstract class ApiControllerBase : ControllerBase
    {
        protected ObjectResult ApiProblem(int statusCode, string title, string? detail = null)
        {
            return Problem(statusCode: statusCode, title: title, detail: detail);
        }
    }
}
