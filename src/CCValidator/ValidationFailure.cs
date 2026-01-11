namespace CCValidator;

/// <summary>
/// Represents a single validation failure.
/// </summary>
public sealed class ValidationFailure
{
  /// <summary>
  /// Create a validation failure.
  /// </summary>
  /// <param name="propertyName">Property name associated with the failure.</param>
  /// <param name="errorMessage">Human-readable error message.</param>
  public ValidationFailure(string propertyName, string errorMessage)
  {
    PropertyName = propertyName;
    ErrorMessage = errorMessage;
  }

  /// <summary>
  /// The property name associated with the failure.
  /// </summary>
  public string PropertyName { get; }

  /// <summary>
  /// Human-readable error message.
  /// </summary>
  public string ErrorMessage { get; }

  /// <summary>
  /// The value that was attempted/validated.
  /// </summary>
  public object? AttemptedValue { get; init; }

  /// <summary>
  /// Optional error code.
  /// </summary>
  public string? ErrorCode { get; init; }

  /// <summary>
  /// Optional severity.
  /// </summary>
  public Severity? Severity { get; init; }

  /// <summary>
  /// Optional custom state.
  /// </summary>
  public object? CustomState { get; init; }
}

/// <summary>
/// Optional severity for a validation failure.
/// </summary>
public enum Severity
{
  Error = 0,
  Warning = 1,
  Info = 2,
}
