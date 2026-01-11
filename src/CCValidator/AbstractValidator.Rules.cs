using System.Linq.Expressions;

namespace CCValidator;

public abstract partial class AbstractValidator<T>
{
  /// <summary>
  /// Define a rule for a property.
  /// </summary>
  /// <typeparam name="TProperty">Property type.</typeparam>
  /// <param name="expression">Expression selecting the property.</param>
  /// <returns>A rule builder for chaining validators.</returns>
  protected IRuleBuilderInitial<T, TProperty> RuleFor<TProperty>(Expression<Func<T, TProperty>> expression)
  {
    var propertyName = ExpressionHelpers.GetPropertyName(expression);
    var getter = expression.Compile();

    var ruleSet = _ruleSetStack.Count == 0 ? null : _ruleSetStack.Peek();
    var rule = new PropertyRule<T, TProperty>(propertyName, getter, CascadeMode, ruleSet, _options);

    if (_conditionStack.Count != 0)
    {
      foreach (var condition in _conditionStack)
        rule.ApplyCondition(condition);
    }

    AddRule(rule);

    return new RuleBuilder<T, TProperty>(rule, MessageProvider, RunDependentRulesScope, _options);
  }

  /// <summary>
  /// Define rules for each element in a collection property.
  /// </summary>
  /// <typeparam name="TElement">Element type.</typeparam>
  /// <param name="expression">Expression selecting a collection property.</param>
  /// <returns>A rule builder for chaining validators that apply to each element.</returns>
  protected IRuleBuilderInitial<T, TElement> RuleForEach<TElement>(Expression<Func<T, IEnumerable<TElement>>> expression)
  {
    ArgumentNullException.ThrowIfNull(expression);

    var propertyName = ExpressionHelpers.GetPropertyName(expression);
    var getter = expression.Compile();

    var ruleSet = _ruleSetStack.Count == 0 ? null : _ruleSetStack.Peek();
    var rule = new ForEachRule<T, TElement>(propertyName, getter, CascadeMode, ruleSet, _options);

    if (_conditionStack.Count != 0)
    {
      foreach (var condition in _conditionStack)
        rule.ApplyCondition(condition);
    }

    AddRule(rule);

    return new RuleBuilder<T, TElement>(rule, MessageProvider, RunDependentRulesScope, _options);
  }

  /// <summary>
  /// Includes rules from another validator.
  /// </summary>
  protected void Include(IValidator<T> validator)
  {
    ArgumentNullException.ThrowIfNull(validator);

    var ruleSet = _ruleSetStack.Count == 0 ? null : _ruleSetStack.Peek();
    var rule = new IncludedValidatorRule<T>(validator, ruleSet, _options);

    if (_conditionStack.Count != 0)
    {
      foreach (var condition in _conditionStack)
        rule.ApplyCondition(condition);
    }

    AddRule(rule);
  }

  private void AddRule(IRule<T> rule)
  {
    if (_dependentRuleHostStack.Count != 0)
    {
      _dependentRuleHostStack.Peek().AddDependentRule(rule);
      return;
    }

    _rules.Add(rule);
  }

  private void RunDependentRulesScope(IDependentRuleHost<T> host, Action action)
  {
    _dependentRuleHostStack.Push(host);
    try
    {
      action();
    }
    finally
    {
      _dependentRuleHostStack.Pop();
    }
  }
}
