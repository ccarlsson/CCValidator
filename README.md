# CCValidator

CCValidator is a FluentValidation-inspired validation library targeting .NET 10 / C# 14.

It supports sync/async validation, rule sets, conditions, validator composition, nested validation, and collection validation.

## Packages

- `CCValidator` – core engine (`AbstractValidator<T>`, rules, execution)
- `CCValidator.DependencyInjection` – DI registration helpers
- `CCValidator.AspNetCore` – ASP.NET Core MVC model validation integration
- `CCValidator.Serilog` – optional Serilog logger adapter

## Install (preview)

Packages are intended to be published as preview versions.

- Example:
  - `dotnet add package CCValidator --version 0.1.0-preview.1`

If you consume the ASP.NET Core or DI integration packages, add those as well:

- `dotnet add package CCValidator.DependencyInjection --version 0.1.0-preview.1`
- `dotnet add package CCValidator.AspNetCore --version 0.1.0-preview.1`

See `docs/ReleasePreview.md` for the recommended pack/publish workflow and version override examples.

## Quick start

```csharp
public sealed class PersonValidator : AbstractValidator<Person>
{
  public PersonValidator()
  {
    RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
  }
}

var validator = new PersonValidator();
var result = validator.Validate(new Person(""));
```

## ASP.NET Core note

MVC model validation is synchronous (model binding calls `Validate`), so async rules are not executed during model binding.

## Docs

- [Getting Started](docs/GettingStarted.md)
- [API Reference](docs/ApiReference.md)
- [ASP.NET Core](docs/AspNetCore.md)
- [DI](docs/DependencyInjection.md)
- [Localization](docs/Localization.md)
- [Logging](docs/Logging.md)
- [Migration From FluentValidation](docs/MigrationFromFluentValidation.md)
- [Publishing a preview package](docs/ReleasePreview.md)

See `COMPATIBILITY.md` for differences vs FluentValidation.
