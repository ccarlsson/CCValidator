using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CCValidator;

internal interface IRule<T>
{
  string? RuleSet { get; }

  IEnumerable<ValidationFailure> Validate(T instance);

  Task<IEnumerable<ValidationFailure>> ValidateAsync(T instance, CancellationToken token);
}

internal interface IDependentRuleHost<T>
{
  void AddDependentRule(IRule<T> rule);
}
