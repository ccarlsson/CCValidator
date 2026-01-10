using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace CCValidator.DependencyInjection;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddValidatorsFromAssembly(
    this IServiceCollection services,
    Assembly assembly,
    ServiceLifetime lifetime = ServiceLifetime.Scoped)
  {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(assembly);

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
