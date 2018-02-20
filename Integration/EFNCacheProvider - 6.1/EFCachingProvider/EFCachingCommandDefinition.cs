// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Linq;

using Alachisoft.NCache.Integrations.EntityFramework.Toolkit;
using System.Data.Entity.Core.Common;
using System.Data.Common;
using System.Data.Entity.Core.Metadata.Edm;
using Alachisoft.NCache.Integrations.EntityFramework.Caching;
using Alachisoft.NCache.Integrations.EntityFramework.Config;

namespace Alachisoft.NCache.Integrations.EntityFramework
{
    /// <summary>
    /// Represents a command definitio
    /// </summary>
    public class EFCachingCommandDefinition : DbCommandDefinitionWrapper
    {
        private System.Data.Entity.Core.Common.DbCommandDefinition _dbCommandDefinition;

        private List<EntitySetBase> affectedEntitySets = new List<EntitySetBase>();
        private List<EdmFunction> functionsUsed = new List<EdmFunction>();

        /// <summary>
        /// Initializes static members of the EFCachingCommandDefinition class.
        /// </summary>
        static EFCachingCommandDefinition()
        {
            NonCacheableFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Edm.CurrentDateTime",
                "Edm.CurrentUtcDateTime",
                "Edm.CurrentDateTimeOffsets",
                "Edm.NewGuid",

                "SqlServer.NEWID",
                "SqlServer.GETDATE",
                "SqlServer.GETUTCDATE",
                "SqlServer.SYSDATETIME",
                "SqlServer.SYSUTCDATETIME",
                "SqlServer.SYSDATETIMEOFFSET",
                "SqlServer.CURRENT_USER",
                "SqlServer.CURRENT_TIMESTAMP",
                "SqlServer.HOST_NAME",
                "SqlServer.USER_NAME",
            };
        }

        internal EFCachingCommandDefinition(System.Data.Entity.Core.Common.DbCommandDefinition wrappedCommandDefinition, System.Data.Entity.Core.Common.CommandTrees.DbCommandTree commandTree)
            : base(wrappedCommandDefinition, commandTree, (cmd, def) => EFDbCommandFactory.GetCommandWrapper(cmd, def))
        {
            this.GetAffectedEntitySets(commandTree);
        }

        /// <summary>
        /// Gets the list of non-cacheable functions (by default includes canonical and SqlServer functions).
        /// </summary>
        /// <value>The non-cacheable functions.</value>
        public static ICollection<string> NonCacheableFunctions { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is a modification command (INSERT, UPDATE or DELETE).
        /// </summary>
        /// <value>
        /// Returns <c>true</c> if this instance is modification command (INSERT, UPDATE, DELETE); otherwise, <c>false</c>.
        /// </value>
        public bool IsModification { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is a stored procedure call.
        /// </summary>
        /// <value>
        /// Returns <c>true</c> if this instance is a stored procedure call; otherwise, <c>false</c>.
        /// </value>
        public bool IsStoredProcedure { get; private set; }

        /// <summary>
        /// Gets the list of entity sets affected by this command.
        /// </summary>
        /// <value>The affected entity sets.</value>
        public IList<EntitySetBase> AffectedEntitySets
        {
            get { return this.affectedEntitySets; }
        }

        /// <summary>
        /// Creates and returns a <see cref="T:System.Data.Common.DbCommandDefinition"/> object associated with the current connection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Data.Common.DbCommandDefinition"/>.
        /// </returns>
        public override DbCommand CreateCommand()
        {
            Application.ApplicationMode mode = Application.Instance.Mode;
            EFCachingCommandDefinition commandDefinition = new EFCachingCommandDefinition(WrappedCommandDefinition, CommandTree);
            switch (mode)
            {
                case Application.ApplicationMode.Analysis:
                    return new EFAnalysisCommand(WrappedCommandDefinition.CreateCommand(), commandDefinition);

                case Application.ApplicationMode.Cache:
                    return new EFCachingCommand(WrappedCommandDefinition.CreateCommand(), commandDefinition);

                default:
                    return new DbCommandWrapper(WrappedCommandDefinition.CreateCommand(), commandDefinition);
            }
        }

        /// <summary>
        /// Determines whether this command definition is cacheable.
        /// </summary>
        /// <returns>
        /// A value of <c>true</c> if this command definition is cacheable; otherwise, <c>false</c>.
        /// </returns>
        public bool IsCacheable()
        {
            ///If this is non-deterministic function, or if it's not a SELECT query
            if (this.functionsUsed.Any(f => IsNonDeterministicFunction(f)) || this.IsModification)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether the specified function is non-deterministic.
        /// </summary>
        /// <param name="function">The function object.</param>
        /// <returns>
        /// A value of <c>true</c> if the function is non-deterministic; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsNonDeterministicFunction(EdmFunction function)
        {
            return NonCacheableFunctions.Contains(function.NamespaceName + "." + function.Name);
        }

        private void GetAffectedEntitySets(System.Data.Entity.Core.Common.CommandTrees.DbCommandTree commandTree)
        {
            System.Data.Entity.Core.Common.CommandTrees.DbExpressionVisitor visitor = new FindAffectedEntitySetsVisitor(this.affectedEntitySets, this.functionsUsed);
            System.Data.Entity.Core.Common.CommandTrees.DbQueryCommandTree queryTree = commandTree as System.Data.Entity.Core.Common.CommandTrees.DbQueryCommandTree;
            if (queryTree != null)
            {
                queryTree.Query.Accept(visitor);
                return;
            }

            System.Data.Entity.Core.Common.CommandTrees.DbFunctionCommandTree fxnTree = commandTree as System.Data.Entity.Core.Common.CommandTrees.DbFunctionCommandTree;
            if (fxnTree != null)
            {
                this.IsStoredProcedure = true;
                return;
            }

            System.Data.Entity.Core.Common.CommandTrees.DbUpdateCommandTree updateTree = commandTree as System.Data.Entity.Core.Common.CommandTrees.DbUpdateCommandTree;
            if (updateTree != null)
            {
                this.IsModification = true;
                updateTree.Target.Expression.Accept(visitor);
                updateTree.Predicate.Accept(visitor);
                if (updateTree.Returning != null)
                {
                    updateTree.Returning.Accept(visitor);
                }

                return;
            }

            System.Data.Entity.Core.Common.CommandTrees.DbInsertCommandTree insertTree = commandTree as System.Data.Entity.Core.Common.CommandTrees.DbInsertCommandTree;
            if (insertTree != null)
            {
                this.IsModification = true;
                insertTree.Target.Expression.Accept(visitor);
                if (insertTree.Returning != null)
                {
                    insertTree.Returning.Accept(visitor);
                }

                return;
            }

            System.Data.Entity.Core.Common.CommandTrees.DbDeleteCommandTree deleteTree = commandTree as System.Data.Entity.Core.Common.CommandTrees.DbDeleteCommandTree;
            if (deleteTree != null)
            {
                this.IsModification = true;
                deleteTree.Target.Expression.Accept(visitor);
                if (deleteTree.Predicate != null)
                {
                    deleteTree.Predicate.Accept(visitor);
                }

                return;
            }

            throw new NotSupportedException("Command tree type " + commandTree.GetType() + " is not supported.");
        }

        /// <summary>
        /// Scans the command tree for occurences of entity sets and functions.
        /// </summary>
        private class FindAffectedEntitySetsVisitor : DbCommandTreeScanner
        {
            private ICollection<EntitySetBase> affectedEntitySets;
            private ICollection<EdmFunction> functionsUsed;

            /// <summary>
            /// Initializes a new instance of the FindAffectedEntitySetsVisitor class.
            /// </summary>
            /// <param name="affectedEntitySets">The affected entity sets.</param>
            /// <param name="functionsUsed">The functions used.</param>
            public FindAffectedEntitySetsVisitor(ICollection<EntitySetBase> affectedEntitySets, ICollection<EdmFunction> functionsUsed)
            {
                this.affectedEntitySets = affectedEntitySets;
                this.functionsUsed = functionsUsed;
            }

            /// <summary>
            /// Implements the visitor pattern for <see cref="T:System.Data.Common.CommandTrees.DbScanExpression"/>.
            /// </summary>
            /// <param name="expression">The <see cref="T:System.Data.Common.CommandTrees.DbScanExpression"/> that is visited.</param>
            public override void Visit(System.Data.Entity.Core.Common.CommandTrees.DbScanExpression expression)
            {
                base.Visit(expression);
                this.affectedEntitySets.Add(expression.Target);
            }

            /// <summary>
            /// Implements the visitor pattern for <see cref="T:System.Data.Common.CommandTrees.DbFunctionExpression"/>.
            /// </summary>
            /// <param name="expression">The <see cref="T:System.Data.Common.CommandTrees.DbFunctionExpression"/> that is visited.</param>
            public override void Visit(System.Data.Entity.Core.Common.CommandTrees.DbFunctionExpression expression)
            {
                base.Visit(expression);
                this.functionsUsed.Add(expression.Function);
            }
        }
    }
}
