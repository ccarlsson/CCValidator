namespace CCValidator.DependencyInjection;

/// <summary>
/// Mutable builder for creating <see cref="CCValidatorOptions"/> in DI registration.
/// </summary>
public sealed class CCValidatorOptionsBuilder
{
  /// <summary>
  /// Default cascade mode for new rules.
  /// </summary>
  public CascadeMode DefaultCascadeMode { get; set; } = CascadeMode.Continue;

  /// <summary>
  /// Provider for default validation messages.
  /// </summary>
  public IValidationMessageProvider MessageProvider { get; set; } = DefaultValidationMessageProvider.Instance;

  /// <summary>
  /// Logger used for internal validation errors.
  /// </summary>
  public IValidationLogger Logger { get; set; } = NullValidationLogger.Instance;

  /// <summary>
  /// Controls whether internal exceptions are converted to failures or thrown.
  /// </summary>
  public ValidationExceptionBehavior ExceptionBehavior { get; set; } = ValidationExceptionBehavior.ConvertToFailure;

  /// <summary>
  /// Error message used when internal exceptions are converted to failures.
  /// </summary>
  public string InternalErrorMessage { get; set; } = "An internal validation error occurred";

  /// <summary>
  /// Error code used when internal exceptions are converted to failures.
  /// </summary>
  public string? InternalErrorCode { get; set; } = "CCV_INTERNAL";

  /// <summary>
  /// Build an immutable <see cref="CCValidatorOptions"/> instance.
  /// </summary>
  public CCValidatorOptions Build()
  {
    return new CCValidatorOptions
    {
      DefaultCascadeMode = DefaultCascadeMode,
      MessageProvider = MessageProvider,
      Logger = Logger,
      ExceptionBehavior = ExceptionBehavior,
      InternalErrorMessage = InternalErrorMessage,
      InternalErrorCode = InternalErrorCode,
    };
  }
}