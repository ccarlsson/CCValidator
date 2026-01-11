using System.Linq.Expressions;

namespace CCValidator;

/// <summary>
/// A validator that can be configured inline (useful for <c>ChildRules</c>).
/// </summary>
public sealed class InlineValidator<T> : AbstractValidator<T>
{
  /// <summary>
  /// Create an inline validator.
  /// </summary>
  public InlineValidator()
  {
  }

  internal InlineValidator(CCValidatorOptions? options)
    : base(options)
  {
  }

  /// <summary>
  /// Define a rule for a property.
  /// </summary>
  public new IRuleBuilderInitial<T, TProperty> RuleFor<TProperty>(Expression<Func<T, TProperty>> expression)
  {
    return base.RuleFor(expression);
  }

  /// <summary>
  /// Define rules for each element in a collection property.
  /// </summary>
  public new IRuleBuilderInitial<T, TElement> RuleForEach<TElement>(Expression<Func<T, IEnumerable<TElement>>> expression)
  {
    return base.RuleForEach(expression);
  }

  /// <summary>
  /// Include rules from another validator.
  /// </summary>
  public new void Include(IValidator<T> validator)
  {
    base.Include(validator);
  }

  /// <summary>
  /// Execute rules inside a named ruleset.
  /// </summary>
  public new void RuleSet(string ruleSetName, Action action)
  {
    base.RuleSet(ruleSetName, action);
  }

  /// <summary>
  /// Apply a condition to all rules defined inside <paramref name="action"/>.
  /// </summary>
  public new void When(Func<T, bool> predicate, Action action)
  {
    base.When(predicate, action);
  }

  /// <summary>
  /// Apply the inverse of <paramref name="predicate"/> to all rules defined inside <paramref name="action"/>.
  /// </summary>
  public new void Unless(Func<T, bool> predicate, Action action)
  {
    base.Unless(predicate, action);
  }
}
