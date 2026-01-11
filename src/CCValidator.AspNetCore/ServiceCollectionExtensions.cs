using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace CCValidator.AspNetCore;

/// <summary>
/// ASP.NET Core MVC integration for CCValidator.
/// </summary>
public static class ServiceCollectionExtensions
{
  /// <summary>
  /// Registers MVC model validation integration that resolves <c>IValidator&lt;T&gt;</c> from DI.
  /// </summary>
  public static IServiceCollection AddCCValidatorAutoValidation(this IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<MvcOptions>, CCValidatorMvcOptionsSetup>());

    return services;
  }

  /// <summary>
  /// Registers MVC model validation integration on an MVC builder.
  /// </summary>
  public static IMvcBuilder AddCCValidatorAutoValidation(this IMvcBuilder builder)
  {
    ArgumentNullException.ThrowIfNull(builder);

    builder.Services.AddCCValidatorAutoValidation();
    return builder;
  }

  private sealed class CCValidatorMvcOptionsSetup : IConfigureOptions<MvcOptions>
  {
    public void Configure(MvcOptions options)
    {
      // Insert early so it runs before the default DataAnnotations provider.
      options.ModelValidatorProviders.Insert(0, new CCValidatorModelValidatorProvider());

      // Ensure any existing providers remain.
      // (No further changes.)
    }
  }
}
