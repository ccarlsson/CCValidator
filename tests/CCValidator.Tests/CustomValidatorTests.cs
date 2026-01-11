namespace CCValidator.Tests;

public sealed class CustomValidatorTests
{
  private sealed record Person(string? Name, int Age);

  private sealed class AdultAgeValidator : IPropertyValidator<Person, int>
  {
    public string DefaultMessage => "must be at least 18";

    public bool IsValid(Person instance, int value) => value >= 18;
  }

  private sealed class NameNotTakenValidator : IAsyncPropertyValidator<Person, string?>
  {
    public string DefaultMessage => "name is already taken";

    public Task<bool> IsValidAsync(Person instance, string? value, CancellationToken token)
    {
      token.ThrowIfCancellationRequested();
      return Task.FromResult(!string.Equals(value, "taken", StringComparison.Ordinal));
    }
  }

  private sealed class SyncPersonValidator : AbstractValidator<Person>
  {
    public SyncPersonValidator()
    {
      RuleFor(x => x.Age)
        .SetValidator(new AdultAgeValidator())
        .WithMessage("must be an adult");
    }
  }

  private sealed class AsyncPersonValidator : AbstractValidator<Person>
  {
    public AsyncPersonValidator()
    {
      RuleFor(x => x.Name)
        .SetAsyncValidator(new NameNotTakenValidator());
    }
  }

  [Fact]
  public void Custom_validator_class_can_be_used_for_sync_validation()
  {
    var validator = new SyncPersonValidator();

    var result = validator.Validate(new Person(Name: null, Age: 17));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("Age", result.Errors[0].PropertyName);
    Assert.Equal("must be an adult", result.Errors[0].ErrorMessage);
    Assert.Equal(17, result.Errors[0].AttemptedValue);
  }

  [Fact]
  public async Task Custom_async_validator_class_can_be_used_in_ValidateAsync()
  {
    var validator = new AsyncPersonValidator();

    var result = await validator.ValidateAsync(new Person(Name: "taken", Age: 20));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("Name", result.Errors[0].PropertyName);
    Assert.Equal("name is already taken", result.Errors[0].ErrorMessage);
    Assert.Equal("taken", result.Errors[0].AttemptedValue);
  }
}
