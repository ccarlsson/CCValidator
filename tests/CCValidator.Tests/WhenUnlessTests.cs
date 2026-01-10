namespace CCValidator.Tests;

public sealed class WhenUnlessTests
{
  private sealed record Person(string? Name);

  private sealed class RuleLevelWhenValidator : AbstractValidator<Person>
  {
    public RuleLevelWhenValidator()
    {
      RuleFor(x => x.Name)
        .NotEmpty()
        .When(x => x.Name is not null);
    }
  }

  private sealed class RuleLevelUnlessValidator : AbstractValidator<Person>
  {
    public RuleLevelUnlessValidator()
    {
      RuleFor(x => x.Name)
        .NotEmpty()
        .Unless(x => x.Name is null);
    }
  }

  private sealed class ValidatorLevelWhenValidator : AbstractValidator<Person>
  {
    public ValidatorLevelWhenValidator()
    {
      When(x => x.Name is not null, () =>
      {
        RuleFor(x => x.Name)
          .NotEmpty();
      });
    }
  }

  private sealed class ValidatorLevelUnlessValidator : AbstractValidator<Person>
  {
    public ValidatorLevelUnlessValidator()
    {
      Unless(x => x.Name is null, () =>
      {
        RuleFor(x => x.Name)
          .NotEmpty();
      });
    }
  }

  private sealed class AsyncRuleLevelWhenValidator : AbstractValidator<Person>
  {
    public AsyncRuleLevelWhenValidator()
    {
      RuleFor(x => x.Name)
        .MustAsync(async (_, ct) =>
        {
          await Task.Yield();
          return false;
        })
        .WithMessage("async failed")
        .When(x => x.Name is not null);
    }
  }

  [Fact]
  public void Rule_level_When_skips_rule_when_condition_is_false()
  {
    var validator = new RuleLevelWhenValidator();

    var result = validator.Validate(new Person(null));

    Assert.True(result.IsValid);
    Assert.Empty(result.Errors);
  }

  [Fact]
  public void Rule_level_When_runs_rule_when_condition_is_true()
  {
    var validator = new RuleLevelWhenValidator();

    var result = validator.Validate(new Person(""));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("must not be empty", result.Errors[0].ErrorMessage);
  }

  [Fact]
  public void Rule_level_Unless_skips_rule_when_condition_is_true()
  {
    var validator = new RuleLevelUnlessValidator();

    var result = validator.Validate(new Person(null));

    Assert.True(result.IsValid);
    Assert.Empty(result.Errors);
  }

  [Fact]
  public void Validator_level_When_applies_to_rules_defined_inside_block()
  {
    var validator = new ValidatorLevelWhenValidator();

    Assert.True(validator.Validate(new Person(null)).IsValid);
    Assert.False(validator.Validate(new Person("")).IsValid);
  }

  [Fact]
  public void Validator_level_Unless_applies_to_rules_defined_inside_block()
  {
    var validator = new ValidatorLevelUnlessValidator();

    Assert.True(validator.Validate(new Person(null)).IsValid);
    Assert.False(validator.Validate(new Person("")).IsValid);
  }

  [Fact]
  public async Task Rule_level_When_skips_async_rule_in_ValidateAsync()
  {
    var validator = new AsyncRuleLevelWhenValidator();

    var result = await validator.ValidateAsync(new Person(null));

    Assert.True(result.IsValid);
    Assert.Empty(result.Errors);
  }
}
