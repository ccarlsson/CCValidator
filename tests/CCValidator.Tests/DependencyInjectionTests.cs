using Microsoft.Extensions.DependencyInjection;
using CCValidator.DependencyInjection;

namespace CCValidator.Tests;

public sealed class DependencyInjectionTests
{
  private sealed record Person(string? Name);
  private sealed record Person2(string? Name);

  private sealed class PersonValidator : AbstractValidator<Person>
  {
    public PersonValidator()
    {
      RuleFor(x => x.Name).NotEmpty();
    }
  }

  private sealed class OptionsAwarePersonValidator : AbstractValidator<Person2>
  {
    public OptionsAwarePersonValidator(CCValidatorOptions options)
      : base(options)
    {
      // Two validators so cascade mode affects how many failures we get.
      RuleFor(x => x.Name)
        .NotEmpty()
        .Matches("^[A-Z]+$");
    }
  }

  [Fact]
  public void AddValidatorsFromAssembly_registers_IValidator_of_T()
  {
    var services = new ServiceCollection();

    services.AddValidatorsFromAssembly(typeof(PersonValidator).Assembly);

    using var provider = services.BuildServiceProvider();

    var validator = provider.GetRequiredService<IValidator<Person>>();

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

    var v1a = scope1.ServiceProvider.GetRequiredService<IValidator<Person>>();
    var v1b = scope1.ServiceProvider.GetRequiredService<IValidator<Person>>();
    var v2 = scope2.ServiceProvider.GetRequiredService<IValidator<Person>>();

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

    var v1 = scope1.ServiceProvider.GetRequiredService<IValidator<Person>>();
    var v2 = scope2.ServiceProvider.GetRequiredService<IValidator<Person>>();

    Assert.Same(v1, v2);
  }

  [Fact]
  public void AddValidatorsFromAssembly_registers_default_CCValidatorOptions_for_constructor_injection()
  {
    var services = new ServiceCollection();

    services.AddValidatorsFromAssembly(typeof(OptionsAwarePersonValidator).Assembly);

    using var provider = services.BuildServiceProvider();

    var validator = provider.GetRequiredService<IValidator<Person2>>();

    Assert.NotNull(validator);
    Assert.IsType<OptionsAwarePersonValidator>(validator);
  }

  [Fact]
  public void AddCCValidator_configures_default_cascade_mode_used_by_validators()
  {
    var services = new ServiceCollection();

    services.AddCCValidator(o => o.DefaultCascadeMode = CascadeMode.Stop);
    services.AddValidatorsFromAssembly(typeof(OptionsAwarePersonValidator).Assembly);

    using var provider = services.BuildServiceProvider();
    var validator = provider.GetRequiredService<IValidator<Person2>>();

    var result = validator.Validate(new Person2(Name: ""));

    Assert.False(result.IsValid);
    Assert.Single(result.Errors);
  }
}
