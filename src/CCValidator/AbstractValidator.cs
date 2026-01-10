using System.Linq.Expressions;

namespace CCValidator;

public abstract class AbstractValidator<T> : IValidator<T>
{
  private readonly List<object> _rules = [];

  public CascadeMode CascadeMode { get; set; } = CascadeMode.Continue;

  protected IRuleBuilderInitial<T, TProperty> RuleFor<TProperty>(Expression<Func<T, TProperty>> expression)
  {
    var propertyName = ExpressionHelpers.GetPropertyName(expression);
    var getter = expression.Compile();

    var rule = new PropertyRule<T, TProperty>(propertyName, getter, CascadeMode);
    _rules.Add(rule);

    return new RuleBuilder<T, TProperty>(rule);
  }

  public virtual ValidationResult Validate(T instance)
  {
    var failures = new List<ValidationFailure>();

    foreach (var ruleObj in _rules)
    {
      // Avoid reflection by storing concrete generic rule instances.
      // For now we use a type test per rule kind.
      switch (ruleObj)
      {
        case PropertyRule<T, string> r:
          failures.AddRange(r.Validate(instance));
          break;
        default:
          // Fallback for other TProperty types via dynamic dispatch.
          // This keeps the skeleton usable while we grow supported rule types.
          failures.AddRange(((dynamic)ruleObj).Validate(instance));
          break;
      }
    }

    return new ValidationResult(failures);
  }

  public virtual Task<ValidationResult> ValidateAsync(T instance, CancellationToken token = default)
  {
    return ValidateInternalAsync(instance, token);
  }

  private async Task<ValidationResult> ValidateInternalAsync(T instance, CancellationToken token)
  {
    token.ThrowIfCancellationRequested();

    var failures = new List<ValidationFailure>();

    foreach (var ruleObj in _rules)
    {
      switch (ruleObj)
      {
        case PropertyRule<T, string> r:
          failures.AddRange(await r.ValidateAsync(instance, token).ConfigureAwait(false));
          break;
        default:
          failures.AddRange(await ((dynamic)ruleObj).ValidateAsync(instance, token).ConfigureAwait(false));
          break;
      }
    }

    return new ValidationResult(failures);
  }
}
