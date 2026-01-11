namespace CCValidator;

/// <summary>
/// Context describing an internal validation error.
/// </summary>
public readonly record struct InternalValidationErrorContext(
  string PropertyName,
  object? AttemptedValue,
  Exception Exception);

/// <summary>
/// Logs internal exceptions that occur during validation.
/// Implementations must be best-effort and must not throw.
/// </summary>
public interface IValidationLogger
{
  /// <summary>
  /// Called when an internal exception is converted into a failure.
  /// </summary>
  void InternalValidationError(InternalValidationErrorContext context);
}

/// <summary>
/// No-op logger.
/// </summary>
public sealed class NullValidationLogger : IValidationLogger
{
  public static NullValidationLogger Instance { get; } = new();

  private NullValidationLogger()
  {
  }

  public void InternalValidationError(InternalValidationErrorContext context)
  {
  }
}