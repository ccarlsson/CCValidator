namespace CCValidator.Tests;

public sealed class OptionsTests
{
  private sealed record Person(string? Name);

  private sealed class CustomMessageProvider : IValidationMessageProvider
  {
    public string NotNull() => "OPT_NOT_NULL";
    public string NotEmpty() => "OPT_NOT_EMPTY";
    public string Must() => "OPT_MUST";
    public string Equal() => "OPT_EQUAL";
    public string NotEqual() => "OPT_NOT_EQUAL";
    public string GreaterThan() => "OPT_GT";
    public string GreaterThanOrEqualTo() => "OPT_GTE";
    public string LessThan() => "OPT_LT";
    public string LessThanOrEqualTo() => "OPT_LTE";
    public string InclusiveBetween() => "OPT_BETWEEN_INCL";
    public string ExclusiveBetween() => "OPT_BETWEEN_EXCL";
    public string MaximumLength(int maximumLength) => $"OPT_MAX_{maximumLength}";
    public string MinimumLength(int minimumLength) => $"OPT_MIN_{minimumLength}";
    public string Length(int minimumLength, int maximumLength) => $"OPT_LEN_{minimumLength}_{maximumLength}";
    public string Matches() => "OPT_MATCHES";
    public string EmailAddress() => "OPT_EMAIL";
  }

  private sealed class OptionsDefaultStopValidator : AbstractValidator<Person>
  {
    public OptionsDefaultStopValidator()
      : base(new CCValidatorOptions { DefaultCascadeMode = CCValidator.CascadeMode.Stop })
    {
      RuleFor(x => x.Name)
        .NotNull()
        .NotEmpty();
    }
  }

  private sealed class OptionsDefaultStop_ValidatorOverridesToContinue : AbstractValidator<Person>
  {
    public OptionsDefaultStop_ValidatorOverridesToContinue()
      : base(new CCValidatorOptions { DefaultCascadeMode = CCValidator.CascadeMode.Stop })
    {
      CascadeMode = CCValidator.CascadeMode.Continue;

      RuleFor(x => x.Name)
        .NotNull()
        .NotEmpty();
    }
  }

  private sealed class OptionsMessageProviderValidator : AbstractValidator<Person>
  {
    public OptionsMessageProviderValidator()
      : base(new CCValidatorOptions { MessageProvider = new CustomMessageProvider() })
    {
      RuleFor(x => x.Name)
        .NotNull();
    }
  }

  private sealed class OptionsMessageProvider_ValidatorOverridesProvider : AbstractValidator<Person>
  {
    public OptionsMessageProvider_ValidatorOverridesProvider()
      : base(new CCValidatorOptions { MessageProvider = new CustomMessageProvider() })
    {
      MessageProvider = DefaultValidationMessageProvider.Instance;

      RuleFor(x => x.Name)
        .NotNull();
    }
  }

  [Fact]
  public void Options_default_cascade_mode_applies_to_rules()
  {
    var validator = new OptionsDefaultStopValidator();

    var result = validator.Validate(new Person(null));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("Name", result.Errors[0].PropertyName);
  }

  [Fact]
  public void Validator_property_override_wins_over_options_default_cascade_mode()
  {
    var validator = new OptionsDefaultStop_ValidatorOverridesToContinue();

    var result = validator.Validate(new Person(null));

    Assert.False(result.IsValid);
    Assert.Equal(2, result.Errors.Count);
  }

  [Fact]
  public void Options_message_provider_is_used_for_default_messages()
  {
    var validator = new OptionsMessageProviderValidator();

    var result = validator.Validate(new Person(null));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("OPT_NOT_NULL", result.Errors[0].ErrorMessage);
  }

  [Fact]
  public void Validator_property_override_wins_over_options_message_provider()
  {
    var validator = new OptionsMessageProvider_ValidatorOverridesProvider();

    var result = validator.Validate(new Person(null));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal(DefaultValidationMessageProvider.Instance.NotNull(), result.Errors[0].ErrorMessage);
  }
}
