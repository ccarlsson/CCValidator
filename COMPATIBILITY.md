# Compatibility notes (vs FluentValidation)

This project aims to be FluentValidation-compatible where feasible, but it is not a byte-for-byte clone.

## Known differences

- Configuration: CCValidator uses explicit `CCValidatorOptions` (and DI registration) instead of static/global configuration.
- Result API: `ValidationResult` exposes `IsValid` (the SRS text used `IValid`, but the FluentValidation-style name is `IsValid`).
- API surface: Only a subset of FluentValidation’s full API is implemented so far.

## Reliability behavior

- By default, internal exceptions during validation are converted to a `ValidationFailure` (with `ErrorCode` = `CCV_INTERNAL`).
- Internal errors can be logged via `IValidationLogger` and the optional `CCValidator.Serilog` adapter.