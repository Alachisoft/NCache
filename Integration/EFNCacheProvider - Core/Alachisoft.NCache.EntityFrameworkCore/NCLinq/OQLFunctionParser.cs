using Alachisoft.NCache.EntityFrameworkCore.NCache;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Alachisoft.NCache.EntityFrameworkCore.NCLinq
{
    class OQLFunctionParser : QueryFunctionParser
    {
        private DbContext currentContext;

        internal OQLFunctionParser(DbContext context)
        {
            currentContext = context;
        }

        public override FunctionInformation Min(MethodCallExpression methodExpression) => BasicAggregateImplementation(methodExpression, "MIN", FunctionInformation.FunctionType.Aggregate);

        public override FunctionInformation Max(MethodCallExpression methodExpression) => BasicAggregateImplementation(methodExpression, "MAX", FunctionInformation.FunctionType.Aggregate);

        public override FunctionInformation Sum(MethodCallExpression methodExpression) => BasicAggregateImplementation(methodExpression, "SUM", FunctionInformation.FunctionType.Aggregate);

        public override FunctionInformation Count(MethodCallExpression methodExpression)
        {
            FunctionInformation information = BasicAggregateImplementation(methodExpression, "COUNT", FunctionInformation.FunctionType.Aggregate);

            // Remove attribute from the COUNT's argument as OQL works on the supposition that the COUNT's argument 
            // is an entity and it is indexed.
            information.MeaningfulArguments[0] = RemoveAttributeAndCheckExistence(information.MeaningfulArguments[0].ToString());

            Logger.Log(
                "Function Information: " + information.ToLog(),
                Microsoft.Extensions.Logging.LogLevel.Debug
            );

            return information;
        }

        public override FunctionInformation Average(MethodCallExpression methodExpression) => BasicAggregateImplementation(methodExpression, "AVG", FunctionInformation.FunctionType.Aggregate);

        public override FunctionInformation GroupBy(MethodCallExpression methodExpression)
        {
            FunctionInformation information = new FunctionInformation
            {
                Name = "GROUP BY",
                MeaningfulArguments = new object[1],
                WhereContribution = default(string),
                Type = FunctionInformation.FunctionType.Miscellaneous
            };

            Expression expression = ((LambdaExpression)((UnaryExpression)methodExpression.Arguments[1]).Operand).Body;

            if (expression.NodeType == ExpressionType.MemberAccess)
            {
                information.MeaningfulArguments[0] = ((MemberExpression)expression).Member.Name;
            }
            else if (expression.NodeType == ExpressionType.Parameter)
            {
                // Handling cases of OrderBy(a => a)
                string fqn = new FullyQualifiedNameVisitor().BringFQN(methodExpression);
                string[] splitFQN = fqn.Split('.');

                information.MeaningfulArguments[0] = splitFQN[splitFQN.Length - 1];
            }

            Logger.Log(
                "Function Information: " + information.ToLog(),
                Microsoft.Extensions.Logging.LogLevel.Debug
            );

            return information;
        }

        public override FunctionInformation OrderBy(MethodCallExpression methodExpression)
        {
            FunctionInformation information = new FunctionInformation
            {
                Name = "ORDER BY",
                MeaningfulArguments = new object[1],
                WhereContribution = default(string),
                Type = FunctionInformation.FunctionType.Miscellaneous
            };

            Expression expression = ((LambdaExpression)((UnaryExpression)methodExpression.Arguments[1]).Operand).Body;

            if (expression.NodeType == ExpressionType.MemberAccess)
            {
                information.MeaningfulArguments[0] = ((MemberExpression)expression).Member.Name;
            }
            else if (expression.NodeType == ExpressionType.Parameter)
            {
                // Handling cases of OrderBy(a => a)
                string fqn = new FullyQualifiedNameVisitor().BringFQN(methodExpression);
                string[] splitFQN = fqn.Split('.');

                information.MeaningfulArguments[0] = splitFQN[splitFQN.Length - 1];
            }

            Logger.Log(
                "Function Information: " + information.ToLog(),
                Microsoft.Extensions.Logging.LogLevel.Debug
            );

            return information;
        }

        public override FunctionInformation OrderByDescending(MethodCallExpression methodExpression)
        {
            FunctionInformation information = OrderBy(methodExpression);

            Logger.Log(
                "Function Information (Descending): " + information.ToLog(),
                Microsoft.Extensions.Logging.LogLevel.Debug
            );

            return information;
        }

        public override FunctionInformation Select(MethodCallExpression methodExpression)
        {
            FunctionInformation information = new FunctionInformation();

            Logger.Log(
                "Function Information (Select): " + information.ToLog(),
                Microsoft.Extensions.Logging.LogLevel.Debug
            );

            return information;
        }

        /* ****************************************************************************************************** *
         * ---------------------------                HELPING METHODS              ------------------------------ *
         * ****************************************************************************************************** */

        private FunctionInformation BasicAggregateImplementation(MethodCallExpression methodExpression, string functionName, FunctionInformation.FunctionType functionType)
        {
            int argumentCount = methodExpression.Arguments.Count;

            FunctionInformation information = new FunctionInformation()
            {
                Name = functionName,
                Type = functionType,
                MeaningfulArguments = new object[1]
            };

            System.Type argumentType = methodExpression.Arguments[0].Type.GenericTypeArguments[0];

            string entityFullName = argumentType.IsInterface ? default(string) : argumentType.FullName;

            string fqn = new FullyQualifiedNameVisitor().BringFQN(methodExpression);

            fqn = fqn ?? entityFullName + (argumentCount > 1 ? GetSelectorBody(methodExpression.Arguments[1]) : "");

            information.MeaningfulArguments[0] = fqn;
            information.WhereContribution = argumentCount > 1 ? OQLBuilder.BuildWhereClause(currentContext, methodExpression.Arguments[1], out OQLBuilder oqlBuilder).Trim() : default(string);

            Logger.Log(
                "Function Information: " + information.ToLog(),
                Microsoft.Extensions.Logging.LogLevel.Debug
            );

            return information;
        }

        private string GetSelectorBody(Expression argument)
        {
            string selector = default(string);

            try
            {
                selector = "." + ((MemberExpression)((LambdaExpression)((UnaryExpression)argument).Operand).Body).Member.Name;
            }
            catch (System.Exception e)
            {
                Logger.Log(e, Microsoft.Extensions.Logging.LogLevel.Debug);
            }

            return selector;
        }

        private string RemoveAttributeAndCheckExistence(string entity)
        {
            if (currentContext.Model.FindEntityType(entity) == null)
            {
                string[] explodedEntity = entity.Split('.');

                string newEntityName = ArrayJoin(explodedEntity, ".", 0, explodedEntity.Length - 1);

                if (newEntityName.IsNullOrEmpty())
                {
                    throw new System.Exception("Entity type and context do not match.");
                }

                return RemoveAttributeAndCheckExistence(newEntityName);
            }

            return entity;
        }

        private string ArrayJoin(string[] arr, string separator = default(string), int startFrom = 0, int endAt = 0)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = startFrom; i < endAt; i++)
            {
                builder
                    .Append(i > 0 ? (separator == default(string) ? "" : separator) : "")
                    .Append(arr[i]);
            }

            return builder.ToString();
        }

        /* ****************************************************************************************************** *
         * -------------------------                SOME CUSTOM VISITORS              --------------------------- *
         * ****************************************************************************************************** */

        private class FullyQualifiedNameVisitor : ExpressionVisitor
        {
            private List<Expression> selectArguments;

            protected internal FullyQualifiedNameVisitor()
            {
                selectArguments = new List<Expression>();
                Logger.Log("About to bring FQN from 'Select' expressions.", Microsoft.Extensions.Logging.LogLevel.Trace);
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                Logger.Log("Visiting node : " + node.ToString(), Microsoft.Extensions.Logging.LogLevel.Trace);

                if (node.Object != null)
                {
                    Visit(node.Object);
                }

                for (int i = 0, n = node.ArgumentCount(); i < n; i++)
                {
                    Visit(node.GetArgument(i));
                }

                if (node.Method.Name.Equals("Select"))
                {
                    foreach (var arg in node.Arguments)
                    {
                        selectArguments.Add(arg);
                    }
                }

                return node;
            }

            protected internal string BringFQN(Expression node)
            {
                StringBuilder fqnBuilder = new StringBuilder();

                Visit(node);

                if (selectArguments.Count > 0)
                {
                    Expression expression = selectArguments[0];

                    fqnBuilder.Append(expression.Type.GenericTypeArguments[0].FullName);
                    Logger.Log("FQN for now is '" + fqnBuilder.ToString() + "'", Microsoft.Extensions.Logging.LogLevel.Trace);

                    for (int i = 1; i < selectArguments.Count; i++)
                    {
                        if (i % 2 != 0)
                        {
                            UnaryExpression unaryExpression = (UnaryExpression)selectArguments[i];
                            LambdaExpression lambdaExpression = (LambdaExpression)unaryExpression.Operand;

                            // Handling cases like :    .Select(a => a)
                            if (!lambdaExpression.Parameters[0].ToString().Equals(lambdaExpression.Body.ToString()))
                            {
                                MemberExpression memberExpression;
                                /*
                                 * If 'Select(entity => new { entity.Attribute })' was used, extract 'entity.Attribute'
                                 * from it as a member expression.
                                 */
                                if (lambdaExpression.Body is NewExpression newExpression)
                                {
                                    memberExpression = (MemberExpression)newExpression.Arguments[newExpression.ArgumentCount() - 1];
                                }
                                else
                                {
                                    memberExpression = (MemberExpression)lambdaExpression.Body;
                                }
                                fqnBuilder.Append("." + memberExpression.Member.Name);
                                Logger.Log("FQN for now is '" + fqnBuilder.ToString() + "'", Microsoft.Extensions.Logging.LogLevel.Trace);
                            }
                        }
                    }
                    Logger.Log("Returning FQN '" + fqnBuilder.ToString() + "'", Microsoft.Extensions.Logging.LogLevel.Trace);
                    return fqnBuilder.ToString();
                }
                return default(string);
            }

            // Not used right now
            private string ExtractGenericTypeName(System.Type type)
            {
                return type.IsInterface ? ExtractGenericTypeName(type.GenericTypeArguments[1]) : type.FullName;
            }
        }
    }
}
