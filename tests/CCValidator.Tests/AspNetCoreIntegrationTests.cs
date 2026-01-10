using CCValidator.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CCValidator.Tests;

public sealed class AspNetCoreIntegrationTests
{
  private sealed record Person(string? Name);

  private sealed class PersonValidator : CCValidator.AbstractValidator<Person>
  {
    public PersonValidator()
    {
      RuleFor(x => x.Name).NotEmpty().WithMessage("name required");
    }
  }

  [Fact]
  public void AddCCValidatorAutoValidation_inserts_model_validator_provider()
  {
    IServiceCollection services = new ServiceCollection();

    services.AddControllers();
    services.AddCCValidatorAutoValidation();

    using var provider = services.BuildServiceProvider();

    var options = provider.GetRequiredService<IOptions<MvcOptions>>().Value;

    Assert.Contains(options.ModelValidatorProviders, p => p.GetType().Name.Contains("CCValidatorModelValidatorProvider"));
  }

  [Fact]
  public void ModelValidator_returns_results_from_registered_IValidator()
  {
    IServiceCollection services = new ServiceCollection();

    services.AddSingleton<CCValidator.IValidator<Person>, PersonValidator>();

    using var provider = services.BuildServiceProvider();

    var httpContext = new DefaultHttpContext { RequestServices = provider };
    var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());

    var metadataProvider = new EmptyModelMetadataProvider();
    var metadata = metadataProvider.GetMetadataForType(typeof(Person));

    var model = new Person("");

    var validationContext = new ModelValidationContext(actionContext, metadata, metadataProvider, container: null, model: model);

    var validator = new CCValidatorModelValidator();
    var results = validator.Validate(validationContext).ToList();

    Assert.Single(results);
    Assert.Equal("Name", results[0].MemberName);
    Assert.Equal("name required", results[0].Message);
  }

  [Fact]
  public void ModelValidator_returns_empty_when_no_validator_registered()
  {
    IServiceCollection services = new ServiceCollection();
    using var provider = services.BuildServiceProvider();

    var httpContext = new DefaultHttpContext { RequestServices = provider };
    var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());

    var metadataProvider = new EmptyModelMetadataProvider();
    var metadata = metadataProvider.GetMetadataForType(typeof(Person));

    var model = new Person("");

    var validationContext = new ModelValidationContext(actionContext, metadata, metadataProvider, container: null, model: model);

    var validator = new CCValidatorModelValidator();
    var results = validator.Validate(validationContext).ToList();

    Assert.Empty(results);
  }
}
