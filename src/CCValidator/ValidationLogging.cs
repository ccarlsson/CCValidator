namespace CCValidator;

public readonly record struct InternalValidationErrorContext(
  string PropertyName,
  object? AttemptedValue,
  Exception Exception);

public interface IValidationLogger
{
  void InternalValidationError(InternalValidationErrorContext context);
}

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