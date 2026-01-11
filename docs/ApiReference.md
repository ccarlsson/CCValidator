# API reference (short)

This page is a quick index of the main public surface.

## Core types (`CCValidator`)

- `AbstractValidator<T>`
  - Define rules via `RuleFor(...)`.
  - Collection rules via `RuleForEach(...)`.
  - Composition via `Include(...)`.
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
  - Default cascade mode, message provider, exception behavior, internal error code/message, logging, and `TimeProvider`.

### Rule building

- `IRuleBuilderInitial<T, TProperty>` / `IRuleBuilderOptions<T, TProperty>`
  - Core fluent methods like `NotEmpty`, `NotNull`, `Length`, `Matches`, `EmailAddress`, comparisons, `Must`/`MustAsync`.
  - Nested validation: `SetValidator(...)` and `ChildRules(...)`.
  - Dependent rules: `DependentRules(...)`.
  - Message metadata: `WithMessage(...)` / `WithErrorCode(...)`.

### Extensibility

- Custom validator classes
  - `IPropertyValidator<T, TProperty>`
  - `IAsyncPropertyValidator<T, TProperty>`
  - Register via `SetValidator(...)` / `SetAsyncValidator(...)`.

### Scoping

- `RuleSet("name", () => { ... })` to tag rules.
- `When(...)` / `Unless(...)` to conditionally apply a scope of rules.

## Dependency injection (`CCValidator.DependencyInjection`)

- `ServiceCollectionExtensions`
  - `AddValidatorsFromAssembly(...)` / `AddValidatorsFromAssemblyContaining<T>(...)`
  - `AddCCValidator(...)` overloads for registering/configuring `CCValidatorOptions`.
- `CCValidatorOptionsBuilder`
  - Builder used by `AddCCValidator(o => { ... })`.

## ASP.NET Core MVC (`CCValidator.AspNetCore`)

- `ServiceCollectionExtensions`
  - `AddCCValidatorAutoValidation()` for MVC model validation integration.

  Note: MVC model validation is synchronous and invokes `Validate` only. Async rules are not executed during model binding.

## Logging (`CCValidator` + optional `CCValidator.Serilog`)

- `IValidationLogger`
  - Receives internal validation exceptions that are converted to failures.
- `SerilogValidationLogger` (in `CCValidator.Serilog`)
  - Logs internal validation errors to Serilog.