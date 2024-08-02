using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace NirvedBackend.Helpers;

public class ModelStateValidationActionFilterAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {    
        var modelState = context.ModelState;
        if (!modelState.IsValid)
        {
            var error = modelState.Values
                .First().Errors
                .First().ErrorMessage;
            context.Result = new BadRequestObjectResult(new {error});
        }
        base.OnActionExecuting(context);
    }
}