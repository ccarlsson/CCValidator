using CCValidator;
using Serilog;

namespace CCValidator.Serilog;

/// <summary>
/// Logs CCValidator internal validation errors to Serilog.
/// </summary>
public sealed class SerilogValidationLogger : IValidationLogger
{
  private readonly ILogger _logger;

  /// <summary>
  /// Create a logger adapter.
  /// </summary>
  /// <param name="logger">Serilog logger.</param>
  public SerilogValidationLogger(ILogger logger)
  {
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  /// <inheritdoc />
  public void InternalValidationError(InternalValidationErrorContext context)
  {
    _logger.Error(
      context.Exception,
      "CCValidator internal validation error for {PropertyName}. AttemptedValue: {@AttemptedValue}",
      context.PropertyName,
      context.AttemptedValue);
  }
}