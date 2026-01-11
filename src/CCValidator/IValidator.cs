namespace CCValidator;

/// <summary>
/// Validates instances of <typeparamref name="T"/> and produces a <see cref="ValidationResult"/>.
/// </summary>
public interface IValidator<in T>
{
  /// <summary>
  /// Validate an instance synchronously.
  /// </summary>
  /// <param name="instance">The instance to validate.</param>
  /// <returns>The validation result.</returns>
  ValidationResult Validate(T instance);

  /// <summary>
  /// Validate an instance asynchronously.
  /// </summary>
  /// <param name="instance">The instance to validate.</param>
  /// <param name="token">Cancellation token.</param>
  /// <returns>The validation result.</returns>
  Task<ValidationResult> ValidateAsync(T instance, CancellationToken token = default);
}
