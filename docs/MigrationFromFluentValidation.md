# Migration from FluentValidation

This library is intentionally similar to FluentValidation, but it is not a complete clone.

## What maps directly

- `AbstractValidator<T>` and `RuleFor(...)`
- `RuleForEach(...)` for collection validation
- Chaining validators: `.NotEmpty().MaximumLength(50)`
- `.WithMessage(...)` and `.WithErrorCode(...)`
- `When`/`Unless` and rule sets
- Validator composition: `Include(...)`
- Nested validators: `SetValidator(...)` / `ChildRules(...)`
- Dependent rules: `DependentRules(...)`
- Sync and async validation (`Validate` / `ValidateAsync`)

## Key differences

- Configuration is explicit:
  - CCValidator uses `CCValidatorOptions` (and DI registration) instead of global/static configuration.
- API surface area is smaller:
  - Only a subset of FluentValidation’s built-in validators and extension points are implemented so far.
- MVC integration:
  - ASP.NET Core model validation runs synchronously; async rules are not executed during model binding.

For a running list, see the repo-level `COMPATIBILITY.md`.