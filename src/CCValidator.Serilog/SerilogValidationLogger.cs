using Serilog;

namespace CCValidator.Serilog;

public sealed class SerilogValidationLogger : IValidationLogger
{
  private readonly ILogger _logger;

  public SerilogValidationLogger(ILogger logger)
  {
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public void InternalValidationError(InternalValidationErrorContext context)
  {
    _logger.Error(
      context.Exception,
      "CCValidator internal validation error for {PropertyName}. AttemptedValue: {@AttemptedValue}",
      context.PropertyName,
      context.AttemptedValue);
  }
}