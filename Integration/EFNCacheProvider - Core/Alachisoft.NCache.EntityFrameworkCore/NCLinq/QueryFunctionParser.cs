using Alachisoft.NCache.EntityFrameworkCore.NCache;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Alachisoft.NCache.EntityFrameworkCore.NCLinq
{
    class FunctionInformation
    {
        internal enum FunctionType
        {
            /// <summary>
            /// Corresponds to a function that is not known to be 
            /// either a DBMS's function nor EF's.
            /// </summary>
            None,
            /// <summary>
            /// An aggregate function in the corresponding DBMS. E.g. MIN, MAX, SUM etc.
            /// </summary>
            Aggregate,
            /// <summary>
            /// A query function in the corresponding DBMS. E.g. SELECT, WHERE, JOIN etc.
            /// </summary>
            QueryMethod,
            /// <summary>
            /// A miscellaneous function in the corresponding DBMS. E.g. ORDER BY, GROUP BY.
            /// </summary>
            Miscellaneous
        }

        internal string Name
        {
            get; set;
        }

        internal FunctionType Type
        {
            get; set;
        }

        internal string WhereContribution
        {
            get; set;
        }

        internal object[] MeaningfulArguments
        {
            get; set;
        }

        internal string ToLog()
        {
            return GetType().Name + " = { "
                    + "Name = '" + Name + "', "
                    + "Type = '" + Type + "', "
                    + "WhereContribution = '" + WhereContribution + "', "
                    + "MeaningfulArguments = '" + MeaningfulArguments + "' "
                + "}";
        }
    }

    abstract class QueryFunctionParser
    {
        private Dictionary<string, string> functions = new Dictionary<string, string>()
        {
            { "any", "Any"},
            { "all", "All"},
            { "sum", "Sum"},
            { "min", "Min"},
            { "max", "Max"},
            { "zip", "Zip"},
            { "last", "Last"},
            { "join", "Join"},
            { "skip", "Skip"},
            { "take", "Take"},
            { "first", "First"},
            { "count", "Count"},
            { "union", "Union"},
            { "where", "Where"},
            { "append", "Append"},
            { "concat", "Concat"},
            { "except", "Except"},
            { "select", "Select"},
            { "average", "Average"},
            { "groupby", "GroupBy"},
            { "orderby", "OrderBy"},
            { "fromsql", "FromSql"},
            { "include", "Include"},
            { "prepend", "Prepend"},
            { "reverse", "Reverse"},
            { "contains", "Contains"},
            { "skiplast", "SkipLast"},
            { "takelast", "TakeLast"},
            { "aggregate", "Aggregate"},
            { "elementat", "ElementAt"},
            { "intersect", "Intersect"},
            { "groupjoin", "GroupJoin"},
            { "skipwhile", "SkipWhile"},
            { "takewhile", "TakeWhile"},
            { "astracking", "AsTracking"},
            { "selectmany", "SelectMany"},
            { "asnotracking", "AsNoTracking"},
            { "sequenceequal", "SequenceEqual"},
            { "lastordefault", "LastOrDefault"},
            { "firstordefault", "FirstOrDefault"},
            { "defaultifempty", "DefaultIfEmpty"},
            { "orderbydescending", "OrderByDescending"},
            { "elementatordefault", "ElementAtOrDefault"},
        };

        internal FunctionInformation Parse(string functionName, MethodCallExpression methodExpression)
        {
            string invokee = default(string);
            object[] parameters = { methodExpression };

            invokee = functions.ContainsValue(functionName)
                        ? functionName
                        : functions.ContainsKey(functionName)
                            ? functions[functionName]
                            : default(string);

            if (!invokee.IsNullOrEmpty())
            {
                Logger.Log(
                    "Will invoke " + invokee + " because of " + methodExpression.Method.Name,
                    Microsoft.Extensions.Logging.LogLevel.Debug
                );

                return (FunctionInformation)GetType().GetMethod(invokee).Invoke(this, parameters);
            }

            Logger.Log(
                methodExpression.Method.Name + " is an unknown function for us right now. Whatever is ahead will probably fail.",
                Microsoft.Extensions.Logging.LogLevel.Debug
            );

            return new FunctionInformation()
            {
                Name = functionName,
                MeaningfulArguments = { },
                WhereContribution = default(string),
                Type = FunctionInformation.FunctionType.None
            };
        }

        public virtual FunctionInformation Any(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation All(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation Sum(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation Min(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation Max(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation Zip(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation Last(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation Join(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation Skip(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation Take(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation First(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation Count(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation Union(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation Where(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation Append(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation Concat(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation Except(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation Select(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation Average(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation GroupBy(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation OrderBy(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation FromSql(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation Include(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation Prepend(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation Reverse(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation Contains(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation SkipLast(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation TakeLast(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation Aggregate(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation ElementAt(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation Intersect(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation GroupJoin(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation SkipWhile(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation TakeWhile(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation AsTracking(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation SelectMany(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation AsNoTracking(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation SequenceEqual(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation LastOrDefault(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation FirstOrDefault(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation DefaultIfEmpty(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation OrderByDescending(MethodCallExpression methodExpression) => throw new NotImplementedException();
        public virtual FunctionInformation ElementAtOrDefault(MethodCallExpression methodExpression) => throw new NotImplementedException();
    }
}
