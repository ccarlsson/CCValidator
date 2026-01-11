namespace CCValidator.Tests;

public sealed class SetValidatorTests
{
  private sealed record Child(string? Name);
  private sealed record Parent(Child? Child);

  private sealed class ChildValidator : AbstractValidator<Child>
  {
    public ChildValidator()
    {
      RuleFor(x => x.Name).NotEmpty();
    }
  }

  private sealed class ParentValidator : AbstractValidator<Parent>
  {
    public ParentValidator()
    {
      RuleFor(x => x.Child).SetValidator(new ChildValidator());
    }
  }

  private sealed class ParentValidator_StopCascade_ChildFirst : AbstractValidator<Parent>
  {
    public ParentValidator_StopCascade_ChildFirst()
    {
      RuleFor(x => x.Child)
        .Cascade(CascadeMode.Stop)
        .SetValidator(new ChildValidator())
        .Must(_ => false);
    }
  }

  private sealed class ParentValidator_WithMessageAfterSetValidator : AbstractValidator<Parent>
  {
    public ParentValidator_WithMessageAfterSetValidator()
    {
      RuleFor(x => x.Child)
        .SetValidator(new ChildValidator())
        .WithMessage("nope");
    }
  }

  [Fact]
  public void SetValidator_prefixes_child_property_name()
  {
    var validator = new ParentValidator();

    var result = validator.Validate(new Parent(new Child("")));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("Child.Name", result.Errors[0].PropertyName);
  }

  [Fact]
  public void SetValidator_is_skipped_when_property_is_null()
  {
    var validator = new ParentValidator();

    var result = validator.Validate(new Parent(null));

    Assert.True(result.IsValid);
    Assert.Empty(result.Errors);
  }

  [Fact]
  public void SetValidator_respects_rule_order_and_cascade_stop()
  {
    var validator = new ParentValidator_StopCascade_ChildFirst();

    var result = validator.Validate(new Parent(new Child("")));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("Child.Name", result.Errors[0].PropertyName);
  }

  [Fact]
  public void WithMessage_after_SetValidator_throws()
  {
    Assert.Throws<InvalidOperationException>(() => new ParentValidator_WithMessageAfterSetValidator());
  }

  [Fact]
  public async Task SetValidator_is_executed_in_async_validation()
  {
    var validator = new ParentValidator();

    var result = await validator.ValidateAsync(new Parent(new Child("")));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("Child.Name", result.Errors[0].PropertyName);
  }
}
