# CCValidator

CCValidator is a FluentValidation-inspired validation library targeting .NET 10 / C# 14.

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

- `docs/GettingStarted.md`
- `docs/ApiReference.md`
- `docs/AspNetCore.md`
- `docs/DependencyInjection.md`
- `docs/Localization.md`
- `docs/Logging.md`
- `docs/MigrationFromFluentValidation.md`
- [Getting Started](docs/GettingStated.md)

See `COMPATIBILITY.md` for differences vs FluentValidation.
