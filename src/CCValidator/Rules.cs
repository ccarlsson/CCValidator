using System.Collections;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace CCValidator;

public interface IRuleBuilderInitial<T, TProperty>
{
  IRuleBuilderInitial<T, TProperty> Cascade(CascadeMode cascadeMode);
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

  public IRuleBuilderOptions<T, TProperty> NotNull()
  {
    _rule.AddValidator(static v => v is not null, "must not be null");
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
{
  private readonly Func<T, TProperty> _getter;

  private readonly List<PropertyValidator> _validators = [];

  public PropertyRule(string propertyName, Func<T, TProperty> getter, CascadeMode cascadeMode)
  {
    PropertyName = propertyName;
    _getter = getter;
    CascadeMode = cascadeMode;
  }

  public string PropertyName { get; }

  public CascadeMode CascadeMode { get; set; }

  public void AddValidator(Func<object?, bool> predicate, string defaultMessage)
  {
    _validators.Add(new PropertyValidator(predicate, defaultMessage));
  }

  public void SetMessageOverrideForLastValidator(string message)
  {
    if (_validators.Count == 0)
      throw new InvalidOperationException("WithMessage must be used after a validator.");

    _validators[^1] = _validators[^1] with { MessageOverride = message };
  }

  public void SetErrorCodeOverrideForLastValidator(string errorCode)
  {
    if (_validators.Count == 0)
      throw new InvalidOperationException("WithErrorCode must be used after a validator.");

    _validators[^1] = _validators[^1] with { ErrorCodeOverride = errorCode };
  }

  public IEnumerable<ValidationFailure> Validate(T instance)
  {
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

  private readonly record struct PropertyValidator(
      Func<object?, bool> Predicate,
      string DefaultMessage)
  {
    public string? MessageOverride { get; init; }
    public string? ErrorCodeOverride { get; init; }
    public string EffectiveMessage => MessageOverride ?? DefaultMessage;
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
