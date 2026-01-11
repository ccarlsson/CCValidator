using System.Linq.Expressions;

namespace CCValidator;

/// <summary>
/// Base type for defining validation rules for <typeparamref name="T"/>.
/// </summary>
public abstract class AbstractValidator<T> : IValidator<T>
{
  private readonly List<IRule<T>> _rules = [];
  private readonly Stack<Func<T, bool>> _conditionStack = new();
  private readonly Stack<string> _ruleSetStack = new();
  private readonly Stack<IDependentRuleHost<T>> _dependentRuleHostStack = new();
  private readonly CCValidatorOptions _options;

  protected AbstractValidator()
    : this(options: null)
  {
  }

  /// <summary>
  /// Create a validator using the provided options.
  /// </summary>
  /// <param name="options">Options controlling defaults and behavior.</param>
  protected AbstractValidator(CCValidatorOptions? options)
  {
    _options = options ?? new CCValidatorOptions();

    CascadeMode = _options.DefaultCascadeMode;
    MessageProvider = _options.MessageProvider;
  }

  public CascadeMode CascadeMode { get; set; }

  public IValidationMessageProvider MessageProvider { get; set; }

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

  /// <summary>
  /// Execute rules inside a named ruleset.
  /// </summary>
  protected void RuleSet(string ruleSetName, Action action)
  {
    ArgumentNullException.ThrowIfNull(ruleSetName);
    ArgumentNullException.ThrowIfNull(action);

    if (ruleSetName.Length == 0)
      throw new ArgumentException("RuleSet name cannot be empty.", nameof(ruleSetName));

    _ruleSetStack.Push(ruleSetName);
    try
    {
      action();
    }
    finally
    {
      _ruleSetStack.Pop();
    }
  }

  /// <summary>
  /// Apply a condition to all rules defined inside <paramref name="action"/>.
  /// </summary>
  protected void When(Func<T, bool> predicate, Action action)
  {
    ArgumentNullException.ThrowIfNull(predicate);
    ArgumentNullException.ThrowIfNull(action);

    _conditionStack.Push(predicate);
    try
    {
      action();
    }
    finally
    {
      _conditionStack.Pop();
    }
  }

  /// <summary>
  /// Apply the inverse of <paramref name="predicate"/> to all rules defined inside <paramref name="action"/>.
  /// </summary>
  protected void Unless(Func<T, bool> predicate, Action action)
  {
    ArgumentNullException.ThrowIfNull(predicate);
    ArgumentNullException.ThrowIfNull(action);

    When(x => !predicate(x), action);
  }

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
