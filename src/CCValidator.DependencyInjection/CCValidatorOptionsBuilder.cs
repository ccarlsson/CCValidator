namespace CCValidator.DependencyInjection;

public sealed class CCValidatorOptionsBuilder
{
  public CascadeMode DefaultCascadeMode { get; set; } = CascadeMode.Continue;

  public IValidationMessageProvider MessageProvider { get; set; } = DefaultValidationMessageProvider.Instance;

  public IValidationLogger Logger { get; set; } = NullValidationLogger.Instance;

  public ValidationExceptionBehavior ExceptionBehavior { get; set; } = ValidationExceptionBehavior.ConvertToFailure;

  public string InternalErrorMessage { get; set; } = "An internal validation error occurred";

  public string? InternalErrorCode { get; set; } = "CCV_INTERNAL";

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