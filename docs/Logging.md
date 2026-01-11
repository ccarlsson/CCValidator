# Logging (Serilog)

CCValidator can log *internal validation errors* (exceptions thrown from conditions, property getters, or validators).

By default, CCValidator converts these exceptions to a `ValidationFailure` (see `CCValidatorOptions.ExceptionBehavior`) and does **not** log them.

## Core hook (`IValidationLogger`)

To log internal errors, set `CCValidatorOptions.Logger`.

```csharp
var options = new CCValidatorOptions
{
  Logger = new MyValidationLogger(),
};
```

## Serilog adapter (`CCValidator.Serilog`)

Reference the optional package project `CCValidator.Serilog` and configure a logger:

```csharp
using CCValidator.Serilog;
using Serilog;

var log = new LoggerConfiguration()
  .MinimumLevel.Information()
  .WriteTo.Console()
  .CreateLogger();

var options = new CCValidatorOptions
{
  Logger = new SerilogValidationLogger(log),
};
```

If you want the logger to be applied in DI scenarios, register the same `CCValidatorOptions` instance in your container and inject it into validators (constructor parameter), then call `base(options)`.