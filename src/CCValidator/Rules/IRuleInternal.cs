using System;
using System.Threading;
using System.Threading.Tasks;

namespace CCValidator;

internal interface IRuleInternal<T, TProperty>
{
  CascadeMode CascadeMode { get; set; }

  void ApplyCondition(Func<T, bool> predicate);

  void AddValidator(Func<object?, bool> predicate, string defaultMessage);

  void AddValidator(Func<T, object?, bool> predicate, string defaultMessage);

  void AddAsyncValidator(Func<object?, CancellationToken, Task<bool>> predicate, string defaultMessage);

  void AddAsyncValidator(Func<T, object?, CancellationToken, Task<bool>> predicate, string defaultMessage);

  void AddChildValidator<TChild>(IValidator<TChild> validator);

  void SetMessageOverrideForLastValidator(string message);

  void SetErrorCodeOverrideForLastValidator(string errorCode);
}
