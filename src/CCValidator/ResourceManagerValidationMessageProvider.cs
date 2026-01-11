using System.Globalization;
using System.Resources;

namespace CCValidator;

public sealed class ResourceManagerValidationMessageProvider : IValidationMessageProvider
{
  private readonly ResourceManager _resourceManager;
  private readonly CultureInfo? _culture;

  public ResourceManagerValidationMessageProvider(ResourceManager resourceManager, CultureInfo? culture = null)
  {
    ArgumentNullException.ThrowIfNull(resourceManager);

    _resourceManager = resourceManager;
    _culture = culture;
  }

  public string NotNull() => GetRequiredString(nameof(NotNull));
  public string NotEmpty() => GetRequiredString(nameof(NotEmpty));
  public string Must() => GetRequiredString(nameof(Must));

  public string Equal() => GetRequiredString(nameof(Equal));
  public string NotEqual() => GetRequiredString(nameof(NotEqual));

  public string GreaterThan() => GetRequiredString(nameof(GreaterThan));
  public string GreaterThanOrEqualTo() => GetRequiredString(nameof(GreaterThanOrEqualTo));
  public string LessThan() => GetRequiredString(nameof(LessThan));
  public string LessThanOrEqualTo() => GetRequiredString(nameof(LessThanOrEqualTo));

  public string InclusiveBetween() => GetRequiredString(nameof(InclusiveBetween));
  public string ExclusiveBetween() => GetRequiredString(nameof(ExclusiveBetween));

  public string MaximumLength(int maximumLength) => Format(nameof(MaximumLength), maximumLength);
  public string MinimumLength(int minimumLength) => Format(nameof(MinimumLength), minimumLength);
  public string Length(int minimumLength, int maximumLength) => Format(nameof(Length), minimumLength, maximumLength);

  public string Matches() => GetRequiredString(nameof(Matches));
  public string EmailAddress() => GetRequiredString(nameof(EmailAddress));

  private CultureInfo Culture => _culture ?? CultureInfo.CurrentUICulture;

  private string GetRequiredString(string name)
  {
    var value = _resourceManager.GetString(name, Culture);
    if (string.IsNullOrEmpty(value))
      throw new InvalidOperationException($"Missing validation message resource '{name}' for culture '{Culture.Name}'.");

    return value;
  }

  private string Format(string name, params object[] args)
  {
    var template = GetRequiredString(name);
    return string.Format(Culture, template, args);
  }
}
