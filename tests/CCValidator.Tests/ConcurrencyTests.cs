namespace CCValidator.Tests;

public sealed class ConcurrencyTests
{
  private sealed record Person(string? Name, int Age);

  private sealed class PersonValidator : AbstractValidator<Person>
  {
    public PersonValidator()
    {
      RuleFor(x => x.Name)
        .NotNull()
        .NotEmpty()
        .MaximumLength(10);

      RuleFor(x => x.Age)
        .InclusiveBetween(0, 150);
    }
  }

  [Fact]
  public async Task Validate_is_thread_safe_for_shared_validator_instances()
  {
    var validator = new PersonValidator();

    const int taskCount = 64;
    const int iterations = 200;

    var tasks = Enumerable.Range(0, taskCount)
      .Select(_ => Task.Run(() =>
      {
        for (var i = 0; i < iterations; i++)
        {
          var result = validator.Validate(new Person(Name: "", Age: 200));

          Assert.False(result.IsValid);
          Assert.True(result.Errors.Count >= 2);
        }
      }))
      .ToArray();

    await Task.WhenAll(tasks);
  }

  [Fact]
  public async Task ValidateAsync_is_thread_safe_for_shared_validator_instances()
  {
    var validator = new PersonValidator();

    const int taskCount = 64;
    const int iterations = 100;

    var tasks = Enumerable.Range(0, taskCount)
      .Select(_ => Task.Run(async () =>
      {
        for (var i = 0; i < iterations; i++)
        {
          var result = await validator.ValidateAsync(new Person(Name: "", Age: 200));

          Assert.False(result.IsValid);
          Assert.True(result.Errors.Count >= 2);
        }
      }))
      .ToArray();

    await Task.WhenAll(tasks);
  }
}
