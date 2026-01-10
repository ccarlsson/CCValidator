using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CCValidator.AspNetCore;

internal sealed class CCValidatorModelValidator : IModelValidator
{
  public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
  {
    ArgumentNullException.ThrowIfNull(context);

    var requestServices = context.ActionContext.HttpContext?.RequestServices;
    if (requestServices is null)
      return Array.Empty<ModelValidationResult>();

    var modelType = context.ModelMetadata.ModelType;
    var validatorType = typeof(IValidator<>).MakeGenericType(modelType);

    var validator = requestServices.GetService(validatorType);
    if (validator is null)
      return Array.Empty<ModelValidationResult>();

    var validateMethod = validatorType.GetMethod("Validate", [modelType]);
    if (validateMethod is null)
      return Array.Empty<ModelValidationResult>();

    var resultObj = validateMethod.Invoke(validator, [context.Model]);
    if (resultObj is not ValidationResult result)
      return Array.Empty<ModelValidationResult>();

    return result.Errors.Select(f => new ModelValidationResult(f.PropertyName, f.ErrorMessage));
  }
}
