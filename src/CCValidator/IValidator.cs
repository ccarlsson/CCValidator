namespace CCValidator;

public interface IValidator<in T>
{
  ValidationResult Validate(T instance);

  Task<ValidationResult> ValidateAsync(T instance, CancellationToken token = default);
}
