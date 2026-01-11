# Dependency injection

## Register validators

Use `CCValidator.DependencyInjection` to register all validators in an assembly:

```csharp
using CCValidator.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddValidatorsFromAssemblyContaining<MyValidator>();
```

By default, validators are registered as `Scoped`. You can override the lifetime:

```csharp
services.AddValidatorsFromAssemblyContaining<MyValidator>(ServiceLifetime.Singleton);
```

## Configure defaults (`CCValidatorOptions`)

Validators can take a `CCValidatorOptions` constructor parameter and pass it to `base(options)`.

To configure these defaults in DI:

```csharp
services.AddCCValidator(o =>
{
  o.DefaultCascadeMode = CascadeMode.Stop;
  o.ExceptionBehavior = ValidationExceptionBehavior.ConvertToFailure;
  o.InternalErrorCode = "CCV_INTERNAL";
});
```

If you don't call `AddCCValidator(...)`, `AddValidatorsFromAssembly(...)` will still register a default `CCValidatorOptions` so constructor injection works.