namespace CCValidator.Tests;

public sealed class MessageProviderTests
{
  private sealed record Person(string? Name);

  private sealed class CustomMessageProvider : IValidationMessageProvider
  {
    public string NotNull() => "CUSTOM_NOT_NULL";
    public string NotEmpty() => "CUSTOM_NOT_EMPTY";
    public string Must() => "CUSTOM_MUST";
    public string Equal() => "CUSTOM_EQUAL";
    public string NotEqual() => "CUSTOM_NOT_EQUAL";
    public string GreaterThan() => "CUSTOM_GT";
    public string GreaterThanOrEqualTo() => "CUSTOM_GTE";
    public string LessThan() => "CUSTOM_LT";
    public string LessThanOrEqualTo() => "CUSTOM_LTE";
    public string InclusiveBetween() => "CUSTOM_BETWEEN_INCL";
    public string ExclusiveBetween() => "CUSTOM_BETWEEN_EXCL";
    public string MaximumLength(int maximumLength) => $"CUSTOM_MAX_{maximumLength}";
    public string MinimumLength(int minimumLength) => $"CUSTOM_MIN_{minimumLength}";
    public string Length(int minimumLength, int maximumLength) => $"CUSTOM_LEN_{minimumLength}_{maximumLength}";
    public string Matches() => "CUSTOM_MATCHES";
    public string EmailAddress() => "CUSTOM_EMAIL";
  }

  private sealed class PersonValidator_CustomProvider_NotNull : AbstractValidator<Person>
  {
    public PersonValidator_CustomProvider_NotNull()
    {
      MessageProvider = new CustomMessageProvider();
      RuleFor(x => x.Name).NotNull();
    }
  }

  private sealed class PersonValidator_CustomProvider_WithMessageOverrides : AbstractValidator<Person>
  {
    public PersonValidator_CustomProvider_WithMessageOverrides()
    {
      MessageProvider = new CustomMessageProvider();
      RuleFor(x => x.Name).NotNull().WithMessage("OVERRIDE");
    }
  }

  [Fact]
  public void Default_messages_come_from_message_provider()
  {
    var validator = new PersonValidator_CustomProvider_NotNull();

    var result = validator.Validate(new Person(null));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("CUSTOM_NOT_NULL", result.Errors[0].ErrorMessage);
  }

  [Fact]
  public void WithMessage_overrides_message_provider_default()
  {
    var validator = new PersonValidator_CustomProvider_WithMessageOverrides();

    var result = validator.Validate(new Person(null));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("OVERRIDE", result.Errors[0].ErrorMessage);
  }
}
