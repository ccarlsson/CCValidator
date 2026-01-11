namespace CCValidator.Tests;

using System.Resources;

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

  [Fact]
  public void ResourceManager_provider_reads_messages_and_formats_templates()
  {
    var resourceManager = new ResourceManager("CCValidator.Tests.TestMessages", typeof(MessageProviderTests).Assembly);

    var notNullValidator = new InlineValidator<Person>(v =>
    {
      v.MessageProvider = new ResourceManagerValidationMessageProvider(resourceManager);
      v.RuleFor(x => x.Name).NotNull();
    });

    var maxValidator = new InlineValidator<Person>(v =>
    {
      v.MessageProvider = new ResourceManagerValidationMessageProvider(resourceManager);
      v.RuleFor(x => x.Name).MaximumLength(3);
    });

    var lengthValidator = new InlineValidator<Person>(v =>
    {
      v.MessageProvider = new ResourceManagerValidationMessageProvider(resourceManager);
      v.RuleFor(x => x.Name).Length(1, 2);
    });

    var nullResult = notNullValidator.Validate(new Person(null));
    Assert.False(nullResult.IsValid);
    Assert.Single(nullResult.Errors);
    Assert.Equal("RES_NOT_NULL", nullResult.Errors[0].ErrorMessage);

    var maxResult = maxValidator.Validate(new Person("abcd"));
    Assert.False(maxResult.IsValid);
    Assert.Single(maxResult.Errors);
    Assert.Equal("RES_MAX_3", maxResult.Errors[0].ErrorMessage);

    var lenResult = lengthValidator.Validate(new Person(""));
    Assert.False(lenResult.IsValid);
    Assert.Single(lenResult.Errors);
    Assert.Equal("RES_LEN_1_2", lenResult.Errors[0].ErrorMessage);
  }

  private sealed class InlineValidator<T> : AbstractValidator<T>
  {
    public InlineValidator(Action<InlineValidator<T>> build)
    {
      build(this);
    }

    public new IRuleBuilderInitial<T, TProperty> RuleFor<TProperty>(System.Linq.Expressions.Expression<Func<T, TProperty>> expression)
      => base.RuleFor(expression);
  }
}
