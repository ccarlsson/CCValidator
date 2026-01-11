namespace CCValidator;

internal sealed class IncludedValidatorRule<T> : IRule<T>
{
  private readonly IValidator<T> _validator;
  private Func<T, bool>? _condition;
  private readonly CCValidatorOptions _options;

  public IncludedValidatorRule(IValidator<T> validator, string? ruleSet, CCValidatorOptions options)
  {
    _validator = validator;
    RuleSet = ruleSet;
    _options = options;
  }

  public string? RuleSet { get; }

  public void ApplyCondition(Func<T, bool> predicate)
  {
    if (_condition is null)
    {
      _condition = predicate;
      return;
    }

    var existing = _condition;
    _condition = x => existing(x) && predicate(x);
  }

  public IEnumerable<ValidationFailure> Validate(T instance)
  {
    if (_condition is not null)
    {
      bool shouldRun;
      try
      {
        shouldRun = _condition(instance);
      }
      catch (Exception ex)
      {
        if (ShouldThrow(ex)) throw;
        return [CreateInternalFailure(ex, instance)];
      }

      if (!shouldRun)
        return Array.Empty<ValidationFailure>();
    }

    try
    {
      return _validator.Validate(instance).Errors;
    }
    catch (Exception ex)
    {
      if (ShouldThrow(ex)) throw;
      return [CreateInternalFailure(ex, instance)];
    }
  }

  public async Task<IEnumerable<ValidationFailure>> ValidateAsync(T instance, CancellationToken token)
  {
    token.ThrowIfCancellationRequested();

    if (_condition is not null)
    {
      bool shouldRun;
      try
      {
        shouldRun = _condition(instance);
      }
      catch (Exception ex)
      {
        if (ShouldThrow(ex, token)) throw;
        return [CreateInternalFailure(ex, instance)];
      }

      if (!shouldRun)
        return Array.Empty<ValidationFailure>();
    }

    try
    {
      var result = await _validator.ValidateAsync(instance, token).ConfigureAwait(false);
      return result.Errors;
    }
    catch (Exception ex)
    {
      if (ShouldThrow(ex, token)) throw;
      return [CreateInternalFailure(ex, instance)];
    }
  }

  private bool ShouldThrow(Exception ex)
  {
    return _options.ExceptionBehavior == ValidationExceptionBehavior.Throw;
  }

  private bool ShouldThrow(Exception ex, CancellationToken token)
  {
    if (_options.ExceptionBehavior == ValidationExceptionBehavior.Throw)
      return true;

    if (ex is OperationCanceledException && token.IsCancellationRequested)
      return true;

    return false;
  }

  private ValidationFailure CreateInternalFailure(Exception ex, object? attemptedValue)
  {
    try
    {
      _options.Logger.InternalValidationError(
        new InternalValidationErrorContext(string.Empty, attemptedValue, ex));
    }
    catch
    {
    }

    return new ValidationFailure(propertyName: string.Empty, _options.InternalErrorMessage)
    {
      AttemptedValue = attemptedValue,
      ErrorCode = _options.InternalErrorCode,
      CustomState = ex,
    };
  }
}
