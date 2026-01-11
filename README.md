# CCValidator

CCValidator is a FluentValidation-inspired validation library targeting .NET 10 / C# 14.

It supports sync/async validation, rule sets, conditions, validator composition, nested validation, and collection validation.

## Packages

- `CCValidator` – core engine (`AbstractValidator<T>`, rules, execution)
- `CCValidator.DependencyInjection` – DI registration helpers
- `CCValidator.AspNetCore` – ASP.NET Core MVC model validation integration
- `CCValidator.Serilog` – optional Serilog logger adapter

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

## Docs

- [Getting Started](docs/GettingStarted.md)
- [API Reference](docs/ApiReference.md)
- [ASP.NET Core](docs/AspNetCore.md)
- [DI](docs/DependencyInjection.md)
- [Localization](docs/Localization.md)
- [Logging](docs/Logging.md)
- [Migration From FluentValidation](docs/MigrationFromFluentValidation.md)

See `COMPATIBILITY.md` for differences vs FluentValidation.
