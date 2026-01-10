namespace CCValidator;

public sealed class ValidationContext<T>
{
  public ValidationContext(T instance)
  {
    InstanceToValidate = instance;
    IncludedRuleSets = new HashSet<string>(StringComparer.Ordinal);
    IncludeRulesNotInRuleSet = true;
  }

  public ValidationContext(T instance, params string[] ruleSets)
  {
    InstanceToValidate = instance;
    IncludedRuleSets = new HashSet<string>(ruleSets ?? Array.Empty<string>(), StringComparer.Ordinal);
    IncludeRulesNotInRuleSet = false;
  }

  public T InstanceToValidate { get; }

  public IReadOnlySet<string> IncludedRuleSets { get; }

  /// <summary>
  /// When true, rules that are not assigned to a named ruleset are included.
  /// Defaults to true for the default constructor and false for the ruleset constructor.
  /// </summary>
  public bool IncludeRulesNotInRuleSet { get; init; }
}
