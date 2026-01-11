# Getting started

## Install

If you’re using NuGet packages:

- `CCValidator` (core)
- optionally `CCValidator.DependencyInjection`, `CCValidator.AspNetCore`, `CCValidator.Serilog`

## Define a validator

```csharp
public sealed record Person(string? Name, int Age);

public sealed class PersonValidator : AbstractValidator<Person>
{
  public PersonValidator()
  {
    RuleFor(x => x.Name)
      .NotEmpty()
      .MaximumLength(50);

    RuleFor(x => x.Age)
      .InclusiveBetween(0, 150);
  }
}
```

## Validate

```csharp
var validator = new PersonValidator();

var result = validator.Validate(new Person(Name: "", Age: 200));
if (!result.IsValid)
{
  foreach (var failure in result.Errors)
    Console.WriteLine($"{failure.PropertyName}: {failure.ErrorMessage}");
}
```

## Async validation

```csharp
RuleFor(x => x.Name)
  .MustAsync(async (name, ct) =>
  {
    await Task.Delay(10, ct);
    return name is not null;
  });
```

For DI and ASP.NET Core usage, see `docs/DependencyInjection.md` and `docs/AspNetCore.md`.