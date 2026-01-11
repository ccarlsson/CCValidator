namespace CCValidator.Tests;

public sealed class RuleForEachTests
{
  private sealed record Child(string? Name);
  private sealed record Parent(IEnumerable<string>? Names, IEnumerable<Child>? Children);

  private sealed class NamesValidator : AbstractValidator<Parent>
  {
    public NamesValidator()
    {
      RuleForEach(x => x.Names!).NotEmpty();
    }
  }

  private sealed class ChildValidator : AbstractValidator<Child>
  {
    public ChildValidator()
    {
      RuleFor(x => x.Name).NotEmpty();
    }
  }

  private sealed class ChildrenValidator : AbstractValidator<Parent>
  {
    public ChildrenValidator()
    {
      RuleForEach(x => x.Children!).SetValidator(new ChildValidator());
    }
  }

  private sealed class NamesValidator_StopCascade : AbstractValidator<Parent>
  {
    public NamesValidator_StopCascade()
    {
      RuleForEach(x => x.Names!)
        .Cascade(CascadeMode.Stop)
        .NotEmpty()
        .Must(_ => false);
    }
  }

  private sealed class NamesValidator_Async : AbstractValidator<Parent>
  {
    public NamesValidator_Async()
    {
      RuleForEach(x => x.Names!).MustAsync((name, _) => Task.FromResult(name == "ok"));
    }
  }

  [Fact]
  public void RuleForEach_prefixes_property_with_index()
  {
    var validator = new NamesValidator();

    var result = validator.Validate(new Parent(new[] { "", "ok" }, Children: null));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("Names[0]", result.Errors[0].PropertyName);
  }

  [Fact]
  public void RuleForEach_is_skipped_when_collection_is_null()
  {
    var validator = new NamesValidator();

    var result = validator.Validate(new Parent(Names: null, Children: null));

    Assert.True(result.IsValid);
    Assert.Empty(result.Errors);
  }

  [Fact]
  public void RuleForEach_SetValidator_prefixes_child_failures_with_index()
  {
    var validator = new ChildrenValidator();

    var result = validator.Validate(new Parent(Names: null, Children: new[] { new Child("") }));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("Children[0].Name", result.Errors[0].PropertyName);
  }

  [Fact]
  public void RuleForEach_respects_cascade_stop_per_element()
  {
    var validator = new NamesValidator_StopCascade();

    var result = validator.Validate(new Parent(new[] { "", "" }, Children: null));

    Assert.False(result.IsValid);
    Assert.Equal(2, result.Errors.Count);
    Assert.Contains(result.Errors, e => e.PropertyName == "Names[0]");
    Assert.Contains(result.Errors, e => e.PropertyName == "Names[1]");
  }

  [Fact]
  public async Task RuleForEach_runs_in_async_validation()
  {
    var validator = new NamesValidator_Async();

    var result = await validator.ValidateAsync(new Parent(new[] { "no" }, Children: null));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("Names[0]", result.Errors[0].PropertyName);
  }
}
