namespace CCValidator.Tests;

public sealed class ChildRulesTests
{
  private sealed record Child(string? Name);
  private sealed record Parent(Child? Child, IEnumerable<Child>? Children);

  private sealed class ParentValidator_ChildRules : AbstractValidator<Parent>
  {
    public ParentValidator_ChildRules()
    {
      RuleFor(x => x.Child)
        .ChildRules(v =>
        {
          v.RuleFor(c => c!.Name).NotEmpty();
        });
    }
  }

  private sealed class ParentValidator_ChildRules_Async : AbstractValidator<Parent>
  {
    public ParentValidator_ChildRules_Async()
    {
      RuleFor(x => x.Child)
        .ChildRules(v =>
        {
          v.RuleFor(c => c!.Name).MustAsync((name, _) => Task.FromResult(name == "ok"));
        });
    }
  }

  private sealed class ParentValidator_RuleForEach_ChildRules : AbstractValidator<Parent>
  {
    public ParentValidator_RuleForEach_ChildRules()
    {
      RuleForEach(x => x.Children!)
        .ChildRules(v =>
        {
          v.RuleFor(c => c!.Name).NotEmpty();
        });
    }
  }

  [Fact]
  public void ChildRules_prefixes_child_property_name()
  {
    var validator = new ParentValidator_ChildRules();

    var result = validator.Validate(new Parent(new Child(""), Children: null));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("Child.Name", result.Errors[0].PropertyName);
  }

  [Fact]
  public void ChildRules_is_skipped_when_property_is_null()
  {
    var validator = new ParentValidator_ChildRules();

    var result = validator.Validate(new Parent(null, Children: null));

    Assert.True(result.IsValid);
    Assert.Empty(result.Errors);
  }

  [Fact]
  public void ChildRules_under_RuleForEach_prefixes_with_index()
  {
    var validator = new ParentValidator_RuleForEach_ChildRules();

    var result = validator.Validate(new Parent(Child: null, Children: new[] { new Child("") }));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("Children[0].Name", result.Errors[0].PropertyName);
  }

  [Fact]
  public async Task ChildRules_runs_in_async_validation()
  {
    var validator = new ParentValidator_ChildRules_Async();

    var result = await validator.ValidateAsync(new Parent(new Child("no"), Children: null));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("Child.Name", result.Errors[0].PropertyName);
  }
}
