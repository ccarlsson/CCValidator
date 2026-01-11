namespace CCValidator;

/// <summary>
/// Controls whether validation continues after a failure within a single rule.
/// </summary>
public enum CascadeMode
{
  /// <summary>
  /// Continue evaluating validators even after a failure.
  /// </summary>
  Continue = 0,

  /// <summary>
  /// Stop evaluating further validators for a rule after the first failure.
  /// </summary>
  Stop = 1,
}
