namespace CCValidator.Tests;

public sealed class BasicValidationTests
{
  private sealed record Person(string? Name);

  private sealed class PersonValidator_NoRules : AbstractValidator<Person>
  {
  }

  private sealed class PersonValidator_NotNull : AbstractValidator<Person>
  {
    public PersonValidator_NotNull()
    {
      RuleFor(x => x.Name).NotNull();
    }
  }

  private sealed class PersonValidator_NotEmpty : AbstractValidator<Person>
  {
    public PersonValidator_NotEmpty()
    {
      RuleFor(x => x.Name).NotEmpty();
    }
  }

  private sealed class PersonValidator_MaxLenWithMessage : AbstractValidator<Person>
  {
    public PersonValidator_MaxLenWithMessage()
    {
      RuleFor(x => x.Name).MaximumLength(3).WithMessage("too long");
    }
  }

  [Fact]
  public void Validate_with_no_rules_is_valid()
  {
    var validator = new PersonValidator_NoRules();

    var result = validator.Validate(new Person("abc"));

    Assert.True(result.IsValid);
    Assert.Empty(result.Errors);
  }

  [Fact]
  public void NotNull_fails_when_property_is_null()
  {
    var validator = new PersonValidator_NotNull();

    var result = validator.Validate(new Person(null));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("Name", result.Errors[0].PropertyName);
    Assert.NotEmpty(result.Errors[0].ErrorMessage);
    Assert.Null(result.Errors[0].AttemptedValue);
  }

  [Fact]
  public void NotEmpty_fails_when_string_is_empty()
  {
    var validator = new PersonValidator_NotEmpty();

    var result = validator.Validate(new Person(""));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("Name", result.Errors[0].PropertyName);
  }

  [Fact]
  public void MaximumLength_fails_when_string_is_too_long_and_uses_custom_message()
  {
    var validator = new PersonValidator_MaxLenWithMessage();

    var result = validator.Validate(new Person("abcd"));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("too long", result.Errors[0].ErrorMessage);
  }
}
