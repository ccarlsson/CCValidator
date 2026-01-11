namespace CCValidator;

/// <summary>
/// Represents the outcome of validation.
/// </summary>
public sealed class ValidationResult
{
  /// <summary>
  /// Create an empty result.
  /// </summary>
  public ValidationResult()
  {
  }

  /// <summary>
  /// Create a result from an existing set of failures.
  /// </summary>
  /// <param name="failures">Validation failures to include.</param>
  public ValidationResult(IEnumerable<ValidationFailure> failures)
  {
    Errors.AddRange(failures);
  }

  /// <summary>
  /// True when there are no failures.
  /// </summary>
  public bool IsValid => Errors.Count == 0;

  /// <summary>
  /// The list of validation failures.
  /// </summary>
  public List<ValidationFailure> Errors { get; } = [];
}
