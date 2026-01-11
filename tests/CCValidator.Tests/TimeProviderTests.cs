namespace CCValidator.Tests;

public sealed class TimeProviderTests
{
  private sealed record Person(DateTimeOffset Timestamp);

  private sealed class FixedTimeProvider : TimeProvider
  {
    private readonly DateTimeOffset _utcNow;

    public FixedTimeProvider(DateTimeOffset utcNow)
    {
      _utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
  }

  private sealed class PersonValidator : AbstractValidator<Person>
  {
    private readonly TimeProvider _timeProvider;

    public PersonValidator(CCValidatorOptions options)
      : base(options)
    {
      _timeProvider = options.TimeProvider;

      RuleFor(x => x.Timestamp)
        .Must(ts => ts <= _timeProvider.GetUtcNow())
        .WithMessage("must be in the past");
    }
  }

  [Fact]
  public void TimeProvider_can_be_overridden_for_deterministic_time_based_rules()
  {
    var now = new DateTimeOffset(2026, 1, 11, 12, 0, 0, TimeSpan.Zero);
    var options = new CCValidatorOptions
    {
      TimeProvider = new FixedTimeProvider(now),
    };

    var validator = new PersonValidator(options);

    var ok = validator.Validate(new Person(now.AddSeconds(-1)));
    Assert.True(ok.IsValid);

    var fail = validator.Validate(new Person(now.AddSeconds(1)));
    Assert.False(fail.IsValid);
    Assert.Single(fail.Errors);
    Assert.Equal("Timestamp", fail.Errors[0].PropertyName);
    Assert.Equal("must be in the past", fail.Errors[0].ErrorMessage);
  }
}
