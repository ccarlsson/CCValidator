namespace CCValidator.Tests;

public sealed class ExceptionPolicyTests
{
  private sealed record Model(object? Value);

  private sealed class DefaultPolicyValidator : AbstractValidator<Model>
  {
    public DefaultPolicyValidator()
    {
      RuleFor(x => x.Value).MaximumLength(1);
    }
  }

  private sealed class ThrowPolicyValidator : AbstractValidator<Model>
  {
    public ThrowPolicyValidator()
      : base(new CCValidatorOptions { ExceptionBehavior = ValidationExceptionBehavior.Throw })
    {
      RuleFor(x => x.Value).MaximumLength(1);
    }
  }

  private sealed record Person(string? Name);

  private sealed class AsyncExceptionValidator : AbstractValidator<Person>
  {
    public AsyncExceptionValidator()
    {
      RuleFor(x => x.Name)
        .MustAsync((_, _) => throw new InvalidOperationException("boom"));
    }
  }

  [Fact]
  public void Default_policy_converts_internal_exceptions_to_failure()
  {
    var validator = new DefaultPolicyValidator();

    var result = validator.Validate(new Model(new object()));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("CCV_INTERNAL", result.Errors[0].ErrorCode);
    Assert.Equal("An internal validation error occurred", result.Errors[0].ErrorMessage);
  }

  [Fact]
  public void Throw_policy_propagates_internal_exceptions()
  {
    var validator = new ThrowPolicyValidator();

    Assert.Throws<NotSupportedException>(() => validator.Validate(new Model(new object())));
  }

  [Fact]
  public async Task Default_policy_converts_async_exceptions_to_failure()
  {
    var validator = new AsyncExceptionValidator();

    var result = await validator.ValidateAsync(new Person("abc"));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("CCV_INTERNAL", result.Errors[0].ErrorCode);
  }
}
