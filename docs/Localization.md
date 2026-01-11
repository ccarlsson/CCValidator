# Localization / Message Providers

CCValidator uses an `IValidationMessageProvider` to supply default error messages for built-in validators.

- `.WithMessage("...")` always overrides the provider.
- If you don’t set anything, `DefaultValidationMessageProvider` is used.

## Per-validator configuration

`AbstractValidator<T>` exposes a `MessageProvider` property.

```csharp
using CCValidator;

public sealed record Person(string? Name);

public sealed class PersonValidator : AbstractValidator<Person>
{
  public PersonValidator()
  {
    MessageProvider = DefaultValidationMessageProvider.Instance;

    RuleFor(x => x.Name)
      .NotNull()
      .NotEmpty();
  }
}
```

## ResourceManager-backed localization

`ResourceManagerValidationMessageProvider` reads messages from `.resx` resources.

### Example `.resx` keys

The provider looks up resource keys by method name:

- `NotNull`
- `NotEmpty`
- `MaximumLength` (format string, expects `{0}`)
- `Length` (format string, expects `{0}` and `{1}`)

Example values:

- `NotNull` → `must not be null`
- `MaximumLength` → `must be {0} characters or fewer`

### Wiring it up

```csharp
using System.Resources;
using CCValidator;

public sealed class PersonValidator : AbstractValidator<Person>
{
  public PersonValidator(ResourceManager resourceManager)
  {
    MessageProvider = new ResourceManagerValidationMessageProvider(resourceManager);

    RuleFor(x => x.Name)
      .NotNull()
      .MaximumLength(10);
  }
}
```

## Culture selection

By default, `ResourceManagerValidationMessageProvider` uses `CultureInfo.CurrentUICulture`.

If you want to force a culture (for tests or a specific tenant), pass it explicitly:

```csharp
using System.Globalization;
using System.Resources;
using CCValidator;

var provider = new ResourceManagerValidationMessageProvider(
  resourceManager,
  culture: CultureInfo.GetCultureInfo("sv-SE"));
```

## Fallback strategy

You can implement a fallback provider by delegating to a resource provider first, then falling back to `DefaultValidationMessageProvider.Instance` when a key is missing.

One simple approach is to wrap calls and catch `InvalidOperationException` thrown for missing keys.
