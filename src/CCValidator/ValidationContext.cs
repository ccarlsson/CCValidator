namespace CCValidator;

/// <summary>
/// Context passed to validation that controls ruleset selection.
/// </summary>
/// <typeparam name="T">Instance type.</typeparam>
public sealed class ValidationContext<T>
{
  /// <summary>
  /// Create a context that includes rules not in a named ruleset.
  /// </summary>
  /// <param name="instance">Instance to validate.</param>
  public ValidationContext(T instance)
  {
    InstanceToValidate = instance;
    IncludedRuleSets = new HashSet<string>(StringComparer.Ordinal);
    IncludeRulesNotInRuleSet = true;
  }

  /// <summary>
  /// Create a context that includes only the given rulesets.
  /// </summary>
  /// <param name="instance">Instance to validate.</param>
  /// <param name="ruleSets">Rulesets to include.</param>
  public ValidationContext(T instance, params string[] ruleSets)
  {
    InstanceToValidate = instance;
    IncludedRuleSets = new HashSet<string>(ruleSets ?? Array.Empty<string>(), StringComparer.Ordinal);
    IncludeRulesNotInRuleSet = false;
  }

  /// <summary>
  /// The instance being validated.
  /// </summary>
  public T InstanceToValidate { get; }

  /// <summary>
  /// Named rulesets included by this context.
  /// </summary>
  public IReadOnlySet<string> IncludedRuleSets { get; }

  /// <summary>
  /// When true, rules that are not assigned to a named ruleset are included.
  /// Defaults to true for the default constructor and false for the ruleset constructor.
  /// </summary>
  public bool IncludeRulesNotInRuleSet { get; init; }
}
