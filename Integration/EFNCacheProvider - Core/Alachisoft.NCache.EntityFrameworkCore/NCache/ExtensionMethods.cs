using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

/// <summary>
/// Defines extension methods
/// </summary>
internal static class ExtensionMethods
{
    /// <summary>
    /// Removes '\r', '\n', and '\t' characters from the string
    /// </summary>
    /// <param name="src">Source string</param>
    /// <returns>Resulting string without tabs and newline characters</returns>
    public static string StripTabsAndNewlines(this string src)
    {
        char[] newChars = new char[src.Length];
        int newStringIndex = 0;
        bool suppressWhiteSpace = false;
        for (int i = 0; i < src.Length; ++i)
        {
            char c = src[i];
            switch (c)
            {
                case '\r':
                case '\n':
                case '\t':
                    if (!suppressWhiteSpace)
                    {
                        newChars[newStringIndex++] = ' ';
                        suppressWhiteSpace = true;
                    }
                    break;
                case ' ':
                    if (!suppressWhiteSpace)
                    {
                        newChars[newStringIndex++] = c;
                        suppressWhiteSpace = true;
                    }
                    break;
                default:
                    newChars[newStringIndex++] = c;
                    suppressWhiteSpace = false;
                    break;
            }
        }

        return new string(newChars, 0, newStringIndex);
    }

    /// <summary>
    /// Determine whether this string is null or empty
    /// </summary>
    /// <param name="src">Source string</param>
    /// <returns>True is string is null or empty, false otherwise</returns>
    public static bool IsNullOrEmpty(this string src)
    {
        return string.IsNullOrEmpty(src);
    }

    /// <summary>
    /// Deep clone the array, only if items in the array are cloneable
    /// </summary>
    /// <param name="array">Array to deep clone</param>
    /// <returns>Copy of array</returns>
    public static Array DeepClone(this Array array)
    {
        if (array == null)
        {
            return array;
        }
        if (array.Length == 0)
        {
            return (Array)array.Clone();
        }

        object[] clone = new object[array.Length];

        for (int i = 0; i < array.Length; i++)
        {
            object obj = array.GetValue(i);
            if (obj != null)
            {
                if (obj is ICloneable)
                {
                    obj = ((ICloneable)obj).Clone();
                }
                clone.SetValue(obj, i);
            }
        }

        return clone;
    }

    /// <summary>
    /// Copied from : https://stackoverflow.com/a/2483054 .
    /// Determines whether a type is anonymous or not.
    /// </summary>
    /// <param name="type">Type parameter whose anonymity is to be determined.</param>
    /// <returns>Boolean indicating whether it was anonymous or not.</returns>
    public static bool IsAnonymous(this Type type)
    {
        if (type == null)
        {
            return false;
        }
        // HACK: The only way to detect anonymous types right now.
        return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
            && type.IsGenericType && type.Name.Contains("AnonymousType")
            && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
            && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
    }

    public static string ToStringWithoutAlias(this Expression expression)
    {
        return Alachisoft.NCache.EntityFrameworkCore.NCLinq.ExpressionStringBuilder.ExpressionToString(expression);
    }

    public static List<string> GetQueryCriteriaStr(Expression expression)
    {
        return Alachisoft.NCache.EntityFrameworkCore.NCLinq.ExpressionStringBuilder.CriteriaToStrings(expression);
    }

    public static List<Expression> GetQueryCriteriaExp(Expression expression)
    {
        return Alachisoft.NCache.EntityFrameworkCore.NCLinq.ExpressionStringBuilder.CriteriaToExpressions(expression);
    }

    public static int ArgumentCount(this NewExpression expression)
    {
        return expression.Arguments.Count();
    }

    public static Expression GetArgument(this NewExpression expression, int index)
    {
        return expression.Arguments[index];
    }

    public static int ArgumentCount(this ElementInit expression)
    {
        return expression.Arguments.Count();
    }

    public static Expression GetArgument(this ElementInit expression, int index)
    {
        return expression.Arguments[index];
    }

    public static int ArgumentCount(this InvocationExpression expression)
    {
        return expression.Arguments.Count();
    }

    public static Expression GetArgument(this InvocationExpression expression, int index)
    {
        return expression.Arguments[index];
    }

    public static int ArgumentCount(this MethodCallExpression expression)
    {
        return expression.Arguments.Count();
    }

    public static Expression GetArgument(this MethodCallExpression expression, int index)
    {
        return expression.Arguments[index];
    }

    public static int ArgumentCount(this IndexExpression expression)
    {
        return expression.Arguments.Count();
    }

    public static Expression GetArgument(this IndexExpression expression, int index)
    {
        return expression.Arguments[index];
    }

    public static int ParameterCount(this LambdaExpression expression)
    {
        return expression.Parameters.Count();
    }

    public static Expression GetParameter(this LambdaExpression expression, int index)
    {
        return expression.Parameters[index];
    }
}
