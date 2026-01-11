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

/// <summary>
/// Entry point for building validation rules for a specific property.
/// </summary>
/// <typeparam name="T">Instance type.</typeparam>
/// <typeparam name="TProperty">Property type.</typeparam>
public interface IRuleBuilderInitial<T, TProperty>
{
  /// <summary>
  /// Overrides cascade mode for this rule.
  /// </summary>
  IRuleBuilderInitial<T, TProperty> Cascade(CascadeMode cascadeMode);

  /// <summary>
  /// Applies a condition to this rule.
  /// </summary>
  IRuleBuilderOptions<T, TProperty> When(Func<T, bool> predicate);

  /// <summary>
  /// Applies the inverse of <paramref name="predicate"/> to this rule.
  /// </summary>
  IRuleBuilderOptions<T, TProperty> Unless(Func<T, bool> predicate);

  /// <summary>
  /// Adds a custom predicate validator.
  /// </summary>
  IRuleBuilderOptions<T, TProperty> Must(Func<TProperty, bool> predicate);

  /// <summary>
  /// Adds a custom async predicate validator.
  /// </summary>
  IRuleBuilderOptions<T, TProperty> MustAsync(Func<TProperty, CancellationToken, Task<bool>> predicate);

  /// <summary>
  /// Adds a nested validator for complex object graphs.
  /// </summary>
  /// <remarks>
  /// If the property value is <see langword="null"/>, the nested validator is not executed.
  /// </remarks>
  IRuleBuilderOptions<T, TProperty> SetValidator<TChild>(IValidator<TChild> validator);
  IRuleBuilderOptions<T, TProperty> Equal(TProperty comparisonValue);
  IRuleBuilderOptions<T, TProperty> Equal(Expression<Func<T, TProperty>> comparisonExpression);
  IRuleBuilderOptions<T, TProperty> NotEqual(TProperty comparisonValue);
  IRuleBuilderOptions<T, TProperty> NotEqual(Expression<Func<T, TProperty>> comparisonExpression);
  IRuleBuilderOptions<T, TProperty> GreaterThan(TProperty value);
  IRuleBuilderOptions<T, TProperty> GreaterThan(Expression<Func<T, TProperty>> comparisonExpression);
  IRuleBuilderOptions<T, TProperty> GreaterThanOrEqualTo(TProperty value);
  IRuleBuilderOptions<T, TProperty> GreaterThanOrEqualTo(Expression<Func<T, TProperty>> comparisonExpression);
  IRuleBuilderOptions<T, TProperty> LessThan(TProperty value);
  IRuleBuilderOptions<T, TProperty> LessThan(Expression<Func<T, TProperty>> comparisonExpression);
  IRuleBuilderOptions<T, TProperty> LessThanOrEqualTo(TProperty value);
  IRuleBuilderOptions<T, TProperty> LessThanOrEqualTo(Expression<Func<T, TProperty>> comparisonExpression);
  IRuleBuilderOptions<T, TProperty> InclusiveBetween(TProperty from, TProperty to);
  IRuleBuilderOptions<T, TProperty> ExclusiveBetween(TProperty from, TProperty to);
  IRuleBuilderOptions<T, TProperty> NotNull();
  IRuleBuilderOptions<T, TProperty> NotEmpty();
  IRuleBuilderOptions<T, TProperty> MaximumLength(int maximumLength);
  IRuleBuilderOptions<T, TProperty> MinimumLength(int minimumLength);
  IRuleBuilderOptions<T, TProperty> Length(int minimumLength, int maximumLength);
  IRuleBuilderOptions<T, TProperty> Matches(string pattern);
  IRuleBuilderOptions<T, TProperty> Matches(string pattern, RegexOptions options);
  IRuleBuilderOptions<T, TProperty> Matches(Regex regex);
  IRuleBuilderOptions<T, TProperty> EmailAddress();
}

/// <summary>
/// Rule builder that also allows setting message and error code.
/// </summary>
public interface IRuleBuilderOptions<T, TProperty> : IRuleBuilderInitial<T, TProperty>
{
  /// <summary>
  /// Overrides the default message for the most recently added validator.
  /// </summary>
  IRuleBuilderOptions<T, TProperty> WithMessage(string message);

  /// <summary>
  /// Overrides the error code for the most recently added validator.
  /// </summary>
  IRuleBuilderOptions<T, TProperty> WithErrorCode(string errorCode);
}

internal sealed class RuleBuilder<T, TProperty> : IRuleBuilderOptions<T, TProperty>
{
  private readonly PropertyRule<T, TProperty> _rule;
  private readonly IValidationMessageProvider _messages;

  private static readonly Regex EmailRegex = new(
    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
    RegexOptions.Compiled | RegexOptions.CultureInvariant);

  public RuleBuilder(PropertyRule<T, TProperty> rule, IValidationMessageProvider messages)
  {
    _rule = rule;
    _messages = messages;
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

internal static class LengthAccessorCache
{
  private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, Func<object, int>?> Cache = new();

  public static bool TryGetLength(object value, out int length)
  {
    var type = value.GetType();

    var accessor = Cache.GetOrAdd(type, CreateAccessor);
    if (accessor is null)
    {
      length = 0;
      return false;
    }

    length = accessor(value);
    return true;
  }

  private static Func<object, int>? CreateAccessor(Type type)
  {
    if (type == typeof(string))
      return static o => ((string)o).Length;

    if (type.IsArray)
      return static o => ((Array)o).Length;

    if (typeof(ICollection).IsAssignableFrom(type))
      return static o => ((ICollection)o).Count;

    // Support ICollection<T>/IReadOnlyCollection<T> (eg HashSet<T>)
    var genericInterfaces = type.GetInterfaces()
      .Where(i => i.IsGenericType)
      .ToArray();

    var collectionIface = genericInterfaces.FirstOrDefault(i => i.GetGenericTypeDefinition() == typeof(ICollection<>))
      ?? genericInterfaces.FirstOrDefault(i => i.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>));

    if (collectionIface is null)
      return null;

    var countProp = collectionIface.GetProperty("Count");
    if (countProp is null)
      return null;

    var objParam = Expression.Parameter(typeof(object), "o");
    var cast = Expression.Convert(objParam, collectionIface);
    var count = Expression.Property(cast, countProp);
    var lambda = Expression.Lambda<Func<object, int>>(Expression.Convert(count, typeof(int)), objParam);
    return lambda.Compile();
  }
}

internal sealed class PropertyRule<T, TProperty>
  : IRule<T>
{
  private readonly Func<T, TProperty> _getter;
  private Func<T, bool>? _condition;
  private readonly CCValidatorOptions _options;

  private readonly List<PropertyValidator> _validators = [];
  private readonly List<AsyncPropertyValidator> _asyncValidators = [];
  private readonly List<ChildValidatorRunner> _childValidators = [];
  private readonly List<ValidatorSlot> _slots = [];

  public PropertyRule(string propertyName, Func<T, TProperty> getter, CascadeMode cascadeMode, string? ruleSet, CCValidatorOptions options)
  {
    PropertyName = propertyName;
    _getter = getter;
    CascadeMode = cascadeMode;
    RuleSet = ruleSet;
    _options = options;
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
    ArgumentNullException.ThrowIfNull(predicate);
    AddValidator((_, value) => predicate(value), defaultMessage);
  }

  public void AddValidator(Func<T, object?, bool> predicate, string defaultMessage)
  {
    ArgumentNullException.ThrowIfNull(predicate);
    _validators.Add(new PropertyValidator(predicate, defaultMessage));
    _slots.Add(ValidatorSlot.ForSync(_validators.Count - 1));
  }

  public void AddAsyncValidator(Func<object?, CancellationToken, Task<bool>> predicate, string defaultMessage)
  {
    _asyncValidators.Add(new AsyncPropertyValidator(predicate, defaultMessage));
    _slots.Add(ValidatorSlot.ForAsync(_asyncValidators.Count - 1));
  }

  public void AddChildValidator<TChild>(IValidator<TChild> validator)
  {
    ArgumentNullException.ThrowIfNull(validator);
    _childValidators.Add(new ChildValidatorRunner(
      Validate: value => validator.Validate((TChild)value),
      ValidateAsync: (value, token) => validator.ValidateAsync((TChild)value, token)));
    _slots.Add(ValidatorSlot.ForChild(_childValidators.Count - 1));
  }

  public void SetMessageOverrideForLastValidator(string message)
  {
    if (_slots.Count == 0)
      throw new InvalidOperationException("WithMessage must be used after a validator.");

    var slot = _slots[^1];

    if (slot.Kind == ValidatorSlotKind.Child)
      throw new InvalidOperationException("WithMessage is not supported after SetValidator.");

    if (slot.Kind == ValidatorSlotKind.Async)
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

    if (slot.Kind == ValidatorSlotKind.Child)
      throw new InvalidOperationException("WithErrorCode is not supported after SetValidator.");

    if (slot.Kind == ValidatorSlotKind.Async)
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

    if (_condition is not null)
    {
      bool shouldRun;
      ValidationFailure? conditionFailure = null;
      try
      {
        shouldRun = _condition(instance);
      }
      catch (Exception ex)
      {
        if (ShouldThrow(ex)) throw;
        shouldRun = false;
        conditionFailure = CreateInternalFailure(ex, attemptedValue: null);
      }

      if (conditionFailure is not null)
      {
        yield return conditionFailure;
        yield break;
      }

      if (!shouldRun)
        yield break;
    }

    TProperty value;
    ValidationFailure? getterFailure = null;
    try
    {
      value = _getter(instance);
    }
    catch (Exception ex)
    {
      if (ShouldThrow(ex)) throw;
      value = default!;
      getterFailure = CreateInternalFailure(ex, attemptedValue: null);
    }

    if (getterFailure is not null)
    {
      yield return getterFailure;
      yield break;
    }

    object? boxed = value;

    foreach (var slot in _slots)
    {
      switch (slot.Kind)
      {
        case ValidatorSlotKind.Sync:
        {
          var validator = _validators[slot.Index];

          bool ok;
          ValidationFailure? predicateFailure = null;
          try
          {
            ok = validator.Predicate(instance, boxed);
          }
          catch (Exception ex)
          {
            if (ShouldThrow(ex)) throw;
            ok = false;
            predicateFailure = CreateInternalFailure(ex, boxed);
          }

          if (predicateFailure is not null)
          {
            yield return predicateFailure;
            if (CascadeMode == CascadeMode.Stop)
              yield break;
            break;
          }

          if (ok) break;

          yield return new ValidationFailure(PropertyName, validator.EffectiveMessage)
          {
            AttemptedValue = boxed,
            ErrorCode = validator.ErrorCodeOverride,
          };

          if (CascadeMode == CascadeMode.Stop)
            yield break;

          break;
        }

        case ValidatorSlotKind.Child:
        {
          if (boxed is null)
            break;

          var childValidator = _childValidators[slot.Index];

          ValidationResult? childResult = null;
          ValidationFailure? childExceptionFailure = null;
          try
          {
            childResult = childValidator.Validate(boxed);
          }
          catch (Exception ex)
          {
            if (ShouldThrow(ex)) throw;
            childExceptionFailure = CreateInternalFailure(ex, boxed);
          }

          if (childExceptionFailure is not null)
          {
            yield return childExceptionFailure;
            if (CascadeMode == CascadeMode.Stop)
              yield break;
            break;
          }

          var any = false;
          foreach (var failure in childResult!.Errors)
          {
            any = true;
            yield return PrefixFailure(PropertyName, failure);
          }

          if (any && CascadeMode == CascadeMode.Stop)
            yield break;

          break;
        }

        case ValidatorSlotKind.Async:
          throw new InvalidOperationException("This validator contains async rules and must be executed with ValidateAsync.");

        default:
          throw new InvalidOperationException($"Unknown validator slot kind: {slot.Kind}.");
      }
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
        return [CreateInternalFailure(ex, attemptedValue: null)];
      }

      if (!shouldRun)
        return Array.Empty<ValidationFailure>();
    }

    var failures = new List<ValidationFailure>();

    TProperty value;
    try
    {
      value = _getter(instance);
    }
    catch (Exception ex)
    {
      if (ShouldThrow(ex, token)) throw;
      return [CreateInternalFailure(ex, attemptedValue: null)];
    }

    object? boxed = value;

    foreach (var slot in _slots)
    {
      token.ThrowIfCancellationRequested();

      switch (slot.Kind)
      {
        case ValidatorSlotKind.Sync:
        {
          var validator = _validators[slot.Index];

          bool ok;
          try
          {
            ok = validator.Predicate(instance, boxed);
          }
          catch (Exception ex)
          {
            if (ShouldThrow(ex, token)) throw;

            failures.Add(CreateInternalFailure(ex, boxed));
            if (CascadeMode == CascadeMode.Stop)
              return failures;

            break;
          }

          if (ok) break;

          failures.Add(new ValidationFailure(PropertyName, validator.EffectiveMessage)
          {
            AttemptedValue = boxed,
            ErrorCode = validator.ErrorCodeOverride,
          });

          if (CascadeMode == CascadeMode.Stop)
            return failures;

          break;
        }

        case ValidatorSlotKind.Async:
        {
          var validator = _asyncValidators[slot.Index];

          bool ok;
          try
          {
            ok = await validator.Predicate(boxed, token).ConfigureAwait(false);
          }
          catch (Exception ex)
          {
            if (ShouldThrow(ex, token)) throw;

            failures.Add(CreateInternalFailure(ex, boxed));
            if (CascadeMode == CascadeMode.Stop)
              return failures;

            break;
          }

          if (ok) break;

          failures.Add(new ValidationFailure(PropertyName, validator.EffectiveMessage)
          {
            AttemptedValue = boxed,
            ErrorCode = validator.ErrorCodeOverride,
          });

          if (CascadeMode == CascadeMode.Stop)
            return failures;

          break;
        }

        case ValidatorSlotKind.Child:
        {
          if (boxed is null)
            break;

          var childValidator = _childValidators[slot.Index];

          ValidationResult childResult;
          try
          {
            childResult = await childValidator.ValidateAsync(boxed, token).ConfigureAwait(false);
          }
          catch (Exception ex)
          {
            if (ShouldThrow(ex, token)) throw;

            failures.Add(CreateInternalFailure(ex, boxed));
            if (CascadeMode == CascadeMode.Stop)
              return failures;

            break;
          }

          if (childResult.Errors.Count == 0)
            break;

          foreach (var failure in childResult.Errors)
            failures.Add(PrefixFailure(PropertyName, failure));

          if (CascadeMode == CascadeMode.Stop)
            return failures;

          break;
        }

        default:
          throw new InvalidOperationException($"Unknown validator slot kind: {slot.Kind}.");
      }
    }

    return failures;
  }

  private bool ShouldThrow(Exception ex)
  {
    return _options.ExceptionBehavior == ValidationExceptionBehavior.Throw;
  }

  private bool ShouldThrow(Exception ex, CancellationToken token)
  {
    if (_options.ExceptionBehavior == ValidationExceptionBehavior.Throw)
      return true;

    // Never swallow cancellation when the provided token is cancelled.
    if (ex is OperationCanceledException && token.IsCancellationRequested)
      return true;

    return false;
  }

  private ValidationFailure CreateInternalFailure(Exception ex, object? attemptedValue)
  {
    try
    {
      _options.Logger.InternalValidationError(
        new InternalValidationErrorContext(PropertyName, attemptedValue, ex));
    }
    catch
    {
      // Best-effort logging must not affect validation.
    }

    return new ValidationFailure(PropertyName, _options.InternalErrorMessage)
    {
      AttemptedValue = attemptedValue,
      ErrorCode = _options.InternalErrorCode,
      CustomState = ex,
    };
  }

  private static ValidationFailure PrefixFailure(string parentPropertyName, ValidationFailure childFailure)
  {
    var propertyName = CombinePropertyName(parentPropertyName, childFailure.PropertyName);
    return new ValidationFailure(propertyName, childFailure.ErrorMessage)
    {
      AttemptedValue = childFailure.AttemptedValue,
      ErrorCode = childFailure.ErrorCode,
      Severity = childFailure.Severity,
      CustomState = childFailure.CustomState,
    };
  }

  private static string CombinePropertyName(string parentPropertyName, string childPropertyName)
  {
    if (string.IsNullOrEmpty(parentPropertyName))
      return childPropertyName;

    if (string.IsNullOrEmpty(childPropertyName))
      return parentPropertyName;

    if (childPropertyName.StartsWith("[", StringComparison.Ordinal))
      return parentPropertyName + childPropertyName;

    return parentPropertyName + "." + childPropertyName;
  }

  private readonly record struct PropertyValidator(
      Func<T, object?, bool> Predicate,
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

  private enum ValidatorSlotKind
  {
    Sync = 0,
    Async = 1,
    Child = 2,
  }

  private readonly record struct ChildValidatorRunner(
    Func<object, ValidationResult> Validate,
    Func<object, CancellationToken, Task<ValidationResult>> ValidateAsync);

  private readonly record struct ValidatorSlot(ValidatorSlotKind Kind, int Index)
  {
    public static ValidatorSlot ForSync(int index) => new(ValidatorSlotKind.Sync, index);
    public static ValidatorSlot ForAsync(int index) => new(ValidatorSlotKind.Async, index);
    public static ValidatorSlot ForChild(int index) => new(ValidatorSlotKind.Child, index);
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
