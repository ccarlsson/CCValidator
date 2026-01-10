namespace CCValidator;

public sealed class ValidationFailure
{
  public ValidationFailure(string propertyName, string errorMessage)
  {
    PropertyName = propertyName;
    ErrorMessage = errorMessage;
  }

  public string PropertyName { get; }

  public string ErrorMessage { get; }

  public object? AttemptedValue { get; init; }

  public string? ErrorCode { get; init; }

  public Severity? Severity { get; init; }

  public object? CustomState { get; init; }
}

public enum Severity
{
  Error = 0,
  Warning = 1,
  Info = 2,
}
