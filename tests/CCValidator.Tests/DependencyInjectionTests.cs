using Microsoft.Extensions.DependencyInjection;
using CCValidator.DependencyInjection;

namespace CCValidator.Tests;

public sealed class DependencyInjectionTests
{
  private sealed record Person(string? Name);

  private sealed class PersonValidator : CCValidator.AbstractValidator<Person>
  {
    public PersonValidator()
    {
      RuleFor(x => x.Name).NotEmpty();
    }
  }

  [Fact]
  public void AddValidatorsFromAssembly_registers_IValidator_of_T()
  {
    var services = new ServiceCollection();

    services.AddValidatorsFromAssembly(typeof(PersonValidator).Assembly);

    using var provider = services.BuildServiceProvider();

    var validator = provider.GetRequiredService<CCValidator.IValidator<Person>>();

    Assert.NotNull(validator);
    Assert.IsType<PersonValidator>(validator);
  }

  [Fact]
  public void AddValidatorsFromAssembly_respects_scoped_lifetime_by_default()
  {
    var services = new ServiceCollection();

    services.AddValidatorsFromAssembly(typeof(PersonValidator).Assembly);

    using var provider = services.BuildServiceProvider();

    using var scope1 = provider.CreateScope();
    using var scope2 = provider.CreateScope();

    var v1a = scope1.ServiceProvider.GetRequiredService<CCValidator.IValidator<Person>>();
    var v1b = scope1.ServiceProvider.GetRequiredService<CCValidator.IValidator<Person>>();
    var v2 = scope2.ServiceProvider.GetRequiredService<CCValidator.IValidator<Person>>();

    Assert.Same(v1a, v1b);
    Assert.NotSame(v1a, v2);
  }

  [Fact]
  public void AddValidatorsFromAssembly_allows_singleton_lifetime()
  {
    var services = new ServiceCollection();

    services.AddValidatorsFromAssembly(typeof(PersonValidator).Assembly, ServiceLifetime.Singleton);

    using var provider = services.BuildServiceProvider();

    using var scope1 = provider.CreateScope();
    using var scope2 = provider.CreateScope();

    var v1 = scope1.ServiceProvider.GetRequiredService<CCValidator.IValidator<Person>>();
    var v2 = scope2.ServiceProvider.GetRequiredService<CCValidator.IValidator<Person>>();

    Assert.Same(v1, v2);
  }
}
