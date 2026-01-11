namespace CCValidator;

/// <summary>
/// Contract for reusable, class-based validators that validate a property value.
/// </summary>
/// <remarks>
/// This exists to support "custom validator classes" in addition to predicate-based <c>Must</c> rules.
/// </remarks>
/// <typeparam name="T">Instance type.</typeparam>
/// <typeparam name="TProperty">Property type.</typeparam>
public interface IPropertyValidator<in T, in TProperty>
{
  /// <summary>
  /// Default message used when the validator fails and no message override is provided.
  /// </summary>
  string DefaultMessage { get; }

  /// <summary>
  /// Returns true when the value is valid.
  /// </summary>
  bool IsValid(T instance, TProperty? value);
}

/// <summary>
/// Contract for reusable, class-based async validators that validate a property value.
/// </summary>
/// <typeparam name="T">Instance type.</typeparam>
/// <typeparam name="TProperty">Property type.</typeparam>
public interface IAsyncPropertyValidator<in T, in TProperty>
{
  /// <summary>
  /// Default message used when the validator fails and no message override is provided.
  /// </summary>
  string DefaultMessage { get; }

  /// <summary>
  /// Returns true when the value is valid.
  /// </summary>
  Task<bool> IsValidAsync(T instance, TProperty? value, CancellationToken token);
}
