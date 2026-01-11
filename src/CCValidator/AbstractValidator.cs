using System.Linq.Expressions;

namespace CCValidator;

public abstract class AbstractValidator<T> : IValidator<T>
{
  private readonly List<IRule<T>> _rules = [];
  private readonly Stack<Func<T, bool>> _conditionStack = new();
  private readonly Stack<string> _ruleSetStack = new();
  private readonly CCValidatorOptions _options;

  protected AbstractValidator()
    : this(options: null)
  {
  }

  protected AbstractValidator(CCValidatorOptions? options)
  {
    _options = options ?? new CCValidatorOptions();

    CascadeMode = _options.DefaultCascadeMode;
    MessageProvider = _options.MessageProvider;
  }

  public CascadeMode CascadeMode { get; set; }

  public IValidationMessageProvider MessageProvider { get; set; }

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

    _rules.Add(rule);

    return new RuleBuilder<T, TProperty>(rule, MessageProvider);
  }

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

  protected void Unless(Func<T, bool> predicate, Action action)
  {
    ArgumentNullException.ThrowIfNull(predicate);
    ArgumentNullException.ThrowIfNull(action);

    When(x => !predicate(x), action);
  }

  public virtual ValidationResult Validate(T instance)
  {
    return Validate(new ValidationContext<T>(instance));
  }

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

  public virtual Task<ValidationResult> ValidateAsync(T instance, CancellationToken token = default)
  {
    return ValidateAsync(new ValidationContext<T>(instance), token);
  }

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
