using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CCValidator;

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
  /// Adds a custom validator class.
  /// </summary>
  IRuleBuilderOptions<T, TProperty> SetValidator(IPropertyValidator<T, TProperty> validator);

  /// <summary>
  /// Adds a custom async validator class.
  /// </summary>
  IRuleBuilderOptions<T, TProperty> SetAsyncValidator(IAsyncPropertyValidator<T, TProperty> validator);

  /// <summary>
  /// Adds a nested validator for complex object graphs.
  /// </summary>
  /// <remarks>
  /// If the property value is <see langword="null"/>, the nested validator is not executed.
  /// </remarks>
  IRuleBuilderOptions<T, TProperty> SetValidator<TChild>(IValidator<TChild> validator);

  /// <summary>
  /// Defines child rules for the current property.
  /// </summary>
  /// <remarks>
  /// This is a convenience for creating an inline validator and attaching it via <c>SetValidator</c>.
  /// </remarks>
  IRuleBuilderOptions<T, TProperty> ChildRules(Action<InlineValidator<TProperty>> action);

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

  /// <summary>
  /// Defines dependent rules that are only executed if this rule produces no failures.
  /// </summary>
  /// <param name="action">Action that defines dependent rules.</param>
  IRuleBuilderOptions<T, TProperty> DependentRules(Action action);
}
