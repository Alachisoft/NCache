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
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Alachisoft.NCache.EntityFrameworkCore.NCLinq
{
    class ValidationResult
    {
        internal bool IsValid
        {
            get; set;
        }

        internal string Reason
        {
            get; set;
        }

        internal string ToLog()
        {
            return GetType().Name + " = { "
                    + "IsValid = '" + IsValid + "', "
                    + "Reason = '" + Reason + "' "
                + "}";
        }
    }

    abstract class QueryAnalyzer : ExpressionVisitor
    {
        protected bool multipleProjectionFlag = false;

        protected Dictionary<string, int> functions = new Dictionary<string, int>()
        {
            { "any", 0},
            { "all", 0},
            { "sum", 0},
            { "min", 0},
            { "max", 0},
            { "zip", 0},
            { "last", 0},
            { "join", 0},
            { "skip", 0},
            { "take", 0},
            { "first", 0},
            { "count", 0},
            { "union", 0},
            { "where", 0},
            { "append", 0},
            { "concat", 0},
            { "except", 0},
            { "select", 0},
            { "average", 0},
            { "groupby", 0},
            { "orderby", 0},
            { "fromsql", 0},
            { "include", 0},
            { "prepend", 0},
            { "reverse", 0},
            { "contains", 0},
            { "skiplast", 0},
            { "takelast", 0},
            { "aggregate", 0},
            { "elementat", 0},
            { "intersect", 0},
            { "groupjoin", 0},
            { "skipwhile", 0},
            { "takewhile", 0},
            { "astracking", 0},
            { "selectmany", 0},
            { "asnotracking", 0},
            { "sequenceequal", 0},
            { "lastordefault", 0},
            { "firstordefault", 0},
            { "defaultifempty", 0},
            { "orderbydescending", 0},
            { "elementatordefault", 0},
        };

        protected QueryAnalyzer(Expression rootNode)
        {
            Visit(rootNode);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            try
            {
                string methodName = node.Method.Name.ToLower();

                if (methodName.Equals("select"))
                {
                    /*
                     * The following means this,
                     * 
                     *      01. Get arguments of select.
                     *      02. Second argument is the expression that is invoked in select.
                     *      03. If it is a new expression, it means it is possibly multiple project expression.
                     *      04. But just in case look at the number of arguments to see if it is actually projecting
                     *          more than one attributes.
                     */
                    UnaryExpression unaryExpression = (UnaryExpression)node.Arguments[1];
                    LambdaExpression lambdaExpression = (LambdaExpression)unaryExpression.Operand;

                    if (lambdaExpression.Body is NewExpression newExpression)
                    {
                        multipleProjectionFlag = newExpression.ArgumentCount() > 1;
                    }
                }
                functions[methodName]++;
            }
            catch (Exception e)
            {
                Logger.Log(e, Microsoft.Extensions.Logging.LogLevel.Warning);
            }

            if (node.Object != null)
            {
                Visit(node.Object);
            }

            for (int i = 0, n = node.ArgumentCount(); i < n; i++)
            {
                Visit(node.GetArgument(i));
            }

            return node;
        }

        internal abstract ValidationResult ValidateQuery();
    }
}
