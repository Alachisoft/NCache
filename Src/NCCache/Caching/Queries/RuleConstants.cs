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
namespace Alachisoft.NCache.Caching.Queries
{
    enum RuleConstants : int
    {
        RULE_QUERY_SELECT                              = 0  , // <Query> ::= SELECT <ObjectType>
        RULE_QUERY_SELECT_WHERE                        = 1  , // <Query> ::= SELECT <ObjectType> WHERE <Expression>
        RULE_QUERY_SELECT2                             = 4  , // <Query> ::= SELECT <AggregateFunction>
        RULE_QUERY_SELECT_WHERE2                       = 5  , // <Query> ::= SELECT <AggregateFunction> WHERE <Expression>
        RULE_EXPRESSION                                = 6  , // <Expression> ::= <OrExpr>
        RULE_OREXPR_OR                                 = 7  , // <OrExpr> ::= <OrExpr> OR <AndExpr>
        RULE_OREXPR                                    = 8  , // <OrExpr> ::= <AndExpr>
        RULE_ANDEXPR_AND                               = 9  , // <AndExpr> ::= <AndExpr> AND <UnaryExpr>
        RULE_ANDEXPR                                   = 10 , // <AndExpr> ::= <UnaryExpr>
        RULE_UNARYEXPR_NOT                             = 11 , // <UnaryExpr> ::= NOT <CompareExpr>
        RULE_UNARYEXPR                                 = 12 , // <UnaryExpr> ::= <CompareExpr>
        RULE_COMPAREEXPR_EQ                            = 13 , // <CompareExpr> ::= <Atrrib> '=' <Value>
        RULE_COMPAREEXPR_EXCLAMEQ                      = 14 , // <CompareExpr> ::= <Atrrib> '!=' <Value>
        RULE_COMPAREEXPR_EQEQ                          = 15 , // <CompareExpr> ::= <Atrrib> '==' <Value>
        RULE_COMPAREEXPR_LTGT                          = 16 , // <CompareExpr> ::= <Atrrib> '<>' <Value>
        RULE_COMPAREEXPR_LT                            = 17 , // <CompareExpr> ::= <Atrrib> '<' <Value>
        RULE_COMPAREEXPR_GT                            = 18 , // <CompareExpr> ::= <Atrrib> '>' <Value>
        RULE_COMPAREEXPR_LTEQ                          = 19 , // <CompareExpr> ::= <Atrrib> '<=' <Value>
        RULE_COMPAREEXPR_GTEQ                          = 20 , // <CompareExpr> ::= <Atrrib> '>=' <Value>
        RULE_COMPAREEXPR_LIKE_STRINGLITERAL            = 21 , // <CompareExpr> ::= <Atrrib> LIKE StringLiteral
        RULE_COMPAREEXPR_LIKE_QUESTION                 = 22 , // <CompareExpr> ::= <Atrrib> LIKE '?'
        RULE_COMPAREEXPR_NOT_LIKE_STRINGLITERAL        = 23 , // <CompareExpr> ::= <Atrrib> NOT LIKE StringLiteral
        RULE_COMPAREEXPR_NOT_LIKE_QUESTION             = 24 , // <CompareExpr> ::= <Atrrib> NOT LIKE '?'
        RULE_COMPAREEXPR_IN                            = 25 , // <CompareExpr> ::= <Atrrib> IN <InList>
        RULE_COMPAREEXPR_NOT_IN                        = 26 , // <CompareExpr> ::= <Atrrib> NOT IN <InList>
        RULE_COMPAREEXPR_IS_NULL                       = 27 , // <CompareExpr> ::= <Atrrib> IS NULL
        RULE_COMPAREEXPR_IS_NOT_NULL                   = 28 , // <CompareExpr> ::= <Atrrib> IS NOT NULL
        RULE_COMPAREEXPR_LPARAN_RPARAN                 = 29 , // <CompareExpr> ::= '(' <Expression> ')'
        RULE_ATRRIB                                    = 30 , // <Atrrib> ::= <ObjectValue>
        RULE_VALUE_MINUS                               = 31 , // <Value> ::= '-' <NumLiteral>
        RULE_VALUE                                     = 32 , // <Value> ::= <NumLiteral>
        RULE_VALUE2                                    = 33 , // <Value> ::= <StrLiteral>
        RULE_VALUE_TRUE                                = 34 , // <Value> ::= true
        RULE_VALUE_FALSE                               = 35 , // <Value> ::= false
        RULE_VALUE3                                    = 36 , // <Value> ::= <Date>
        RULE_DATE_DATETIME_DOT_NOW                     = 37 , // <Date> ::= DateTime '.' now
        RULE_DATE_DATETIME_LPARAN_STRINGLITERAL_RPARAN = 38 , // <Date> ::= DateTime '(' StringLiteral ')'
        RULE_STRLITERAL_STRINGLITERAL                  = 39 , // <StrLiteral> ::= StringLiteral
        RULE_STRLITERAL_NULL                           = 40 , // <StrLiteral> ::= NULL
        RULE_STRLITERAL_QUESTION                       = 41 , // <StrLiteral> ::= '?'
        RULE_NUMLITERAL_INTEGERLITERAL                 = 42 , // <NumLiteral> ::= IntegerLiteral
        RULE_NUMLITERAL_REALLITERAL                    = 43 , // <NumLiteral> ::= RealLiteral
        RULE_OBJECTTYPE_TIMES                          = 44 , // <ObjectType> ::= '*'
        RULE_OBJECTTYPE_DOLLARTEXTDOLLAR               = 45 , // <ObjectType> ::= '$Text$'
        RULE_OBJECTTYPE                                = 46 , // <ObjectType> ::= <Property>
        RULE_OBJECTATTRIBUTE_IDENTIFIER                = 47 , // <ObjectAttribute> ::= Identifier
        RULE_DELETEPARAMS_DOLLARTEXTDOLLAR             = 48 , // <DeleteParams> ::= '$Text$'
        RULE_DELETEPARAMS                              = 49 , // <DeleteParams> ::= <Property>
        RULE_PROPERTY_DOT_IDENTIFIER                   = 50 , // <Property> ::= <Property> '.' Identifier
        RULE_PROPERTY_IDENTIFIER                       = 51 , // <Property> ::= Identifier
        RULE_TYPEPLUSATTRIBUTE_DOT                     = 52 , // <TypePlusAttribute> ::= <Property> '.' <ObjectAttribute>
        RULE_AGGREGATEFUNCTION                         = 53 , // <AggregateFunction> ::= <SumFunction>
        RULE_AGGREGATEFUNCTION2                        = 54 , // <AggregateFunction> ::= <CountFunction>
        RULE_AGGREGATEFUNCTION3                        = 55 , // <AggregateFunction> ::= <MinFunction>
        RULE_AGGREGATEFUNCTION4                        = 56 , // <AggregateFunction> ::= <MaxFunction>
        RULE_AGGREGATEFUNCTION5                        = 57 , // <AggregateFunction> ::= <AverageFunction>
        RULE_SUMFUNCTION_SUMLPARAN_RPARAN              = 58 , // <SumFunction> ::= 'SUM(' <TypePlusAttribute> ')'
        RULE_COUNTFUNCTION_COUNTLPARAN_RPARAN          = 59 , // <CountFunction> ::= 'COUNT(' <Property> ')'
        RULE_MINFUNCTION_MINLPARAN_RPARAN              = 60 , // <MinFunction> ::= 'MIN(' <TypePlusAttribute> ')'
        RULE_MAXFUNCTION_MAXLPARAN_RPARAN              = 61 , // <MaxFunction> ::= 'MAX(' <TypePlusAttribute> ')'
        RULE_AVERAGEFUNCTION_AVGLPARAN_RPARAN          = 62 , // <AverageFunction> ::= 'AVG(' <TypePlusAttribute> ')'
        RULE_OBJECTATTRIBUTE_KEYWORD_DOT_IDENTIFIER    = 63 , // <ObjectAttribute> ::= Keyword '.' Identifier
        RULE_OBJECTVALUE_KEYWORD_DOT_IDENTIFIER        = 64 , // <ObjectValue> ::= Keyword '.' Identifier
        RULE_INLIST_LPARAN_RPARAN                      = 65 , // <InList> ::= '(' <ListType> ')'
        RULE_LISTTYPE                                  = 66 , // <ListType> ::= <NumLiteralList>
        RULE_LISTTYPE2                                 = 67 , // <ListType> ::= <StrLiteralList>
        RULE_LISTTYPE3                                 = 68 , // <ListType> ::= <DateList>
        RULE_NUMLITERALLIST_COMMA                      = 69 , // <NumLiteralList> ::= <NumLiteral> ',' <NumLiteralList>
        RULE_NUMLITERALLIST                            = 70 , // <NumLiteralList> ::= <NumLiteral>
        RULE_STRLITERALLIST_COMMA                      = 71 , // <StrLiteralList> ::= <StrLiteral> ',' <StrLiteralList>
        RULE_STRLITERALLIST                            = 72 , // <StrLiteralList> ::= <StrLiteral>
        RULE_DATELIST_COMMA                            = 73 , // <DateList> ::= <Date> ',' <DateList>
        RULE_DATELIST                                  = 74   // <DateList> ::= <Date>
    };
}
