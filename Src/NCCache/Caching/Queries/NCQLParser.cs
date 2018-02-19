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
    public class NCQLParser : Alachisoft.NCache.Parser.Parser
    {
        ILogger _ncacheLog;
		NCQLParserRule _parserRule;

        ILogger NCacheLog
        {
            get { return _ncacheLog; }
        }

		public NCQLParser(string resourceName, ILogger NCacheLog)
		{
            this._ncacheLog = NCacheLog;
			_parserRule = new NCQLParserRule(NCacheLog);
            System.Reflection.Assembly asm = GetType().Assembly;
            Stream s = asm.GetManifestResourceStream(resourceName);
			base.LoadGrammar(s);
		}
       

        public ParseMessage Parse(TextReader Source, bool GenerateContext)
		{
        
            ParseMessage Response;
			bool done = false;

			OpenStream(Source);
			TrimReductions = true;

			do
			{
				Response = Parse();
				switch(Response)
				{
					case ParseMessage.LexicalError:
						//Cannot recognize token
						done = true;
						break;

					case ParseMessage.SyntaxError:
						//Expecting a different token
						foreach(Token t in GetTokens())
							if(NCacheLog.IsInfoEnabled) NCacheLog.Info(t.Name);
						done = true; // stop if there are multiple errors on one line
						break;

					case ParseMessage.Reduction:
						//Create a customized object to store the reduction
						if(GenerateContext)
							CurrentReduction = CreateNewObject(CurrentReduction);
						break;

					case ParseMessage.Accept:
						//Success!
						done = true;
						break;

					case ParseMessage.TokenRead:
						//You don't have to do anything here.
						break;

					case ParseMessage.InternalError:
						//INTERNAL ERROR! Something is horribly wrong.
						done = true;
						break;

					case ParseMessage.CommentError:
						//COMMENT ERROR! Unexpected end of file
						done = true;
						break;
				}
			} while(!done);
			
			CloseFile();
			return Response;
        }

       

        private Reduction CreateNewObject(Reduction reduction)
        {
			Reduction result = null;
			switch((RuleConstants)Enum.ToObject(typeof(RuleConstants), reduction.ParentRule.TableIndex))
			{
                case RuleConstants.RULE_QUERY_SELECT :
                ////<Query> ::= SELECT <ObjectType>
                result = _parserRule.CreateRULE_QUERY_SELECT(reduction);
                break;
                case RuleConstants.RULE_QUERY_SELECT_WHERE :
                ////<Query> ::= SELECT <ObjectType> WHERE <Expression>
                result = _parserRule.CreateRULE_QUERY_SELECT_WHERE(reduction);
                break;
                case RuleConstants.RULE_QUERY_SELECT2 :
                ////<Query> ::= SELECT <AggregateFunction>
                result = _parserRule.CreateRULE_QUERY_SELECT2(reduction);
                break;
                case RuleConstants.RULE_QUERY_SELECT_WHERE2 :
                ////<Query> ::= SELECT <AggregateFunction> WHERE <Expression>
                result = _parserRule.CreateRULE_QUERY_SELECT_WHERE2(reduction);
                break;
                case RuleConstants.RULE_EXPRESSION :
                ////<Expression> ::= <OrExpr>
                result = _parserRule.CreateRULE_EXPRESSION(reduction);
                break;
                case RuleConstants.RULE_OREXPR_OR :
                ////<OrExpr> ::= <OrExpr> OR <AndExpr>
                result = _parserRule.CreateRULE_OREXPR_OR(reduction);
                break;
                case RuleConstants.RULE_OREXPR :
                ////<OrExpr> ::= <AndExpr>
                result = _parserRule.CreateRULE_OREXPR(reduction);
                break;
                case RuleConstants.RULE_ANDEXPR_AND :
                ////<AndExpr> ::= <AndExpr> AND <UnaryExpr>
                result = _parserRule.CreateRULE_ANDEXPR_AND(reduction);
                break;
                case RuleConstants.RULE_ANDEXPR :
                ////<AndExpr> ::= <UnaryExpr>
                result = _parserRule.CreateRULE_ANDEXPR(reduction);
                break;
                case RuleConstants.RULE_UNARYEXPR_NOT :
                ////<UnaryExpr> ::= NOT <CompareExpr>
                result = _parserRule.CreateRULE_UNARYEXPR_NOT(reduction);
                break;
                case RuleConstants.RULE_UNARYEXPR :
                ////<UnaryExpr> ::= <CompareExpr>
                result = _parserRule.CreateRULE_UNARYEXPR(reduction);
                break;
                case RuleConstants.RULE_COMPAREEXPR_EQ :
                ////<CompareExpr> ::= <Atrrib> '=' <Value>
                result = _parserRule.CreateRULE_COMPAREEXPR_EQ(reduction);
                break;
                case RuleConstants.RULE_COMPAREEXPR_EXCLAMEQ :
                ////<CompareExpr> ::= <Atrrib> '!=' <Value>
                result = _parserRule.CreateRULE_COMPAREEXPR_EXCLAMEQ(reduction);
                break;
                case RuleConstants.RULE_COMPAREEXPR_EQEQ :
                ////<CompareExpr> ::= <Atrrib> '==' <Value>
                result = _parserRule.CreateRULE_COMPAREEXPR_EQEQ(reduction);
                break;
                case RuleConstants.RULE_COMPAREEXPR_LTGT :
                ////<CompareExpr> ::= <Atrrib> '<>' <Value>
                result = _parserRule.CreateRULE_COMPAREEXPR_LTGT(reduction);
                break;
                case RuleConstants.RULE_COMPAREEXPR_LT :
                ////<CompareExpr> ::= <Atrrib> '<' <Value>
                result = _parserRule.CreateRULE_COMPAREEXPR_LT(reduction);
                break;
                case RuleConstants.RULE_COMPAREEXPR_GT :
                ////<CompareExpr> ::= <Atrrib> '>' <Value>
                result = _parserRule.CreateRULE_COMPAREEXPR_GT(reduction);
                break;
                case RuleConstants.RULE_COMPAREEXPR_LTEQ :
                ////<CompareExpr> ::= <Atrrib> '<=' <Value>
                result = _parserRule.CreateRULE_COMPAREEXPR_LTEQ(reduction);
                break;
                case RuleConstants.RULE_COMPAREEXPR_GTEQ :
                ////<CompareExpr> ::= <Atrrib> '>=' <Value>
                result = _parserRule.CreateRULE_COMPAREEXPR_GTEQ(reduction);
                break;
                case RuleConstants.RULE_COMPAREEXPR_LIKE_STRINGLITERAL :
                ////<CompareExpr> ::= <Atrrib> LIKE StringLiteral
                result = _parserRule.CreateRULE_COMPAREEXPR_LIKE_STRINGLITERAL(reduction);
                break;
                case RuleConstants.RULE_COMPAREEXPR_LIKE_QUESTION :
                ////<CompareExpr> ::= <Atrrib> LIKE '?'
                result = _parserRule.CreateRULE_COMPAREEXPR_LIKE_QUESTION(reduction);
                break;
                case RuleConstants.RULE_COMPAREEXPR_NOT_LIKE_STRINGLITERAL :
                ////<CompareExpr> ::= <Atrrib> NOT LIKE StringLiteral
                result = _parserRule.CreateRULE_COMPAREEXPR_NOT_LIKE_STRINGLITERAL(reduction);
                break;
                case RuleConstants.RULE_COMPAREEXPR_NOT_LIKE_QUESTION :
                ////<CompareExpr> ::= <Atrrib> NOT LIKE '?'
                result = _parserRule.CreateRULE_COMPAREEXPR_NOT_LIKE_QUESTION(reduction);
                break;
                case RuleConstants.RULE_COMPAREEXPR_IN :
                ////<CompareExpr> ::= <Atrrib> IN <InList>
                result = _parserRule.CreateRULE_COMPAREEXPR_IN(reduction);
                break;
                case RuleConstants.RULE_COMPAREEXPR_NOT_IN :
                ////<CompareExpr> ::= <Atrrib> NOT IN <InList>
                result = _parserRule.CreateRULE_COMPAREEXPR_NOT_IN(reduction);
                break;
                case RuleConstants.RULE_COMPAREEXPR_IS_NULL :
                ////<CompareExpr> ::= <Atrrib> IS NULL
                result = _parserRule.CreateRULE_COMPAREEXPR_IS_NULL(reduction);
                break;
                case RuleConstants.RULE_COMPAREEXPR_IS_NOT_NULL :
                ////<CompareExpr> ::= <Atrrib> IS NOT NULL
                result = _parserRule.CreateRULE_COMPAREEXPR_IS_NOT_NULL(reduction);
                break;
                case RuleConstants.RULE_COMPAREEXPR_LPARAN_RPARAN :
                ////<CompareExpr> ::= '(' <Expression> ')'
                result = _parserRule.CreateRULE_COMPAREEXPR_LPARAN_RPARAN(reduction);
                break;
                case RuleConstants.RULE_ATRRIB :
                ////<Atrrib> ::= <ObjectValue>
                result = _parserRule.CreateRULE_ATRRIB(reduction);
                break;
                case RuleConstants.RULE_VALUE_MINUS :
                ////<Value> ::= '-' <NumLiteral>
                result = _parserRule.CreateRULE_VALUE_MINUS(reduction);
                break;
                case RuleConstants.RULE_VALUE :
                ////<Value> ::= <NumLiteral>
                result = _parserRule.CreateRULE_VALUE(reduction);
                break;
                case RuleConstants.RULE_VALUE2 :
                ////<Value> ::= <StrLiteral>
                result = _parserRule.CreateRULE_VALUE2(reduction);
                break;
                case RuleConstants.RULE_VALUE_TRUE :
                ////<Value> ::= true
                result = _parserRule.CreateRULE_VALUE_TRUE(reduction);
                break;
                case RuleConstants.RULE_VALUE_FALSE :
                ////<Value> ::= false
                result = _parserRule.CreateRULE_VALUE_FALSE(reduction);
                break;
                case RuleConstants.RULE_VALUE3 :
                ////<Value> ::= <Date>
                result = _parserRule.CreateRULE_VALUE3(reduction);
                break;
                case RuleConstants.RULE_DATE_DATETIME_DOT_NOW :
                ////<Date> ::= DateTime '.' now
                result = _parserRule.CreateRULE_DATE_DATETIME_DOT_NOW(reduction);
                break;
                case RuleConstants.RULE_DATE_DATETIME_LPARAN_STRINGLITERAL_RPARAN :
                ////<Date> ::= DateTime '(' StringLiteral ')'
                result = _parserRule.CreateRULE_DATE_DATETIME_LPARAN_STRINGLITERAL_RPARAN(reduction);
                break;
                case RuleConstants.RULE_STRLITERAL_STRINGLITERAL :
                ////<StrLiteral> ::= StringLiteral
                result = _parserRule.CreateRULE_STRLITERAL_STRINGLITERAL(reduction);
                break;
                case RuleConstants.RULE_STRLITERAL_NULL :
                ////<StrLiteral> ::= NULL
                result = _parserRule.CreateRULE_STRLITERAL_NULL(reduction);
                break;
                case RuleConstants.RULE_STRLITERAL_QUESTION :
                ////<StrLiteral> ::= '?'
                result = _parserRule.CreateRULE_STRLITERAL_QUESTION(reduction);
                break;
                case RuleConstants.RULE_NUMLITERAL_INTEGERLITERAL :
                ////<NumLiteral> ::= IntegerLiteral
                result = _parserRule.CreateRULE_NUMLITERAL_INTEGERLITERAL(reduction);
                break;
                case RuleConstants.RULE_NUMLITERAL_REALLITERAL :
                ////<NumLiteral> ::= RealLiteral
                result = _parserRule.CreateRULE_NUMLITERAL_REALLITERAL(reduction);
                break;
                case RuleConstants.RULE_OBJECTTYPE_TIMES :
                ////<ObjectType> ::= '*'
                result = _parserRule.CreateRULE_OBJECTTYPE_TIMES(reduction);
                break;
                case RuleConstants.RULE_OBJECTTYPE_DOLLARTEXTDOLLAR :
                ////<ObjectType> ::= '$Text$'
                result = _parserRule.CreateRULE_OBJECTTYPE_DOLLARTEXTDOLLAR(reduction);
                break;
                case RuleConstants.RULE_OBJECTTYPE :
                ////<ObjectType> ::= <Property>
                result = _parserRule.CreateRULE_OBJECTTYPE(reduction);
                break;
                case RuleConstants.RULE_OBJECTATTRIBUTE_IDENTIFIER :
                ////<ObjectAttribute> ::= Identifier
                result = _parserRule.CreateRULE_OBJECTATTRIBUTE_IDENTIFIER(reduction);
                break;
                case RuleConstants.RULE_DELETEPARAMS_DOLLARTEXTDOLLAR :
                ////<DeleteParams> ::= '$Text$'
                result = _parserRule.CreateRULE_DELETEPARAMS_DOLLARTEXTDOLLAR(reduction);
                break;
                case RuleConstants.RULE_DELETEPARAMS :
                ////<DeleteParams> ::= <Property>
                result = _parserRule.CreateRULE_DELETEPARAMS(reduction);
                break;
                case RuleConstants.RULE_PROPERTY_DOT_IDENTIFIER :
                ////<Property> ::= <Property> '.' Identifier
                result = _parserRule.CreateRULE_PROPERTY_DOT_IDENTIFIER(reduction);
                break;
                case RuleConstants.RULE_PROPERTY_IDENTIFIER :
                ////<Property> ::= Identifier
                result = _parserRule.CreateRULE_PROPERTY_IDENTIFIER(reduction);
                break;
                case RuleConstants.RULE_TYPEPLUSATTRIBUTE_DOT :
                ////<TypePlusAttribute> ::= <Property> '.' <ObjectAttribute>
                result = _parserRule.CreateRULE_TYPEPLUSATTRIBUTE_DOT(reduction);
                break;
                case RuleConstants.RULE_AGGREGATEFUNCTION :
                ////<AggregateFunction> ::= <SumFunction>
                result = _parserRule.CreateRULE_AGGREGATEFUNCTION(reduction);
                break;
                case RuleConstants.RULE_AGGREGATEFUNCTION2 :
                ////<AggregateFunction> ::= <CountFunction>
                result = _parserRule.CreateRULE_AGGREGATEFUNCTION2(reduction);
                break;
                case RuleConstants.RULE_AGGREGATEFUNCTION3 :
                ////<AggregateFunction> ::= <MinFunction>
                result = _parserRule.CreateRULE_AGGREGATEFUNCTION3(reduction);
                break;
                case RuleConstants.RULE_AGGREGATEFUNCTION4 :
                ////<AggregateFunction> ::= <MaxFunction>
                result = _parserRule.CreateRULE_AGGREGATEFUNCTION4(reduction);
                break;
                case RuleConstants.RULE_AGGREGATEFUNCTION5 :
                ////<AggregateFunction> ::= <AverageFunction>
                result = _parserRule.CreateRULE_AGGREGATEFUNCTION5(reduction);
                break;
                case RuleConstants.RULE_SUMFUNCTION_SUMLPARAN_RPARAN :
                ////<SumFunction> ::= 'SUM(' <TypePlusAttribute> ')'
                result = _parserRule.CreateRULE_SUMFUNCTION_SUMLPARAN_RPARAN(reduction);
                break;
                case RuleConstants.RULE_COUNTFUNCTION_COUNTLPARAN_RPARAN :
                ////<CountFunction> ::= 'COUNT(' <Property> ')'
                result = _parserRule.CreateRULE_COUNTFUNCTION_COUNTLPARAN_RPARAN(reduction);
                break;
                case RuleConstants.RULE_MINFUNCTION_MINLPARAN_RPARAN :
                ////<MinFunction> ::= 'MIN(' <TypePlusAttribute> ')'
                result = _parserRule.CreateRULE_MINFUNCTION_MINLPARAN_RPARAN(reduction);
                break;
                case RuleConstants.RULE_MAXFUNCTION_MAXLPARAN_RPARAN :
                ////<MaxFunction> ::= 'MAX(' <TypePlusAttribute> ')'
                result = _parserRule.CreateRULE_MAXFUNCTION_MAXLPARAN_RPARAN(reduction);
                break;
                case RuleConstants.RULE_AVERAGEFUNCTION_AVGLPARAN_RPARAN :
                ////<AverageFunction> ::= 'AVG(' <TypePlusAttribute> ')'
                result = _parserRule.CreateRULE_AVERAGEFUNCTION_AVGLPARAN_RPARAN(reduction);
                break;
                case RuleConstants.RULE_OBJECTATTRIBUTE_KEYWORD_DOT_IDENTIFIER :
                ////<ObjectAttribute> ::= Keyword '.' Identifier
                result = _parserRule.CreateRULE_OBJECTATTRIBUTE_KEYWORD_DOT_IDENTIFIER(reduction);
                break;
                case RuleConstants.RULE_OBJECTVALUE_KEYWORD_DOT_IDENTIFIER :
                ////<ObjectValue> ::= Keyword '.' Identifier
                result = _parserRule.CreateRULE_OBJECTVALUE_KEYWORD_DOT_IDENTIFIER(reduction);
                break;
                case RuleConstants.RULE_INLIST_LPARAN_RPARAN :
                ////<InList> ::= '(' <ListType> ')'
                result = _parserRule.CreateRULE_INLIST_LPARAN_RPARAN(reduction);
                break;
                case RuleConstants.RULE_LISTTYPE :
                ////<ListType> ::= <NumLiteralList>
                result = _parserRule.CreateRULE_LISTTYPE(reduction);
                break;
                case RuleConstants.RULE_LISTTYPE2 :
                ////<ListType> ::= <StrLiteralList>
                result = _parserRule.CreateRULE_LISTTYPE2(reduction);
                break;
                case RuleConstants.RULE_LISTTYPE3 :
                ////<ListType> ::= <DateList>
                result = _parserRule.CreateRULE_LISTTYPE3(reduction);
                break;
                case RuleConstants.RULE_NUMLITERALLIST_COMMA :
                ////<NumLiteralList> ::= <NumLiteral> ',' <NumLiteralList>
                result = _parserRule.CreateRULE_NUMLITERALLIST_COMMA(reduction);
                break;
                case RuleConstants.RULE_NUMLITERALLIST :
                ////<NumLiteralList> ::= <NumLiteral>
                result = _parserRule.CreateRULE_NUMLITERALLIST(reduction);
                break;
                case RuleConstants.RULE_STRLITERALLIST_COMMA :
                ////<StrLiteralList> ::= <StrLiteral> ',' <StrLiteralList>
                result = _parserRule.CreateRULE_STRLITERALLIST_COMMA(reduction);
                break;
                case RuleConstants.RULE_STRLITERALLIST :
                ////<StrLiteralList> ::= <StrLiteral>
                result = _parserRule.CreateRULE_STRLITERALLIST(reduction);
                break;
                case RuleConstants.RULE_DATELIST_COMMA :
                ////<DateList> ::= <Date> ',' <DateList>
                result = _parserRule.CreateRULE_DATELIST_COMMA(reduction);
                break;
                case RuleConstants.RULE_DATELIST :
                ////<DateList> ::= <Date>
                result = _parserRule.CreateRULE_DATELIST(reduction);
                break;
           
            }
			if (result == null)
				result = reduction;
			return result;
        }



    }
}
