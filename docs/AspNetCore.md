# ASP.NET Core integration

CCValidator integrates with ASP.NET Core MVC model validation by inserting a model validator provider.

## Register auto-validation

```csharp
using CCValidator.AspNetCore;
using CCValidator.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services
  .AddControllers()
  .AddCCValidatorAutoValidation();

// Register validators
builder.Services.AddValidatorsFromAssemblyContaining<MyValidator>();
```

At runtime, the model validator resolves `IValidator<TModel>` from `HttpContext.RequestServices`.
If no validator is registered for a model type, no validation errors are produced.

## Configure defaults

If your validators accept `CCValidatorOptions` and pass it to `base(options)`, register options via DI:

```csharp
builder.Services.AddCCValidator(o =>
{
  o.DefaultCascadeMode = CascadeMode.Stop;
  o.ExceptionBehavior = ValidationExceptionBehavior.ConvertToFailure;
});
```

## Notes

- This integration is synchronous (MVC calls `Validate`), so async rules are not executed during model binding.
- For localization and logging, see `docs/Localization.md` and `docs/Logging.md`.