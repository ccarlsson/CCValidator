using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CCValidator.DependencyInjection;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddCCValidator(this IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    services.TryAddSingleton(static _ => new CCValidatorOptions());
    return services;
  }

  public static IServiceCollection AddCCValidator(this IServiceCollection services, CCValidatorOptions options)
  {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(options);

    services.Replace(ServiceDescriptor.Singleton(options));
    return services;
  }

  public static IServiceCollection AddCCValidator(
    this IServiceCollection services,
    Action<CCValidatorOptionsBuilder> configure)
  {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(configure);

    var builder = new CCValidatorOptionsBuilder();
    configure(builder);

    return services.AddCCValidator(builder.Build());
  }

  public static IServiceCollection AddValidatorsFromAssembly(
    this IServiceCollection services,
    Assembly assembly,
    ServiceLifetime lifetime = ServiceLifetime.Scoped)
  {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(assembly);

    // Ensure validators can take CCValidatorOptions via constructor injection.
    services.AddCCValidator();

    foreach (var type in assembly.DefinedTypes)
    {
      if (type.IsAbstract || type.IsInterface)
        continue;

      // Skip open-generic types (DI can't register/instantiate these as-is).
      if (type.ContainsGenericParameters)
        continue;

      var validatorInterfaces = type.ImplementedInterfaces
        .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>))
        .Where(i => !i.ContainsGenericParameters)
        .ToArray();

      if (validatorInterfaces.Length == 0)
        continue;

      foreach (var validatorInterface in validatorInterfaces)
      {
        // Register the concrete validator as IValidator<TModel>.
        services.Add(new ServiceDescriptor(validatorInterface, type.AsType(), lifetime));
      }
    }

    return services;
  }

  public static IServiceCollection AddValidatorsFromAssemblyContaining<T>(
    this IServiceCollection services,
    ServiceLifetime lifetime = ServiceLifetime.Scoped)
  {
    return services.AddValidatorsFromAssembly(typeof(T).Assembly, lifetime);
  }
}
