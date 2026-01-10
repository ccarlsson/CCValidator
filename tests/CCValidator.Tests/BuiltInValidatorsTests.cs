namespace CCValidator.Tests;

using System.Linq.Expressions;
using System.Text.RegularExpressions;

public sealed class BuiltInValidatorsTests
{
  private sealed record Person(int Age, int Score, string? Email, string? Name);

  private sealed record CollectionsModel(int[]? Numbers, HashSet<int>? Set);

  [Fact]
  public void Equal_and_NotEqual_work()
  {
    var validator = new InlineValidator<Person>(v =>
    {
      v.RuleFor(x => x.Age).Equal(10).WithMessage("eq");
      v.RuleFor(x => x.Score).NotEqual(5).WithMessage("neq");
    });

    var ok = validator.Validate(new Person(Age: 10, Score: 4, Email: null, Name: null));
    Assert.True(ok.IsValid);

    var bad = validator.Validate(new Person(Age: 9, Score: 5, Email: null, Name: null));
    Assert.False(bad.IsValid);
    Assert.Equal(2, bad.Errors.Count);
    Assert.Equal("eq", bad.Errors[0].ErrorMessage);
    Assert.Equal("neq", bad.Errors[1].ErrorMessage);
  }

  [Fact]
  public void Comparison_validators_work()
  {
    var validator = new InlineValidator<Person>(v =>
    {
      v.RuleFor(x => x.Age).GreaterThan(18).WithMessage("gt");
      v.RuleFor(x => x.Score).LessThanOrEqualTo(100).WithMessage("lte");
    });

    Assert.True(validator.Validate(new Person(Age: 19, Score: 100, Email: null, Name: null)).IsValid);

    var bad = validator.Validate(new Person(Age: 18, Score: 101, Email: null, Name: null));
    Assert.False(bad.IsValid);
    Assert.Equal(2, bad.Errors.Count);
    Assert.Equal("gt", bad.Errors[0].ErrorMessage);
    Assert.Equal("lte", bad.Errors[1].ErrorMessage);
  }

  [Fact]
  public void InclusiveBetween_includes_endpoints()
  {
    var validator = new InlineValidator<Person>(v =>
    {
      v.RuleFor(x => x.Age).InclusiveBetween(1, 3).WithMessage("between");
    });

    Assert.True(validator.Validate(new Person(Age: 1, Score: 0, Email: null, Name: null)).IsValid);
    Assert.True(validator.Validate(new Person(Age: 3, Score: 0, Email: null, Name: null)).IsValid);

    var bad = validator.Validate(new Person(Age: 0, Score: 0, Email: null, Name: null));
    Assert.False(bad.IsValid);
    Assert.Single(bad.Errors);
    Assert.Equal("between", bad.Errors[0].ErrorMessage);
  }

  [Fact]
  public void ExclusiveBetween_excludes_endpoints()
  {
    var validator = new InlineValidator<Person>(v =>
    {
      v.RuleFor(x => x.Age).ExclusiveBetween(1, 3).WithMessage("between");
    });

    Assert.True(validator.Validate(new Person(Age: 2, Score: 0, Email: null, Name: null)).IsValid);

    Assert.False(validator.Validate(new Person(Age: 1, Score: 0, Email: null, Name: null)).IsValid);
    Assert.False(validator.Validate(new Person(Age: 3, Score: 0, Email: null, Name: null)).IsValid);
  }

  [Fact]
  public void EmailAddress_accepts_null_and_rejects_invalid_strings()
  {
    var validator = new InlineValidator<Person>(v =>
    {
      v.RuleFor(x => x.Email).EmailAddress().WithMessage("email");
    });

    Assert.True(validator.Validate(new Person(Age: 0, Score: 0, Email: null, Name: null)).IsValid);
    Assert.True(validator.Validate(new Person(Age: 0, Score: 0, Email: "a@b.com", Name: null)).IsValid);

    var bad = validator.Validate(new Person(Age: 0, Score: 0, Email: "not-an-email", Name: null));
    Assert.False(bad.IsValid);
    Assert.Single(bad.Errors);
    Assert.Equal("email", bad.Errors[0].ErrorMessage);
  }

  [Fact]
  public void Length_validators_support_arrays_and_generic_collections()
  {
    var validator = new InlineValidator<CollectionsModel>(v =>
    {
      v.RuleFor(x => x.Numbers).MinimumLength(2).WithMessage("min");
      v.RuleFor(x => x.Set).MaximumLength(2).WithMessage("max");
    });

    Assert.True(validator.Validate(new CollectionsModel(Numbers: new[] { 1, 2 }, Set: new HashSet<int> { 1, 2 })).IsValid);

    var bad = validator.Validate(new CollectionsModel(Numbers: new[] { 1 }, Set: new HashSet<int> { 1, 2, 3 }));
    Assert.False(bad.IsValid);
    Assert.Equal(2, bad.Errors.Count);
    Assert.Equal("min", bad.Errors[0].ErrorMessage);
    Assert.Equal("max", bad.Errors[1].ErrorMessage);
  }

  [Fact]
  public void Matches_with_options_honors_regex_options()
  {
    var validator = new InlineValidator<Person>(v =>
    {
      v.RuleFor(x => x.Name).Matches("^abc$", RegexOptions.IgnoreCase).WithMessage("matches");
    });

    Assert.True(validator.Validate(new Person(Age: 0, Score: 0, Email: null, Name: null)).IsValid);
    Assert.True(validator.Validate(new Person(Age: 0, Score: 0, Email: null, Name: "ABC")).IsValid);

    var bad = validator.Validate(new Person(Age: 0, Score: 0, Email: null, Name: "ab"));
    Assert.False(bad.IsValid);
    Assert.Single(bad.Errors);
    Assert.Equal("matches", bad.Errors[0].ErrorMessage);
  }

  [Fact]
  public void Matches_with_regex_instance_uses_given_regex()
  {
    var regex = new Regex("^abc$", RegexOptions.IgnoreCase);

    var validator = new InlineValidator<Person>(v =>
    {
      v.RuleFor(x => x.Name).Matches(regex).WithMessage("matches");
    });

    Assert.True(validator.Validate(new Person(Age: 0, Score: 0, Email: null, Name: "ABC")).IsValid);

    var bad = validator.Validate(new Person(Age: 0, Score: 0, Email: null, Name: "abcd"));
    Assert.False(bad.IsValid);
    Assert.Single(bad.Errors);
    Assert.Equal("matches", bad.Errors[0].ErrorMessage);
  }

  [Fact]
  public void Equal_and_NotEqual_can_compare_to_other_property()
  {
    var validator = new InlineValidator<Person>(v =>
    {
      v.RuleFor(x => x.Age).Equal(x => x.Score).WithMessage("eq");
      v.RuleFor(x => x.Age).NotEqual(x => x.Score).WithMessage("neq");
    });

    var ok1 = validator.Validate(new Person(Age: 10, Score: 10, Email: null, Name: null));
    Assert.False(ok1.IsValid);
    Assert.Single(ok1.Errors);
    Assert.Equal("neq", ok1.Errors[0].ErrorMessage);

    var ok2 = validator.Validate(new Person(Age: 10, Score: 11, Email: null, Name: null));
    Assert.False(ok2.IsValid);
    Assert.Single(ok2.Errors);
    Assert.Equal("eq", ok2.Errors[0].ErrorMessage);
  }

  [Fact]
  public void Comparison_validators_can_compare_to_other_property()
  {
    var validator = new InlineValidator<Person>(v =>
    {
      v.RuleFor(x => x.Age).LessThan(x => x.Score).WithMessage("lt");
      v.RuleFor(x => x.Score).GreaterThanOrEqualTo(x => x.Age).WithMessage("gte");
    });

    Assert.True(validator.Validate(new Person(Age: 5, Score: 10, Email: null, Name: null)).IsValid);

    var bad = validator.Validate(new Person(Age: 10, Score: 10, Email: null, Name: null));
    Assert.False(bad.IsValid);
    Assert.Single(bad.Errors);
    Assert.Equal("lt", bad.Errors[0].ErrorMessage);
  }

  private sealed class InlineValidator<T> : CCValidator.AbstractValidator<T>
  {
    public InlineValidator(Action<InlineValidator<T>> build)
    {
      build(this);
    }

    public new CCValidator.IRuleBuilderInitial<T, TProperty> RuleFor<TProperty>(System.Linq.Expressions.Expression<Func<T, TProperty>> expression)
      => base.RuleFor(expression);
  }
}
