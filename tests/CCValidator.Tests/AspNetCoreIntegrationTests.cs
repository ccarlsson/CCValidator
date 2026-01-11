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

  private sealed class PersonValidator : AbstractValidator<Person>
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

    services.AddSingleton<IValidator<Person>, PersonValidator>();

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

  [Fact]
  public void ModelValidator_returns_empty_when_model_is_null()
  {
    IServiceCollection services = new ServiceCollection();
    services.AddSingleton<IValidator<Person>, PersonValidator>();

    using var provider = services.BuildServiceProvider();

    var httpContext = new DefaultHttpContext { RequestServices = provider };
    var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());

    var metadataProvider = new EmptyModelMetadataProvider();
    var metadata = metadataProvider.GetMetadataForType(typeof(Person));

    var validationContext = new ModelValidationContext(actionContext, metadata, metadataProvider, container: null, model: null);

    var validator = new CCValidatorModelValidator();
    var results = validator.Validate(validationContext).ToList();

    Assert.Empty(results);
  }

  [Fact]
  public void ModelValidator_can_be_invoked_multiple_times_for_same_model_type()
  {
    IServiceCollection services = new ServiceCollection();
    services.AddSingleton<IValidator<Person>, PersonValidator>();

    using var provider = services.BuildServiceProvider();

    var httpContext = new DefaultHttpContext { RequestServices = provider };
    var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());

    var metadataProvider = new EmptyModelMetadataProvider();
    var metadata = metadataProvider.GetMetadataForType(typeof(Person));

    var validator = new CCValidatorModelValidator();

    var results1 = validator.Validate(new ModelValidationContext(actionContext, metadata, metadataProvider, container: null, model: new Person(""))).ToList();
    var results2 = validator.Validate(new ModelValidationContext(actionContext, metadata, metadataProvider, container: null, model: new Person(""))).ToList();

    Assert.Single(results1);
    Assert.Single(results2);
    Assert.Equal("Name", results1[0].MemberName);
    Assert.Equal("name required", results1[0].Message);
    Assert.Equal(results1[0].MemberName, results2[0].MemberName);
    Assert.Equal(results1[0].Message, results2[0].Message);
  }
}
