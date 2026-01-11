namespace CCValidator;

public sealed class CCValidatorOptions
{
  public CascadeMode DefaultCascadeMode { get; init; } = CascadeMode.Continue;

  public IValidationMessageProvider MessageProvider { get; init; } = DefaultValidationMessageProvider.Instance;
}
