namespace CCValidator.Tests;

public sealed class CascadeModeTests
{
  private sealed record Person(string? Name);

  private sealed class ContinueValidator : AbstractValidator<Person>
  {
    public ContinueValidator()
    {
      RuleFor(x => x.Name)
        .Cascade(CCValidator.CascadeMode.Continue)
        .NotNull()
        .NotEmpty();
    }
  }

  private sealed class StopValidator : AbstractValidator<Person>
  {
    public StopValidator()
    {
      RuleFor(x => x.Name)
        .Cascade(CCValidator.CascadeMode.Stop)
        .NotNull()
        .NotEmpty();
    }
  }

  private sealed class StopValidator_SecondWouldAlsoFail : AbstractValidator<Person>
  {
    public StopValidator_SecondWouldAlsoFail()
    {
      RuleFor(x => x.Name)
        .Cascade(CCValidator.CascadeMode.Stop)
        .NotEmpty()
        .MinimumLength(2);
    }
  }

  [Fact]
  public void Continue_returns_all_failures_in_chain()
  {
    var validator = new ContinueValidator();

    var result = validator.Validate(new Person(null));

    Assert.False(result.IsValid);
    Assert.Equal(2, result.Errors.Count);
    Assert.All(result.Errors, f => Assert.Equal("Name", f.PropertyName));
  }

  [Fact]
  public void Stop_returns_only_first_failure_in_chain()
  {
    var validator = new StopValidator();

    var result = validator.Validate(new Person(null));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("Name", result.Errors[0].PropertyName);
  }

  [Fact]
  public void Stop_prevents_later_validators_from_running()
  {
    var validator = new StopValidator_SecondWouldAlsoFail();

    var result = validator.Validate(new Person(""));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("Name", result.Errors[0].PropertyName);
  }
}
