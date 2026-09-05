using Domain.Excpetions;
using Domain.Excpetions.IdentityModule;
using Domain.Excpetions.LegalAiModule;
using Shared.ErrorModels;
using System.Net;

namespace LegalAI.MiddleWares {
    public class GlobalErrorHandelingMiddleware {

        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalErrorHandelingMiddleware> _logger;

        public GlobalErrorHandelingMiddleware(RequestDelegate next, ILogger<GlobalErrorHandelingMiddleware> logger) {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpcontext) {
            try {
                await _next(httpcontext);
                // Check If End Point Is Exist
                if (httpcontext.Response.StatusCode == (int)HttpStatusCode.NotFound)
                    await HandleNotFoundEndPointAsync(httpcontext);
            } catch (Exception ex) {
                // log Exception
                _logger.LogError($"Something Went Wrong {ex}");
                // Handle Exception
                await HandleExceptionAsync(httpcontext, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext httpcontext, Exception ex) {
            // Set Context Type ==> Application\json
            httpcontext.Response.ContentType = "application/json";

            // Standard Response
            var response = new ErrorDetails() {
                ErrorMessage = ex.Message
            };

            // Get Status Code With New Switch C# 8.0
            httpcontext.Response.StatusCode = ex switch {
                NotFoundExcpetion => (int) HttpStatusCode.NotFound, // 404
                BadRequestException => (int) HttpStatusCode.BadRequest, // 400
                UnAuthorizedException => StatusCodes.Status401Unauthorized, // 401
                AiServiceUnavailableException => StatusCodes.Status503ServiceUnavailable,
                ValidationException validationException => HandleValidationException(validationException, response),
                _ => (int) HttpStatusCode.InternalServerError // 500
            };
            response.StatusCode = httpcontext.Response.StatusCode;

            await httpcontext.Response.WriteAsync(response.ToString());
        }

        private int HandleValidationException(ValidationException validationException, ErrorDetails response) {
            response.Errors = validationException.Errors;
            return StatusCodes.Status400BadRequest;
        }

        private async Task HandleNotFoundEndPointAsync(HttpContext httpcontext) {
            httpcontext.Response.ContentType = "application/json";
            var response = new ErrorDetails {
                StatusCode = (int) HttpStatusCode.NotFound,
                ErrorMessage = $"The End Point {httpcontext.Request.Path} Not Found"
            }.ToString();
            await httpcontext.Response.WriteAsync(response);
        }
    }
}
