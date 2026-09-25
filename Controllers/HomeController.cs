using Microsoft.AspNetCore.Mvc;

namespace StowellCoAPI.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var htmlContent = @"
            <html>
            <head><title>Welcome to My API</title></head>
            <body>
                <h1>Welcome to the API!</h1>
                <p>This is a simple home page for your API.</p>
                <p><a href='/swagger'>Go to Swagger UI</a></p>
            </body>
            </html>";

            return Content(htmlContent, "text/html");
        }

    }
}
