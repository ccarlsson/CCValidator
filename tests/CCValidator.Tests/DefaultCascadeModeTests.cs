namespace CCValidator.Tests;

public sealed class DefaultCascadeModeTests
{
  private sealed record Person(string? Name);

  private sealed class DefaultStopValidator : AbstractValidator<Person>
  {
    public DefaultStopValidator()
    {
      CascadeMode = CCValidator.CascadeMode.Stop;

      RuleFor(x => x.Name)
        .NotNull()
        .NotEmpty();
    }
  }

  private sealed class DefaultContinueValidator : AbstractValidator<Person>
  {
    public DefaultContinueValidator()
    {
      CascadeMode = CCValidator.CascadeMode.Continue;

      RuleFor(x => x.Name)
        .NotNull()
        .NotEmpty();
    }
  }

  private sealed class DefaultStop_RuleOverridesToContinue : AbstractValidator<Person>
  {
    public DefaultStop_RuleOverridesToContinue()
    {
      CascadeMode = CCValidator.CascadeMode.Stop;

      RuleFor(x => x.Name)
        .Cascade(CCValidator.CascadeMode.Continue)
        .NotNull()
        .NotEmpty();
    }
  }

  [Fact]
  public void Validator_default_Stop_is_applied_to_rules_without_explicit_Cascade()
  {
    var validator = new DefaultStopValidator();

    var result = validator.Validate(new Person(null));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("Name", result.Errors[0].PropertyName);
    Assert.Equal("must not be null", result.Errors[0].ErrorMessage);
    Assert.Null(result.Errors[0].AttemptedValue);
  }

  [Fact]
  public void Validator_default_Continue_is_applied_to_rules_without_explicit_Cascade()
  {
    var validator = new DefaultContinueValidator();

    var result = validator.Validate(new Person(null));

    Assert.False(result.IsValid);
    Assert.Equal(2, result.Errors.Count);
    Assert.Equal("Name", result.Errors[0].PropertyName);
    Assert.Equal("must not be null", result.Errors[0].ErrorMessage);
    Assert.Equal("Name", result.Errors[1].PropertyName);
    Assert.Equal("must not be empty", result.Errors[1].ErrorMessage);
  }

  [Fact]
  public void Rule_level_Cascade_overrides_validator_default()
  {
    var validator = new DefaultStop_RuleOverridesToContinue();

    var result = validator.Validate(new Person(null));

    Assert.False(result.IsValid);
    Assert.Equal(2, result.Errors.Count);
    Assert.Equal("Name", result.Errors[0].PropertyName);
    Assert.Equal("must not be null", result.Errors[0].ErrorMessage);
    Assert.Equal("Name", result.Errors[1].PropertyName);
    Assert.Equal("must not be empty", result.Errors[1].ErrorMessage);
  }
}
