using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CCValidator.AspNetCore;

internal sealed class CCValidatorModelValidatorProvider : IModelValidatorProvider
{
  public void CreateValidators(ModelValidatorProviderContext context)
  {
    ArgumentNullException.ThrowIfNull(context);

    // We add a single model validator that will attempt to resolve IValidator<T> from RequestServices.
    // If no validator is registered for the current model type, it returns no results.
    context.Results.Add(new ValidatorItem
    {
      IsReusable = true,
      Validator = new CCValidatorModelValidator(),
    });
  }
}
