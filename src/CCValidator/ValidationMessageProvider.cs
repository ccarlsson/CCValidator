namespace CCValidator;

public interface IValidationMessageProvider
{
  string NotNull();
  string NotEmpty();
  string Must();

  string Equal();
  string NotEqual();

  string GreaterThan();
  string GreaterThanOrEqualTo();
  string LessThan();
  string LessThanOrEqualTo();

  string InclusiveBetween();
  string ExclusiveBetween();

  string MaximumLength(int maximumLength);
  string MinimumLength(int minimumLength);
  string Length(int minimumLength, int maximumLength);

  string Matches();
  string EmailAddress();
}

public sealed class DefaultValidationMessageProvider : IValidationMessageProvider
{
  public static readonly DefaultValidationMessageProvider Instance = new();

  private DefaultValidationMessageProvider()
  {
  }

  public string NotNull() => "must not be null";

  public string NotEmpty() => "must not be empty";

  public string Must() => "is not valid";

  public string Equal() => "must be equal to the specified value";

  public string NotEqual() => "must not be equal to the specified value";

  public string GreaterThan() => "must be greater than the specified value";

  public string GreaterThanOrEqualTo() => "must be greater than or equal to the specified value";

  public string LessThan() => "must be less than the specified value";

  public string LessThanOrEqualTo() => "must be less than or equal to the specified value";

  public string InclusiveBetween() => "must be between the specified values (inclusive)";

  public string ExclusiveBetween() => "must be between the specified values (exclusive)";

  public string MaximumLength(int maximumLength) => $"must be {maximumLength} characters or fewer";

  public string MinimumLength(int minimumLength) => $"must be {minimumLength} characters or more";

  public string Length(int minimumLength, int maximumLength) => $"must be between {minimumLength} and {maximumLength} characters";

  public string Matches() => "is not in the correct format";

  public string EmailAddress() => "is not a valid email address";
}
