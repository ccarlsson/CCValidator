using System.Collections.Concurrent;
using System.Linq.Expressions;
using CCValidator;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CCValidator.AspNetCore;

internal sealed class CCValidatorModelValidator : IModelValidator
{
  private static readonly ConcurrentDictionary<Type, Func<object, object?, ValidationResult>> ValidateDelegates = new();

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

    // MVC can invoke validation when the model is null. Keep behavior predictable.
    var model = context.Model;
    if (model is null)
      return Array.Empty<ModelValidationResult>();

    var invoker = ValidateDelegates.GetOrAdd(modelType, CreateValidateDelegate);

    ValidationResult result;
    try
    {
      result = invoker(validator, model);
    }
    catch
    {
      // Model validation should not throw.
      return Array.Empty<ModelValidationResult>();
    }

    if (result.Errors.Count == 0)
      return Array.Empty<ModelValidationResult>();

    return result.Errors.Select(f => new ModelValidationResult(f.PropertyName, f.ErrorMessage));
  }

  private static Func<object, object?, ValidationResult> CreateValidateDelegate(Type modelType)
  {
    var validatorType = typeof(IValidator<>).MakeGenericType(modelType);

    var validatorObj = Expression.Parameter(typeof(object), "validator");
    var modelObj = Expression.Parameter(typeof(object), "model");

    var castValidator = Expression.Convert(validatorObj, validatorType);

    // For non-nullable value types, MVC can't pass null if the model exists, but keep the expression valid.
    Expression castModel;
    if (modelType.IsValueType && Nullable.GetUnderlyingType(modelType) is null)
      castModel = Expression.Unbox(modelObj, modelType);
    else
      castModel = Expression.Convert(modelObj, modelType);

    var validateMethod = validatorType.GetMethod("Validate", [modelType]);
    if (validateMethod is null)
    {
      return static (_, _) => new ValidationResult();
    }

    var call = Expression.Call(castValidator, validateMethod, castModel);
    var lambda = Expression.Lambda<Func<object, object?, ValidationResult>>(call, validatorObj, modelObj);
    return lambda.Compile();
  }
}
