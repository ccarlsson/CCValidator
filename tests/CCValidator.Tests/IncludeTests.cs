namespace CCValidator.Tests;

public sealed class IncludeTests
{
  private sealed record Person(string? Name, bool Enabled = true);

  private sealed class NameNotEmptyValidator : AbstractValidator<Person>
  {
    public NameNotEmptyValidator()
    {
      RuleFor(x => x.Name).NotEmpty();
    }
  }

  private sealed class IncludingValidator : AbstractValidator<Person>
  {
    public IncludingValidator()
    {
      Include(new NameNotEmptyValidator());
    }
  }

  private sealed class IncludingValidator_WithWhen : AbstractValidator<Person>
  {
    public IncludingValidator_WithWhen()
    {
      When(x => x.Enabled, () => Include(new NameNotEmptyValidator()));
    }
  }

  private sealed class IncludingValidator_InRuleSet : AbstractValidator<Person>
  {
    public IncludingValidator_InRuleSet()
    {
      RuleSet("A", () => Include(new NameNotEmptyValidator()));
    }
  }

  private sealed class AsyncNameValidator : AbstractValidator<Person>
  {
    public AsyncNameValidator()
    {
      RuleFor(x => x.Name).MustAsync((_, _) => Task.FromResult(false));
    }
  }

  private sealed class IncludingAsyncValidator : AbstractValidator<Person>
  {
    public IncludingAsyncValidator()
    {
      Include(new AsyncNameValidator());
    }
  }

  [Fact]
  public void Include_runs_included_rules()
  {
    var validator = new IncludingValidator();

    var result = validator.Validate(new Person(""));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("Name", result.Errors[0].PropertyName);
  }

  [Fact]
  public void Include_respects_When_blocks()
  {
    var validator = new IncludingValidator_WithWhen();

    var result = validator.Validate(new Person("", Enabled: false));

    Assert.True(result.IsValid);
    Assert.Empty(result.Errors);
  }

  [Fact]
  public void Include_respects_rulesets()
  {
    var validator = new IncludingValidator_InRuleSet();

    var defaultResult = validator.Validate(new Person(""));
    Assert.True(defaultResult.IsValid);

    var rulesetResult = validator.Validate(new ValidationContext<Person>(new Person(""), "A"));
    Assert.False(rulesetResult.IsValid);
    Assert.Single(rulesetResult.Errors);
    Assert.Equal("Name", rulesetResult.Errors[0].PropertyName);
  }

  [Fact]
  public async Task Include_is_executed_in_async_validation()
  {
    var validator = new IncludingAsyncValidator();

    var result = await validator.ValidateAsync(new Person("ok"));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("Name", result.Errors[0].PropertyName);
  }
}
