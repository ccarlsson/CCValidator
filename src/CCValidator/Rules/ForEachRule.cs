using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CCValidator;

internal sealed class ForEachRule<T, TElement>
  : IRule<T>, IRuleInternal<T, TElement>, IDependentRuleHost<T>
{
  private readonly Func<T, IEnumerable<TElement>> _getter;
  private Func<T, bool>? _condition;
  private readonly CCValidatorOptions _options;

  private readonly List<ElementValidator> _validators = [];
  private readonly List<AsyncElementValidator> _asyncValidators = [];
  private readonly List<ChildValidatorRunner> _childValidators = [];
  private readonly List<ValidatorSlot> _slots = [];
  private readonly List<IRule<T>> _dependentRules = [];

  public ForEachRule(string propertyName, Func<T, IEnumerable<TElement>> getter, CascadeMode cascadeMode, string? ruleSet, CCValidatorOptions options)
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
    _validators.Add(new ElementValidator(predicate, defaultMessage));
    _slots.Add(ValidatorSlot.ForSync(_validators.Count - 1));
  }

  public void AddAsyncValidator(Func<object?, CancellationToken, Task<bool>> predicate, string defaultMessage)
  {
    ArgumentNullException.ThrowIfNull(predicate);
    AddAsyncValidator((_, value, token) => predicate(value, token), defaultMessage);
  }

  public void AddAsyncValidator(Func<T, object?, CancellationToken, Task<bool>> predicate, string defaultMessage)
  {
    ArgumentNullException.ThrowIfNull(predicate);
    _asyncValidators.Add(new AsyncElementValidator(predicate, defaultMessage));
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

  public void AddDependentRule(IRule<T> rule)
  {
    ArgumentNullException.ThrowIfNull(rule);
    _dependentRules.Add(rule);
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
        conditionFailure = CreateInternalFailure(ex, attemptedValue: null, propertyName: PropertyName);
      }

      if (conditionFailure is not null)
      {
        yield return conditionFailure;
        yield break;
      }

      if (!shouldRun)
        yield break;
    }

    IEnumerable<TElement> values;
    ValidationFailure? getterFailure = null;
    try
    {
      values = _getter(instance);
    }
    catch (Exception ex)
    {
      if (ShouldThrow(ex)) throw;
      values = Array.Empty<TElement>();
      getterFailure = CreateInternalFailure(ex, attemptedValue: null, propertyName: PropertyName);
    }

    if (getterFailure is not null)
    {
      yield return getterFailure;
      yield break;
    }

    if (values is null)
      yield break;

    var anyFailures = false;

    var index = 0;

    IEnumerator<TElement> enumerator;
    ValidationFailure? enumeratorFailure = null;
    try
    {
      enumerator = values.GetEnumerator();
    }
    catch (Exception ex)
    {
      if (ShouldThrow(ex)) throw;
      enumerator = ((IEnumerable<TElement>)Array.Empty<TElement>()).GetEnumerator();
      enumeratorFailure = CreateInternalFailure(ex, attemptedValue: null, propertyName: PropertyName);
    }

    if (enumeratorFailure is not null)
    {
      yield return enumeratorFailure;
      yield break;
    }

    using (enumerator as IDisposable)
    {
      while (true)
      {
        bool moved;
        TElement element;
        ValidationFailure? enumerationFailure = null;
        try
        {
          moved = enumerator.MoveNext();
          if (!moved) break;
          element = enumerator.Current;
        }
        catch (Exception ex)
        {
          if (ShouldThrow(ex)) throw;
          moved = false;
          element = default!;
          enumerationFailure = CreateInternalFailure(ex, attemptedValue: null, propertyName: PropertyName);
        }

        if (enumerationFailure is not null)
        {
          yield return enumerationFailure;
          yield break;
        }

        var elementPropertyName = $"{PropertyName}[{index}]";
        object? boxed = element;

        var stopElement = false;
        foreach (var slot in _slots)
        {
          if (stopElement) break;

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
                  predicateFailure = CreateInternalFailure(ex, boxed, elementPropertyName);
                }

                if (predicateFailure is not null)
                {
                  anyFailures = true;
                  yield return predicateFailure;
                  if (CascadeMode == CascadeMode.Stop)
                    stopElement = true;
                  break;
                }

                if (ok) break;

                anyFailures = true;
                yield return new ValidationFailure(elementPropertyName, validator.EffectiveMessage)
                {
                  AttemptedValue = boxed,
                  ErrorCode = validator.ErrorCodeOverride,
                };

                if (CascadeMode == CascadeMode.Stop)
                  stopElement = true;

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
                  childExceptionFailure = CreateInternalFailure(ex, boxed, elementPropertyName);
                }

                if (childExceptionFailure is not null)
                {
                  anyFailures = true;
                  yield return childExceptionFailure;
                  if (CascadeMode == CascadeMode.Stop)
                    stopElement = true;
                  break;
                }

                var any = false;
                foreach (var failure in childResult!.Errors)
                {
                  any = true;
                  yield return PrefixFailure(elementPropertyName, failure);
                }

                if (any)
                  anyFailures = true;

                if (any && CascadeMode == CascadeMode.Stop)
                  stopElement = true;

                break;
              }

            case ValidatorSlotKind.Async:
              throw new InvalidOperationException("This validator contains async rules and must be executed with ValidateAsync.");

            default:
              throw new InvalidOperationException($"Unknown validator slot kind: {slot.Kind}.");
          }
        }

        index++;
      }
    }

    if (!anyFailures && _dependentRules.Count != 0)
    {
      foreach (var rule in _dependentRules)
      {
        foreach (var failure in rule.Validate(instance))
          yield return failure;
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
        return [CreateInternalFailure(ex, attemptedValue: null, propertyName: PropertyName)];
      }

      if (!shouldRun)
        return Array.Empty<ValidationFailure>();
    }

    IEnumerable<TElement> values;
    try
    {
      values = _getter(instance);
    }
    catch (Exception ex)
    {
      if (ShouldThrow(ex, token)) throw;
      return [CreateInternalFailure(ex, attemptedValue: null, propertyName: PropertyName)];
    }

    if (values is null)
      return Array.Empty<ValidationFailure>();

    var failures = new List<ValidationFailure>();

    var index = 0;
    IEnumerator<TElement> enumerator;
    try
    {
      enumerator = values.GetEnumerator();
    }
    catch (Exception ex)
    {
      if (ShouldThrow(ex, token)) throw;
      return [CreateInternalFailure(ex, attemptedValue: null, propertyName: PropertyName)];
    }

    using (enumerator as IDisposable)
    {
      while (true)
      {
        token.ThrowIfCancellationRequested();

        bool moved;
        TElement element;
        try
        {
          moved = enumerator.MoveNext();
          if (!moved) break;
          element = enumerator.Current;
        }
        catch (Exception ex)
        {
          if (ShouldThrow(ex, token)) throw;
          failures.Add(CreateInternalFailure(ex, attemptedValue: null, propertyName: PropertyName));
          return failures;
        }

        var elementPropertyName = $"{PropertyName}[{index}]";
        object? boxed = element;

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

                  failures.Add(CreateInternalFailure(ex, boxed, elementPropertyName));
                  if (CascadeMode == CascadeMode.Stop)
                    goto NextElement;

                  break;
                }

                if (ok) break;

                failures.Add(new ValidationFailure(elementPropertyName, validator.EffectiveMessage)
                {
                  AttemptedValue = boxed,
                  ErrorCode = validator.ErrorCodeOverride,
                });

                if (CascadeMode == CascadeMode.Stop)
                  goto NextElement;

                break;
              }

            case ValidatorSlotKind.Async:
              {
                var validator = _asyncValidators[slot.Index];

                bool ok;
                try
                {
                  ok = await validator.Predicate(instance, boxed, token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                  if (ShouldThrow(ex, token)) throw;

                  failures.Add(CreateInternalFailure(ex, boxed, elementPropertyName));
                  if (CascadeMode == CascadeMode.Stop)
                    goto NextElement;

                  break;
                }

                if (ok) break;

                failures.Add(new ValidationFailure(elementPropertyName, validator.EffectiveMessage)
                {
                  AttemptedValue = boxed,
                  ErrorCode = validator.ErrorCodeOverride,
                });

                if (CascadeMode == CascadeMode.Stop)
                  goto NextElement;

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

                  failures.Add(CreateInternalFailure(ex, boxed, elementPropertyName));
                  if (CascadeMode == CascadeMode.Stop)
                    goto NextElement;

                  break;
                }

                if (childResult.Errors.Count == 0)
                  break;

                foreach (var failure in childResult.Errors)
                  failures.Add(PrefixFailure(elementPropertyName, failure));

                if (CascadeMode == CascadeMode.Stop)
                  goto NextElement;

                break;
              }

            default:
              throw new InvalidOperationException($"Unknown validator slot kind: {slot.Kind}.");
          }
        }

      NextElement:
        index++;
      }
    }

    if (failures.Count == 0 && _dependentRules.Count != 0)
    {
      foreach (var rule in _dependentRules)
        failures.AddRange(await rule.ValidateAsync(instance, token).ConfigureAwait(false));
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

    if (ex is OperationCanceledException && token.IsCancellationRequested)
      return true;

    return false;
  }

  private ValidationFailure CreateInternalFailure(Exception ex, object? attemptedValue, string propertyName)
  {
    try
    {
      _options.Logger.InternalValidationError(
        new InternalValidationErrorContext(propertyName, attemptedValue, ex));
    }
    catch
    {
    }

    return new ValidationFailure(propertyName, _options.InternalErrorMessage)
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

  private readonly record struct ElementValidator(
    Func<T, object?, bool> Predicate,
    string DefaultMessage)
  {
    public string? MessageOverride { get; init; }
    public string? ErrorCodeOverride { get; init; }
    public string EffectiveMessage => MessageOverride ?? DefaultMessage;
  }

  private readonly record struct AsyncElementValidator(
    Func<T, object?, CancellationToken, Task<bool>> Predicate,
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

  private readonly record struct ValidatorSlot(ValidatorSlotKind Kind, int Index)
  {
    public static ValidatorSlot ForSync(int index) => new(ValidatorSlotKind.Sync, index);
    public static ValidatorSlot ForAsync(int index) => new(ValidatorSlotKind.Async, index);
    public static ValidatorSlot ForChild(int index) => new(ValidatorSlotKind.Child, index);
  }

  private readonly record struct ChildValidatorRunner(
    Func<object, ValidationResult> Validate,
    Func<object, CancellationToken, Task<ValidationResult>> ValidateAsync);
}
