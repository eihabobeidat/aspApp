using API.Handlers;
using System.Net;
using System.Text.Json;

namespace API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _environment;
        public ExceptionMiddleware( RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment environment )
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        //InvokeAsync => should be named, since our framework excepts this naming convention for error handling.
        //HttpContext => will provide us access to the current processed request in the middleware.
        public async Task InvokeAsync( HttpContext httpContext )
        {
            try
            {
                await _next(httpContext);
                // after the awaitng next we can alter the http context based on the status code to unify error response
                // and even unify the response in general
                // note that all response types will be going normally here except the throwen errors will be handled below

            }
            catch ( Exception ex )
            {
                _logger.LogError(ex, ex.Message);
                httpContext.Response.ContentType = "application/json";
                httpContext.Response.StatusCode = ( int ) HttpStatusCode.InternalServerError;

                var response = _environment.IsDevelopment() ?
                    new ApiException(httpContext.Response.StatusCode, ex.Message, ex.StackTrace?.ToString() ?? "static message, No stack trace message") :
                    new ApiException(httpContext.Response.StatusCode, ex.Message, "Interval Server Error");

                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

                var jsonResponseBody = JsonSerializer.Serialize(response, options);

                await httpContext.Response.WriteAsync(jsonResponseBody);
            }
        }
    }
}
