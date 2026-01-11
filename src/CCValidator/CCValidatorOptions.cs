namespace CCValidator;

public enum ValidationExceptionBehavior
{
  ConvertToFailure = 0,
  Throw = 1,
}

public sealed class CCValidatorOptions
{
  public CascadeMode DefaultCascadeMode { get; init; } = CascadeMode.Continue;

  public IValidationMessageProvider MessageProvider { get; init; } = DefaultValidationMessageProvider.Instance;

  public ValidationExceptionBehavior ExceptionBehavior { get; init; } = ValidationExceptionBehavior.ConvertToFailure;

  public string InternalErrorMessage { get; init; } = "An internal validation error occurred";

  public string? InternalErrorCode { get; init; } = "CCV_INTERNAL";
}
