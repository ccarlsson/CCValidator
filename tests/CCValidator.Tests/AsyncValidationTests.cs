namespace CCValidator.Tests;

public sealed class AsyncValidationTests
{
  private sealed record Person(string? Name);

  private sealed class AsyncMustFalseValidator : CCValidator.AbstractValidator<Person>
  {
    public AsyncMustFalseValidator()
    {
      RuleFor(x => x.Name)
        .MustAsync(async (name, ct) =>
        {
          await Task.Delay(10, ct);
          return false;
        })
        .WithMessage("async failed");
    }
  }

  private sealed class AsyncStopValidator : CCValidator.AbstractValidator<Person>
  {
    public AsyncStopValidator()
    {
      RuleFor(x => x.Name)
        .Cascade(CCValidator.CascadeMode.Stop)
        .MustAsync(async (_, ct) =>
        {
          await Task.Yield();
          return false;
        })
        .WithMessage("first")
        .MustAsync(async (_, ct) =>
        {
          await Task.Yield();
          return false;
        })
        .WithMessage("second");
    }
  }

  private sealed class AsyncCancellationValidator : CCValidator.AbstractValidator<Person>
  {
    public AsyncCancellationValidator()
    {
      RuleFor(x => x.Name)
        .MustAsync(async (_, ct) =>
        {
          await Task.Delay(TimeSpan.FromSeconds(10), ct);
          return true;
        });
    }
  }

  [Fact]
  public async Task ValidateAsync_executes_async_validators_and_returns_failures()
  {
    var validator = new AsyncMustFalseValidator();

    var result = await validator.ValidateAsync(new Person("abc"));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("Name", result.Errors[0].PropertyName);
    Assert.Equal("async failed", result.Errors[0].ErrorMessage);
    Assert.Equal("abc", result.Errors[0].AttemptedValue);
  }

  [Fact]
  public void Validate_throws_when_async_validators_exist()
  {
    var validator = new AsyncMustFalseValidator();

    Assert.Throws<InvalidOperationException>(() => validator.Validate(new Person("abc")));
  }

  [Fact]
  public async Task Cascade_Stop_applies_to_async_validators()
  {
    var validator = new AsyncStopValidator();

    var result = await validator.ValidateAsync(new Person("abc"));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal("first", result.Errors[0].ErrorMessage);
  }

  [Fact]
  public async Task ValidateAsync_respects_cancellation_token()
  {
    var validator = new AsyncCancellationValidator();

    using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => validator.ValidateAsync(new Person("abc"), cts.Token));
  }
}
