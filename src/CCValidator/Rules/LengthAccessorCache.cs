using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;

namespace CCValidator;

internal static class LengthAccessorCache
{
  private static readonly ConcurrentDictionary<Type, Func<object, int>?> Cache = new();

  public static bool TryGetLength(object value, out int length)
  {
    var type = value.GetType();

    var accessor = Cache.GetOrAdd(type, CreateAccessor);
    if (accessor is null)
    {
      length = 0;
      return false;
    }

    length = accessor(value);
    return true;
  }

  private static Func<object, int>? CreateAccessor(Type type)
  {
    if (type == typeof(string))
      return static o => ((string)o).Length;

    if (type.IsArray)
      return static o => ((Array)o).Length;

    if (typeof(ICollection).IsAssignableFrom(type))
      return static o => ((ICollection)o).Count;

    // Support ICollection<T>/IReadOnlyCollection<T> (eg HashSet<T>)
    var genericInterfaces = type.GetInterfaces()
      .Where(i => i.IsGenericType)
      .ToArray();

    var collectionIface = genericInterfaces.FirstOrDefault(i => i.GetGenericTypeDefinition() == typeof(ICollection<>))
      ?? genericInterfaces.FirstOrDefault(i => i.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>));

    if (collectionIface is null)
      return null;

    var countProp = collectionIface.GetProperty("Count");
    if (countProp is null)
      return null;

    var objParam = Expression.Parameter(typeof(object), "o");
    var cast = Expression.Convert(objParam, collectionIface);
    var count = Expression.Property(cast, countProp);
    var lambda = Expression.Lambda<Func<object, int>>(Expression.Convert(count, typeof(int)), objParam);
    return lambda.Compile();
  }
}
