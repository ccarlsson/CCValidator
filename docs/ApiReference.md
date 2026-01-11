# API reference (short)

This page is a quick index of the main public surface.

## Core types (`CCValidator`)

- `AbstractValidator<T>`
  - Define rules via `RuleFor(...)`.
  - Execute rules via `Validate(...)` / `ValidateAsync(...)`.
  - Control defaults via `CascadeMode` and `MessageProvider`.
- `IValidator<T>`
  - Interface for validators used by DI and ASP.NET integration.
- `ValidationResult`
  - `IsValid` and `Errors`.
- `ValidationFailure`
  - `PropertyName`, `ErrorMessage`, and optional metadata (`AttemptedValue`, `ErrorCode`, `Severity`, `CustomState`).
- `ValidationContext<T>`
  - Controls ruleset selection for validation.
- `CCValidatorOptions`
  - Default cascade mode, message provider, exception behavior, internal error code/message, and logging.

### Rule building

- `IRuleBuilderInitial<T, TProperty>` / `IRuleBuilderOptions<T, TProperty>`
  - Core fluent methods like `NotEmpty`, `NotNull`, `Length`, `Matches`, `EmailAddress`, comparisons, `Must`/`MustAsync`.
  - Customization via `WithMessage(...)` / `WithErrorCode(...)`.

## Dependency injection (`CCValidator.DependencyInjection`)

- `ServiceCollectionExtensions`
  - `AddValidatorsFromAssembly(...)` / `AddValidatorsFromAssemblyContaining<T>(...)`
  - `AddCCValidator(...)` overloads for registering/configuring `CCValidatorOptions`.
- `CCValidatorOptionsBuilder`
  - Builder used by `AddCCValidator(o => { ... })`.

## ASP.NET Core MVC (`CCValidator.AspNetCore`)

- `ServiceCollectionExtensions`
  - `AddCCValidatorAutoValidation()` for MVC model validation integration.

## Logging (`CCValidator` + optional `CCValidator.Serilog`)

- `IValidationLogger`
  - Receives internal validation exceptions that are converted to failures.
- `SerilogValidationLogger` (in `CCValidator.Serilog`)
  - Logs internal validation errors to Serilog.