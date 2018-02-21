// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Alachisoft.NCache.EntityFrameworkCore.NCache;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Alachisoft.NCache.EntityFrameworkCore.NCLinq
{
    //Created by Darkness in darkness
    sealed class OQLBuilder : ExpressionVisitor
    {
        private const string Dot = ".";
        private const string Space = " ";
        private const string Comma = ",";

        private string projectedObject;
        private string aggregateFunction;

        private DbContext currentContext;
        private QueryFunctionParser functionParser;
        private readonly StringBuilder whereBuilder;

        private readonly List<string> andAble;
        private readonly List<string> supportedAggregates;
        private readonly List<string> supportedQueryMethods;
        private readonly List<string> unsupportedAggregates;
        private readonly List<string> unsupportedQueryMethods;
        private readonly List<string> unsupportedMiscellaneous;

        private readonly Dictionary<string, string> supportedMiscellaneous;
        private readonly Dictionary<string, string> miscellaneousOperations;

        private OQLBuilder(DbContext context)
        {
            currentContext = context;

            projectedObject = default(string);
            aggregateFunction = default(string);

            whereBuilder = new StringBuilder();
            functionParser = new OQLFunctionParser(context);
            miscellaneousOperations = new Dictionary<string, string>();

            andAble = new List<string>()
            {
                "where",
            };
            supportedAggregates = new List<string>()
            {
                "min",
                "max",
                "sum",
                "count",
                "average",
            };
            supportedQueryMethods = new List<string>()
            {
                "where",
                "select",
            };
            unsupportedAggregates = new List<string>()
            {
                "any",
                "all",
                "last",
                "first",
                "except",
                "contains",
                "elementat",
                "sequenceequal",
                "lastordefault",
                "firstordefault",
                "elementatordefault",
            };
            unsupportedQueryMethods = new List<string>()
            {
                "zip",
                "join",
                "union",
                "include",
                "prepend",
                "intersect",
                "groupjoin",
                "selectmany",
            };
            unsupportedMiscellaneous = new List<string>()
            {
                "take",
                "skip",
                "append",
                "concat",
                "fromsql",
                "reverse",
                "skiplast",
                "takelast",
                "aggregate",
                "takewhile",
                "skipwhile",
                "astracking",
                "asnotracking",
                "defaultifempty",
            };
            supportedMiscellaneous = new Dictionary<string, string>
            {
                { "groupby", "GROUP BY" },
                { "orderby", "ORDER BY" },
                { "orderbydescending", "ORDER BY" },
            };
        }

        private string BuildQuery()
        {
            Logger.Log("Building OQL query.", Microsoft.Extensions.Logging.LogLevel.Trace);

            StringBuilder oqlStringBuilder = new StringBuilder("SELECT" + Space);

            if (aggregateFunction != default(string))
            {
                oqlStringBuilder
                    .Append(aggregateFunction)
                    .Append(Space);
            }
            else
            {
                oqlStringBuilder
                    .Append(projectedObject)
                    .Append(Space);
            }

            Logger.Log("Projection added.", Microsoft.Extensions.Logging.LogLevel.Trace);

            if (whereBuilder.Length > 0)
            {
                // Remove the extra AND at the end of WHERE criteria before 
                // appending it to the end result.
                if (whereBuilder.ToString().EndsWith("AND" + Space))
                {
                    whereBuilder.Remove(
                        whereBuilder.Length - ("AND" + Space).Length,
                        ("AND" + Space).Length
                    );
                }
                oqlStringBuilder
                    .Append("WHERE")
                    .Append(Space)
                    .Append(whereBuilder.ToString());
            }

            Logger.Log("Where criteria (if exists) added.", Microsoft.Extensions.Logging.LogLevel.Trace);

            InjectMiscellaneousFunctions(oqlStringBuilder);

            Logger.Log("OrderBy/GroupBy (if exists) injected.", Microsoft.Extensions.Logging.LogLevel.Trace);

            return oqlStringBuilder.ToString().Trim();
        }

        internal static string BuildWhereClause(DbContext context, Expression node, out OQLBuilder builder)
        {
            builder = new OQLBuilder(context);

            builder.Visit(node);

            return builder.whereBuilder.ToString();
        }

        internal static string ExpressionToOQL(DbContext context, Expression node, out OQLBuilder oqlBuilder)
        {
            Debug.Assert(node != null);

            ValidationResult result = new OQLQueryAnalyzer(node).ValidateQuery();

            if (!result.IsValid)
            {
                throw new Exception(result.Reason);
            }

            oqlBuilder = new OQLBuilder(context);

            oqlBuilder.Visit(node);

            return oqlBuilder.BuildQuery();
        }

        internal string GetAggregateUsed()
        {
            return aggregateFunction;
        }

        /* ********************************************************************************************************
         * --------------------------------                VISITORS              -------------------------------- *
         ******************************************************************************************************** */

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            Logger.Log("VisitMethodCall : " + node.ToString(), Microsoft.Extensions.Logging.LogLevel.Trace);

            object evalResult = EvaluateMethodAndCall(node);

            if (evalResult != null)
            {
                PadPossibleStringValueToWhere(evalResult)
                        .Append(Space);
                /*
                 * We don't need to visit further nodes.
                 * Doing so will pad the arguments of the 
                 * method invoked which shouldn't be done.
                 */
                return node;
            }

            if (node.Object != null)
            {
                Visit(node.Object);
            }

            for (int i = 0, n = node.ArgumentCount(); i < n; i++)
            {
                Visit(node.GetArgument(i));
            }

            // Add an 'AND' for separating query data criteria.
            if (IsAndAble(node.Method.Name))
            {
                if (whereBuilder.Length > 0)
                {
                    whereBuilder.Append("AND").Append(Space);
                }
            }

            return node;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            Logger.Log("VisitUnary : " + node.ToString(), Microsoft.Extensions.Logging.LogLevel.Trace);

            Visit(node.Operand);

            return node;
        }

        protected override Expression VisitNew(NewExpression node)
        {
            Logger.Log("VisitNew : " + node.ToString(), Microsoft.Extensions.Logging.LogLevel.Trace);

            if (IsDateTime(node))
            {
                whereBuilder
                    .Append("DateTime('")
                    .Append(InvokeConstructor(node).ToString())
                    .Append("')")
                    .Append(Space);

                return node;
            }
            if (node.Type.IsAnonymous())
            {
                // For Select function with anonymous returns e.g.
                // Select(entity => new { entity.Attribute }),
                // do not visit further.
                return node;
            }
            return base.VisitNew(node);
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            Logger.Log("VisitLambda : " + node.ToString(), Microsoft.Extensions.Logging.LogLevel.Trace);

            if (node.Body != null)
            {
                if (node.Body.NodeType != ExpressionType.Constant && node.Body.NodeType != ExpressionType.MemberAccess && node.Body.NodeType != ExpressionType.Parameter)
                {
                    Visit(node.Body);
                }
            }

            return node;
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            Logger.Log("VisitBinary : " + node.ToString(), Microsoft.Extensions.Logging.LogLevel.Trace);

            if (node.Left != null)
            {
                Visit(node.Left);
            }

            switch (node.NodeType)
            {
                case ExpressionType.Equal:
                    whereBuilder
                        .Append("=")
                        .Append(Space);
                    break;
                case ExpressionType.NotEqual:
                    whereBuilder
                        .Append("!=")
                        .Append(Space);
                    break;
                case ExpressionType.GreaterThan:
                    whereBuilder
                        .Append(">")
                        .Append(Space);
                    break;
                case ExpressionType.LessThan:
                    whereBuilder
                        .Append("<")
                        .Append(Space);
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    whereBuilder
                        .Append(">=")
                        .Append(Space);
                    break;
                case ExpressionType.LessThanOrEqual:
                    whereBuilder
                        .Append("<=")
                        .Append(Space);
                    break;
                case ExpressionType.AndAlso:
                    whereBuilder
                        .Append("AND")
                        .Append(Space);
                    break;
                case ExpressionType.OrElse:
                    whereBuilder
                        .Append("OR")
                        .Append(Space);
                    break;
                default:
                    // Do nothing
                    // Probably throw an exception
                    throw new Exception("Unsupported binary operation used.");
            }

            if (node.Right != null)
            {
                Visit(node.Right);
            }

            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            Logger.Log("VisitMember : " + node.ToString(), Microsoft.Extensions.Logging.LogLevel.Trace);

            if (node.Expression != null)
            {
                object value = GetMemberAccessValue(node);

                if (value != null)
                {
                    PadPossibleStringValueToWhere(value)
                        .Append(Space);

                    return node;
                }
                else
                {
                    Visit(node.Expression);
                }
            }

            if (node.Expression == null)
            {
                if (IsDateTime(node))
                {
                    object dateTimeValue = default(object);
                    FieldInfo field = node.Type.GetField(node.Member.Name);
                    PropertyInfo property = node.Type.GetProperty(node.Member.Name);

                    if (field != null)
                    {
                        dateTimeValue = field.GetValue(node);
                    }
                    else if (property != null)
                    {
                        dateTimeValue = property.GetValue(node);
                    }
                    else
                    {
                        // Hopefully such a case won't arise
                    }
                    whereBuilder
                        .Append("DateTime('")
                        .Append(dateTimeValue.ToString())      // Value of DateTime
                        .Append("')")
                        .Append(Space);
                }
                else
                {
                    // --------------------------- //
                    // This case hasn't arisen yet //
                    // --------------------------- //
                }
            }
            else
            {
                whereBuilder
                    .Append(Dot)
                    .Append(node.Member.Name)
                    .Append(Space);
            }

            return node;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            Logger.Log("VisitParameter : " + node.ToString(), Microsoft.Extensions.Logging.LogLevel.Trace);

            whereBuilder
                .Append("this");

            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            Logger.Log("VisitConstant : " + node.ToString(), Microsoft.Extensions.Logging.LogLevel.Trace);

            if (node.Value != null)
            {
                if (node.Type.IsClass)
                {
                    if (node.Type == typeof(String))
                    {
                        whereBuilder
                            .Append("'")
                            .Append(node.Value)
                            .Append("'")
                            .Append(Space);
                    }
                    else
                    {
                        if (node.Type.GenericTypeArguments != null)
                        {
                            if (node.Type.GenericTypeArguments.Length > 0)
                            {
                                if (currentContext.Model.FindEntityType(node.Type.GenericTypeArguments[0].FullName) != null)
                                {
                                    projectedObject = node.Type.GenericTypeArguments[0].FullName;
                                }
                                else
                                {
                                    // Reaching here means you're trying to query something that is not an entity
                                    throw new Exception("Entity type and context do not match.");
                                }
                            }
                        }
                    }
                }
                else if (node.Type.IsPrimitive)
                {
                    whereBuilder
                        .Append(node.Value)
                        .Append(Space);
                }
            }
            return node;
        }

        /* ********************************************************************************************************
         * -------------------------                SOME HELPING METHODS              --------------------------- *
         ******************************************************************************************************** */

        private object EvaluateArgument(Expression argument)
        {
            object returnVal = default(object);

            switch (argument.NodeType)
            {
                case ExpressionType.Call:
                    returnVal = InvokeMethodCall((MethodCallExpression)argument);
                    break;
                case ExpressionType.Constant:
                    returnVal = GetConstantValue((ConstantExpression)argument);
                    break;
                case ExpressionType.MemberAccess:
                    returnVal = GetMemberAccessValue((MemberExpression)argument);
                    break;
            }

            return returnVal;
        }

        private object EvaluateMethodAndCall(MethodCallExpression methodExpression)
        {
            //
            // Halt user from using unsupported operations
            //
            if (IsUnsupportedQueryMethod(methodExpression.Method.Name))
            {
                throw new Exception(methodExpression.Method.Name + " feature is not supported.");
            }
            if (IsUnsupportedAggregate(methodExpression.Method.Name))
            {
                throw new Exception(methodExpression.Method.Name + " aggregate is not supported.");
            }
            if (IsUnsupportedMiscellaneous(methodExpression.Method.Name))
            {
                throw new Exception(methodExpression.Method.Name + " is not supported.");
            }

            //
            // Now let's see what we can do with the stuff we support
            //
            if (IsSupportedQueryMethod(methodExpression.Method.Name))
            {
                // Do nothing. Go with the flow.

                Logger.Log(methodExpression.Method.Name + " is a supported query method.", Microsoft.Extensions.Logging.LogLevel.Trace);

                return null;
            }
            if (IsSupportedAggregate(methodExpression.Method.Name))
            {
                // Keep the aggregate in mind and go with the flow.

                Logger.Log(methodExpression.Method.Name + " is a supported aggregate function.", Microsoft.Extensions.Logging.LogLevel.Trace);

                if (aggregateFunction == default(string))
                {
                    FunctionInformation information = functionParser.Parse(methodExpression.Method.Name, methodExpression);
                    //
                    // If something else needs to be done.
                    //
                    aggregateFunction = BuildFunction(information, out string where);
                }
                else
                {
                    throw new Exception("More than one aggregates are not supported!");
                }
                return null;
            }
            if (IsSupportedMiscellaneous(methodExpression.Method.Name))
            {
                Logger.Log(methodExpression.Method.Name + " is a supported miscellaneous function.", Microsoft.Extensions.Logging.LogLevel.Trace);

                if (miscellaneousOperations.ContainsKey(methodExpression.Method.Name.ToLower()))
                {
                    throw new Exception("More than one " + methodExpression.Method.Name + " is not supported.");
                }
                else
                {
                    FunctionInformation information = functionParser.Parse(methodExpression.Method.Name, methodExpression);
                    //
                    // If something else needs to be done.
                    //
                    miscellaneousOperations.Add(
                        methodExpression.Method.Name.ToLower(),
                        BuildFunction(information, out string where)
                    );
                }
                return null;
            }
            if (IsDateTime(methodExpression))
            {
                Logger.Log("Encountered DateTime in Method Call.", Microsoft.Extensions.Logging.LogLevel.Trace);

                whereBuilder
                    .Append("DateTime('")
                    .Append(InvokeMethodCall(methodExpression).ToString())
                    .Append("')");
                return '\t';
            }
            else
            {
                // We have a method that needs to be invoked and the result needs to be 
                // returned to the calling method. This method is not part of LINQ. The 
                // user passed this method for criteria checking.

                if (methodExpression.Object != null)
                {
                }

                // [NOTE] : Behavioral methods are still invoked.

                try
                {
                    return InvokeMethodCall(methodExpression);
                }
                catch (TargetInvocationException tie)
                {
                    Logger.Log(tie, Microsoft.Extensions.Logging.LogLevel.Critical);
                }
            }
            return null;
        }

        private object InvokeMethodCall(MethodCallExpression methodCall)
        {
            return Expression.Lambda(methodCall).Compile().DynamicInvoke();
        }

        private object InvokeConstructor(NewExpression node)
        {
            int argCount = node.ArgumentCount();

            object[] arguments = argCount > 0 ? new object[argCount] : Type.EmptyTypes;

            for (int i = 0; i < argCount; i++)
            {
                arguments[i] = EvaluateArgument(node.GetArgument(i));
            }

            return Activator.CreateInstance(node.Type, arguments);
        }

        private object GetConstantValue(ConstantExpression constant)
        {
            return constant.Value;
        }

        private object GetMemberAccessValue(MemberExpression member)
        {
            if (member.Expression != null)
            {
                if (IsDateTime(member.Expression))
                {
                    object returnVal = default(object);

                    if (member.Expression.NodeType == ExpressionType.New)
                    {
                        DateTime instance = (DateTime)InvokeConstructor((NewExpression)member.Expression);

                        FieldInfo field = typeof(DateTime).GetField(member.Member.Name);
                        PropertyInfo property = typeof(DateTime).GetProperty(member.Member.Name);

                        if (property != null)
                        {
                            returnVal = property.GetValue(instance);
                        }
                        else if (field != null)
                        {
                            returnVal = field.GetValue(instance);
                        }
                    }
                    if (returnVal != default(object))
                    {
                        whereBuilder
                            .Append("DateTime('")
                            .Append(returnVal.ToString())
                            .Append("')");
                    }
                    return '\t';
                }
                else
                {
                    FieldInfo field = member.Expression.Type.GetRuntimeField(member.Member.Name);
                    if (field != null)
                    {
                        return field.GetValue(
                            member.Expression.GetType().GetProperty("Value").GetValue(member.Expression)
                        );
                    }
                }
            }
            return null;
        }

        // Not used right now
        private bool IsBool(Expression node)
        {
            return node.Type == typeof(bool) || node.Type == typeof(bool?);
        }

        private bool IsDateTime(Expression node)
        {
            return node.Type == typeof(DateTime) || node.Type == typeof(DateTime?);
        }

        // Not used right now
        private bool IsValidEntity(object entity)
        {
            return currentContext.Model.FindEntityType(entity.GetType().ToString()) != null;
        }

        /* ********************************************************************************************************
         * ---------------------------                SUPPORTED CHECKERS              --------------------------- *
         ******************************************************************************************************** */

        private bool IsSupportedAggregate(string methodName)
        {
            return supportedAggregates.Contains(methodName.ToLower());
        }

        private bool IsSupportedQueryMethod(string methodName)
        {
            return supportedQueryMethods.Contains(methodName.ToLower());
        }

        private bool IsSupportedMiscellaneous(string methodName)
        {
            return supportedMiscellaneous.ContainsKey(methodName.ToLower());
        }

        /* ********************************************************************************************************
         * -------------------------                UNSUPPORTED CHECKERS              --------------------------- *
         ******************************************************************************************************** */

        private bool IsUnsupportedAggregate(string methodName)
        {
            return unsupportedAggregates.Contains(methodName.ToLower());
        }

        private bool IsUnsupportedQueryMethod(string methodName)
        {
            return unsupportedQueryMethods.Contains(methodName.ToLower());
        }

        private bool IsUnsupportedMiscellaneous(string methodName)
        {
            return unsupportedMiscellaneous.Contains(methodName.ToLower());
        }

        /* ********************************************************************************************************
         * ----------------------------                OTHER CHECKERS              ------------------------------ *
         ******************************************************************************************************** */

        private bool IsAndAble(string methodName)
        {
            return andAble.Contains(methodName.ToLower());
        }

        /* ********************************************************************************************************
         * ----------------------                SOME HELPING METHODS AGAIN              ------------------------ *
         ******************************************************************************************************** */

        private StringBuilder PadPossibleStringValueToWhere(object value)
        {
            if (value is string)
            {
                whereBuilder
                    .Append("'")
                    .Append(value.ToString())
                    .Append("'");
            }
            else
            {
                whereBuilder
                    .Append(value.ToString());
            }
            return whereBuilder;
        }

        // Not used right now
        private void Add<T>(List<T> list, params T[] elems)
        {
            foreach (T elem in elems)
            {
                list.Add(elem);
            }
        }

        // Not used right now
        private void Add<T1, T2>(Dictionary<T1, T2> dictionary, params KeyValuePair<T1, T2>[] elems)
        {
            foreach (KeyValuePair<T1, T2> elem in elems)
            {
                dictionary.Add(elem.Key, elem.Value);
            }
        }

        private string BuildFunction(FunctionInformation information, out string whereCriteria)
        {
            StringBuilder builder = new StringBuilder();

            whereCriteria = information.WhereContribution;

            switch (information.Type)
            {
                case FunctionInformation.FunctionType.None:
                    //
                    // Do nothing
                    //
                    break;
                case FunctionInformation.FunctionType.Aggregate:
                    builder.Append(information.Name).Append("(");

                    for (int i = 0; i < information.MeaningfulArguments.Length; i++)
                    {
                        object arg = information.MeaningfulArguments[i];
                        builder.Append(i > 0 ? Comma + Space : "");
                        builder.Append(arg.ToString());
                    }
                    builder.Append(")");
                    break;
                case FunctionInformation.FunctionType.QueryMethod:
                    //
                    // It would would seem that nothing needs to be done here as of yet
                    //
                    break;
                case FunctionInformation.FunctionType.Miscellaneous:

                    for (int i = 0; i < information.MeaningfulArguments.Length; i++)
                    {
                        object arg = information.MeaningfulArguments[i];
                        builder.Append(i > 0 ? Comma + Space : "");
                        builder.Append("this").Append(Dot).Append(arg.ToString());
                    }
                    break;
            }
            return builder.ToString();
        }

        private void InjectMiscellaneousFunctions(StringBuilder oqlStringBuilder)
        {
            if (miscellaneousOperations.Count > 0)
            {
                // Think of a not so hardcoded implementation for the following code.

                if (miscellaneousOperations.ContainsKey("groupby"))
                {
                    oqlStringBuilder
                        .Append(supportedMiscellaneous["groupby"])
                        .Append(Space)
                        .Append(miscellaneousOperations["groupby"])
                        .Append(Space);

                    int index = oqlStringBuilder.ToString().IndexOf("SELECT" + Space) + ("SELECT" + Space).Length;

                    Logger.Log("Injecting GroupBy.", Microsoft.Extensions.Logging.LogLevel.Trace);
                    Logger.Log(
                        "Injecting '" + (miscellaneousOperations["groupby"] + Comma + Space) + "' at index : " + index,
                        Microsoft.Extensions.Logging.LogLevel.Debug
                    );

                    oqlStringBuilder.Insert(index, miscellaneousOperations["groupby"] + Comma + Space);
                }

                if (miscellaneousOperations.ContainsKey("orderby"))
                {
                    int index = oqlStringBuilder.ToString().IndexOf("SELECT" + Space) + ("SELECT" + Space).Length;

                    Logger.Log("Injecting OrderBy.", Microsoft.Extensions.Logging.LogLevel.Trace);

                    if (miscellaneousOperations.ContainsKey("groupby"))
                    {
                        if (!miscellaneousOperations["groupby"].Equals(miscellaneousOperations["orderby"]))
                        {
                            Logger.Log(
                                "Injecting '" + (miscellaneousOperations["orderby"] + Comma + Space) + "' at index : " + index,
                                Microsoft.Extensions.Logging.LogLevel.Debug
                            );

                            oqlStringBuilder.Insert(index, miscellaneousOperations["orderby"] + Comma + Space);
                        }
                        else
                        {
                            Logger.Log(
                                "GroupBy and OrderBy have been applied on the same column.",
                                Microsoft.Extensions.Logging.LogLevel.Debug
                            );
                        }
                    }
                    else
                    {
                        // An exception needs to be thrown here for using order by without group by.
                        // The code however, has been through a lot of checks to get at this stage.
                        // So it is safe even to not throw an exception here as this part of the code 
                        // will only be reached IF order by was used with group by.
                        //
                        // If the checking mechanism is changed in the future, an exception will need 
                        // to be thrown here for safety.

                        Logger.Log("Injecting OrderBy without GroupBy.", Microsoft.Extensions.Logging.LogLevel.Critical);
                        Logger.Log(
                            "Injecting '" + (miscellaneousOperations["orderby"] + Comma + Space) + "' at index : " + index + " without GroupBy",
                            Microsoft.Extensions.Logging.LogLevel.Critical
                        );

                        oqlStringBuilder.Insert(index, miscellaneousOperations["orderby"] + Comma + Space);
                    }

                    oqlStringBuilder
                        .Append(supportedMiscellaneous["orderby"])
                        .Append(Space)
                        .Append(miscellaneousOperations["orderby"])
                        .Append(Space);
                }

                if (miscellaneousOperations.ContainsKey("orderbydescending"))
                {
                    oqlStringBuilder
                        .Append(supportedMiscellaneous["orderbydescending"])
                        .Append(Space)
                        .Append(miscellaneousOperations["orderbydescending"])
                        .Append(Space)
                        .Append("DESC")
                        .Append(Space);

                    Logger.Log("Injecting OrderByDescending.", Microsoft.Extensions.Logging.LogLevel.Trace);

                    int index = oqlStringBuilder.ToString().IndexOf("SELECT" + Space) + ("SELECT" + Space).Length;

                    if (miscellaneousOperations.ContainsKey("groupby"))
                    {
                        if (!miscellaneousOperations["groupby"].Equals(miscellaneousOperations["orderbydescending"]))
                        {
                            Logger.Log(
                                "Injecting '" + (miscellaneousOperations["orderbydescending"] + Comma + Space) + "' at index : " + index,
                                Microsoft.Extensions.Logging.LogLevel.Debug
                            );

                            oqlStringBuilder.Insert(index, miscellaneousOperations["orderbydescending"] + Comma + Space);
                        }
                        else
                        {
                            Logger.Log(
                                "GroupBy and OrderByDescending have been applied on the same column.",
                                Microsoft.Extensions.Logging.LogLevel.Debug
                            );
                        }
                    }
                    else if (miscellaneousOperations.ContainsKey("orderby"))
                    {
                        Logger.Log("Injecting OrderByDescending without GroupBy.", Microsoft.Extensions.Logging.LogLevel.Critical);

                        if (!miscellaneousOperations["orderby"].Equals(miscellaneousOperations["orderbydescending"]))
                        {
                            Logger.Log(
                                "Injecting '" + (miscellaneousOperations["orderbydescending"] + Comma + Space) + "' at index : " + index + " without GroupBy.",
                                Microsoft.Extensions.Logging.LogLevel.Critical
                            );

                            oqlStringBuilder.Insert(index, miscellaneousOperations["orderbydescending"] + Comma + Space);
                        }
                        else
                        {
                            Logger.Log(
                                "OrderBy and OrderByDescending have been applied on the same column without GroupBy.",
                                Microsoft.Extensions.Logging.LogLevel.Critical
                            );
                        }
                    }
                    else
                    {
                        Logger.Log("Injecting OrderByDescending without GroupBy or OrderBy.", Microsoft.Extensions.Logging.LogLevel.Critical);
                        Logger.Log(
                            "Injecting '" + (miscellaneousOperations["orderbydescending"] + Comma + Space) + "' at index : " + index + " without GroupBy or OrderBy.",
                            Microsoft.Extensions.Logging.LogLevel.Critical
                        );

                        oqlStringBuilder.Insert(index, miscellaneousOperations["orderbydescending"] + Comma + Space);
                    }
                }
            }
        }
    }
}
