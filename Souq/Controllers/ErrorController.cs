using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Souq.Controllers
{
    public class ErrorController : Controller
    {
        // Handles 404, 403, etc
        [Route("error/{statusCode}")]
        public IActionResult StatusCode(int statusCode)
        {
            return statusCode switch
            {
                404 => View("NotFound"),
                403 => View("AccessDenied"),
                _ => View("Error")
            };
        }

        // Handles unhandled exceptions
        [Route("error")]
        public IActionResult Error()
        {
            /*
                In development we want to see the full
                exception details. In production we show
                a friendly page.
            */
            var exceptionFeature = HttpContext.Features
                .Get<IExceptionHandlerFeature>();

            if (exceptionFeature != null)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Unhandled exception: " +
                    $"{exceptionFeature.Error.Message}");
            }

            return View("Error");
        }

    }
}
