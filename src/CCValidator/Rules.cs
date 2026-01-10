using System.Collections;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace CCValidator;

internal interface IRule<T>
{
  string? RuleSet { get; }

  IEnumerable<ValidationFailure> Validate(T instance);

  Task<IEnumerable<ValidationFailure>> ValidateAsync(T instance, CancellationToken token);
}

public interface IRuleBuilderInitial<T, TProperty>
{
  IRuleBuilderInitial<T, TProperty> Cascade(CascadeMode cascadeMode);
  IRuleBuilderOptions<T, TProperty> When(Func<T, bool> predicate);
  IRuleBuilderOptions<T, TProperty> Unless(Func<T, bool> predicate);
  IRuleBuilderOptions<T, TProperty> Must(Func<TProperty, bool> predicate);
  IRuleBuilderOptions<T, TProperty> MustAsync(Func<TProperty, CancellationToken, Task<bool>> predicate);
  IRuleBuilderOptions<T, TProperty> NotNull();
  IRuleBuilderOptions<T, TProperty> NotEmpty();
  IRuleBuilderOptions<T, TProperty> MaximumLength(int maximumLength);
  IRuleBuilderOptions<T, TProperty> MinimumLength(int minimumLength);
  IRuleBuilderOptions<T, TProperty> Length(int minimumLength, int maximumLength);
  IRuleBuilderOptions<T, TProperty> Matches(string pattern);
}

public interface IRuleBuilderOptions<T, TProperty> : IRuleBuilderInitial<T, TProperty>
{
  IRuleBuilderOptions<T, TProperty> WithMessage(string message);
  IRuleBuilderOptions<T, TProperty> WithErrorCode(string errorCode);
}

internal sealed class RuleBuilder<T, TProperty> : IRuleBuilderOptions<T, TProperty>
{
  private readonly PropertyRule<T, TProperty> _rule;

  public RuleBuilder(PropertyRule<T, TProperty> rule)
  {
    _rule = rule;
  }

  public IRuleBuilderInitial<T, TProperty> Cascade(CascadeMode cascadeMode)
  {
    _rule.CascadeMode = cascadeMode;
    return this;
  }

  public IRuleBuilderOptions<T, TProperty> When(Func<T, bool> predicate)
  {
    ArgumentNullException.ThrowIfNull(predicate);
    _rule.ApplyCondition(predicate);
    return this;
  }

  public IRuleBuilderOptions<T, TProperty> Unless(Func<T, bool> predicate)
  {
    ArgumentNullException.ThrowIfNull(predicate);
    _rule.ApplyCondition(x => !predicate(x));
    return this;
  }

  public IRuleBuilderOptions<T, TProperty> NotNull()
  {
    _rule.AddValidator(static v => v is not null, "must not be null");
    return this;
  }

  public IRuleBuilderOptions<T, TProperty> Must(Func<TProperty, bool> predicate)
  {
    ArgumentNullException.ThrowIfNull(predicate);

    _rule.AddValidator(
      v => v is null ? predicate(default!) : predicate((TProperty)v),
      "is not valid");

    return this;
  }

  public IRuleBuilderOptions<T, TProperty> MustAsync(Func<TProperty, CancellationToken, Task<bool>> predicate)
  {
    ArgumentNullException.ThrowIfNull(predicate);

    _rule.AddAsyncValidator(
      async (v, ct) =>
      {
        if (v is null) return await predicate(default!, ct).ConfigureAwait(false);
        return await predicate((TProperty)v, ct).ConfigureAwait(false);
      },
      "is not valid");

    return this;
  }

  public IRuleBuilderOptions<T, TProperty> NotEmpty()
  {
    _rule.AddValidator(NotEmptyImpl, "must not be empty");
    return this;

    static bool NotEmptyImpl(object? value)
    {
      if (value is null) return false;

      if (value is string s)
      {
        return s.Length != 0;
      }

      if (value is IEnumerable enumerable)
      {
        var enumerator = enumerable.GetEnumerator();
        try
        {
          return enumerator.MoveNext();
        }
        finally
        {
          (enumerator as IDisposable)?.Dispose();
        }
      }

      return true;
    }
  }

  public IRuleBuilderOptions<T, TProperty> MaximumLength(int maximumLength)
  {
    _rule.AddValidator(v => v is null || GetLength(v) <= maximumLength, $"must be {maximumLength} characters or fewer");
    return this;
  }

  public IRuleBuilderOptions<T, TProperty> MinimumLength(int minimumLength)
  {
    _rule.AddValidator(v => v is null || GetLength(v) >= minimumLength, $"must be {minimumLength} characters or more");
    return this;
  }

  public IRuleBuilderOptions<T, TProperty> Length(int minimumLength, int maximumLength)
  {
    _rule.AddValidator(v => v is null || (GetLength(v) >= minimumLength && GetLength(v) <= maximumLength), $"must be between {minimumLength} and {maximumLength} characters");
    return this;
  }

  public IRuleBuilderOptions<T, TProperty> Matches(string pattern)
  {
    var regex = new Regex(pattern, RegexOptions.Compiled);
    _rule.AddValidator(v => v is null || (v is string s && regex.IsMatch(s)), "is not in the correct format");
    return this;
  }

  public IRuleBuilderOptions<T, TProperty> WithMessage(string message)
  {
    _rule.SetMessageOverrideForLastValidator(message);
    return this;
  }

  public IRuleBuilderOptions<T, TProperty> WithErrorCode(string errorCode)
  {
    _rule.SetErrorCodeOverrideForLastValidator(errorCode);
    return this;
  }

  private static int GetLength(object value)
  {
    return value switch
    {
      string s => s.Length,
      _ => throw new NotSupportedException($"Length validators currently support string only. Value type: {value.GetType().FullName}"),
    };
  }
}

internal sealed class PropertyRule<T, TProperty>
  : IRule<T>
{
  private readonly Func<T, TProperty> _getter;
  private Func<T, bool>? _condition;

  private readonly List<PropertyValidator> _validators = [];
  private readonly List<AsyncPropertyValidator> _asyncValidators = [];
  private readonly List<ValidatorSlot> _slots = [];

  public PropertyRule(string propertyName, Func<T, TProperty> getter, CascadeMode cascadeMode, string? ruleSet)
  {
    PropertyName = propertyName;
    _getter = getter;
    CascadeMode = cascadeMode;
    RuleSet = ruleSet;
  }

  public string PropertyName { get; }

  public string? RuleSet { get; }

  public CascadeMode CascadeMode { get; set; }

  public bool HasAsyncValidators => _asyncValidators.Count != 0;

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

  public void AddValidator(Func<object?, bool> predicate, string defaultMessage)
  {
    _validators.Add(new PropertyValidator(predicate, defaultMessage));
    _slots.Add(ValidatorSlot.ForSync(_validators.Count - 1));
  }

  public void AddAsyncValidator(Func<object?, CancellationToken, Task<bool>> predicate, string defaultMessage)
  {
    _asyncValidators.Add(new AsyncPropertyValidator(predicate, defaultMessage));
    _slots.Add(ValidatorSlot.ForAsync(_asyncValidators.Count - 1));
  }

  public void SetMessageOverrideForLastValidator(string message)
  {
    if (_slots.Count == 0)
      throw new InvalidOperationException("WithMessage must be used after a validator.");

    var slot = _slots[^1];
    if (slot.IsAsync)
    {
      _asyncValidators[slot.Index] = _asyncValidators[slot.Index] with { MessageOverride = message };
      return;
    }

    _validators[slot.Index] = _validators[slot.Index] with { MessageOverride = message };
  }

  public void SetErrorCodeOverrideForLastValidator(string errorCode)
  {
    if (_slots.Count == 0)
      throw new InvalidOperationException("WithErrorCode must be used after a validator.");

    var slot = _slots[^1];
    if (slot.IsAsync)
    {
      _asyncValidators[slot.Index] = _asyncValidators[slot.Index] with { ErrorCodeOverride = errorCode };
      return;
    }

    _validators[slot.Index] = _validators[slot.Index] with { ErrorCodeOverride = errorCode };
  }

  public IEnumerable<ValidationFailure> Validate(T instance)
  {
    if (HasAsyncValidators)
      throw new InvalidOperationException("This validator contains async rules and must be executed with ValidateAsync.");

    if (_condition is not null && !_condition(instance))
      yield break;

    var value = _getter(instance);
    object? boxed = value;

    foreach (var validator in _validators)
    {
      if (validator.Predicate(boxed)) continue;

      yield return new ValidationFailure(PropertyName, validator.EffectiveMessage)
      {
        AttemptedValue = boxed,
        ErrorCode = validator.ErrorCodeOverride,
      };

      if (CascadeMode == CascadeMode.Stop)
        yield break;
    }
  }

  public async Task<IEnumerable<ValidationFailure>> ValidateAsync(T instance, CancellationToken token)
  {
    token.ThrowIfCancellationRequested();

    if (_condition is not null && !_condition(instance))
      return Array.Empty<ValidationFailure>();

    var failures = new List<ValidationFailure>();

    var value = _getter(instance);
    object? boxed = value;

    foreach (var validator in _validators)
    {
      if (validator.Predicate(boxed))
        continue;

      failures.Add(new ValidationFailure(PropertyName, validator.EffectiveMessage)
      {
        AttemptedValue = boxed,
        ErrorCode = validator.ErrorCodeOverride,
      });

      if (CascadeMode == CascadeMode.Stop)
        return failures;
    }

    foreach (var validator in _asyncValidators)
    {
      token.ThrowIfCancellationRequested();

      if (await validator.Predicate(boxed, token).ConfigureAwait(false))
        continue;

      failures.Add(new ValidationFailure(PropertyName, validator.EffectiveMessage)
      {
        AttemptedValue = boxed,
        ErrorCode = validator.ErrorCodeOverride,
      });

      if (CascadeMode == CascadeMode.Stop)
        return failures;
    }

    return failures;
  }

  private readonly record struct PropertyValidator(
      Func<object?, bool> Predicate,
      string DefaultMessage)
  {
    public string? MessageOverride { get; init; }
    public string? ErrorCodeOverride { get; init; }
    public string EffectiveMessage => MessageOverride ?? DefaultMessage;
  }

  private readonly record struct AsyncPropertyValidator(
      Func<object?, CancellationToken, Task<bool>> Predicate,
      string DefaultMessage)
  {
    public string? MessageOverride { get; init; }
    public string? ErrorCodeOverride { get; init; }
    public string EffectiveMessage => MessageOverride ?? DefaultMessage;
  }

  private readonly record struct ValidatorSlot(bool IsAsync, int Index)
  {
    public static ValidatorSlot ForSync(int index) => new(false, index);
    public static ValidatorSlot ForAsync(int index) => new(true, index);
  }
}

internal static class ExpressionHelpers
{
  public static string GetPropertyName<T, TProperty>(Expression<Func<T, TProperty>> expression)
  {
    // Minimal implementation: supports x => x.Property and x => (object)x.Property
    Expression body = expression.Body;
    if (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } u)
      body = u.Operand;

    if (body is MemberExpression m)
      return m.Member.Name;

    return expression.ToString();
  }
}
