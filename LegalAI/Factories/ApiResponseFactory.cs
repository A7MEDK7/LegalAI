using Microsoft.AspNetCore.Mvc;
using Shared.ErrorModels;
using System.Net;

namespace LegalAI.Factories {
    public class ApiResponseFactory {
        // Context => ModelState => Dictionary <string, ModelEntry>
        // String => Key, Name Of Parameter
        // ModelStateDictionary => object, Errors
        public static IActionResult CustomValidationErrorResponse
        (ActionContext context) {
            // 1. Get All Error In ModelState
            var errors = context.ModelState.Where(error =>
             error.Value!.Errors.Any()).Select(error => new ValidationError {
                 Field = error.Key,
                 Errors = error.Value!.Errors.Select(e => e.ErrorMessage)
             });
            // 2. Create Custom Response
            var response = new ValidationErrorResponse {
                StatusCode = (int)HttpStatusCode.BadRequest,
                ErrorMessage = "Validation Failed",
                Errors = errors
            };
            // 3. Return
            return new BadRequestObjectResult(response);
        }
    }
}
