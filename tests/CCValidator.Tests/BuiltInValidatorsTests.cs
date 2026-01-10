namespace CCValidator.Tests;

using System.Linq.Expressions;

public sealed class BuiltInValidatorsTests
{
  private sealed record Person(int Age, int Score, string? Email, string? Name);

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
