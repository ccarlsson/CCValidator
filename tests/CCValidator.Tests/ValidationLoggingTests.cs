namespace CCValidator.Tests;

public sealed class ValidationLoggingTests
{
  private sealed class CaptureLogger : IValidationLogger
  {
    public List<InternalValidationErrorContext> Entries { get; } = [];

    public void InternalValidationError(InternalValidationErrorContext context)
    {
      Entries.Add(context);
    }
  }

  private sealed class ThrowingLogger : IValidationLogger
  {
    public void InternalValidationError(InternalValidationErrorContext context)
    {
      throw new InvalidOperationException("logger failed");
    }
  }

  private sealed record Model(string Value);

  private sealed class ThrowingValidator : AbstractValidator<Model>
  {
    public ThrowingValidator(CCValidatorOptions options)
      : base(options)
    {
      RuleFor(x => x.Value)
        .Must(_ => throw new InvalidOperationException("boom"));
    }
  }

  [Fact]
  public void Validate_WhenInternalExceptionOccurs_LogsAndReturnsInternalFailure()
  {
    var logger = new CaptureLogger();
    var options = new CCValidatorOptions
    {
      Logger = logger,
      ExceptionBehavior = ValidationExceptionBehavior.ConvertToFailure,
    };

    var validator = new ThrowingValidator(options);
    var result = validator.Validate(new Model("hello"));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Single(logger.Entries);
    Assert.Equal("Value", logger.Entries[0].PropertyName);
    Assert.IsType<InvalidOperationException>(logger.Entries[0].Exception);
  }

  [Fact]
  public void Validate_WhenLoggerThrows_DoesNotAffectValidationResult()
  {
    var options = new CCValidatorOptions
    {
      Logger = new ThrowingLogger(),
      ExceptionBehavior = ValidationExceptionBehavior.ConvertToFailure,
    };

    var validator = new ThrowingValidator(options);
    var result = validator.Validate(new Model("hello"));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
    Assert.Equal(options.InternalErrorCode, result.Errors[0].ErrorCode);
  }
}