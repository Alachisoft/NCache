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
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Alachisoft.NCache.EntityFrameworkCore
{
    internal class QueryHelper
    {
        internal static bool IsSeperateStorageEligible<T>(IQueryable<T> query, CachingOptions options)
        {
            Logger.Log(
                "IsSeparateStorageEligible with " + query.ToString() + " and " + options.ToLog() + "?",
                Microsoft.Extensions.Logging.LogLevel.Trace
            );

            IEntityType entityType = query.GetDbContext().Model.FindEntityType(query.ElementType.FullName);

            // Full Projection
            if (entityType != null)
            {
                // If User specified key based caching
                if (options.StoreAs == StoreAs.SeperateEntities)
                {
                    if (entityType.FindPrimaryKey() == null)
                        return false;
                    else
                        return true;
                }
                else
                    return false;
            }
            else
                return false;
        }

        internal static bool CanDirectPkFetch<T>(IQueryable<T> query, CachingOptions options, out string directKey)
        {
            Logger.Log(
                "CanDirectPkFetch with " + query.ToString() + " and " + options.ToLog(),
                Microsoft.Extensions.Logging.LogLevel.Trace
            );

            directKey = null;
            IEntityType entityType = query.GetDbContext().Model.FindEntityType(query.ElementType.FullName);

            // Full Projection
            if (entityType != null)
            {
                IKey key = entityType.FindPrimaryKey();
                List<IProperty> primaryKeys = key.Properties.ToList();
                Dictionary<IProperty, object> pks = new Dictionary<IProperty, object>();
                foreach (IProperty pk in primaryKeys)
                {
                    pks.Add(pk, null);
                }

                List<Expression> queryCriteria = ExtensionMethods.GetQueryCriteriaExp(query.Expression);

                if (queryCriteria.Count == 0)
                    return false;

                // Verify Entity has PK and query criteria has PK with Equality only
                foreach (Expression exp in queryCriteria)
                {
                    if (!IsExpressionPkEquality(exp, pks))
                        return false;
                }

                // Create Instance then get key
                object instance = Activator.CreateInstance(query.ElementType);
                foreach (var pk in pks)
                {
                    instance.GetType().GetProperty(pk.Key.Name).SetValue(instance, pk.Value);
                }
                // Finally get key
                directKey = QueryCacheManager.Cache.DefaultKeyGen.GetKey(query.GetDbContext(), instance);
                return true;
            }
            else
                return false;
        }

        internal static bool IsExpressionPkEquality(Expression expression, Dictionary<IProperty, object> primaryKeys)
        {
            UnaryExpression unaryExp = expression as UnaryExpression;
            if (unaryExp != null)
                return IsExpressionPkEquality(unaryExp, primaryKeys);
            else
                return false;
        }

        private static bool IsExpressionPkEquality(UnaryExpression UnaryExp, Dictionary<IProperty, object> primaryKeys)
        {
            LambdaExpression lambdaExp = UnaryExp.Operand as LambdaExpression;
            if (lambdaExp != null)
                return IsExpressionPkEquality(lambdaExp, primaryKeys);
            else
                return false;
        }

        private static bool IsExpressionPkEquality(LambdaExpression lambdaExp, Dictionary<IProperty, object> primaryKeys)
        {
            BinaryExpression binaryExp = lambdaExp.Body as BinaryExpression;
            if (binaryExp != null)
                return IsExpressionPkEquality(binaryExp, primaryKeys);
            else
                return false;
        }

        private static bool IsExpressionPkEquality(BinaryExpression binaryExp, Dictionary<IProperty, object> primaryKeys)
        {
            MemberExpression memberExp = binaryExp.Left as MemberExpression;
            if (memberExp != null)
                return IsPkEquality(binaryExp, primaryKeys) && binaryExp.NodeType == ExpressionType.Equal;

            memberExp = binaryExp.Right as MemberExpression;
            if (memberExp != null)
                return IsPkEquality(binaryExp, primaryKeys) && binaryExp.NodeType == ExpressionType.Equal;

            // M pretty sure And and Or should not be there in the following check but lets go with the flow.
            if (binaryExp.NodeType == ExpressionType.And || binaryExp.NodeType == ExpressionType.AndAlso || binaryExp.NodeType == ExpressionType.Or || binaryExp.NodeType == ExpressionType.OrElse)
            {
                bool leftExpResult = false;
                bool rightExpResult = false;

                BinaryExpression lBinaryExp = binaryExp.Left as BinaryExpression;
                if (lBinaryExp != null)
                    leftExpResult = IsExpressionPkEquality(lBinaryExp, primaryKeys);

                BinaryExpression rBinaryExp = binaryExp.Right as BinaryExpression;
                if (rBinaryExp != null)
                    rightExpResult = IsExpressionPkEquality(rBinaryExp, primaryKeys);

                // XOR Gate
                return leftExpResult != rightExpResult;
            }
            else
                return false;
        }

        private static bool IsPkEquality(BinaryExpression binaryExp, Dictionary<IProperty, object> primaryKeys)
        {
            MemberExpression memberExp = binaryExp.Left as MemberExpression;
            if (memberExp != null)
            {
                foreach (var key in primaryKeys.Keys)
                {
                    if (key.Name == memberExp.Member.Name)
                    {
                        object value;
                        if (GetValueFromExpression(binaryExp.Right, out value))
                        {
                            primaryKeys[key] = value;
                            return true;
                        }
                        else
                            return false;
                    }
                }
                return false;
            }
            else
            {
                memberExp = binaryExp.Right as MemberExpression;
                if (memberExp != null)
                {
                    foreach (var key in primaryKeys.Keys)
                    {
                        if (key.Name == memberExp.Member.Name)
                        {
                            object value;
                            if (GetValueFromExpression(binaryExp.Left, out value))
                            {
                                primaryKeys[key] = value;
                                return true;
                            }
                            else
                                return false;
                        }
                    }
                    return false;
                }
                else
                    return false;
            }
        }

        private static bool GetValueFromExpression(Expression valueExpression, out object value)
        {
            value = null;

            // For variable values
            MemberExpression memberExp = valueExpression as MemberExpression;
            if (memberExp != null)
            {
                var expVisitor = new WalkVisitor();
                expVisitor.Visit(memberExp);
                value = expVisitor.Value;
                return value != null;
            }

            // For constant values
            if (valueExpression is ConstantExpression)
            {
                value = Expression.Lambda(valueExpression).Compile().DynamicInvoke();
                return true;
            }

            // For Method call (tested on private static method in main with 1 parameter)
            if (valueExpression is MethodCallExpression)
            {
                value = Expression.Lambda(valueExpression).Compile().DynamicInvoke();
                return true;
            }
            return false;
        }
    }

    //ExpressionVisitor
    internal class WalkVisitor : ExpressionVisitor
    {
        public object Value
        {
            get;
            private set;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            var e = base.VisitMember(node);
            var c = node.Expression as ConstantExpression;
            if (c != null)
            {
                Type t = c.Value.GetType();
                var x = t.InvokeMember(node.Member.Name, BindingFlags.GetField, null, c.Value, null);
                Value = x;
            }
            return e;
        }
    }
}
