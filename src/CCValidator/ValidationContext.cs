namespace CCValidator;

public sealed class ValidationContext<T>
{
  public ValidationContext(T instance)
  {
    InstanceToValidate = instance;
  }

  public T InstanceToValidate { get; }
}
