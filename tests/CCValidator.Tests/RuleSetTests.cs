namespace CCValidator.Tests;

public sealed class RuleSetTests
{
  private sealed record Person(string? Name);

  private sealed class PersonValidator_WithDefaultAndNamedRuleSet : AbstractValidator<Person>
  {
    public PersonValidator_WithDefaultAndNamedRuleSet()
    {
      // Default rule (not in a ruleset)
      RuleFor(x => x.Name)
        .NotEmpty()
        .WithMessage("default");

      // Named ruleset rule
      RuleSet("Names", () =>
      {
        RuleFor(x => x.Name)
          .MinimumLength(2)
          .WithMessage("names");
      });
    }
  }

  private sealed class PersonValidator_AsyncRuleSet : AbstractValidator<Person>
  {
    public PersonValidator_AsyncRuleSet()
    {
      RuleSet("Async", () =>
      {
        RuleFor(x => x.Name)
          .MustAsync(async (_, ct) =>
          {
            await Task.Yield();
            return false;
          })
          .WithMessage("async");
      });
    }
  }

  [Fact]
  public void Validate_runs_only_default_rules_by_default()
  {
    var validator = new PersonValidator_WithDefaultAndNamedRuleSet();

    var result = validator.Validate(new Person(""));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("default", result.Errors[0].ErrorMessage);
  }

  [Fact]
  public void Validate_with_ruleset_context_runs_only_that_ruleset_by_default()
  {
    var validator = new PersonValidator_WithDefaultAndNamedRuleSet();

    var context = new ValidationContext<Person>(new Person("a"), "Names");
    var result = validator.Validate(context);

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("names", result.Errors[0].ErrorMessage);
  }

  [Fact]
  public void Validate_with_ruleset_context_can_include_default_rules_explicitly()
  {
    var validator = new PersonValidator_WithDefaultAndNamedRuleSet();

    var context = new ValidationContext<Person>(new Person(""), "Names")
    {
      IncludeRulesNotInRuleSet = true,
    };

    var result = validator.Validate(context);

    Assert.False(result.IsValid);
    Assert.Equal(2, result.Errors.Count);
    Assert.Equal("default", result.Errors[0].ErrorMessage);
    Assert.Equal("names", result.Errors[1].ErrorMessage);
  }

  [Fact]
  public async Task ValidateAsync_honors_ruleset_selection()
  {
    var validator = new PersonValidator_AsyncRuleSet();

    var context = new ValidationContext<Person>(new Person("abc"), "Async");
    var result = await validator.ValidateAsync(context);

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("async", result.Errors[0].ErrorMessage);
  }
}
