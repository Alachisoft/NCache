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

using System.Linq.Expressions;

namespace Alachisoft.NCache.EntityFrameworkCore.NCLinq
{
    internal static class ExpressionVisitorUtils
    {
        public static Expression[] VisitBlockExpressions(ExpressionVisitor visitor, BlockExpression block)
        {
            Expression[] newNodes = null;
            for (int i = 0, n = block.Expressions.Count; i < n; i++)
            {
                Expression curNode = block.Expressions[i];
                Expression node = visitor.Visit(curNode);

                if (newNodes != null)
                {
                    newNodes[i] = node;
                }
                else if (!object.ReferenceEquals(node, curNode))
                {
                    newNodes = new Expression[n];
                    for (int j = 0; j < i; j++)
                    {
                        newNodes[j] = block.Expressions[j];
                    }
                    newNodes[i] = node;
                }
            }
            return newNodes;
        }

        public static ParameterExpression[] VisitParameters(ExpressionVisitor visitor, ParameterExpression nodes, string callerName)
        {
            return null;
        }

        public static Expression[] VisitArguments(ExpressionVisitor visitor, IArgumentProvider nodes)
        {
            Expression[] newNodes = null;
            for (int i = 0, n = nodes.ArgumentCount; i < n; i++)
            {
                Expression curNode = nodes.GetArgument(i);
                Expression node = visitor.Visit(curNode);

                if (newNodes != null)
                {
                    newNodes[i] = node;
                }
                else if (!object.ReferenceEquals(node, curNode))
                {
                    newNodes = new Expression[n];
                    for (int j = 0; j < i; j++)
                    {
                        newNodes[j] = nodes.GetArgument(j);
                    }
                    newNodes[i] = node;
                }
            }
            return newNodes;
        }
    }
}
