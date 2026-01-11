namespace CCValidator;

public abstract partial class AbstractValidator<T>
{
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
}
