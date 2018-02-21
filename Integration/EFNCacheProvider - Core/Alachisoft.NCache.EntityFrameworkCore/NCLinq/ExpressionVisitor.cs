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

using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace Alachisoft.NCache.EntityFrameworkCore.NCLinq
{
    /// <summary>
    /// Represents a visitor or rewriter for expression trees.
    /// </summary>
    /// <remarks>
    /// This class is designed to be inherited to create more specialized
    /// classes whose functionality requires traversing, examining or copying
    /// an expression tree.
    /// </remarks>
    public abstract class ExpressionVisitor : System.Linq.Expressions.ExpressionVisitor
    {
        /// <summary>
        /// Initializes a new instance of <seealso cref="ExpressionVisitor"/>.
        /// </summary>
        protected ExpressionVisitor()
        {
        }

        /// <summary>
        /// Dispatches the expression to one of the more specialized visit methods in this class.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        public override Expression Visit(Expression node)
        {
            return base.Visit(node);
        }


        /// <summary>
        /// Dispatches the list of expressions to one of the more specialized visit methods in this class.
        /// </summary>
        /// <param name="nodes">The expressions to visit.</param>
        /// <returns>The modified expression list, if any of the elements were modified;
        /// otherwise, returns the original expression list.</returns>
        internal new ReadOnlyCollection<Expression> Visit(ReadOnlyCollection<Expression> nodes)
        {
            ContractUtils.RequiresNotNull(nodes, nameof(nodes));
            Expression[] newNodes = null;
            for (int i = 0, n = nodes.Count; i < n; i++)
            {
                Expression node = Visit(nodes[i]);

                if (newNodes != null)
                {
                    newNodes[i] = node;
                }
                else if (!object.ReferenceEquals(node, nodes[i]))
                {
                    newNodes = new Expression[n];
                    for (int j = 0; j < i; j++)
                    {
                        newNodes[j] = nodes[j];
                    }
                    newNodes[i] = node;
                }
            }
            if (newNodes == null)
            {
                return nodes;
            }
            return new TrueReadOnlyCollection<Expression>(newNodes);
        }

        private Expression[] VisitArguments(IArgumentProvider nodes)
        {
            return ExpressionVisitorUtils.VisitArguments(this, nodes);
        }

        private ParameterExpression[] VisitParameters(ParameterExpression nodes, string callerName)
        {
            return ExpressionVisitorUtils.VisitParameters(this, nodes, callerName);
        }

        /// <summary>
        /// Visits all nodes in the collection using a specified element visitor.
        /// </summary>
        /// <typeparam name="T">The type of the nodes.</typeparam>
        /// <param name="nodes">The nodes to visit.</param>
        /// <param name="elementVisitor">A delegate that visits a single element,
        /// optionally replacing it with a new element.</param>
        /// <returns>The modified node list, if any of the elements were modified;
        /// otherwise, returns the original node list.</returns>
        internal new static ReadOnlyCollection<T> Visit<T>(ReadOnlyCollection<T> nodes, Func<T, T> elementVisitor)
        {
            ContractUtils.RequiresNotNull(nodes, nameof(nodes));
            ContractUtils.RequiresNotNull(elementVisitor, nameof(elementVisitor));
            T[] newNodes = null;
            for (int i = 0, n = nodes.Count; i < n; i++)
            {
                T node = elementVisitor(nodes[i]);
                if (newNodes != null)
                {
                    newNodes[i] = node;
                }
                else if (!object.ReferenceEquals(node, nodes[i]))
                {
                    newNodes = new T[n];
                    for (int j = 0; j < i; j++)
                    {
                        newNodes[j] = nodes[j];
                    }
                    newNodes[i] = node;
                }
            }
            if (newNodes == null)
            {
                return nodes;
            }
            return new TrueReadOnlyCollection<T>(newNodes);
        }

        /// <summary>
        /// Visits an expression, casting the result back to the original expression type.
        /// </summary>
        /// <typeparam name="T">The type of the expression.</typeparam>
        /// <param name="node">The expression to visit.</param>
        /// <param name="callerName">The name of the calling method; used to report to report a better error message.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        /// <exception cref="InvalidOperationException">The visit method for this node returned a different type.</exception>
        internal new T VisitAndConvert<T>(T node, string callerName) where T : Expression
        {
            if (node == null)
            {
                return null;
            }
            node = Visit(node) as T;
            if (node == null)
            {
                throw Error.MustRewriteToSameNode(callerName, typeof(T), callerName);
            }
            return node;
        }

        /// <summary>
        /// Visits an expression, casting the result back to the original expression type.
        /// </summary>
        /// <typeparam name="T">The type of the expression.</typeparam>
        /// <param name="nodes">The expression to visit.</param>
        /// <param name="callerName">The name of the calling method; used to report to report a better error message.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        /// <exception cref="InvalidOperationException">The visit method for this node returned a different type.</exception>
        internal new ReadOnlyCollection<T> VisitAndConvert<T>(ReadOnlyCollection<T> nodes, string callerName) where T : Expression
        {
            ContractUtils.RequiresNotNull(nodes, nameof(nodes));
            T[] newNodes = null;
            for (int i = 0, n = nodes.Count; i < n; i++)
            {
                T node = Visit(nodes[i]) as T;
                if (node == null)
                {
                    throw Error.MustRewriteToSameNode(callerName, typeof(T), callerName);
                }

                if (newNodes != null)
                {
                    newNodes[i] = node;
                }
                else if (!object.ReferenceEquals(node, nodes[i]))
                {
                    newNodes = new T[n];
                    for (int j = 0; j < i; j++)
                    {
                        newNodes[j] = nodes[j];
                    }
                    newNodes[i] = node;
                }
            }
            if (newNodes == null)
            {
                return nodes;
            }
            return new TrueReadOnlyCollection<T>(newNodes);
        }

        /// <summary>
        /// Visits the children of the <see cref="BinaryExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        protected override Expression VisitBinary(BinaryExpression node)
        {
            // Walk children in evaluation order: left, conversion, right
            return ValidateBinary(
                node,
                node.Update(
                    Visit(node.Left),
                    VisitAndConvert(node.Conversion, nameof(VisitBinary)),
                    Visit(node.Right)
                )
            );
        }

        /// <summary>
        /// Visits the children of the <see cref="ConditionalExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        protected override Expression VisitConditional(ConditionalExpression node)
        {
            return node.Update(Visit(node.Test), Visit(node.IfTrue), Visit(node.IfFalse));
        }

        /// <summary>
        /// Visits the <see cref="ConstantExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        protected override Expression VisitConstant(ConstantExpression node)
        {
            return node;
        }

        /// <summary>
        /// Visits the <see cref="DebugInfoExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        protected override Expression VisitDebugInfo(DebugInfoExpression node)
        {
            return node;
        }

        /// <summary>
        /// Visits the <see cref="DefaultExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        protected override Expression VisitDefault(DefaultExpression node)
        {
            return node;
        }

        /// <summary>
        /// Visits the children of the <see cref="GotoExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        protected override Expression VisitGoto(GotoExpression node)
        {
            return node.Update(VisitLabelTarget(node.Target), Visit(node.Value));
        }

        /// <summary>
        /// Visits the <see cref="LabelTarget"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        protected override LabelTarget VisitLabelTarget(LabelTarget node)
        {
            return node;
        }

        /// <summary>
        /// Visits the children of the <see cref="LabelExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        protected override Expression VisitLabel(LabelExpression node)
        {
            return node.Update(VisitLabelTarget(node.Target), Visit(node.DefaultValue));
        }

        /// <summary>
        /// Visits the children of the <see cref="LoopExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        protected override Expression VisitLoop(LoopExpression node)
        {
            return node.Update(VisitLabelTarget(node.BreakLabel), VisitLabelTarget(node.ContinueLabel), Visit(node.Body));
        }

        /// <summary>
        /// Visits the children of the <see cref="MemberExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        protected override Expression VisitMember(MemberExpression node)
        {
            return node.Update(Visit(node.Expression));
        }

        /// <summary>
        /// Visits the children of the <see cref="NewArrayExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        protected override Expression VisitNewArray(NewArrayExpression node)
        {
            return node.Update(Visit(node.Expressions));
        }

        /// <summary>
        /// Visits the children of the <see cref="NewExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
        protected override Expression VisitNew(NewExpression node)
        {
            Expression[] a = VisitArguments(node);
            if (a == null)
            {
                return node;
            }

            return node.Update(a);
        }

        /// <summary>
        /// Visits the <see cref="ParameterExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node;
        }

        /// <summary>
        /// Visits the children of the <see cref="RuntimeVariablesExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        protected override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
        {
            return node.Update(VisitAndConvert(node.Variables, nameof(VisitRuntimeVariables)));
        }

        /// <summary>
        /// Visits the children of the <see cref="SwitchCase"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        protected override SwitchCase VisitSwitchCase(SwitchCase node)
        {
            return node.Update(Visit(node.TestValues), Visit(node.Body));
        }

        /// <summary>
        /// Visits the children of the <see cref="SwitchExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        protected override Expression VisitSwitch(SwitchExpression node)
        {
            return ValidateSwitch(
                node,
                node.Update(
                    Visit(node.SwitchValue),
                    Visit(node.Cases, VisitSwitchCase),
                    Visit(node.DefaultBody)
                )
            );
        }

        /// <summary>
        /// Visits the children of the <see cref="CatchBlock"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        protected override CatchBlock VisitCatchBlock(CatchBlock node)
        {
            return node.Update(VisitAndConvert(node.Variable, nameof(VisitCatchBlock)), Visit(node.Filter), Visit(node.Body));
        }

        /// <summary>
        /// Visits the children of the <see cref="TryExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        protected override Expression VisitTry(TryExpression node)
        {
            return node.Update(
                Visit(node.Body),
                Visit(node.Handlers, VisitCatchBlock),
                Visit(node.Finally),
                Visit(node.Fault)
            );
        }

        /// <summary>
        /// Visits the children of the <see cref="TypeBinaryExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        protected override Expression VisitTypeBinary(TypeBinaryExpression node)
        {
            return node.Update(Visit(node.Expression));
        }

        /// <summary>
        /// Visits the children of the <see cref="UnaryExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        protected override Expression VisitUnary(UnaryExpression node)
        {
            return ValidateUnary(node, node.Update(Visit(node.Operand)));
        }

        /// <summary>
        /// Visits the children of the <see cref="MemberInitExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            return node.Update(
                VisitAndConvert(node.NewExpression, nameof(VisitMemberInit)),
                Visit(node.Bindings, VisitMemberBinding)
            );
        }

        /// <summary>
        /// Visits the children of the <see cref="ListInitExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        protected override Expression VisitListInit(ListInitExpression node)
        {
            return node.Update(
                VisitAndConvert(node.NewExpression, nameof(VisitListInit)),
                Visit(node.Initializers, VisitElementInit)
            );
        }

        /// <summary>
        /// Visits the children of the <see cref="ElementInit"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        protected override ElementInit VisitElementInit(ElementInit node)
        {
            return node.Update(Visit(node.Arguments));
        }

        /// <summary>
        /// Visits the children of the <see cref="MemberBinding"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        protected override MemberBinding VisitMemberBinding(MemberBinding node)
        {
            switch (node.BindingType)
            {
                case MemberBindingType.Assignment:
                    return VisitMemberAssignment((MemberAssignment)node);
                case MemberBindingType.MemberBinding:
                    return VisitMemberMemberBinding((MemberMemberBinding)node);
                case MemberBindingType.ListBinding:
                    return VisitMemberListBinding((MemberListBinding)node);
                default:
                    throw Error.UnhandledBindingType(node.BindingType);
            }
        }

        /// <summary>
        /// Visits the children of the <see cref="MemberAssignment"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            return node.Update(Visit(node.Expression));
        }

        /// <summary>
        /// Visits the children of the <see cref="MemberMemberBinding"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding node)
        {
            return node.Update(Visit(node.Bindings, VisitMemberBinding));
        }

        /// <summary>
        /// Visits the children of the <see cref="MemberListBinding"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        protected override MemberListBinding VisitMemberListBinding(MemberListBinding node)
        {
            return node.Update(Visit(node.Initializers, VisitElementInit));
        }

        //
        // Prevent some common cases of invalid rewrites.
        //
        // Essentially, we don't want the rewritten node to be semantically
        // bound by the factory, which may do the wrong thing. Instead we
        // require derived classes to be explicit about what they want to do if
        // types change.
        //
        private static UnaryExpression ValidateUnary(UnaryExpression before, UnaryExpression after)
        {
            if (before != after && before.Method == null)
            {
                if (after.Method != null)
                {
                    throw Error.MustRewriteWithoutMethod(after.Method, nameof(VisitUnary));
                }

                // rethrow has null operand
                if (before.Operand != null && after.Operand != null)
                {
                    ValidateChildType(before.Operand.Type, after.Operand.Type, nameof(VisitUnary));
                }
            }
            return after;
        }

        private static BinaryExpression ValidateBinary(BinaryExpression before, BinaryExpression after)
        {
            if (before != after && before.Method == null)
            {
                if (after.Method != null)
                {
                    throw Error.MustRewriteWithoutMethod(after.Method, nameof(VisitBinary));
                }

                ValidateChildType(before.Left.Type, after.Left.Type, nameof(VisitBinary));
                ValidateChildType(before.Right.Type, after.Right.Type, nameof(VisitBinary));
            }
            return after;
        }

        // We wouldn't need this if switch didn't infer the method.
        private static SwitchExpression ValidateSwitch(SwitchExpression before, SwitchExpression after)
        {
            // If we did not have a method, we don't want to bind to one,
            // it might not be the right thing.
            if (before.Comparison == null && after.Comparison != null)
            {
                throw Error.MustRewriteWithoutMethod(after.Comparison, nameof(VisitSwitch));
            }
            return after;
        }

        // Value types must stay as the same type, otherwise it's now a
        // different operation, e.g. adding two doubles vs adding two ints.
        private static void ValidateChildType(Type before, Type after, string methodName)
        {
            if (before.IsValueType)
            {
                if (AreEquivalent(before, after))
                {
                    // types are the same value type
                    return;
                }
            }
            else if (!after.IsValueType)
            {
                // both are reference types
                return;
            }

            // Otherwise, it's an invalid type change.
            throw Error.MustRewriteChildToSameType(before, after, methodName);
        }

        private static bool AreEquivalent(Type t1, Type t2) => t1 != null && t1.IsEquivalentTo(t2);
    }
}
