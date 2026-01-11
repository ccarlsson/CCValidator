namespace CCValidator;

public abstract partial class AbstractValidator<T>
{
  /// <inheritdoc />
  public virtual ValidationResult Validate(T instance)
  {
    return Validate(new ValidationContext<T>(instance));
  }

  /// <summary>
  /// Validate using a context (enables ruleset selection).
  /// </summary>
  public virtual ValidationResult Validate(ValidationContext<T> context)
  {
    ArgumentNullException.ThrowIfNull(context);

    var failures = new List<ValidationFailure>();

    foreach (var ruleObj in _rules)
    {
      if (!ShouldRunRule(ruleObj.RuleSet, context))
        continue;

      failures.AddRange(ruleObj.Validate(context.InstanceToValidate));
    }

    return new ValidationResult(failures);
  }

  /// <inheritdoc />
  public virtual Task<ValidationResult> ValidateAsync(T instance, CancellationToken token = default)
  {
    return ValidateAsync(new ValidationContext<T>(instance), token);
  }

  /// <summary>
  /// Validate asynchronously using a context (enables ruleset selection).
  /// </summary>
  public virtual Task<ValidationResult> ValidateAsync(ValidationContext<T> context, CancellationToken token = default)
  {
    return ValidateInternalAsync(context, token);
  }

  private async Task<ValidationResult> ValidateInternalAsync(ValidationContext<T> context, CancellationToken token)
  {
    token.ThrowIfCancellationRequested();

    ArgumentNullException.ThrowIfNull(context);

    var failures = new List<ValidationFailure>();

    foreach (var ruleObj in _rules)
    {
      if (!ShouldRunRule(ruleObj.RuleSet, context))
        continue;

      failures.AddRange(await ruleObj.ValidateAsync(context.InstanceToValidate, token).ConfigureAwait(false));
    }

    return new ValidationResult(failures);
  }

  private static bool ShouldRunRule(string? ruleSet, ValidationContext<T> context)
  {
    if (string.IsNullOrEmpty(ruleSet))
      return context.IncludeRulesNotInRuleSet;

    return context.IncludedRuleSets.Contains(ruleSet);
  }
}
