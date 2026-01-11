namespace CCValidator;

/// <summary>
/// Controls how internal exceptions during validation are handled.
/// </summary>
public enum ValidationExceptionBehavior
{
  /// <summary>
  /// Convert exceptions thrown during validation into a <see cref="ValidationFailure"/>.
  /// </summary>
  ConvertToFailure = 0,

  /// <summary>
  /// Re-throw exceptions encountered during validation.
  /// </summary>
  Throw = 1,
}

/// <summary>
/// Per-validator defaults and policies. Intended to be immutable and safe to share.
/// </summary>
public sealed class CCValidatorOptions
{
  /// <summary>
  /// Default cascade mode for new rules.
  /// </summary>
  public CascadeMode DefaultCascadeMode { get; init; } = CascadeMode.Continue;

  /// <summary>
  /// Provider for default validation messages.
  /// </summary>
  public IValidationMessageProvider MessageProvider { get; init; } = DefaultValidationMessageProvider.Instance;

  /// <summary>
  /// Time provider used by time-based rules.
  /// </summary>
  /// <remarks>
  /// Defaults to <see cref="TimeProvider.System"/>. Override this in tests to make time-based validation deterministic.
  /// </remarks>
  public TimeProvider TimeProvider { get; init; } = TimeProvider.System;

  /// <summary>
  /// Logger used for internal validation errors.
  /// </summary>
  public IValidationLogger Logger { get; init; } = NullValidationLogger.Instance;

  /// <summary>
  /// Controls whether internal exceptions are converted to failures or thrown.
  /// </summary>
  public ValidationExceptionBehavior ExceptionBehavior { get; init; } = ValidationExceptionBehavior.ConvertToFailure;

  /// <summary>
  /// Error message used when internal exceptions are converted to failures.
  /// </summary>
  public string InternalErrorMessage { get; init; } = "An internal validation error occurred";

  /// <summary>
  /// Error code used when internal exceptions are converted to failures.
  /// </summary>
  public string? InternalErrorCode { get; init; } = "CCV_INTERNAL";
}
