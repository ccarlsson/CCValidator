using System;
using System.Collections;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CCValidator;

internal sealed class RuleBuilder<T, TProperty> : IRuleBuilderOptions<T, TProperty>
{
  private readonly IRuleInternal<T, TProperty> _rule;
  private readonly IValidationMessageProvider _messages;
  private readonly Action<IDependentRuleHost<T>, Action>? _dependentRulesRunner;
  private readonly CCValidatorOptions? _options;

  private static readonly Regex EmailRegex = new(
    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
    RegexOptions.Compiled | RegexOptions.CultureInvariant);

  public RuleBuilder(
    IRuleInternal<T, TProperty> rule,
    IValidationMessageProvider messages,
    Action<IDependentRuleHost<T>, Action>? dependentRulesRunner = null,
    CCValidatorOptions? options = null)
  {
    _rule = rule;
    _messages = messages;
    _dependentRulesRunner = dependentRulesRunner;
    _options = options;
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
    _rule.AddValidator(static v => v is not null, _messages.NotNull());
    return this;
  }

  public IRuleBuilderOptions<T, TProperty> Equal(TProperty comparisonValue)
  {
    _rule.AddValidator(v => Equals(v, comparisonValue), _messages.Equal());
    return this;
  }

  public IRuleBuilderOptions<T, TProperty> Equal(Expression<Func<T, TProperty>> comparisonExpression)
  {
    ArgumentNullException.ThrowIfNull(comparisonExpression);
    var getter = comparisonExpression.Compile();

    _rule.AddValidator((instance, v) => Equals(v, getter(instance)), _messages.Equal());
    return this;
  }

  public IRuleBuilderOptions<T, TProperty> NotEqual(TProperty comparisonValue)
  {
    _rule.AddValidator(v => !Equals(v, comparisonValue), _messages.NotEqual());
    return this;
  }

  public IRuleBuilderOptions<T, TProperty> NotEqual(Expression<Func<T, TProperty>> comparisonExpression)
  {
    ArgumentNullException.ThrowIfNull(comparisonExpression);
    var getter = comparisonExpression.Compile();

    _rule.AddValidator((instance, v) => !Equals(v, getter(instance)), _messages.NotEqual());
    return this;
  }

  public IRuleBuilderOptions<T, TProperty> GreaterThan(TProperty value)
  {
    if (value is null) throw new ArgumentNullException(nameof(value));
    _rule.AddValidator(v => v is null || CompareObjects(v, value) > 0, _messages.GreaterThan());
    return this;
  }

  public IRuleBuilderOptions<T, TProperty> GreaterThan(Expression<Func<T, TProperty>> comparisonExpression)
  {
    ArgumentNullException.ThrowIfNull(comparisonExpression);
    var getter = comparisonExpression.Compile();

    _rule.AddValidator(
      (instance, v) =>
      {
        if (v is null) return true;
        var other = getter(instance);
        if (other is null) return true;
        return CompareObjects(v, other) > 0;
      },
      _messages.GreaterThan());
    return this;
  }

  public IRuleBuilderOptions<T, TProperty> GreaterThanOrEqualTo(TProperty value)
  {
    if (value is null) throw new ArgumentNullException(nameof(value));
    _rule.AddValidator(v => v is null || CompareObjects(v, value) >= 0, _messages.GreaterThanOrEqualTo());
    return this;
  }

  public IRuleBuilderOptions<T, TProperty> GreaterThanOrEqualTo(Expression<Func<T, TProperty>> comparisonExpression)
  {
    ArgumentNullException.ThrowIfNull(comparisonExpression);
    var getter = comparisonExpression.Compile();

    _rule.AddValidator(
      (instance, v) =>
      {
        if (v is null) return true;
        var other = getter(instance);
        if (other is null) return true;
        return CompareObjects(v, other) >= 0;
      },
      _messages.GreaterThanOrEqualTo());
    return this;
  }

  public IRuleBuilderOptions<T, TProperty> LessThan(TProperty value)
  {
    if (value is null) throw new ArgumentNullException(nameof(value));
    _rule.AddValidator(v => v is null || CompareObjects(v, value) < 0, _messages.LessThan());
    return this;
  }

  public IRuleBuilderOptions<T, TProperty> LessThan(Expression<Func<T, TProperty>> comparisonExpression)
  {
    ArgumentNullException.ThrowIfNull(comparisonExpression);
    var getter = comparisonExpression.Compile();

    _rule.AddValidator(
      (instance, v) =>
      {
        if (v is null) return true;
        var other = getter(instance);
        if (other is null) return true;
        return CompareObjects(v, other) < 0;
      },
      _messages.LessThan());
    return this;
  }

  public IRuleBuilderOptions<T, TProperty> LessThanOrEqualTo(TProperty value)
  {
    if (value is null) throw new ArgumentNullException(nameof(value));
    _rule.AddValidator(v => v is null || CompareObjects(v, value) <= 0, _messages.LessThanOrEqualTo());
    return this;
  }

  public IRuleBuilderOptions<T, TProperty> LessThanOrEqualTo(Expression<Func<T, TProperty>> comparisonExpression)
  {
    ArgumentNullException.ThrowIfNull(comparisonExpression);
    var getter = comparisonExpression.Compile();

    _rule.AddValidator(
      (instance, v) =>
      {
        if (v is null) return true;
        var other = getter(instance);
        if (other is null) return true;
        return CompareObjects(v, other) <= 0;
      },
      _messages.LessThanOrEqualTo());
    return this;
  }

  public IRuleBuilderOptions<T, TProperty> InclusiveBetween(TProperty from, TProperty to)
  {
    if (from is null) throw new ArgumentNullException(nameof(from));
    if (to is null) throw new ArgumentNullException(nameof(to));

    _rule.AddValidator(
      v => v is null || (CompareObjects(v, from) >= 0 && CompareObjects(v, to) <= 0),
      _messages.InclusiveBetween());
    return this;
  }

  public IRuleBuilderOptions<T, TProperty> ExclusiveBetween(TProperty from, TProperty to)
  {
    if (from is null) throw new ArgumentNullException(nameof(from));
    if (to is null) throw new ArgumentNullException(nameof(to));

    _rule.AddValidator(
      v => v is null || (CompareObjects(v, from) > 0 && CompareObjects(v, to) < 0),
      _messages.ExclusiveBetween());
    return this;
  }

  public IRuleBuilderOptions<T, TProperty> Must(Func<TProperty, bool> predicate)
  {
    ArgumentNullException.ThrowIfNull(predicate);

    _rule.AddValidator(
      v => v is null ? predicate(default!) : predicate((TProperty)v),
      _messages.Must());

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
      _messages.Must());

    return this;
  }

  public IRuleBuilderOptions<T, TProperty> SetValidator(IPropertyValidator<T, TProperty> validator)
  {
    ArgumentNullException.ThrowIfNull(validator);

    _rule.AddValidator(
      (instance, v) => validator.IsValid(instance, v is null ? default : (TProperty)v),
      validator.DefaultMessage);

    return this;
  }

  public IRuleBuilderOptions<T, TProperty> SetAsyncValidator(IAsyncPropertyValidator<T, TProperty> validator)
  {
    ArgumentNullException.ThrowIfNull(validator);

    _rule.AddAsyncValidator(
      (instance, v, ct) => validator.IsValidAsync(instance, v is null ? default : (TProperty)v, ct),
      validator.DefaultMessage);

    return this;
  }

  public IRuleBuilderOptions<T, TProperty> SetValidator<TChild>(IValidator<TChild> validator)
  {
    ArgumentNullException.ThrowIfNull(validator);
    if (!typeof(TChild).IsAssignableFrom(typeof(TProperty)))
    {
      throw new ArgumentException(
        $"Validator type {typeof(TChild).FullName} is not compatible with property type {typeof(TProperty).FullName}.",
        nameof(validator));
    }

    _rule.AddChildValidator(validator);
    return this;
  }

  public IRuleBuilderOptions<T, TProperty> NotEmpty()
  {
    _rule.AddValidator(NotEmptyImpl, _messages.NotEmpty());
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
    _rule.AddValidator(v => v is null || GetLength(v) <= maximumLength, _messages.MaximumLength(maximumLength));
    return this;
  }

  public IRuleBuilderOptions<T, TProperty> MinimumLength(int minimumLength)
  {
    _rule.AddValidator(v => v is null || GetLength(v) >= minimumLength, _messages.MinimumLength(minimumLength));
    return this;
  }

  public IRuleBuilderOptions<T, TProperty> Length(int minimumLength, int maximumLength)
  {
    _rule.AddValidator(v => v is null || (GetLength(v) >= minimumLength && GetLength(v) <= maximumLength), _messages.Length(minimumLength, maximumLength));
    return this;
  }

  public IRuleBuilderOptions<T, TProperty> Matches(string pattern)
  {
    return Matches(pattern, RegexOptions.None);
  }

  public IRuleBuilderOptions<T, TProperty> Matches(string pattern, RegexOptions options)
  {
    ArgumentNullException.ThrowIfNull(pattern);
    return Matches(new Regex(pattern, options | RegexOptions.Compiled | RegexOptions.CultureInvariant));
  }

  public IRuleBuilderOptions<T, TProperty> Matches(Regex regex)
  {
    ArgumentNullException.ThrowIfNull(regex);
    _rule.AddValidator(v => v is null || (v is string s && regex.IsMatch(s)), _messages.Matches());
    return this;
  }

  public IRuleBuilderOptions<T, TProperty> EmailAddress()
  {
    _rule.AddValidator(v => v is null || (v is string s && EmailRegex.IsMatch(s)), _messages.EmailAddress());
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

  public IRuleBuilderOptions<T, TProperty> DependentRules(Action action)
  {
    ArgumentNullException.ThrowIfNull(action);

    if (_dependentRulesRunner is null)
      throw new InvalidOperationException("DependentRules is not available in this context.");

    if (_rule is not IDependentRuleHost<T> host)
      throw new InvalidOperationException("DependentRules is only supported for rules that can host dependent rules.");

    _dependentRulesRunner(host, action);
    return this;
  }

  public IRuleBuilderOptions<T, TProperty> ChildRules(Action<InlineValidator<TProperty>> action)
  {
    ArgumentNullException.ThrowIfNull(action);

    var inline = new InlineValidator<TProperty>(_options);
    action(inline);
    _rule.AddChildValidator(inline);
    return this;
  }

  private static int GetLength(object value)
  {
    if (LengthAccessorCache.TryGetLength(value, out var length))
      return length;

    throw new NotSupportedException($"Length validators currently support string and common collection types. Value type: {value.GetType().FullName}");
  }

  private static int CompareObjects(object left, object right)
  {
    if (left is IComparable comparable)
      return comparable.CompareTo(right);

    throw new NotSupportedException($"Comparison validators require IComparable. Value type: {left.GetType().FullName}");
  }
}
