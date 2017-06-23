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

using System.IO;
using System.Collections;
using System.Runtime.Serialization;
using System.Diagnostics;

using Alachisoft.NCache.Caching.Queries;

namespace Alachisoft.NCache.Caching.Queries.Filters
{
    class ExpressionBuilder
    {
        public static readonly Predicate TRUE_PREDICATE = new AlwaysTruePredicate();
        public static readonly Predicate FALSE_PREDICATE = new AlwaysFalsePredicate();

        static public Predicate CreateSumFunctionPredicate(string attributeName)
        {
            AggregateFunctionPredicate predicate = new SumPredicate();
            predicate.AttributeName = attributeName;
            return predicate;
        }

        static public Predicate CreateCountFunctionPredicate()
        {
            AggregateFunctionPredicate predicate = new CountPredicate();
            return predicate;
        }

        static public Predicate CreateAverageFunctionPredicate(string attributeName)
        {
            AggregateFunctionPredicate predicate = new AveragePredicate();
            predicate.AttributeName = attributeName;
            return predicate;
        }

        static public Predicate CreateMinFunctionPredicate(string attributeName)
        {
            AggregateFunctionPredicate predicate = new MinPredicate();
            predicate.AttributeName = attributeName;
            return predicate;
        }

        static public Predicate CreateMaxFunctionPredicate(string attributeName)
        {
            AggregateFunctionPredicate predicate = new MaxPredicate();
            predicate.AttributeName = attributeName;
            return predicate;
        }

        static public Predicate CreateLogicalAndPredicate(Predicate lhsPred, Predicate rhsPred)
        {
            if (lhsPred.Equals(FALSE_PREDICATE) || rhsPred.Equals(FALSE_PREDICATE))
                return FALSE_PREDICATE;
            if (lhsPred.Equals(TRUE_PREDICATE))
                return rhsPred;
            if (rhsPred.Equals(TRUE_PREDICATE))
                return lhsPred;

            LogicalAndPredicate inc = null;
            if (lhsPred is LogicalAndPredicate)
                inc = lhsPred as LogicalAndPredicate;

            if (inc == null || inc.Inverse)
            {
                inc = new LogicalAndPredicate();
                inc.Children.Add(lhsPred);
            }
            inc.Children.Add(rhsPred);

            return inc;
        }

        static public Predicate CreateLogicalOrPredicate(Predicate lhsPred, Predicate rhsPred)
        {
            if (lhsPred.Equals(TRUE_PREDICATE) || rhsPred.Equals(TRUE_PREDICATE))
                return TRUE_PREDICATE;
            if (lhsPred.Equals(FALSE_PREDICATE))
                return rhsPred;
            if (rhsPred.Equals(FALSE_PREDICATE))
                return lhsPred;

            LogicalAndPredicate inc = null;
            if (lhsPred is LogicalAndPredicate)
            {
                inc = lhsPred as LogicalAndPredicate;
            }

            if (inc == null || !inc.Inverse)
            {
                inc = new LogicalAndPredicate();
                inc.Invert();
                inc.Children.Add(lhsPred);
            }
            inc.Children.Add(rhsPred);

            return inc;
        }

        static public Predicate CreateEqualsPredicate(object o, object v)
        {
            bool lhsIsGen = o is IGenerator;
            bool rhsIsGen = v is IGenerator;

            if (lhsIsGen || rhsIsGen)
            {
                if (lhsIsGen && rhsIsGen)
                {
                    object lhs = ((IGenerator)o).Evaluate();
                    object rhs = ((IGenerator)v).Evaluate();
                    return lhs.Equals(rhs) ? TRUE_PREDICATE : FALSE_PREDICATE;
                }

                IFunctor func = lhsIsGen ? (IFunctor)v : (IFunctor)o;
                IGenerator gen = lhsIsGen ? (IGenerator)o : (IGenerator)v;

                return new FunctorEqualsGeneratorPredicate(func, gen);
            }

            return new FunctorEqualsFunctorPredicate((IFunctor)o, (IFunctor)v);
        }

        static public Predicate CreateNotEqualsPredicate(object o, object v)
        {
            Predicate pred = CreateEqualsPredicate(o, v);
            if (pred.Equals(TRUE_PREDICATE))
                return FALSE_PREDICATE;
            if (pred.Equals(FALSE_PREDICATE))
                return TRUE_PREDICATE;

            pred.Invert();
            return pred;
        }

        static public Predicate CreateGreaterPredicate(object o, object v)
        {
            bool lhsIsGen = o is IGenerator;
            bool rhsIsGen = v is IGenerator;

            if (lhsIsGen || rhsIsGen)
            {
                if (lhsIsGen && rhsIsGen)
                {
                    object lhs = ((IGenerator)o).Evaluate();
                    object rhs = ((IGenerator)v).Evaluate();
                    return Comparer.Default.Compare(lhs, rhs) > 0
                        ? TRUE_PREDICATE : FALSE_PREDICATE;
                }

                IFunctor func = lhsIsGen ? (IFunctor)v : (IFunctor)o;
                IGenerator gen = lhsIsGen ? (IGenerator)o : (IGenerator)v;

                return new FunctorGreaterGeneratorPredicate(func, gen);
            }

            return new FunctorGreaterFunctorPredicate((IFunctor)o, (IFunctor)v);
        }

        static public Predicate CreateGreaterEqualsPredicate(object o, object v)
        {
            Predicate pred = CreateLesserPredicate(o, v);
            if (pred.Equals(TRUE_PREDICATE))
                return FALSE_PREDICATE;
            if (pred.Equals(FALSE_PREDICATE))
                return TRUE_PREDICATE;

            pred.Invert();
            return pred;
        }

        static public Predicate CreateLikePatternPredicate(object o, object pattern)
        {
            IFunctor functor = o as IFunctor;
            IGenerator generator = pattern as IGenerator;
            return new FunctorLikePatternPredicate(functor, generator);
        }

        static public Predicate CreateLesserPredicate(object o, object v)
        {
            bool lhsIsGen = o is IGenerator;
            bool rhsIsGen = v is IGenerator;

            if (lhsIsGen || rhsIsGen)
            {
                if (lhsIsGen && rhsIsGen)
                {
                    object lhs = ((IGenerator)o).Evaluate();
                    object rhs = ((IGenerator)v).Evaluate();
                    return Comparer.Default.Compare(lhs, rhs) < 0
                        ? TRUE_PREDICATE : FALSE_PREDICATE;
                }

                IFunctor func = lhsIsGen ? (IFunctor)v : (IFunctor)o;
                IGenerator gen = lhsIsGen ? (IGenerator)o : (IGenerator)v;

                return new FunctorLesserGeneratorPredicate(func, gen);
            }

            return new FunctorLesserFunctorPredicate((IFunctor)o, (IFunctor)v);
        }
        static public Predicate CreateLesserEqualsPredicate(object o, object v)
        {
            Predicate pred = CreateGreaterPredicate(o, v);
            if (pred.Equals(TRUE_PREDICATE))
                return FALSE_PREDICATE;
            if (pred.Equals(FALSE_PREDICATE))
                return TRUE_PREDICATE;

            pred.Invert();
            return pred;
        }
    }

    #region /		Generators		/

    #endregion

    #region /		Functors		/

    #endregion

    #region /		Unary Predicates		/

    #endregion

    #region /		Predicates		/

    #endregion

    #region /		Binary Predicates		/

    #endregion
}
