using System.Linq.Expressions;

namespace CCValidator;

internal static class ExpressionHelpers
{
  public static string GetPropertyName<T, TProperty>(Expression<Func<T, TProperty>> expression)
  {
    // Minimal implementation: supports x => x.Property and x => (object)x.Property
    Expression body = expression.Body;
    if (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } u)
      body = u.Operand;

    if (body is MemberExpression m)
      return m.Member.Name;

    return expression.ToString();
  }
}
