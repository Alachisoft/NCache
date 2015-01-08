// Copyright (c) 2015 Alachisoft
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
using System.IO;
using System.Runtime.Serialization;
using System.Diagnostics;

using Alachisoft.NCache.Common;
using Alachisoft.NCache.Parser;
using Alachisoft.NCache.Caching.Queries.Filters;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Logger;


namespace Alachisoft.NCache.Caching.Queries
{
  

    public class NCQLParserRule
    {
         ILogger _ncacheLog;

        public ILogger NCacheLog
        {
            get { return _ncacheLog; }
        }
        
		public NCQLParserRule()
		{

		}

        public NCQLParserRule(ILogger NCacheLog)
		{
            this._ncacheLog = NCacheLog;
		}
      
		/// Implements <Query> ::= SELECT <TypeIdentifier>      
		public  Reduction CreateRULE_QUERY_SELECT(Reduction reduction)
		{
			object selectType = ((Reduction)((Token)reduction.GetToken(1)).Data).Tag;

            Predicate selectTypePredicate = selectType as Predicate;

            if (selectTypePredicate == null)
                reduction.Tag = new IsOfTypePredicate(selectType.ToString());
            else
                reduction.Tag = selectTypePredicate;
			
            if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_QUERY_SELECT");
			return null;
		}

        ///Implements <Query> ::= SELECT <AggregateFunction>
        public Reduction CreateRULE_QUERY_SELECT2(Reduction reduction)
        {
            return CreateRULE_QUERY_SELECT(reduction);
        }

        public Reduction CreateRULE_DELETEPARAMS_DOLLARTEXTDOLLAR(Reduction reduction)
        {
            reduction.Tag = "System.String";
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_OBJECTTYPE_DOLLARTEXTDOLLAR");
            return null;
        }

        public Reduction CreateRULE_DELETEPARAMS(Reduction reduction)
        {
            return null;
        }

		/// Implements <Query> ::= SELECT <TypeIdentifier> WHERE <Expression>      
        public  Reduction CreateRULE_QUERY_SELECT_WHERE(Reduction reduction)
		{
            //selectType can be one of the following depending on the query text: -
            //1. A plain string that is the name of Type; we can build IsOfTypePredicate from this.
            //2. AggregateFunctionPredicate that has IsOfTypePredicate set as its ChildPredicate // cant be this, grammer changed
            //3. IsOfTypePredicate

			object selectType = ((Reduction)reduction.GetToken(1).Data).Tag;

            Predicate lhs = null;
			Predicate rhs = (Predicate)((Reduction)reduction.GetToken(3).Data).Tag;
            Predicate selectTypePredicate = selectType as Predicate;
            Predicate result = null;

            //1. selectType is string 
            if (selectTypePredicate == null)
            {
                lhs = new IsOfTypePredicate(selectType.ToString());
                result = ExpressionBuilder.CreateLogicalAndPredicate(lhs, rhs);
            }
            ////2. selectType is AggregateFunctionPredicate
            //3. selectType is IsOfTypePredicate
            else
            {
                lhs = selectTypePredicate;
                result = ExpressionBuilder.CreateLogicalAndPredicate(lhs, rhs);
            }

            reduction.Tag = result;
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_QUERY_SELECT_WHERE");

			return null;
		}

        public Reduction CreateRULE_QUERY_SELECT_WHERE2(Reduction reduction)
        {
            //selectType can be one of the following depending on the query text: -
            // AggregateFunctionPredicate that has IsOfTypePredicate set as its ChildPredicate

            object selectType = ((Reduction)reduction.GetToken(1).Data).Tag;

            Predicate lhs = null;
            Predicate rhs = (Predicate)((Reduction)reduction.GetToken(3).Data).Tag;
            Predicate selectTypePredicate = selectType as Predicate;
            Predicate result = null;

            AggregateFunctionPredicate parentPredicate = selectTypePredicate as AggregateFunctionPredicate;
            lhs = parentPredicate.ChildPredicate;
            parentPredicate.ChildPredicate = ExpressionBuilder.CreateLogicalAndPredicate(lhs, rhs);
            result = parentPredicate;

            reduction.Tag = result;
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_QUERY_SELECT_WHERE2");
            return null;
        }


		/// Implements <Expression> ::= <OrExpr>      
        public  Reduction CreateRULE_EXPRESSION(Reduction reduction)
		{
			reduction.Tag = ((Reduction)((Token)reduction.GetToken(0)).Data).Tag;
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_EXPRESSION");
			return null;
		}
		
		/// Implements <OrExpr> ::= <OrExpr> OR <AndExpr>      
        public  Reduction CreateRULE_OREXPR_OR(Reduction reduction)
		{
			Predicate lhs = (Predicate)((Reduction)reduction.GetToken(0).Data).Tag;
			Predicate rhs = (Predicate)((Reduction)reduction.GetToken(2).Data).Tag;
			reduction.Tag = ExpressionBuilder.CreateLogicalOrPredicate(lhs, rhs);
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_OREXPR_OR");
			return null;
		}
		
		/// Implements <OrExpr> ::= <AndExpr>      
        public  Reduction CreateRULE_OREXPR(Reduction reduction)
		{
			reduction.Tag = ((Reduction)((Token)reduction.GetToken(0)).Data).Tag;
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_OREXPR");
			return null;
		}
		
		/// Implements <AndExpr> ::= <AndExpr> AND <UnaryExpr>      
        public  Reduction CreateRULE_ANDEXPR_AND(Reduction reduction)
		{
			Predicate lhs = (Predicate)((Reduction)reduction.GetToken(0).Data).Tag;
			Predicate rhs = (Predicate)((Reduction)reduction.GetToken(2).Data).Tag;
			reduction.Tag = ExpressionBuilder.CreateLogicalAndPredicate(lhs, rhs);
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_ANDEXPR_AND");
			return null;
		}
		
		/// Implements <AndExpr> ::= <UnaryExpr>      
        public  Reduction CreateRULE_ANDEXPR(Reduction reduction)
		{
			reduction.Tag = ((Reduction)((Token)reduction.GetToken(0)).Data).Tag;
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_ANDEXPR");
			return null;
		}
		
		/// Implements <UnaryExpr> ::= NOT <CompareExpr>      
        public  Reduction CreateRULE_UNARYEXPR_NOT(Reduction reduction)
		{
			Predicate pred = (Predicate)((Reduction)((Token)reduction.GetToken(1)).Data).Tag;
			pred.Invert();
			reduction.Tag = pred;
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_UNARYEXPR_NOT");
			return null;
		}
		
		/// Implements <UnaryExpr> ::= <CompareExpr>      
        public  Reduction CreateRULE_UNARYEXPR(Reduction reduction)
		{
			reduction.Tag = ((Reduction)((Token)reduction.GetToken(0)).Data).Tag;
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_UNARYEXPR");
			return null;
		}
		
		/// Implements <CompareExpr> ::= <Value> '=' <Value>      
        public  Reduction CreateRULE_COMPAREEXPR_EQ(Reduction reduction)
		{
			return CreateRULE_COMPAREEXPR_EQEQ(reduction);
		}
		
		/// Implements <CompareExpr> ::= <Value> '!=' <Value>      
        public  Reduction CreateRULE_COMPAREEXPR_EXCLAMEQ(Reduction reduction)
		{
			return CreateRULE_COMPAREEXPR_LTGT(reduction);
		}
		
		/// Implements <CompareExpr> ::= <Value> '==' <Value>      
        public  Reduction CreateRULE_COMPAREEXPR_EQEQ(Reduction reduction)
		{
			object lhs = ((Reduction)((Token)reduction.GetToken(0)).Data).Tag;
			object rhs = ((Reduction)((Token)reduction.GetToken(2)).Data).Tag;
			
			reduction.Tag = ExpressionBuilder.CreateEqualsPredicate(lhs, rhs);
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_COMPAREEXPR_EQEQ");
			return null;
		}
		
		/// Implements <CompareExpr> ::= <Value> '<>' <Value>      
        public  Reduction CreateRULE_COMPAREEXPR_LTGT(Reduction reduction)
		{
			object lhs = ((Reduction)((Token)reduction.GetToken(0)).Data).Tag;
			object rhs = ((Reduction)((Token)reduction.GetToken(2)).Data).Tag;
			
			reduction.Tag = ExpressionBuilder.CreateNotEqualsPredicate(lhs, rhs);
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_COMPAREEXPR_LTGT");
			return null;
		}
		
		/// Implements <CompareExpr> ::= <Value> '<' <Value>      
        public  Reduction CreateRULE_COMPAREEXPR_LT(Reduction reduction)
		{
			object lhs = ((Reduction)((Token)reduction.GetToken(0)).Data).Tag;
			object rhs = ((Reduction)((Token)reduction.GetToken(2)).Data).Tag;
			
			reduction.Tag = ExpressionBuilder.CreateLesserPredicate(lhs, rhs);
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_COMPAREEXPR_LT");
			return null;
		}
		
		/// Implements <CompareExpr> ::= <Value> '>' <Value>      
        public  Reduction CreateRULE_COMPAREEXPR_GT(Reduction reduction)
		{
			object lhs = ((Reduction)((Token)reduction.GetToken(0)).Data).Tag;
			object rhs = ((Reduction)((Token)reduction.GetToken(2)).Data).Tag;
			
			reduction.Tag = ExpressionBuilder.CreateGreaterPredicate(lhs, rhs);
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_COMPAREEXPR_GT");
			return null;
		}
		
		/// Implements <CompareExpr> ::= <Value> '<=' <Value>      
        public  Reduction CreateRULE_COMPAREEXPR_LTEQ(Reduction reduction)
		{
			object lhs = ((Reduction)((Token)reduction.GetToken(0)).Data).Tag;
			object rhs = ((Reduction)((Token)reduction.GetToken(2)).Data).Tag;
			
			reduction.Tag = ExpressionBuilder.CreateLesserEqualsPredicate(lhs, rhs);
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_COMPAREEXPR_LTEQ");
			return null;
		}
		
		/// Implements <CompareExpr> ::= <Value> '>=' <Value>      
        public  Reduction CreateRULE_COMPAREEXPR_GTEQ(Reduction reduction)
		{
			object lhs = ((Reduction)((Token)reduction.GetToken(0)).Data).Tag;
			object rhs = ((Reduction)((Token)reduction.GetToken(2)).Data).Tag;
			
			reduction.Tag = ExpressionBuilder.CreateGreaterEqualsPredicate(lhs, rhs);
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_COMPAREEXPR_GTEQ");
			return null;
		}
		
		/// Implements <CompareExpr> ::= <Value> LIKE StringLiteral      
        public  Reduction CreateRULE_COMPAREEXPR_LIKE_STRINGLITERAL(Reduction reduction)
		{
            object lhs = ((Reduction)((Token)reduction.GetToken(0)).Data).Tag;
            RuntimeValue rhs = new RuntimeValue();
            Predicate predicate = ExpressionBuilder.CreateLikePatternPredicate(lhs, rhs);
			reduction.Tag = predicate;
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_COMPAREEXPR_LIKE_STRINGLITERAL");
			return null;
		}
		
		/// Implements <CompareExpr> ::= <Value> NOT LIKE StringLiteral      
        public  Reduction CreateRULE_COMPAREEXPR_NOT_LIKE_STRINGLITERAL(Reduction reduction)
		{
            object lhs = ((Reduction)((Token)reduction.GetToken(0)).Data).Tag;
            RuntimeValue rhs = new RuntimeValue();
            Predicate predicate = ExpressionBuilder.CreateLikePatternPredicate(lhs, rhs);
			predicate.Invert();
			reduction.Tag = predicate;
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_COMPAREEXPR_LIKE_STRINGLITERAL");
			return null;
		}
		
		/// Implements <CompareExpr> ::= <Value> IN <InList>      
        public  Reduction CreateRULE_COMPAREEXPR_IN(Reduction reduction)
		{
            object lhs = ((Reduction)((Token)reduction.GetToken(0)).Data).Tag;
            IsInListPredicate pred = ((Reduction)((Token)reduction.GetToken(2)).Data).Tag as IsInListPredicate;
            pred.Functor = lhs as IFunctor;
            reduction.Tag = pred;
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_COMPAREEXPR_IN");
			return null;
		}
		
		/// Implements <CompareExpr> ::= <Value> NOT IN <InList>      
        public  Reduction CreateRULE_COMPAREEXPR_NOT_IN(Reduction reduction)
		{
            object lhs = ((Reduction)((Token)reduction.GetToken(0)).Data).Tag;
			IsInListPredicate pred = (IsInListPredicate)
				((Reduction)((Token)reduction.GetToken(3)).Data).Tag;
			pred.Invert();
            pred.Functor = lhs as IFunctor;
			reduction.Tag = pred;
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_COMPAREEXPR_NOT_IN");
			return null;
		}
		
		/// Implements <CompareExpr> ::= <Value> IS NUll      
        public  Reduction CreateRULE_COMPAREEXPR_IS_NULL(Reduction reduction)
		{
            object lhs = ((Reduction)((Token)reduction.GetToken(0)).Data).Tag;
			Predicate predicate = new IsNullPredicate(lhs as IFunctor);
			reduction.Tag = predicate;
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("RULE_COMPAREEXPR_IS_NULL");
			return null;
		}
		
		/// Implements <CompareExpr> ::= <Value> IS NOT NUll      
        public  Reduction CreateRULE_COMPAREEXPR_IS_NOT_NULL(Reduction reduction)
		{
            object lhs = ((Reduction)((Token)reduction.GetToken(0)).Data).Tag;
			Predicate predicate = new IsNullPredicate(lhs as IFunctor);
			predicate.Invert();
			reduction.Tag = predicate;
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("RULE_COMPAREEXPR_IS_NOT_NULL");
			return null;
		}
		
		/// Implements <CompareExpr> ::= '(' <Expression> ')'      
        public  Reduction CreateRULE_COMPAREEXPR_LPARAN_RPARAN(Reduction reduction)
		{
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_COMPAREEXPR_LPARAN_RPARAN");
			reduction.Tag = ((Reduction)((Token)reduction.GetToken(1)).Data).Tag;
			return null;
		}
		
		/// Implements <Value> ::= <ObjectValue>      
        public  Reduction CreateRULE_VALUE(Reduction reduction)
		{
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_VALUE");
			reduction.Tag = ((Reduction)((Token)reduction.GetToken(0)).Data).Tag;
			return null;
		}
		
		/// Implements <Value> ::= '-' <NumLiteral>      
        public  Reduction CreateRULE_VALUE_MINUS(Reduction reduction)
		{
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("RULE_VALUE_MINUS");
			object functor = ((Reduction)((Token)reduction.GetToken(0)).Data).Tag;
			if(functor is IntegerConstantValue)
				reduction.Tag =  new IntegerConstantValue("-" + reduction.GetToken(1).Data.ToString());
			else
				reduction.Tag =  new DoubleConstantValue("-" + reduction.GetToken(1).Data.ToString());
			return null;
		}
		
		/// Implements <Value> ::= <NumLiteral>      
        public  Reduction CreateRULE_VALUE2(Reduction reduction)
		{
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_VALUE2");
			reduction.Tag = ((Reduction)((Token)reduction.GetToken(0)).Data).Tag;
			return null;
		}
		
		/// Implements <Value> ::= <StrLiteral>      
        public  Reduction CreateRULE_VALUE3(Reduction reduction)
		{
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_VALUE3");
			reduction.Tag = ((Reduction)((Token)reduction.GetToken(0)).Data).Tag;
			return null;
		}
		
		/// Implements <Value> ::= true      
        public  Reduction CreateRULE_VALUE_TRUE(Reduction reduction)
		{
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_VALUE_TRUE");
			reduction.Tag = new TrueValue();
			return null;
		}
		
		/// Implements <Value> ::= false      
        public  Reduction CreateRULE_VALUE_FALSE(Reduction reduction)
		{
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_VALUE_FALSE");
			reduction.Tag = new FalseValue();
			return null;
		}
		
		/// Implements <Value> ::= <Date>      
        public  Reduction CreateRULE_VALUE4(Reduction reduction)
		{
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_VALUE4");
			reduction.Tag = ((Reduction)((Token)reduction.GetToken(0)).Data).Tag;
			return null;
		}
		
		/// Implements <Date> ::= DateTime '.' now      
        public  Reduction CreateRULE_DATE_DATETIME_DOT_NOW(Reduction reduction)
		{
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_DATE_DATETIME_DOT_NOW");
			reduction.Tag = new DateTimeConstantValue();
			return null;
		}
		
		/// Implements <Date> ::= DateTime '(' <StrLiteral> ')'      
        public  Reduction CreateRULE_DATE_DATETIME_LPARAN_RPARAN(Reduction reduction)
		{
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_DATE_DATETIME_LPARAN_RPARAN");
			reduction.Tag = new DateTimeConstantValue(reduction.GetToken(2).Data.ToString());
			return null;
		}
		
		/// Implements <StrLiteral> ::= StringLiteral      
        public  Reduction CreateRULE_STRLITERAL_STRINGLITERAL(Reduction reduction)
		{
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_STRLITERAL_STRINGLITERAL");
			reduction.Tag = new StringConstantValue(reduction.GetToken(0).Data.ToString());
			return null;
		}
		
		/// Implements <StrLiteral> ::= NUll      
        public  Reduction CreateRULE_STRLITERAL_NULL(Reduction reduction)
		{
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_STRLITERAL_NULL");
			reduction.Tag = new NullValue();
			return null;
		}
		
		/// Implements <StrLiteral> ::= ?      
        public  Reduction CreateRULE_STRLITERAL_QUESTION(Reduction reduction)
		{
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_STRLITERAL_QUESTION");
			reduction.Tag = new RuntimeValue();
			return null;
		}

		/// Implements <NumLiteral> ::= IntegerLiteral      
        public  Reduction CreateRULE_NUMLITERAL_INTEGERLITERAL(Reduction reduction)
		{
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_NUMLITERAL_INTEGERLITERAL");
			reduction.Tag = new IntegerConstantValue(reduction.GetToken(0).Data.ToString());
			return null;
		}
		
		/// Implements <NumLiteral> ::= RealLiteral      
        public  Reduction CreateRULE_NUMLITERAL_REALLITERAL(Reduction reduction)
		{
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_NUMLITERAL_REALLITERAL");
			reduction.Tag = new DoubleConstantValue(reduction.GetToken(0).Data.ToString());
			return null;
		}
		
		/// Implements <TypeIdentifier> ::= '*'      
        public  Reduction CreateRULE_OBJECTTYPE_TIMES(Reduction reduction)
		{
            reduction.Tag = "*";
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_OBJECTTYPE_TIMES");
			return null;
		}

        /// Implements <TypeIdentifier> ::= '$Text$'      
        public  Reduction CreateRULE_OBJECTTYPE_DOLLARTEXTDOLLAR(Reduction reduction)
        {
            reduction.Tag = "System.String";
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_OBJECTTYPE_DOLLARTEXTDOLLAR");
            return null;
        }
		
		/// Implements <TypeIdentifier> ::= <Identifier>      
        public  Reduction CreateRULE_OBJECTTYPE_IDENTIFIER(Reduction reduction)
		{
			reduction.Tag = ((Token)reduction.GetToken(0)).Data;
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_TYPEIDENTIFIER_IDENTIFIER");
			return null;
		}
		
		/// Implements <TypeIdentifier> ::= <TypeIdentifier> '.' <Identifier>      
        public  Reduction CreateRULE_OBJECTTYPE_IDENTIFIER_DOT(Reduction reduction)
		{
            string lhs = ((Reduction)reduction.GetToken(0).Data).Tag.ToString();
            string rhs = reduction.GetToken(2).Data.ToString();
			reduction.Tag = lhs + "." + rhs;
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_OBJECTTYPE_IDENTIFIER_DOT");
			return null;
		}

        /// Implements <ObjectType> ::= <AggregateFunction>      
        public  Reduction CreateRULE_OBJECTTYPE2(Reduction reduction)
		{
            return null;
		}
		
        /// Implements <ObjectAttribute> ::= Identifier      
        public  Reduction CreateRULE_OBJECTATTRIBUTE_IDENTIFIER(Reduction reduction)
		{
            string memberName = reduction.GetToken(0).Data.ToString();
            reduction.Tag = memberName;
            if (NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_OBJECTATTRIBUTE_IDENTIFIER");
            return null;
		}

		/// Implements <ObjectValue> ::= Keyword      
        public  Reduction CreateRULE_OBJECTVALUE_KEYWORD(Reduction reduction)
		{
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_OBJECTVALUE_KEYWORD");
			reduction.Tag = new IdentityFunction();
			return null;
		}
		
		/// Implements <ObjectValue> ::= Keyword '.' <Property>      
        public  Reduction CreateRULE_OBJECTVALUE_KEYWORD_DOT(Reduction reduction)
		{
			object pred = ((Reduction)((Token)reduction.GetToken(2)).Data).Tag;
			if(pred is IFunctor)
				reduction.Tag = pred;
			else
			{
				reduction.Tag = new MemberFunction(pred.ToString());
			}
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_IDENTIFIER_KEYWORD");
			return null;
		}
		
		/// Implements <Property> ::= <Property> '.' <Identifier>      
        public  Reduction CreateRULE_PROPERTY_DOT(Reduction reduction)
		{
			IFunctor nested = 
				new MemberFunction(((Reduction)((Token)reduction.GetToken(2)).Data).Tag.ToString());
			IFunctor func = 
				new MemberFunction(((Reduction)((Token)reduction.GetToken(0)).Data).Tag.ToString());

			reduction.Tag = new CompositeFunction(func, nested);
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("RULE_PROPERTY_DOT -> " + reduction.Tag);
			return null;
		}
		
		/// Implements <Property> ::= <Identifier>      
        public  Reduction CreateRULE_PROPERTY(Reduction reduction)
		{
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_PROPERTY");
			reduction.Tag = ((Token)reduction.GetToken(0)).Data;
			return null;
		}
		
		/// Implements <Identifier> ::= Identifier      
        public  Reduction CreateRULE_IDENTIFIER_IDENTIFIER(Reduction reduction)
		{
			reduction.Tag = ((Token)reduction.GetToken(0)).Data.ToString();
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("RULE_IDENTIFIER_IDENTIFIER -> " + reduction.Tag);
			return null;
		}
		
		/// Implements <Identifier> ::= Keyword      
        public  Reduction CreateRULE_IDENTIFIER_KEYWORD(Reduction reduction)
		{
			reduction.Tag = ((Token)reduction.GetToken(0)).Data;
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("RULE_IDENTIFIER_KEYWORD -> " + reduction.Tag);
			return null;
		}
		
		/// Implements <InList> ::= '(' <ListType> ')'      
        public  Reduction CreateRULE_INLIST_LPARAN_RPARAN(Reduction reduction)
		{
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_INLIST_LPARAN_RPARAN");
            object obj = ((Reduction)((Token)reduction.GetToken(1)).Data).Tag;
            
            if (obj is ConstantValue || obj is RuntimeValue)
            {
                IsInListPredicate pred = new IsInListPredicate();
                pred.Append(obj);
                reduction.Tag = pred;
            }
            else
            {
                reduction.Tag = ((Reduction)((Token)reduction.GetToken(1)).Data).Tag;
            }
			
            return null;
		}
		
		/// Implements <ListType> ::= <NumLiteralList>      
        public  Reduction CreateRULE_LISTTYPE(Reduction reduction)
		{
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_LISTTYPE");
			reduction.Tag = ((Reduction)((Token)reduction.GetToken(0)).Data).Tag;
			return null;
		}
		
		/// Implements <ListType> ::= <StrLiteralList>      
        public  Reduction CreateRULE_LISTTYPE2(Reduction reduction)
		{
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_LISTTYPE2");
			reduction.Tag = ((Reduction)((Token)reduction.GetToken(0)).Data).Tag;
			return null;
		}
		
		/// Implements <ListType> ::= <DateList>      
        public  Reduction CreateRULE_LISTTYPE3(Reduction reduction)
		{
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_LISTTYPE3");
			reduction.Tag = ((Reduction)((Token)reduction.GetToken(0)).Data).Tag;
			return null;
		}
		
		/// Implements <NumLiteralList> ::= <NumLiteral> ',' <NumLiteralList>      
        public  Reduction CreateRULE_NUMLITERALLIST_COMMA(Reduction reduction)
		{
			return CreateInclusionList(reduction);
		}
		
		/// Implements <NumLiteralList> ::= <NumLiteral>      
        public  Reduction CreateRULE_NUMLITERALLIST(Reduction reduction)
		{
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_NUMLITERALLIST");
            IsInListPredicate pred = new IsInListPredicate();
            pred.Append(((Reduction)reduction.GetToken(0).Data).Tag);
            reduction.Tag = pred;
			return null;
		}
		
		/// Implements <StrLiteralList> ::= <StrLiteral> ',' <StrLiteralList>      
        public  Reduction CreateRULE_STRLITERALLIST_COMMA(Reduction reduction)
		{
			return CreateInclusionList(reduction);
		}
		
		/// Implements <StrLiteralList> ::= <StrLiteral>      
        public  Reduction CreateRULE_STRLITERALLIST(Reduction reduction)
		{
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_STRLITERALLIST");
			reduction.Tag = ((Reduction)((Token)reduction.GetToken(0)).Data).Tag;
			return null;
		}

		/// Implements <DateList> ::= <Date> ',' <DateList>      
        public  Reduction CreateRULE_DATELIST_COMMA(Reduction reduction)
		{
			return CreateInclusionList(reduction);
		}
		
		/// Implements <DateList> ::= <Date>      
        public  Reduction CreateRULE_DATELIST(Reduction reduction)
		{
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_DATELIST");
			reduction.Tag = ((Reduction)((Token)reduction.GetToken(0)).Data).Tag;
			return null;
		}

		//self create
		//=========================
        public  Reduction CreateRULE_ATRRIB(Reduction reduction)
		{
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("CreateRULE_ATRRIB");
			reduction.Tag = ((Reduction)((Token)reduction.GetToken(0)).Data).Tag;
			return null;
		}

        public  Reduction CreateRULE_DATE_DATETIME_LPARAN_STRINGLITERAL_RPARAN(Reduction reduction)
		{
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("RULE_DATE_DATETIME_LPARAN_STRINGLITERAL_RPARAN");
            string dateTime = reduction.GetToken(2).Data.ToString().Trim('\'');
            reduction.Tag = new DateTimeConstantValue(dateTime);
			return null;
		}

        public  Reduction CreateRULE_OBJECTVALUE_KEYWORD_DOT_IDENTIFIER(Reduction reduction)
		{
			if(NCacheLog.IsInfoEnabled) NCacheLog.Info("RULE_OBJECTVALUE_KEYWORD_DOT_IDENTIFIER");
			string memName = reduction.GetToken(2).Data.ToString();
            reduction.Tag = new MemberFunction(memName);
			return null;
		}

        /// Implements <SumFunction> ::= 'SUM(' <TypePlusAttribute> ')'      
        public  Reduction CreateRULE_SUMFUNCTION_SUMLPARAN_RPARAN(Reduction reduction)
		{
            Reduction typePlusAttributeReduction = (Reduction)reduction.GetToken(1).Data;

            string typeName = ((Reduction)typePlusAttributeReduction.GetToken(0).Data).Tag as string;
            string memberName = ((Reduction)typePlusAttributeReduction.GetToken(2).Data).Tag as string;

            Predicate childPredicate = new IsOfTypePredicate(typeName);
            AggregateFunctionPredicate sumFunctionPredicate = ExpressionBuilder.CreateSumFunctionPredicate(memberName) as AggregateFunctionPredicate;
            sumFunctionPredicate.ChildPredicate = childPredicate;
            reduction.Tag = sumFunctionPredicate;
            return null; 
		}
		
		/// Implements <CountFunction> ::= 'COUNT(' <Property> ')'      
        public  Reduction CreateRULE_COUNTFUNCTION_COUNTLPARAN_TIMES_RPARAN(Reduction reduction)
		{
            string typeName = ((Reduction)reduction.GetToken(1).Data).Tag as string;
            Predicate childPredicate = new IsOfTypePredicate(typeName);
            AggregateFunctionPredicate countFunctionPredicate = ExpressionBuilder.CreateCountFunctionPredicate() as AggregateFunctionPredicate;
            countFunctionPredicate.ChildPredicate = childPredicate;
            reduction.Tag = countFunctionPredicate;
            return null; 
		}

        /// Implements <MinFunction> ::= 'MIN(' <TypePlusAttribute> ')'      
        public  Reduction CreateRULE_MINFUNCTION_MINLPARAN_RPARAN(Reduction reduction)
		{
            Reduction typePlusAttributeReduction = (Reduction)reduction.GetToken(1).Data;

            string typeName = ((Reduction)typePlusAttributeReduction.GetToken(0).Data).Tag as string;
            string memberName = ((Reduction)typePlusAttributeReduction.GetToken(2).Data).Tag as string;

            Predicate childPredicate = new IsOfTypePredicate(typeName);
            AggregateFunctionPredicate minFunctionPredicate = ExpressionBuilder.CreateMinFunctionPredicate(memberName) as AggregateFunctionPredicate;
            minFunctionPredicate.ChildPredicate = childPredicate;
            reduction.Tag = minFunctionPredicate;
            return null;  
		}

        /// Implements <MaxFunction> ::= 'MAX(' <TypePlusAttribute> ')'      
        public  Reduction CreateRULE_MAXFUNCTION_MAXLPARAN_RPARAN(Reduction reduction)
		{
            Reduction typePlusAttributeReduction = (Reduction)reduction.GetToken(1).Data;

            string typeName = ((Reduction)typePlusAttributeReduction.GetToken(0).Data).Tag as string;
            string memberName = ((Reduction)typePlusAttributeReduction.GetToken(2).Data).Tag as string;

            Predicate childPredicate = new IsOfTypePredicate(typeName);
            AggregateFunctionPredicate maxFunctionPredicate = ExpressionBuilder.CreateMaxFunctionPredicate(memberName) as AggregateFunctionPredicate;
            maxFunctionPredicate.ChildPredicate = childPredicate;
            reduction.Tag = maxFunctionPredicate;
            return null;  
		}
		
		/// Implements <AverageFunction> ::= 'AVG(' <TypePlusAttribute> ')'      
        public  Reduction CreateRULE_AVERAGEFUNCTION_AVGLPARAN_RPARAN(Reduction reduction)
		{
            Reduction typePlusAttributeReduction = (Reduction)reduction.GetToken(1).Data;

            string typeName = ((Reduction)typePlusAttributeReduction.GetToken(0).Data).Tag as string;
            string memberName = ((Reduction)typePlusAttributeReduction.GetToken(2).Data).Tag as string;

            Predicate childPredicate = new IsOfTypePredicate(typeName);
            AggregateFunctionPredicate avgFunctionPredicate = ExpressionBuilder.CreateAverageFunctionPredicate(memberName) as AggregateFunctionPredicate;
            avgFunctionPredicate.ChildPredicate = childPredicate;
            reduction.Tag = avgFunctionPredicate;
            return null; 
		}

        /// Implements <CompareExpr> ::= <Value> LIKE Question      
        public Reduction CreateRULE_COMPAREEXPR_LIKE_QUESTION(Reduction reduction)
        {
            return CreateRULE_COMPAREEXPR_LIKE_STRINGLITERAL(reduction);
        }

        /// Implements <CompareExpr> ::= <Value> NOT LIKE Question      
        public Reduction CreateRULE_COMPAREEXPR_NOT_LIKE_QUESTION(Reduction reduction)
        {
            return CreateRULE_COMPAREEXPR_NOT_LIKE_STRINGLITERAL(reduction);
        }

        /// Implements <CompareExpr> ::= <Value> ObjectType   
        public Reduction CreateRULE_OBJECTTYPE(Reduction reduction)
        {
            return null;
        }

        /// Implements <CompareExpr> ::= <Value> Property DOT Identifier 
        public Reduction CreateRULE_PROPERTY_DOT_IDENTIFIER(Reduction reduction)
        {
            return CreateRULE_OBJECTTYPE_IDENTIFIER_DOT(reduction);
        }

        /// Implements <CompareExpr> ::= <Value> Property Identifier
        public Reduction CreateRULE_PROPERTY_IDENTIFIER(Reduction reduction)
        {
            return CreateRULE_OBJECTTYPE_IDENTIFIER(reduction);
        }

        /// Implements <CompareExpr> ::= <Value> Type Plus Attribute DOT   
        public Reduction CreateRULE_TYPEPLUSATTRIBUTE_DOT(Reduction reduction)
        {
            return null;
        }

        /// Implements <CompareExpr> ::= <Value> Aggregate Function
        public Reduction CreateRULE_AGGREGATEFUNCTION(Reduction reduction)
        {
            return null;
        }

        /// Implements <CompareExpr> ::= <Value> Aggregate Function2  
        public Reduction CreateRULE_AGGREGATEFUNCTION2(Reduction reduction)
        {
            return null;
        }

        /// Implements <CompareExpr> ::= <Value> Aggregate Function3
        public Reduction CreateRULE_AGGREGATEFUNCTION3(Reduction reduction)
        {
            return null;
        }

        /// Implements <CompareExpr> ::= <Value> Aggregate Function4
        public Reduction CreateRULE_AGGREGATEFUNCTION4(Reduction reduction)
        {
            return null;
        }

        /// Implements <CompareExpr> ::= <Value> Aggregate Function5
        public Reduction CreateRULE_AGGREGATEFUNCTION5(Reduction reduction)
        {
            return null;
        }

        /// Implements <CompareExpr> ::= <Value> COUNT Function COUNT LParan RParan
        public Reduction CreateRULE_COUNTFUNCTION_COUNTLPARAN_RPARAN(Reduction reduction)
        {
            return CreateRULE_COUNTFUNCTION_COUNTLPARAN_TIMES_RPARAN(reduction);
        }

		//========================


        public  Reduction CreateInclusionList(Reduction reduction)
		{
			object tag = ((Reduction)reduction.GetToken(2).Data).Tag;
			IsInListPredicate inc = null;
			if(tag is IsInListPredicate)
				inc = tag as IsInListPredicate;
			else
			{
                inc = new IsInListPredicate();
				inc.Append(tag);
			}
			inc.Append(((Reduction)reduction.GetToken(0).Data).Tag);
			reduction.Tag = inc;
			return null;
		}


        ///Implements <ObjectAttribute> ::= Keyword '.' Identifier
        public Reduction CreateRULE_OBJECTATTRIBUTE_KEYWORD_DOT_IDENTIFIER(Reduction reduction)
        {
            return CreateRULE_OBJECTVALUE_KEYWORD_DOT_IDENTIFIER(reduction);
        }
    }
}
