using System.Linq.Expressions;

namespace CCValidator;

/// <summary>
/// Base type for defining validation rules for <typeparamref name="T"/>.
/// </summary>
public abstract partial class AbstractValidator<T> : IValidator<T>
{
  private readonly List<IRule<T>> _rules = [];
  private readonly Stack<Func<T, bool>> _conditionStack = new();
  private readonly Stack<string> _ruleSetStack = new();
  private readonly Stack<IDependentRuleHost<T>> _dependentRuleHostStack = new();
  private readonly CCValidatorOptions _options;

  protected AbstractValidator()
    : this(options: null)
  {
  }

  /// <summary>
  /// Create a validator using the provided options.
  /// </summary>
  /// <param name="options">Options controlling defaults and behavior.</param>
  protected AbstractValidator(CCValidatorOptions? options)
  {
    _options = options ?? new CCValidatorOptions();

    CascadeMode = _options.DefaultCascadeMode;
    MessageProvider = _options.MessageProvider;
  }

  public CascadeMode CascadeMode { get; set; }

  public IValidationMessageProvider MessageProvider { get; set; }
}
