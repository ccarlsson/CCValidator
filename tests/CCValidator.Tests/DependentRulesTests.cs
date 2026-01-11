namespace CCValidator.Tests;

public sealed class DependentRulesTests
{
  private sealed record Person(string? Name, string? Surname, string[]? Tags);

  private sealed class Validator_Sync : AbstractValidator<Person>
  {
    public Validator_Sync()
    {
      RuleFor(x => x.Name)
        .NotNull()
        .DependentRules(() =>
        {
          RuleFor(x => x.Surname).NotEmpty();
        });
    }
  }

  private sealed class Validator_Async : AbstractValidator<Person>
  {
    public Validator_Async()
    {
      RuleFor(x => x.Name)
        .MustAsync((name, _) => Task.FromResult(name == "ok"))
        .DependentRules(() =>
        {
          RuleFor(x => x.Surname).NotEmpty();
        });
    }
  }

  private sealed class Validator_ForEach : AbstractValidator<Person>
  {
    public Validator_ForEach()
    {
      RuleForEach(x => x.Tags!)
        .NotEmpty()
        .DependentRules(() =>
        {
          RuleFor(x => x.Name).NotEmpty();
        });
    }
  }

  [Fact]
  public void DependentRules_does_not_run_when_parent_rule_fails()
  {
    var validator = new Validator_Sync();

    var result = validator.Validate(new Person(Name: null, Surname: "", Tags: null));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("Name", result.Errors[0].PropertyName);
  }

  [Fact]
  public void DependentRules_runs_when_parent_rule_passes()
  {
    var validator = new Validator_Sync();

    var result = validator.Validate(new Person(Name: "x", Surname: "", Tags: null));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("Surname", result.Errors[0].PropertyName);
  }

  [Fact]
  public async Task DependentRules_works_in_async_validation()
  {
    var validator = new Validator_Async();

    var parentFails = await validator.ValidateAsync(new Person(Name: "no", Surname: "", Tags: null));
    Assert.False(parentFails.IsValid);
    Assert.Single(parentFails.Errors);
    Assert.Equal("Name", parentFails.Errors[0].PropertyName);

    var dependentFails = await validator.ValidateAsync(new Person(Name: "ok", Surname: "", Tags: null));
    Assert.False(dependentFails.IsValid);
    Assert.Single(dependentFails.Errors);
    Assert.Equal("Surname", dependentFails.Errors[0].PropertyName);
  }

  [Fact]
  public void DependentRules_after_RuleForEach_runs_only_when_all_elements_pass()
  {
    var validator = new Validator_ForEach();

    var elementsFail = validator.Validate(new Person(Name: "", Surname: null, Tags: [""]));
    Assert.False(elementsFail.IsValid);
    Assert.Single(elementsFail.Errors);
    Assert.Equal("Tags[0]", elementsFail.Errors[0].PropertyName);

    var dependentFails = validator.Validate(new Person(Name: "", Surname: null, Tags: ["ok"]));
    Assert.False(dependentFails.IsValid);
    Assert.Single(dependentFails.Errors);
    Assert.Equal("Name", dependentFails.Errors[0].PropertyName);
  }
}
