namespace CCValidator;

public sealed class ValidationResult
{
  public ValidationResult()
  {
  }

  public ValidationResult(IEnumerable<ValidationFailure> failures)
  {
    Errors.AddRange(failures);
  }

  public bool IsValid => Errors.Count == 0;

  public List<ValidationFailure> Errors { get; } = [];
}
