// Copyright (c) 2017 Alachisoft
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Data.Common;
using Alachisoft.NCache.Web.Caching;
using System.Collections;

namespace Alachisoft.NCache.Linq
{
    internal class NCacheExpressionParser : ExpressionVisitor
    {
        private readonly NCacheExpressionParser outerStatement = null;

        private AggregateType aggregateType = AggregateType.None;

        private readonly StringBuilder sb = new StringBuilder();

        private readonly int indentLevel = -1;

        private SelectHandler selectHandler = null;

        private WhereHandler whereHandler = null;

        private readonly Queue<MethodCallExpression> queryableMethods = new Queue<MethodCallExpression>();

        private NCacheExpressionParser()
            : this(-1, null)
        {
        }

        private NCacheExpressionParser(int indentLevel)
            : this(indentLevel, null)
        {
        }

        private NCacheExpressionParser(int indentLevel, NCacheExpressionParser outerStatement)
            : this(indentLevel, outerStatement, AggregateType.None)
        {
        }

        private NCacheExpressionParser(int indentLevel, NCacheExpressionParser outerStatement,
                                   AggregateType aggregateType)
        {

            this.indentLevel = indentLevel;
            this.outerStatement = outerStatement;
            this.aggregateType = aggregateType;
        }

        private static NCacheExpressionParser GetNCacheExpressionParser(Expression expression)
        {
           var ncacheExpressionParser = new NCacheExpressionParser();

            ncacheExpressionParser.Translate(expression);

            return ncacheExpressionParser;
        }

        internal static object ExecuteExpression(Cache cache, Expression expression)
        {
            var x = Evaluator.PartialEval(expression);
            return GetNCacheExpressionParser(expression).Execute(cache, expression);
        }

        private object Execute(Cache cache, Expression expression)
        {
            LambdaExpressionHandler lambdaHandler = null;
            Hashtable queryValues = null; 
            if (selectHandler.IsAggregate)
            {
                var selector = selectHandler.Selector;
                if (whereHandler != null)
                    queryValues = whereHandler.QueryValues;
                AggregateExecutor executor = (AggregateExecutor)(Activator.CreateInstance(
                                                typeof(AggregateExecutor),
                                                cache, this, expression.Type, whereHandler == null ? null : queryValues));
                return executor.GetResult();
            }
            else
            {
                if (expression.NodeType == ExpressionType.Constant)
                {
                    return Activator.CreateInstance(
                                     typeof(ConstantEnumerable<>).MakeGenericType(expression.Type));
                }

                var selector = selectHandler.Selector;
                if (whereHandler != null)
                    queryValues = whereHandler.QueryValues;
                var executor = Activator.CreateInstance(
                                                typeof(Executor<>).MakeGenericType(selector.Body.Type),
                                                cache, this, Evaluator.PartialEval(expression), selectHandler.ClassType, selectHandler.PropertyToReturn, queryValues);

                if (queryableMethods.Count == 0)
                {
                    return executor;
                }

                var result = ExecuteQueryableMethod(executor,
                                              new Stack<MethodCallExpression>(queryableMethods),
                                              selector.Body.Type);

                return result;
            }
        }

        private object ExecuteQueryableMethod(object executor,
                                              Stack<MethodCallExpression> queryableMethods,
                                              Type executorSourceType)
        {

            var queryableMethod = queryableMethods.Pop();

            var queryableExecutor = GetQueryableExecutor(executor, executorSourceType,
                                                         queryableMethod);

            var source = Queryable.AsQueryable((System.Collections.IEnumerable)executor);

            if (queryableMethods.Count == 0)
            {
                return queryableExecutor.DynamicInvoke(source);
            }

            return ExecuteQueryableMethod(queryableExecutor.DynamicInvoke(source),
                                          queryableMethods,
                                          queryableMethod.Type.GetGenericArguments()[0]);
        }

        private static Delegate GetQueryableExecutor(object executor, Type executorSourceType, MethodCallExpression queryableMethod)
        {

            var args = queryableMethod.Arguments.Where((arg, index) => index != 0).ToArray();

            var key = queryableMethod.Type.GUID +
                      queryableMethod.Arguments[0].Type.GUID.ToString() +
                      queryableMethod.Method.Name +
                      string.Join(string.Empty,
                                 (from arg in args
                                  select arg.ToString()).ToArray());


            Type sourceType = QueryableMethodsProvider.GetQueryableType(executorSourceType);

            var queryableArgs = new Expression[args.Length + 1];

            var source = Expression.Parameter(sourceType, "source");

            queryableArgs[0] = source;

            for (int i = 0; i < args.Length; i++)
            {
                queryableArgs[i + 1] = args[i];
            }

            var queryableExecutor = Expression.Lambda(Expression.Call(queryableMethod.Method,
                                                                      queryableArgs),
                                                      source);

            var result = queryableExecutor.Compile();
            return result;
        }

        private string Translate(Expression expression)
        {

            if (expression.NodeType == ExpressionType.Constant &&
                (expression as ConstantExpression).Type != typeof(object))
            {
                return string.Empty;
            }

            if (sb.Length != 0)
            {
                return sb.ToString();
            }

            this.Visit(Evaluator.PartialEval(expression));

            EmitSelectStatement();
            return sb.ToString();
        }

        bool expectingSelect = false;

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {

            if (m.Method.DeclaringType == typeof(Queryable))
            {

                switch (m.Method.Name)
                {
                    case "Max":
                        if (m.Arguments.Count > 1)
                        {
                            selectHandler =
                            SelectHandler.GetSelectHandler(indentLevel + 1, m, AggregateType.Max);
                            if (selectHandler == null)
                            {
                                this.queryableMethods.Enqueue(m);
                            }
                        }
                        else
                        {
                            aggregateType = AggregateType.Max;
                            expectingSelect = true;
                        }
                        this.Visit(m.Arguments[0]);
                        break;
                    case "Min":
                        if (m.Arguments.Count > 1)
                        {
                            selectHandler =
                            SelectHandler.GetSelectHandler(indentLevel + 1, m, AggregateType.Min);
                            if (selectHandler == null)
                            {
                                this.queryableMethods.Enqueue(m);
                            }
                        }
                        else
                        {
                            aggregateType = AggregateType.Min;
                            expectingSelect = true;
                        }
                        
                        this.Visit(m.Arguments[0]);
                        break;
                    case "Count":
                        selectHandler =
                              SelectHandler.GetSelectHandlerForCount(m);
                        if (selectHandler == null)
                        {
                            this.queryableMethods.Enqueue(m);
                        }
                        aggregateType = AggregateType.Count;
                        this.Visit(m.Arguments[0]);
                        break;
                    case "Average":
                        if (m.Arguments.Count > 1)
                        {
                            selectHandler =
                                SelectHandler.GetSelectHandler(indentLevel + 1, m, AggregateType.Average);
                            if (selectHandler == null)
                            {
                                this.queryableMethods.Enqueue(m);
                            }
                        }
                        else
                        {
                            aggregateType = AggregateType.Average;
                            expectingSelect = true;
                        }
                        
                        this.Visit(m.Arguments[0]);
                        break;

                    case "Sum":
                        if (m.Arguments.Count > 1)
                        {
                            selectHandler =
                            SelectHandler.GetSelectHandler(indentLevel + 1, m, AggregateType.Sum);
                            if (selectHandler == null)
                            {
                                this.queryableMethods.Enqueue(m);
                            }
                        }
                        else
                        {
                            aggregateType = AggregateType.Sum;
                            expectingSelect = true;
                        }
                       
                        this.Visit(m.Arguments[0]);
                        break;

                    case "Select":
                        expectingSelect = false;
                        selectHandler =
                            SelectHandler.GetSelectHandler(indentLevel + 1, m, aggregateType);
                        if (selectHandler == null)
                        {
                            this.queryableMethods.Enqueue(m);
                        }
                        this.Visit(m.Arguments[0]);
                        break;

                    case "Where":
                        int parameterBaseIndex = outerStatement == null ? 0 : outerStatement.ParameterCount;
                        WhereHandler handler = WhereHandler.GetWhereHandler(indentLevel + 1, m, parameterBaseIndex);
                        if (handler == null)
                        {
                            this.queryableMethods.Enqueue(m);
                        }
                        else
                        {
                            if (whereHandler == null)
                            {
                                whereHandler = handler;
                            }
                            else
                            {
                                whereHandler.Merge(handler);
                            }
                        }
                        this.Visit(m.Arguments[0]);
                        break;
                    case "OrderBy":
                    case "OrderByDescending":
                    case "ThenBy":
                    case "ThenByDescending":
                    case "Join":
                        throw new NotSupportedException(string.Format("The method '{0}' is not supported", m.Method.Name));
                    default:
                        queryableMethods.Enqueue(m);
                        this.Visit(m.Arguments[0]);
                        break;
                }
            }
            else
            {

                throw new NotSupportedException(string.Format("The method '{0}' is not supported", m.Method.Name));
            }

            if (expectingSelect)
            {
                throw new NotSupportedException("The query syntax is not supported.");
            }
            return m;
        }

        private void EmitSelectStatement()
        {

            GetSelectClause();

            GetWhereClause(false);

        }

        private void GetSelectClause()
        {
            
            InitSelectHandler();

            sb.Append(selectHandler.GetSelectClause(true));
            return;
        }

        private void GetWhereClause(bool hasJoinClause)
        {

            if (whereHandler != null)
            {

                if (selectHandler != null)
                {
                    try
                    {
                        sb.Append(selectHandler.ReplaceAliases(whereHandler.GetWhereClause(false)));
                    }
                    catch (NullReferenceException ex)
                    {
                        sb.Append(whereHandler.GetWhereClause(false));
                    }
                    return;
                }
            }
        }

        private bool IsTopLevelOrderBy()
        {
            return false;
        }

        private void GetTableAlias()
        {
            sb.Append(" AS " + GetTableAlias(indentLevel));
            sb.Append(Environment.NewLine);

        }

        private static bool IsAggregateMethod(MethodCallExpression m)
        {

            if (m.Method.DeclaringType != typeof(Queryable) &&
                m.Method.DeclaringType != typeof(Enumerable))
            {

                return false;
            }

            switch (m.Method.Name)
            {

                case "Count":
                case "Average":
                case "Max":
                case "Min":
                case "Sum":
                    return true;
                default:
                    return false;
            }
        }

        private static string GetTableAlias(int indentLevel)
        {
            return "";
        }

        private void InitSelectHandler()
        {

            if (selectHandler != null)
            {
                return;
            }

            Type returnType = GetReturnType();
                       if (returnType == null)
            {
                throw new InvalidOperationException("Cannot translate statement");
            }

            selectHandler = SelectHandler.GetSelectHandler(indentLevel + 1, returnType);
        }

        private Type GetReturnType()
        {

            if (whereHandler != null)
            {
                return whereHandler.ReturnType;
            }
            return null;
        }

        private string GetTableName()
        {
            return GetTableName(selectHandler.ClassType);
        }

        private string GetNQLStatement()
        {
            return sb.ToString();
        }

        private int ParameterCount
        {
            get
            {
                if (whereHandler != null)
                {
                    return whereHandler.ParameterCount;
                }
                return 0;
            }
        }

        private static string GetIndentation(int indentLevel)
        {

            StringBuilder sb = new StringBuilder(indentLevel);

            for (int i = 0; i < indentLevel; i++)
            {
                sb.Append("\t");
            }

            return sb.ToString();
        }

        private static Expression StripQuotes(Expression e)
        {

            while (e.NodeType == ExpressionType.Quote)
            {
                e = ((UnaryExpression)e).Operand;
            }

            return e;
        }

        private static LambdaExpression GetLambdaExpression(Expression expression)
        {

            var selectorLambda = StripQuotes(expression) as LambdaExpression;

            if (selectorLambda == null)
            {

                var constantValue = (ConstantExpression)expression;

                selectorLambda = (LambdaExpression)constantValue.Value;
            }

            return selectorLambda;
        }

        private static string GetTableName(Type tableType)
        {
            return tableType.FullName;
        }






        private enum AggregateType
        {
            None,
            Count,
            Sum,
            Min,
            Max,
            Average
        }

        private class SelectHandler
        {

            private readonly int indentLevel;

            private readonly AggregateType aggregateType = AggregateType.None;

            private readonly Type returnType = null;

            private readonly Type classType = null;

            private readonly LambdaExpressionHandler lambdaHandler = null;

            private readonly LambdaExpression selector = null;

            private readonly string selectorExpression = null;

            private string _property;

            public string PropertyToReturn
            {
                get
                {
                    return _property;
                }
            }

            public int IndentationLevel
            {
                get
                {
                    return indentLevel;
                }
            }

            public Type ReturnType
            {
                get
                {
                    return returnType;
                }
            }

            public Type ClassType
            {
                get
                {
                    return classType;
                }
            }

            private SelectHandler(int indentLevel,
                                  MethodCallExpression expression, AggregateType aggregateType)
            {

                this.indentLevel = indentLevel;

                this.aggregateType = aggregateType;

                selector = GetLambdaExpression(expression.Arguments[1]);

                returnType = selector.Type.GetGenericArguments()[1];

                classType = selector.Parameters[0].Type;


                lambdaHandler = new LambdaExpressionHandler(indentLevel, selector, aggregateType);

                _property = lambdaHandler.Property;

                selectorExpression = lambdaHandler.GetExpressionAsString(true).ToString();
            }

            private SelectHandler(int indentLevel, Type returnType) :
                this(indentLevel,
                     QueryableMethodsProvider.GetSelectCall(returnType), AggregateType.None)
            {
            }
            //For Count()
            private SelectHandler(AggregateType aggregateType, MethodCallExpression m)
            {
                this.aggregateType = aggregateType;
                this.classType = m.Arguments[0].Type.GetGenericArguments()[0];
               
            }

            public static SelectHandler GetSelectHandler(int indentLevel,
                                                         MethodCallExpression expression,
                                                         AggregateType aggregateType)
            {

      

                var selector = GetLambdaExpression(expression.Arguments[1]).Parameters[0];

                if (selector.Type.Name == "IGrouping`2")
                {
                    return null;
                }

                SelectHandler selectHandler = new SelectHandler(indentLevel,
                                                                expression, aggregateType);

                return selectHandler;
            }

            public static SelectHandler GetSelectHandlerForCount(MethodCallExpression m)
            {
                SelectHandler selectHandler = new SelectHandler(AggregateType.Count, m);
                

                return selectHandler;
            }

            public static SelectHandler GetSelectHandler(int indentLevel, Type returnType)
            {
                return new SelectHandler(indentLevel, returnType);
            }

            public string GetSelectClause(bool emitTableAlias)
            {

                StringBuilder sb = new StringBuilder();

                sb.Append(GetIndentation(indentLevel));

                sb.Append("SELECT ");

                EmitAlias(emitTableAlias, sb);

                return sb.ToString();
            }

            private void EmitAlias(bool emitTableAlias, StringBuilder sb)
            {

                if (emitTableAlias)
                {
                    if (aggregateType == AggregateType.None)
                    {
                        sb.Append(GetTableName(classType));
                    }
                    else if (aggregateType == AggregateType.Count) 
                    {
                        sb.Append("COUNT(" + GetTableName(classType) + ")");
                    }
                    else
                    {
                        sb.Append(GetFields(GetTableName(classType)));
                    }
                }
            }

            private string GetFields(string tableAlias)
            {

                var accessedFields = lambdaHandler.GetAccessedFields();

                string fieldList = null;

                if (accessedFields.Length != 0)
                {
                    fieldList = GetFieldsFromSelector(accessedFields);
                }
                else
                {
                    fieldList = GetFieldsFromReturnType(tableAlias);
                    if (fieldList == "")
                        fieldList = "* ";
                }

                var aggregateExpression = ReplaceAliases(selectorExpression);

                switch (aggregateType)
                {
                    case AggregateType.Average:
                        return "AVG(" + tableAlias + "." +  aggregateExpression + ")";
                    case AggregateType.Count:
                        return "COUNT(" + tableAlias + ")";
                    case AggregateType.Max:
                        return "MAX(" + tableAlias + "." + aggregateExpression + ")";
                    case AggregateType.Min:
                        return "MIN(" + tableAlias + "." + aggregateExpression + ")";
                    case AggregateType.Sum:
                        return "SUM(" + tableAlias + "." + aggregateExpression + ")";
                    default:
                        throw new InvalidOperationException();
                }
            }

            private string GetFieldsFromReturnType(string tableAlias)
            {

                var separator = string.Empty;

                if (tableAlias != string.Empty)
                {
                    separator = ".";
                }


                return string.Join(", ", (from property in returnType.GetProperties()
                                          where property.PropertyType.IsValueType ||
                                                property.PropertyType == typeof(string)
                                          orderby property.Name
                                          select tableAlias + separator + property.Name)
                                      .ToArray());
            }

            private string GetFieldsFromSelector(string[] fields)
            {

                return ReplaceAliases(string.Join(", ", fields));
            }

            public string ReplaceAliases(string expression)
            {
                StringBuilder sb = new StringBuilder(lambdaHandler.ReplaceAliases(expression));
                return sb.ToString();
            }

            public LambdaExpression Selector
            {
                get
                {
                    return selector;
                }
            }

            public bool IsAggregate
            {
                get { return !(this.aggregateType == AggregateType.None); }
            }
        }

        private class WhereHandler
        {

            private readonly Type returnType = null;

            private readonly int indentLevel;

            private readonly List<LambdaExpressionHandler> lambdaHandler = new List<LambdaExpressionHandler>();

            public Type ReturnType
            {
                get
                {
                    return returnType;
                }
            }

            private WhereHandler(int indentLevel,
                                 MethodCallExpression expression,
                                 int parameterBaseIndex)
            {

                this.indentLevel = indentLevel;

                returnType = expression.Arguments[0].Type.GetGenericArguments()[0];

                Expression e = StripQuotes(expression.Arguments[1]);

                LambdaExpression lambda = GetLambdaExpression(expression.Arguments[1]);

                lambdaHandler.Add(new LambdaExpressionHandler(indentLevel,
                                                            lambda,
                                                            parameterBaseIndex));
            }

            public static WhereHandler GetWhereHandler(int indentLevel,
                                                       MethodCallExpression expression,
                                                       int parameterBaseIndex)
            {

                var selector = GetLambdaExpression(expression.Arguments[1]).Parameters[0];

                if (selector.Type.Name == "IGrouping`2")
                {
                    return null;
                }

                return new WhereHandler(indentLevel, expression, parameterBaseIndex);
            }

            public void Merge(WhereHandler handler)
            {
                lambdaHandler.Add(handler.lambdaHandler[0]);
            }

            public string GetWhereClause(bool replaceAliases)
            {
                StringBuilder builder = new StringBuilder(" WHERE ");
                for (int i = 0; i < lambdaHandler.Count; i++)
                {
                    builder.Append(lambdaHandler[i].GetExpressionAsString(replaceAliases));
                    if (i < lambdaHandler.Count - 1)
                    {
                        builder.Append(" AND ");
                    }
                }
                return builder.ToString();
            }

            public Hashtable QueryValues 
            {
                get
                {
                    Hashtable vals = new Hashtable();
                    foreach (LambdaExpressionHandler handler in lambdaHandler)
                    {
                        foreach (DictionaryEntry entry in handler.QueryValues)
                        {
                            vals.Add(entry.Key, entry.Value);
                        }
                    }
                    return vals;
                }
            }

            public int ParameterCount
            {
                get
                {
                    int paramCount = 0;
                    foreach (LambdaExpressionHandler handler in lambdaHandler)
                    {
                        paramCount += handler.ParameterCount;
                    }
                    return paramCount;
                }
            }
        }

        private class LambdaExpressionHandler : ExpressionVisitor
        {
            private Type type;
            //@UH
            private readonly AggregateType aggregateType = AggregateType.None;

            private readonly LambdaExpression lambdaExpression = null;

            private readonly Guid lambaExpressionId = Guid.Empty;

            private readonly int indentLevel;

            private readonly Dictionary<string, string> aliases = new Dictionary<string, string>();

            private readonly List<string> accessedColumns = new List<string>();

            private readonly Stack<Expression> terms = new Stack<Expression>();

            private StringBuilder sb = new StringBuilder();

            private Hashtable queryValues = new Hashtable();

            public object _queryValueLock = new object();

            private int parameterCount = 0;

            private string _property;

            public Hashtable QueryValues
            {
                get { return queryValues; }
            }
            public string Property
            {
                get { return _property; }
                private set { _property = value; }
            }

            public int ParameterCount
            {
                get
                {
                    return parameterCount;
                }
            }

            public LambdaExpressionHandler(int indentLevel, LambdaExpression lambdaExpression)
                : this(indentLevel, lambdaExpression, 0, AggregateType.None)
            {
            }

            public LambdaExpressionHandler(int indentLevel, LambdaExpression lambdaExpression, int parameterBaseIndex)
                : this(indentLevel, lambdaExpression, parameterBaseIndex, AggregateType.None)
            {
            }

            public LambdaExpressionHandler(int indentLevel, LambdaExpression lambdaExpression, AggregateType aggregateType)
                : this(indentLevel, lambdaExpression, 0, aggregateType)
            {
            }

            public LambdaExpressionHandler(int indentLevel, LambdaExpression lambdaExpression,
                                           int parameterBaseIndex, AggregateType aggregateType)
            {

                this.indentLevel = indentLevel;

                this.lambdaExpression = lambdaExpression;

                this.parameterCount = parameterBaseIndex;

                this.aggregateType = aggregateType;

                lambaExpressionId = lambdaExpression.Body.Type.GUID;

                this.Visit(lambdaExpression);

                GetExpressionAsString(false);
            }

            protected override Expression VisitMethodCall(MethodCallExpression m)
            {

                this.Visit(m.Object);

                this.VisitExpressionList(m.Arguments);

                terms.Push(m);

                return m;
            }

            protected override Expression VisitUnary(UnaryExpression u)
            {

                if (u.NodeType == ExpressionType.Quote)
                {
                    return this.Visit(StripQuotes(u));
                }
                terms.Push(u);

                return u;
            }

            protected override Expression VisitBinary(BinaryExpression b)
            {

                this.Visit(b.Left);

                this.Visit(b.Right);

                terms.Push(b);

                return b;
            }

            protected override Expression VisitConstant(ConstantExpression c)
            {
                terms.Push(c);
                return c;
            }

            protected override Expression VisitParameter(ParameterExpression p)
            {

                terms.Push(p);

                return p;

            }

            protected override Expression VisitMemberAccess(MemberExpression m)
            {

                // lambdaExpression.Parameters[0].Type may look like
                // <>f__AnonymousType0`2[[Order],[Customer]]
                // as a result of a join 
                // we need to check for generic parameters
                var genericParameters = lambdaExpression.Parameters[0].Type.GetGenericArguments();

                if ((m.Member.DeclaringType == lambdaExpression.Parameters[0].Type ||
                     genericParameters.Contains(m.Member.DeclaringType))
                    && (m.Type.IsValueType || m.Type == typeof(string)))
                {
                    accessedColumns.Add(GetHashedName(m));
                }
                terms.Push(m);
                return m;
            }

            protected override NewExpression VisitNew(NewExpression newExpression)
            {

                foreach (var argument in newExpression.Arguments)
                {
                    this.Visit(argument);
                }

                terms.Push(newExpression);

                return newExpression;
            }

            protected override ElementInit VisitElementInitializer(ElementInit initializer)
            {

                throw new InvalidOperationException();

            }

            protected override Expression VisitTypeIs(TypeBinaryExpression b)
            {

                throw new InvalidOperationException();


            }

            protected override Expression VisitConditional(ConditionalExpression c)
            {
                if ((bool)(c.Test as ConstantExpression).Value == true)
                {
                    terms.Push(c.IfTrue);
                    return c.IfTrue;
                }

                terms.Push(c.IfFalse);
                return c.IfFalse;
            }

            protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding)
            {

                throw new InvalidOperationException();

            }

            protected override MemberListBinding VisitMemberListBinding(MemberListBinding binding)
            {

                throw new InvalidOperationException();

            }

            protected override IEnumerable<MemberBinding> VisitBindingList(ReadOnlyCollection<MemberBinding> original)
            {

                throw new InvalidOperationException();

            }

            protected override IEnumerable<ElementInit> VisitElementInitializerList(ReadOnlyCollection<ElementInit> original)
            {

                throw new InvalidOperationException();

            }

            protected override Expression VisitMemberInit(MemberInitExpression init)
            {

                throw new InvalidOperationException();

            }

            protected override Expression VisitListInit(ListInitExpression init)
            {

                throw new InvalidOperationException();

            }

            protected override Expression VisitNewArray(NewArrayExpression na)
            {

                throw new InvalidOperationException();

            }

            protected override Expression VisitInvocation(InvocationExpression iv)
            {

                throw new InvalidOperationException();

            }

            public StringBuilder GetExpressionAsString(bool replaceAliases)
            {

                EvaluateTerms();

                var result = sb.ToString();

                return new StringBuilder(ReplaceAliases(result, replaceAliases));
            }

            private void EvaluateTerms()
            {

                if (sb.Length > 0)
                {
                    // terms have already been evaluated
                    return;
                }

                while (terms.Count > 0)
                {

                    GetExpression();

                    if (terms.Count == 1 && terms.Peek().NodeType == ExpressionType.Constant)
                    {
                        break;
                    }

                    if (terms.Count > 1 && terms.Peek().NodeType == ExpressionType.Constant)
                    {
                        GetOperandValue();
                    }
                }

                sb = new StringBuilder((terms.Pop() as ConstantExpression).Value.ToString());
            }

            private void GetExpression()
            {

                var op = StripQuotes(terms.Pop());
                type = op.Type;
                switch (op.NodeType)
                {
                    case ExpressionType.And:
                    case ExpressionType.AndAlso:
                        GetBinaryOperation(" AND ");
                        break;
                    case ExpressionType.Or:
                    case ExpressionType.OrElse:
                        GetBinaryOperation(" OR ");
                        break;
                    case ExpressionType.Equal:
                        GetBinaryOperation(" = ");
                        break;
                    case ExpressionType.NotEqual:
                        GetBinaryOperation(" <> ");
                        break;
                    case ExpressionType.LessThan:
                        GetBinaryOperation(" < ");
                        break;
                    case ExpressionType.LessThanOrEqual:
                        GetBinaryOperation(" <= ");
                        break;
                    case ExpressionType.GreaterThan:
                        GetBinaryOperation(" > ");
                        break;
                    case ExpressionType.GreaterThanOrEqual:
                        GetBinaryOperation(" >= ");
                        break;
                    /*case ExpressionType.ExclusiveOr:*/
                    case ExpressionType.Add:
                        GetBinaryOperation(" + ");
                        break;
                    case ExpressionType.Subtract:
                        GetBinaryOperation(" - ");
                        break;
                    case ExpressionType.Multiply:
                        GetBinaryOperation(" * ");
                        break;
                    case ExpressionType.Divide:
                        GetBinaryOperation(@" \ ");
                        break;
                    case ExpressionType.Modulo:
                        GetBinaryOperation(" % ");
                        break;
                    case ExpressionType.Not:
                        GetUnaryExpression(" NOT ");
                        break;
                    /**/
                    /*case ExpressionType.Coalesce:*/
                    case ExpressionType.Convert:
                        GetConversion(op as UnaryExpression);
                        break;
                    case ExpressionType.Lambda:
                        GetLambda(op as LambdaExpression);
                        break;
                    case ExpressionType.New:
                        GetNew(op as NewExpression);
                        break;
                    case ExpressionType.MemberAccess:
                        GetMemberAccess(op as MemberExpression);
                        break;
                    case ExpressionType.Parameter:
                        GetParameterValue(op as ParameterExpression);
                        break;
                    case ExpressionType.Constant:
                        GetConstantValue(op as ConstantExpression);
                        break;
                    case ExpressionType.Call:
                        GetMethodCall(op as MethodCallExpression);
                        break;
                    default:
                        throw new NotSupportedException(
                            string.Format("The operator '{0}' is not supported", op.NodeType));
                }

            }

            private void GetUnaryExpression(string op)
            {
                string unaryOperand = GetUnaryOperand();
                terms.Push(Expression.Constant(
                                new BoxedConstant(op + " (" + unaryOperand + ")"))
                          );
            }

            private void GetBinaryOperation(string op)
            {

                string rightOperand;

                string leftOperand;

                GetBinaryOperands(out rightOperand, out leftOperand);
                
                if (!leftOperand.Contains("?"))
                {
                    if (rightOperand.StartsWith("this."))
                        rightOperand = rightOperand.Replace("this.", "");

                    lock (this._queryValueLock)
                    {
						if (!queryValues.Contains(rightOperand))
						{
                            if (!type.FullName.Equals("System.Char"))
                                queryValues.Add(rightOperand, Convert.ChangeType(leftOperand, type));
                            else
                            {
                                char value=Convert.ToChar(Int32.Parse(leftOperand));
                                queryValues.Add(rightOperand, value);
                            }
						}
						else
						{
							ArrayList arrayList;
							object previousValue= queryValues[rightOperand];
							if(previousValue is ArrayList)
							{
								arrayList = (ArrayList)previousValue;
								object[] arrayItems = arrayList.ToArray();
								arrayList.Clear();
								arrayList.Add(Convert.ChangeType(leftOperand, type));
								arrayList.AddRange(arrayItems);
							}
							else
							{
								arrayList = new ArrayList();
								arrayList.Add(Convert.ChangeType(leftOperand, type));
								arrayList.Add(previousValue);
								queryValues.Remove(rightOperand);
								queryValues.Add(rightOperand, arrayList);
							}
						}
                    }
                    rightOperand = "this." + rightOperand;
                    leftOperand = "?";

                }

                terms.Push(Expression.Constant(
                                new BoxedConstant(rightOperand + op + leftOperand)
                           ));
            }

            private void GetLambda(LambdaExpression lambda)
            {

                if (lambda.Body.Type != typeof(void))
                {
                    terms.Push(Expression.Constant(
                                    new BoxedConstant(lambda.ToString()))
                               );
                }
            }

            private void GetConversion(UnaryExpression op)
            {

                switch (op.Type.Name)
                {
                    case "Boolean":
                    case "Char":
                    case "Enum":
                    case "Guid":
                    case "String":
                    case "DateTime":
                    case "Decimal":
                    case "Int16":
                    case "Int32":
                    case "Int64":
                    case "IntPtr":
                    case "UInt16":
                    case "UInt32":
                    case "UInt64":
                    case "UIntPtr":
                    case "Byte":
                    case "SByte":
                    case "Double":
                    case "Single":
                    case "Nullable`1":
                        //wrong
                        terms.Push(op.Operand);
                        break;
                    default:
                        throw new NotSupportedException(
                            string.Format("The conversion to '{0}' is not supported", op.Type.Name));
                }
            }

            private void GetConstantValue(ConstantExpression c)
            {

                if (Type.GetTypeCode(c.Value.GetType()) == TypeCode.Object)
                {
                    if (c.Value.GetType().Name.StartsWith("Query`1"))
                    {
                        terms.Push(Expression.Constant(
                            new BoxedConstant(
                                 GetTableName(c.Value.GetType().GetGenericArguments()[0])
                                            )));
                    }
                    else if (c.Value.GetType() == typeof(BoxedConstant))
                    {
                        terms.Push(Expression.Constant(
                                     ((BoxedConstant)c.Value).Expression));
                        return;
                    }
                }

                terms.Push(Expression.Constant(new BoxedConstant(c.Value.ToString())));

                parameterCount++;
            }

            private void GetParameterValue(ParameterExpression p)
            {
                terms.Push(Expression.Constant(p.Name));
            }

            private void GetMemberAccess(MemberExpression m)
            {

                if (m.Expression != null)
                {

                    terms.Push(Expression.Constant(
                               new BoxedConstant(GetHashedName(m))));
                    return;
                }

                terms.Push(Expression.Constant(
                               new BoxedConstant(string.Empty)));
            }

            private void GetMethodCall(MethodCallExpression m)
            {

                if (m.Method.DeclaringType == typeof(Queryable) ||
                    m.Method.DeclaringType == typeof(Enumerable))
                {
                    GetQueryableMethodCall(m);
                    return;
                }
                else if (m.Method.DeclaringType == typeof(string))
                {
                    GetStringMethodCall(m);
                    return;
                }

                throw new ArgumentException();
            }

            private void GetQueryableMethodCall(MethodCallExpression m)
            {


                object value = null;

                string leftOperand;
                string rightOperand;

                switch (m.Method.Name)
                {
                    case "Select":
                    case "Where":
                        GetBinaryOperands(out leftOperand, out rightOperand);
                        value = m.Method.Name.ToUpper();
                        break;
                    case "OrderBy":
                    case "OrderByDescending":
                    case "ThenBy":
                    case "ThenByDescending":
                        GetBinaryOperands(out leftOperand, out rightOperand);
                        value = m.Method.Name.ToUpper();
                        break;
                    case "Count":
                    case "Average":
                    case "Max":
                    case "Min":
                    case "Sum":
                        var x = GetSourceType(m);

                        if (x == lambdaExpression.Parameters[0].Type)
                        {
                            value = GetAggregate(m);
                        }
                        else
                        {
                            // no send the lamda to another LambdaExpressionHandler
                            value = m.Method.Name.ToUpper();
                        }
                        break;
                    default:
                        for (int i = 0; i < m.Arguments.Count; i++)
                        {
                            GetUnaryOperand();
                        }
                        value = m.Method.Name.ToUpper();
                        break;
                }
                terms.Push(Expression.Constant(
                              new BoxedConstant(value.ToString())));
            }

            private void GetStringMethodCall(MethodCallExpression m)
            {

                string value = string.Empty;

                string left;
                string right;
                string val;

                switch (m.Method.Name)
                {
                    case "StartsWith":
                        GetBinaryOperands(out left, out right);
               
                        {
                            queryValues.Add(left.Replace("this.", ""), right+"*");
                            right = "?";
                        }
                        value = left + " LIKE " + right ;
                        break;
                    case "EndsWith":
                        GetBinaryOperands(out left, out right);
                        {
                            queryValues.Add(left.Replace("this.", ""), "*" + right);
                            right = "?";
                        }
                        value = left + " LIKE " + right;
                        break;
                    case "Contains":
                        GetBinaryOperands(out left, out right);
                        {
                            queryValues.Add(left.Replace("this.", ""), "*" + right + "*");
                            right = "?";
                        }
                        value = left + " LIKE " + right;
                        break;
                    case "Equals":
                        GetBinaryOperands(out left, out right);
                        {
                            queryValues.Add(left.Replace("this.", ""), right);
                            right = "?";
                        }
                        value = left + " = " + right;
                        break;
                    case "Substring":
                        GetBinaryOperands(out left, out right);
                        val = GetOperandValue();
                        value = "Substring(" + val + ", " + left + ", " + right + ")";
                        break;
                    case "ToUpper":
                        val = GetOperandValue();
                        value = "Upper(" + val + ")";
                        break;
                    case "ToLower":
                        val = GetOperandValue();
                        value = "Lower(" + val + ")";
                        break;
                    default:
                        throw new ArgumentException();
                }

                terms.Push(Expression.Constant(
                               new BoxedConstant(value)));
            }

            private string GetCount(MethodCallExpression method)
            {

                GetOperandValue();

                var sourceType = method.Method.GetGenericArguments()[0];

                var declaringType = GetSourceType(method.Arguments[0]);

                var foreignKey = GetForeignKey(declaringType, method.Arguments[0].Type);

                var foreignKeyExpression = Expression.MakeMemberAccess(
                                                Expression.Parameter(sourceType, sourceType.Name),
                                                sourceType.GetProperty(foreignKey));

                var whereCondition = Expression.Equal(foreignKeyExpression,
                                        Expression.Constant(
                                            new BoxedConstant(GetTableAlias(indentLevel) + "." +
                                                              GetPrimaryKey(declaringType))));

                var whereCall = QueryableMethodsProvider.GetWhereCall(sourceType, "source", whereCondition);

                var selectCall = QueryableMethodsProvider.GetSelectCall(whereCall);

                NCacheExpressionParser projector = new NCacheExpressionParser(indentLevel + 1, null,
                                                         GetAggregateTypeFromName(method.Method.Name));

                projector.Translate(selectCall);

                accessedColumns.Add(GetProjectionSql(indentLevel, projector));

                return GetProjectionSql(indentLevel + 1, projector);
            }

            private string GetAggregate(MethodCallExpression method)
            {

                if (method.Arguments.Count == 1)
                {
                    return GetCount(method);
                }

                GetOperandValue();
                GetOperandValue();

                var accessLambda = (LambdaExpression)method.Arguments[1];

                var sourceType = accessLambda.Parameters[0].Type;

                if (sourceType != lambdaExpression.Parameters[0].Type
                    && accessLambda.Body.NodeType == ExpressionType.Call)
                {
                    return GetNestedAggregate(method);
                }

                var selectorParam = Expression.Parameter(sourceType,
                                                         accessLambda.Parameters[0].Name);

                var projectionSelector = Expression.Lambda(accessLambda.Body, selectorParam);

                var whereCall = GetCorrelation(method, sourceType);

                var selectCall = QueryableMethodsProvider.GetSelectCall(whereCall, projectionSelector);

                NCacheExpressionParser projector =
                    new NCacheExpressionParser(indentLevel + 1, null,
                                         GetAggregateTypeFromName(method.Method.Name));

                projector.Translate(selectCall);

                accessedColumns.Add(GetProjectionSql(indentLevel, projector));

                return GetProjectionSql(indentLevel + 1, projector);
            }

            private string GetNestedAggregate(MethodCallExpression method)
            {

                var accessLambda = (LambdaExpression)method.Arguments[1];

                var sourceType = accessLambda.Parameters[0].Type;
                var whereCall = GetCorrelation(method, sourceType);

                var sumCall = Expression.Call(
                                typeof(Queryable).GetMethods()
                                    .Where(m => m.Name == method.Method.Name &&
                                                m.ReturnType == method.Type &&
                                                m.GetParameters().Length == 2)
                                    .Single().MakeGenericMethod(sourceType),
                                    whereCall, accessLambda);

                var foreignKey = ((StripQuotes(whereCall.Arguments[1]) as LambdaExpression).Body
                                   as BinaryExpression).Left;

                var keyValueType = typeof(KeyValuePair<int, int>)
                                    .GetGenericTypeDefinition()
                                    .MakeGenericType(foreignKey.Type,
                                                     accessLambda.Body.Type);

                var keyValueConstructor =
                    keyValueType.GetConstructor(new Type[]{foreignKey.Type,
                                                           accessLambda.Body.Type});

                var newKeyValue = Expression.New(keyValueConstructor,
                                                 new Expression[]{foreignKey,
                                                                  sumCall
                                                                  },
                                                 new PropertyInfo[]{
                                                         keyValueType.GetProperty("Key"),
                                                         keyValueType.GetProperty("Value")
                                                     });

                var selectorParam = Expression.Parameter(sourceType, "source");

                var projectionSelector = Expression.Lambda(newKeyValue, selectorParam);

                var aggregateSelect = QueryableMethodsProvider.GetSelectCall(
                                            whereCall, projectionSelector);

                NCacheExpressionParser projector =
                    new NCacheExpressionParser(indentLevel + 1, null,
                                            AggregateType.None);

                projector.Translate(aggregateSelect);

                accessedColumns.Add(GetProjectionSql(indentLevel, projector));

                return GetProjectionSql(indentLevel, projector);
            }

            private MethodCallExpression GetCorrelation(MethodCallExpression method, Type sourceType)
            {

                var declaringType = lambdaExpression.Parameters[0].Type;

                BinaryExpression whereCondition = null;

                // if for example the declaring type looks like
                // <>f__AnonymousType0`2[[Order],[Customer]]
                // as a result of a join 
                // we need to correlate both order and customer
                var genericArguments = declaringType.GetGenericArguments();

                if (genericArguments.Length == 0)
                {

                    whereCondition = GetCorrelationCondition(method, sourceType, declaringType,
                                                             GetTableAlias(indentLevel) + ".");
                }
                else
                {
                    var theType = genericArguments
                                     .Where(t => t.GetProperties()
                                     .Any(p => p.PropertyType == method.Arguments[0].Type))
                                     .Single();

                    whereCondition = GetCorrelationCondition(method, sourceType, theType,
                                        GetTableAlias(indentLevel) + "." + theType.GUID + ".");
                }

                var whereCall = QueryableMethodsProvider.GetWhereCall(sourceType, "source", whereCondition);

                return whereCall;
            }

            private BinaryExpression GetCorrelationCondition(MethodCallExpression method,
                                                             Type sourceType,
                                                             Type declaringType,
                                                             string tableAlias)
            {

                var foreignKey = GetForeignKey(declaringType, method.Arguments[0].Type);

                var foreignKeyExpression = Expression.MakeMemberAccess(
                                                Expression.Parameter(sourceType, sourceType.Name),
                                                sourceType.GetProperty(foreignKey));

                var whereCondition = Expression.Equal(foreignKeyExpression,
                                        Expression.Constant(
                                            new BoxedConstant(tableAlias +
                                                              GetPrimaryKey(declaringType))));
                return whereCondition;
            }

            private AggregateType GetAggregateTypeFromName(string name)
            {

                switch (name)
                {
                    case "Count":
                        return AggregateType.Count;
                    case "Sum":
                        return AggregateType.Sum;
                    case "Min":
                        return AggregateType.Min;
                    case "Max":
                        return AggregateType.Max;
                    case "Average":
                        return AggregateType.Average;
                }
                throw new ArgumentException();
            }

            private void GetNew(NewExpression newExpression)
            {

                foreach (var argument in newExpression.Arguments)
                {
                    GetOperandValue();
                }

                var args = newExpression.Arguments;

                var members = newExpression.Members;

                if (newExpression.Type != lambdaExpression.Body.Type)
                {

                    var lambdaHandler = new LambdaExpressionHandler(indentLevel + 1,
                                            Expression.Lambda(newExpression,
                                                Expression.Parameter(
                                                    lambdaExpression.Parameters[0].Type,
                                                    "source")));


                    foreach (var column in lambdaHandler.aliases)
                    {
                        aliases[lambaExpressionId + "." + column.Key] = column.Value;
                        aliases[column.Key] = column.Value;
                    }
                }
                else
                {

                    for (int i = 0; i < args.Count; i++)
                    {

                        if (args[i].NodeType != ExpressionType.MemberAccess ||
                            //hack - should check if MemberAccess has a corresponding column
                            // in db
                            !(args[i].Type.IsValueType || args[i].Type == typeof(string)))
                        {
                            continue;
                        }

                        string memberName = null;

                        if (newExpression.Members[i].Name.StartsWith("get_"))
                        {
                            memberName = newExpression.Members[i].Name.Substring(4);
                        }
                        else
                        {
                            memberName = newExpression.Members[i].Name;
                        }

                        var key = lambaExpressionId + "." + memberName;

                        aliases[key] = GetHashedName((args[i] as MemberExpression));
                    }
                }

                terms.Push(Expression.Constant(
                                new BoxedConstant(newExpression.ToString())));
            }

            private void GetBinaryOperands(out string rightOperand, out string leftOperand)
            {

                leftOperand = GetOperandValue();

                rightOperand = GetOperandValue();
            }

            private string GetUnaryOperand()
            {

                return GetOperandValue();
            }

            private string GetOperandValue()
            {

                while (terms.Peek().Type != typeof(BoxedConstant))
                {
                    GetExpression();
                }

                var result = terms.Pop();

                return (result as ConstantExpression).Value.ToString();
            }

            private string GetProjectionSql(int indentLevel, NCacheExpressionParser project)
            {

                return Environment.NewLine +
                    GetIndentation(indentLevel) +
                    "(" +
                    Environment.NewLine +
                    project.GetNQLStatement() +
                    GetIndentation(indentLevel) +
                    ")" +
                    Environment.NewLine;
            }

            private string GetHashedName(MemberExpression m)
            {

                string memberName = null;

                if (m.Type == typeof(string) || m.Type.IsValueType)
                {
                    memberName = m.Member.Name;
                }
                else
                {
                    memberName = m.Type.GUID.ToString();
                }

                if (m.Expression.NodeType == ExpressionType.MemberAccess)
                {
                    return GetHashedName((MemberExpression)m.Expression) + "." + memberName;
                }
                Property = memberName;
                //@UH
                if (this.aggregateType == AggregateType.None)
                {
                    return "this" + "." + memberName;
                }
                else 
                {
                    return memberName;
                }
            }

            private Type GetSourceType(Expression expression)
            {

                switch (expression.NodeType)
                {
                    case ExpressionType.MemberAccess:
                        return GetSourceType(
                            (expression as MemberExpression).Expression);
                    case ExpressionType.Convert:
                    case ExpressionType.ConvertChecked:
                        return GetSourceType((expression as UnaryExpression).Operand);
                    case ExpressionType.Constant:
                    case ExpressionType.Parameter:
                        return expression.Type;
                    case ExpressionType.Call:
                        var method = expression as MethodCallExpression;

                        return GetSourceType(method.Arguments[0]);
                    default:
                        throw new ArgumentException();
                }
            }

            private string GetPrimaryKey(Type sourceType)
            {
                return "";
            }

            private string GetForeignKey(Type sourceType, Type memberType)
            {
                return "";
            }

            public string[] GetAccessedFields()
            {

                return accessedColumns.Distinct().ToArray();
            }

            public string ReplaceAliases(string expression)
            {

                return ReplaceAliases(expression, true);
            }

            public string ReplaceAliases(string expression, bool replaceAliases)
            {

                if (!replaceAliases)
                {
                    return expression;
                }

                var result = new StringBuilder(expression);

                foreach (var column in aliases)
                {
                    result.Replace(column.Key, column.Value);
                }

                return expression;
            }

        }

        private class BoxedConstant
        {

            private string expression = null;

            public BoxedConstant(string expression)
            {
                this.expression = expression;
            }

            public string Expression
            {
                get
                {
                    return expression;
                }
            }

            public static bool operator ==(string s, BoxedConstant bc)
            {
                throw new InvalidOperationException();
            }

            public static bool operator !=(string s, BoxedConstant bc)
            {
                throw new InvalidOperationException();
            }

            public static bool operator ==(int i, BoxedConstant bc)
            {
                throw new InvalidOperationException();
            }

            public static bool operator !=(int i, BoxedConstant bc)
            {
                throw new InvalidOperationException();
            }

            public override string ToString()
            {
                return expression;
            }
        }

        private static class QueryableMethodsProvider
        {

            private static readonly MethodInfo[] queryableMethods = typeof(Queryable).GetMethods();

            private static readonly MethodInfo selectMethod =
                                            (from q in queryableMethods
                                             where q.Name == "Select" && q.GetGenericArguments().Length == 2
                                             select q.GetGenericMethodDefinition()).First();

            private static readonly MethodInfo whereMethod =
                                            (from q in queryableMethods
                                             where q.Name == "Where" && q.GetGenericArguments().Length == 1
                                             select q.GetGenericMethodDefinition()).First();

            private static readonly Type queryableType = typeof(System.Linq.IQueryable<IQueryable<int>>)
                                                        .GetGenericTypeDefinition();

            public static MethodCallExpression GetSelectCall(Type sourceType)
            {

                var queryableType = QueryableMethodsProvider.GetQueryableType(sourceType);

                var sourceParam = Expression.Parameter(queryableType, "source");

                var selectorParam = Expression.Parameter(sourceType, "param");

                var projectionSelector = Expression.Lambda(selectorParam, selectorParam);

                return GetSelectCall(sourceParam, projectionSelector);
            }

            public static MethodCallExpression GetSelectCall(Expression source)
            {

                var sourceType = source.Type.GetGenericArguments()[0];

                var queryableType = QueryableMethodsProvider.GetQueryableType(sourceType);

                var sourceParam = Expression.Parameter(queryableType, "source");

                var selectorParam = Expression.Parameter(sourceType, "param");

                var projectionSelector = Expression.Lambda(selectorParam, selectorParam);

                return GetSelectCall(source, projectionSelector);
            }

            public static MethodCallExpression GetSelectCall(Expression source, LambdaExpression projectionSelector)
            {

                var selectQuery = QueryableMethodsProvider
                                        .GetSelectMethod(source.Type.GetGenericArguments()[0],
                                                         projectionSelector.Type.GetGenericArguments()[1]);

                return Expression.Call(selectQuery, source, Expression.Constant(projectionSelector));
            }

            public static MethodCallExpression GetSelectCall(Type sourceType, LambdaExpression projectionSelector)
            {

                var queryableType = QueryableMethodsProvider.GetQueryableType(sourceType);

                var sourceParam = Expression.Parameter(queryableType, "source");

                return GetSelectCall(sourceParam, projectionSelector);
            }

            public static MethodCallExpression GetWhereCall(Type sourceType, string sourceName, BinaryExpression condition)
            {

                var queryableType = QueryableMethodsProvider.GetQueryableType(sourceType);

                var whereLambda = Expression.Lambda(condition, Expression.Parameter(sourceType, sourceName));

                var whereQuery = QueryableMethodsProvider.GetWhereMethod(sourceType);

                var queryableSource = Expression.Parameter(queryableType, "source");

                var whereCall = Expression.Call(whereQuery, queryableSource, whereLambda);

                return whereCall;
            }

            private static MethodInfo GetSelectMethod(Type tableType, Type projectionSelectorType)
            {
                return selectMethod.MakeGenericMethod(tableType, projectionSelectorType); ;
            }

            private static MethodInfo GetWhereMethod(Type tableType)
            {
                return whereMethod.MakeGenericMethod(tableType); ;
            }

            public static Type GetQueryableType(Type tableType)
            {
                return queryableType.MakeGenericType(tableType);
            }
        }

        private class Binder : ExpressionVisitor
        {

            private readonly LambdaExpression selector = null;

            private readonly LambdaExpression binderLambda = null;

            private readonly Delegate binderMethod = null;

            private readonly Dictionary<string, int> fieldPositions = new Dictionary<string, int>();

            private readonly ParameterExpression reader = Expression.Parameter(typeof(DbDataReader),
                                                                               "reader");

            private static readonly MethodInfo getBoolean = typeof(DbDataReader).GetMethod("GetBoolean");
            private static readonly MethodInfo getByte = typeof(DbDataReader).GetMethod("GetByte");
            private static readonly MethodInfo getChar = typeof(DbDataReader).GetMethod("GetChar");
            private static readonly MethodInfo getDateTime = typeof(DbDataReader).GetMethod("GetDateTime");
            private static readonly MethodInfo getDecimal = typeof(DbDataReader).GetMethod("GetDecimal");
            private static readonly MethodInfo getDouble = typeof(DbDataReader).GetMethod("GetDouble");
            private static readonly MethodInfo getGUID = typeof(DbDataReader).GetMethod("GetGuid");
            private static readonly MethodInfo getInt16 = typeof(DbDataReader).GetMethod("GetInt16");
            private static readonly MethodInfo getInt32 = typeof(DbDataReader).GetMethod("GetInt32");
            private static readonly MethodInfo getInt64 = typeof(DbDataReader).GetMethod("GetInt64");
            private static readonly MethodInfo getString = typeof(DbDataReader).GetMethod("GetString");
            private static readonly MethodInfo getValue = typeof(DbDataReader).GetMethod("GetValue");

            private static readonly MethodInfo isDbNull = typeof(DbDataReader).GetMethod("IsDBNull");

            private static readonly MethodInfo convert =
                (from m in typeof(Binder).GetMethods(BindingFlags.NonPublic |
                                                     BindingFlags.Static)
                 where m.Name == "Convert"
                 select m).First().GetGenericMethodDefinition();

            private static readonly MethodInfo partialEval =
                    (from partial in typeof(Evaluator).GetMethods()

                     where partial.Name == "PartialEval" && partial.GetParameters().Count() == 1
                     select partial).First();

            private Binder(LambdaExpression selector)
            {


                this.selector = selector;

                if (selector.Body.NodeType != ExpressionType.Parameter)
                {
                    binderLambda = Expression.Lambda(((LambdaExpression)this.Visit(selector)).Body,
                                                     reader);
                }
                else
                {
                    binderLambda = GetBindingLambda(selector);
                }

                binderMethod = binderLambda.Compile();
            }

            internal static Delegate GetBinder(LambdaExpression selector)
            {

                string key = selector.Parameters[0].Type.GUID +
                             selector.ToString() +
                             selector.Type.GUID;

                Binder binder = new Binder(selector);

                return binder.binderMethod;
            }

            protected override Expression VisitMemberAccess(MemberExpression m)
            {

                if (GetAccessedType(m) != selector.Parameters[0].Type)
                {
                    return m;
                }

                int fieldPosition = GetFieldPosition(m);

                return GetFieldReader(m, fieldPosition);
            }

            protected override Expression VisitMethodCall(MethodCallExpression m)
            {

                if (!IsAggregateMethod(m))
                {
                    if ((m.Method.DeclaringType == typeof(Queryable) ||
                         m.Method.DeclaringType == typeof(Enumerable))
                         && m.Type.Name == "IQueryable`1")
                    {

                        var converter = convert.MakeGenericMethod(m.Type);

                        return Expression.Convert(Expression.Call(partialEval,
                                                                  base.VisitMethodCall(m)),
                                                  m.Type,
                                                  converter);

                    }
                    return base.VisitMethodCall(m);
                }

                if (m.Arguments[0].NodeType != ExpressionType.MemberAccess)
                {
                    return base.VisitMethodCall(m);
                }

                if (GetAccessedType(m.Arguments[0] as MemberExpression) != selector.Parameters[0].Type)
                {
                    return m;
                }

                int fieldPosition = GetFieldPosition(m.ToString());

                return GetFieldReader(m, fieldPosition);
            }

            private Expression GetFieldReader(Expression m, int fieldPosition)
            {

                var field = Expression.Constant(fieldPosition, typeof(int));

                var readerExpression = GetReaderExpression(m, field);

                var isDbNullExpression = Expression.Call(reader, isDbNull, field);

                var conditionalExpression =
                    Expression.Condition(Expression.Not(isDbNullExpression),
                                         readerExpression,
                                         Expression.Convert(Expression.Constant(null),
                                                             readerExpression.Type));

                return conditionalExpression;
            }

            private Expression GetReaderExpression(Expression m, ConstantExpression field)
            {

                MethodInfo getReaderMethod = GetReaderMethod(m);

                var readerExpression = Expression.Call(reader, getReaderMethod, field);

                if (getReaderMethod.ReturnType == m.Type)
                {
                    return readerExpression;
                }

                return Expression.Convert(readerExpression, m.Type);
            }

            private static MethodInfo GetReaderMethod(Expression m)
            {

                Type memberType = GetMemberType(m);

                MethodInfo getMethod = null;

                switch (Type.GetTypeCode(memberType))
                {
                    case TypeCode.Boolean:
                        getMethod = getBoolean;
                        break;
                    case TypeCode.Byte:
                        getMethod = getByte;
                        break;
                    case TypeCode.Char:
                        getMethod = getChar;
                        break;
                    case TypeCode.DateTime:
                        getMethod = getDateTime;
                        break;
                    case TypeCode.Decimal:
                        getMethod = getDecimal;
                        break;
                    case TypeCode.Double:
                        getMethod = getDouble;
                        break;
                    case TypeCode.Int16:
                        getMethod = getInt16;
                        break;
                    case TypeCode.Int32:
                        getMethod = getInt32;
                        break;
                    case TypeCode.Int64:
                        getMethod = getInt64;
                        break;
                    case TypeCode.String:
                        getMethod = getString;
                        break;
                    case TypeCode.Object:
                        getMethod = getValue;
                        break;
                    default:
                        if (m.Type == typeof(Guid))
                        {
                            getMethod = getGUID;
                        }
                        else
                        {
                            getMethod = getValue;
                        }
                        break;
                }
                return getMethod;
            }

            private int GetFieldPosition(MemberExpression m)
            {

                return GetFieldPosition(m.Member.Name);
            }

            private int GetFieldPosition(string fieldName)
            {

                int fieldPosition = 0;

                if (fieldPositions.ContainsKey(fieldName))
                {
                    fieldPosition = fieldPositions[fieldName];
                    return fieldPosition;
                }

                fieldPosition = fieldPositions.Count();

                fieldPositions.Add(fieldName, fieldPosition);

                return fieldPosition;
            }

            private static Type GetMemberType(Expression m)
            {

                Type memberType = null;

                if (m.Type.Name == "Nullable`1")
                {
                    memberType = m.Type.GetGenericArguments()[0];
                }
                else
                {
                    memberType = m.Type;
                }
                return memberType;
            }

            private static Type GetAccessedType(MemberExpression m)
            {

                if (m.Expression.NodeType == ExpressionType.MemberAccess)
                {
                    return GetAccessedType((MemberExpression)m.Expression);
                }

                return m.Expression.Type;
            }

            private LambdaExpression GetBindingLambda(LambdaExpression selector)
            {

                var instanceType = selector.Body.Type;

                // this is a hack
                var properties = (from property in instanceType.GetProperties()
                                  where property.PropertyType.IsValueType ||
                                        property.PropertyType == typeof(string)
                                  orderby property.Name
                                  select instanceType.GetField("_" + property.Name,
                                                               BindingFlags.Instance |
                                                               BindingFlags.NonPublic))
                                  .ToArray();

                var bindings = new MemberBinding[properties.Length];

                for (int i = 0; i < properties.Length; i++)
                {
                    var callMethod = GetFieldReader(
                                        Expression.MakeMemberAccess(
                                            Expression.Parameter(instanceType, "param"),
                                            properties[i]),
                                        i);

                    bindings[i] = Expression.Bind(properties[i], callMethod);
                }

                return Expression.Lambda(Expression.MemberInit(Expression.New(instanceType),
                                         bindings),
                                         reader);
            }

            private static object Convert<T>(Expression m)
            {

                var methodCall = m as MethodCallExpression;

                return (object)Expression.Lambda(methodCall).Compile().DynamicInvoke();
            }

        }

        private class Executor<T> : ExpressionVisitor, IEnumerable<T>
        {

            private readonly DbConnection cachedConnection = null;

            private readonly NCacheExpressionParser NCacheExpressionParser = null;

            private Type _classType = null;

            private readonly Func<DbDataReader, object> binder = null;

            private readonly List<object> parameters = new List<object>();

            private List<T> result = null;

            private IDictionary returnValues = null;

            private Cache _cache;

            private Type _type;

            private string _property;

            private Hashtable _queryValues;

            public Executor(Cache cache,
                            NCacheExpressionParser NCacheExpressionParser,
                            Expression expression, Type classType, string property, Hashtable queryVal)
            {

                _type = typeof(object);
                _classType = classType;
                this.Visit(expression);
                this.NCacheExpressionParser = NCacheExpressionParser;
                this._cache = cache;
                _property = property;
                this._queryValues = queryVal;
            }

            public IEnumerator<T> GetEnumerator()
            {
                GetResult();

                foreach (var element in result.ToList())
                {
                    yield return element;
                }
            }

            private void GetResult()
            {
                if (result != null)
                    return;

                result = new List<T>();
                string CommandText = NCacheExpressionParser.GetNQLStatement();
                if (_queryValues == null)
                    _queryValues = new Hashtable();
                returnValues = _cache.SearchEntries(CommandText, _queryValues);
                IDictionaryEnumerator ie = returnValues.GetEnumerator();

                while (ie.MoveNext())
                {
                    if (_classType.IsAssignableFrom(typeof(T)))
                        result.Add((T)(ie.Value));
                    else
                    {
                        object retrievedProperty=null;
                        foreach (PropertyInfo info in ie.Value.GetType().GetProperties())
                        {
                            if (info.CanRead)
                            {
                                if (info.Name == _property)
                                    retrievedProperty = info.GetValue(ie.Value, null);
                            }
                        }
                        if (retrievedProperty != null)
                        {
                            result.Add((T)retrievedProperty);
                        }

                    }
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator(); 
            }
            
            protected override Expression VisitConstant(ConstantExpression c)
            {

                if (c.Value == null)
                {
                    parameters.Insert(0, "NULL");
                }
                else
                {
                    switch (Type.GetTypeCode(c.Value.GetType()))
                    {
                        case TypeCode.Object:
                            break;
                        default:
                            parameters.Insert(0, c.Value);
                            break;
                    }
                }

                return c;
            }

            protected override Expression VisitConditional(ConditionalExpression c)
            {

                if ((bool)(c.Test as ConstantExpression).Value == true)
                {
                    return this.Visit(c.IfTrue);
                }

                return this.Visit(c.IfFalse);
            }
        }

        private class AggregateExecutor : ExpressionVisitor
        {

            private readonly DbConnection cachedConnection = null;

            private readonly NCacheExpressionParser NCacheExpressionParser = null;

            private Type _classType = null;

            private readonly Func<DbDataReader, object> binder = null;

            private readonly List<object> parameters = new List<object>();

            private ICollection returnVal = null;

            private Cache _cache;

            private Type _type;

            private Hashtable _queryValues;

            public AggregateExecutor(Cache cache,
                            NCacheExpressionParser NCacheExpressionParser,
                            Type returnType, Hashtable queryVal)
            {
                _type = returnType;
                this.NCacheExpressionParser = NCacheExpressionParser;
                this._cache = cache;
                this._queryValues = queryVal;
            }

            public object GetResult()
            {
                string CommandText = NCacheExpressionParser.GetNQLStatement();
                if (_queryValues == null)
                    _queryValues = new Hashtable();
                returnVal = _cache.Search(CommandText, _queryValues);
                if (returnVal.Count > 0)
                {
                    foreach (object r in returnVal)
                    {
                        return Convert.ChangeType(r, _type);
                    }
                }
                throw new NoItemFoundException("No items found.");
            }
        }

        private class ConstantEnumerable<T> : IEnumerable<object>
        {

            public ConstantEnumerable()
            {
            }

            public IEnumerator<object> GetEnumerator()
            {
                yield break;
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

        }

     
    }

    public class NoItemFoundException : ApplicationException
    {
        public NoItemFoundException(string message)
            : base(message)
        {
        }
    }
}
