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

namespace Alachisoft.NCache.Caching.Queries
{
    enum RuleConstants : int
    {
        RULE_QUERY                                     = 0  , // <Query> ::= <SelectQuery>
        RULE_QUERY_ORDERBY                             = 1  , // <Query> ::= <SelectQuery> 'ORDER BY' <OrderByValueList>
        RULE_QUERY_DELETE                              = 2  , // <Query> ::= DELETE <DeleteParams>
        RULE_QUERY_DELETE_WHERE                        = 3  , // <Query> ::= DELETE <DeleteParams> WHERE <Expression>
        RULE_QUERY_SELECT                              = 4  , // <Query> ::= SELECT <AggregateFunction>
        RULE_QUERY_SELECT_WHERE                        = 5  , // <Query> ::= SELECT <AggregateFunction> WHERE <Expression>
        RULE_SELECTQUERY_SELECT                        = 6  , // <SelectQuery> ::= SELECT <ObjectType>
        RULE_SELECTQUERY_SELECT_WHERE                  = 7  , // <SelectQuery> ::= SELECT <ObjectType> WHERE <Expression>
        RULE_SELECTQUERY_SELECT_GROUPBY                = 8  , // <SelectQuery> ::= SELECT <AggregateFunction> 'GROUP BY' <ObjectAttributeList>
        RULE_SELECTQUERY_SELECT_WHERE_GROUPBY          = 9  , // <SelectQuery> ::= SELECT <AggregateFunction> WHERE <Expression> 'GROUP BY' <ObjectAttributeList>
        RULE_SELECTQUERY_SELECT_GROUPBY2               = 10 , // <SelectQuery> ::= SELECT <GroupByValueList> 'GROUP BY' <ObjectAttributeList>
        RULE_SELECTQUERY_SELECT_WHERE_GROUPBY2         = 11 , // <SelectQuery> ::= SELECT <GroupByValueList> WHERE <Expression> 'GROUP BY' <ObjectAttributeList>
        RULE_EXPRESSION                                = 12 , // <Expression> ::= <OrExpr>
        RULE_OREXPR_OR                                 = 13 , // <OrExpr> ::= <OrExpr> OR <AndExpr>
        RULE_OREXPR                                    = 14 , // <OrExpr> ::= <AndExpr>
        RULE_ANDEXPR_AND                               = 15 , // <AndExpr> ::= <AndExpr> AND <UnaryExpr>
        RULE_ANDEXPR                                   = 16 , // <AndExpr> ::= <UnaryExpr>
        RULE_UNARYEXPR_NOT                             = 17 , // <UnaryExpr> ::= NOT <CompareExpr>
        RULE_UNARYEXPR                                 = 18 , // <UnaryExpr> ::= <CompareExpr>
        RULE_COMPAREEXPR_EQ                            = 19 , // <CompareExpr> ::= <Atrrib> '=' <Value>
        RULE_COMPAREEXPR_EXCLAMEQ                      = 20 , // <CompareExpr> ::= <Atrrib> '!=' <Value>
        RULE_COMPAREEXPR_EQEQ                          = 21 , // <CompareExpr> ::= <Atrrib> '==' <Value>
        RULE_COMPAREEXPR_LTGT                          = 22 , // <CompareExpr> ::= <Atrrib> '<>' <Value>
        RULE_COMPAREEXPR_LT                            = 23 , // <CompareExpr> ::= <Atrrib> '<' <Value>
        RULE_COMPAREEXPR_GT                            = 24 , // <CompareExpr> ::= <Atrrib> '>' <Value>
        RULE_COMPAREEXPR_LTEQ                          = 25 , // <CompareExpr> ::= <Atrrib> '<=' <Value>
        RULE_COMPAREEXPR_GTEQ                          = 26 , // <CompareExpr> ::= <Atrrib> '>=' <Value>
        RULE_COMPAREEXPR_LIKE_STRINGLITERAL            = 27 , // <CompareExpr> ::= <Atrrib> LIKE StringLiteral
        RULE_COMPAREEXPR_LIKE_QUESTION                 = 28 , // <CompareExpr> ::= <Atrrib> LIKE '?'
        RULE_COMPAREEXPR_NOT_LIKE_STRINGLITERAL        = 29 , // <CompareExpr> ::= <Atrrib> NOT LIKE StringLiteral
        RULE_COMPAREEXPR_NOT_LIKE_QUESTION             = 30 , // <CompareExpr> ::= <Atrrib> NOT LIKE '?'
        RULE_COMPAREEXPR_IN                            = 31 , // <CompareExpr> ::= <Atrrib> IN <InList>
        RULE_COMPAREEXPR_NOT_IN                        = 32 , // <CompareExpr> ::= <Atrrib> NOT IN <InList>
        RULE_COMPAREEXPR_IS_NULL                       = 33 , // <CompareExpr> ::= <Atrrib> IS NULL
        RULE_COMPAREEXPR_IS_NOT_NULL                   = 34 , // <CompareExpr> ::= <Atrrib> IS NOT NULL
        RULE_COMPAREEXPR_LPARAN_RPARAN                 = 35 , // <CompareExpr> ::= '(' <Expression> ')'
        RULE_ATRRIB                                    = 36 , // <Atrrib> ::= <ObjectValue>
        RULE_VALUE_MINUS                               = 37 , // <Value> ::= '-' <NumLiteral>
        RULE_VALUE                                     = 38 , // <Value> ::= <NumLiteral>
        RULE_VALUE2                                    = 39 , // <Value> ::= <StrLiteral>
        RULE_VALUE_TRUE                                = 40 , // <Value> ::= true
        RULE_VALUE_FALSE                               = 41 , // <Value> ::= false
        RULE_VALUE3                                    = 42 , // <Value> ::= <Date>
        RULE_DATE_DATETIME_DOT_NOW                     = 43 , // <Date> ::= DateTime '.' now
        RULE_DATE_DATETIME_LPARAN_STRINGLITERAL_RPARAN = 44 , // <Date> ::= DateTime '(' StringLiteral ')'
        RULE_STRLITERAL_STRINGLITERAL                  = 45 , // <StrLiteral> ::= StringLiteral
        RULE_STRLITERAL_NULL                           = 46 , // <StrLiteral> ::= NULL
        RULE_STRLITERAL_QUESTION                       = 47 , // <StrLiteral> ::= '?'
        RULE_NUMLITERAL_INTEGERLITERAL                 = 48 , // <NumLiteral> ::= IntegerLiteral
        RULE_NUMLITERAL_REALLITERAL                    = 49 , // <NumLiteral> ::= RealLiteral
        RULE_OBJECTTYPE_TIMES                          = 50 , // <ObjectType> ::= '*'
        RULE_OBJECTTYPE_DOLLARTEXTDOLLAR               = 51 , // <ObjectType> ::= '$Text$'
        RULE_OBJECTTYPE                                = 52 , // <ObjectType> ::= <Property>
        RULE_OBJECTATTRIBUTE_IDENTIFIER                = 53 , // <ObjectAttribute> ::= Identifier
        RULE_DELETEPARAMS_DOLLARTEXTDOLLAR             = 54 , // <DeleteParams> ::= '$Text$'
        RULE_DELETEPARAMS                              = 55 , // <DeleteParams> ::= <Property>
        RULE_PROPERTY_DOT_IDENTIFIER                   = 56 , // <Property> ::= <Property> '.' Identifier
        RULE_PROPERTY_IDENTIFIER                       = 57 , // <Property> ::= Identifier
        RULE_TYPEPLUSATTRIBUTE_DOT                     = 58 , // <TypePlusAttribute> ::= <Property> '.' <ObjectAttribute>
        RULE_AGGREGATEFUNCTION                         = 59 , // <AggregateFunction> ::= <SumFunction>
        RULE_AGGREGATEFUNCTION2                        = 60 , // <AggregateFunction> ::= <CountFunction>
        RULE_AGGREGATEFUNCTION3                        = 61 , // <AggregateFunction> ::= <MinFunction>
        RULE_AGGREGATEFUNCTION4                        = 62 , // <AggregateFunction> ::= <MaxFunction>
        RULE_AGGREGATEFUNCTION5                        = 63 , // <AggregateFunction> ::= <AverageFunction>
        RULE_SUMFUNCTION_SUMLPARAN_RPARAN              = 64 , // <SumFunction> ::= 'SUM(' <TypePlusAttribute> ')'
        RULE_COUNTFUNCTION_COUNTLPARAN_RPARAN          = 65 , // <CountFunction> ::= 'COUNT(' <Property> ')'
        RULE_MINFUNCTION_MINLPARAN_RPARAN              = 66 , // <MinFunction> ::= 'MIN(' <TypePlusAttribute> ')'
        RULE_MAXFUNCTION_MAXLPARAN_RPARAN              = 67 , // <MaxFunction> ::= 'MAX(' <TypePlusAttribute> ')'
        RULE_AVERAGEFUNCTION_AVGLPARAN_RPARAN          = 68 , // <AverageFunction> ::= 'AVG(' <TypePlusAttribute> ')'
        RULE_OBJECTATTRIBUTE_KEYWORD_DOT_IDENTIFIER    = 69 , // <ObjectAttribute> ::= Keyword '.' Identifier
        RULE_OBJECTVALUE_KEYWORD_DOT_IDENTIFIER        = 70 , // <ObjectValue> ::= Keyword '.' Identifier
        RULE_OBJECTVALUE_KEYWORD_DOT_TAG               = 71 , // <ObjectValue> ::= Keyword '.' Tag
        RULE_INLIST_LPARAN_RPARAN                      = 72 , // <InList> ::= '(' <ListType> ')'
        RULE_LISTTYPE                                  = 73 , // <ListType> ::= <NumLiteralList>
        RULE_LISTTYPE2                                 = 74 , // <ListType> ::= <StrLiteralList>
        RULE_LISTTYPE3                                 = 75 , // <ListType> ::= <DateList>
        RULE_NUMLITERALLIST_COMMA                      = 76 , // <NumLiteralList> ::= <NumLiteral> ',' <NumLiteralList>
        RULE_NUMLITERALLIST                            = 77 , // <NumLiteralList> ::= <NumLiteral>
        RULE_STRLITERALLIST_COMMA                      = 78 , // <StrLiteralList> ::= <StrLiteral> ',' <StrLiteralList>
        RULE_STRLITERALLIST                            = 79 , // <StrLiteralList> ::= <StrLiteral>
        RULE_DATELIST_COMMA                            = 80 , // <DateList> ::= <Date> ',' <DateList>
        RULE_DATELIST                                  = 81 , // <DateList> ::= <Date>
        RULE_GROUPBYVALUELIST_COMMA                    = 82 , // <GroupByValueList> ::= <ObjectAttribute> ',' <GroupByValueList>
        RULE_GROUPBYVALUELIST                          = 83 , // <GroupByValueList> ::= <AggregateFunctionList>
        RULE_AGGREGATEFUNCTIONLIST_COMMA               = 84 , // <AggregateFunctionList> ::= <AggregateFunction> ',' <AggregateFunctionList>
        RULE_AGGREGATEFUNCTIONLIST                     = 85 , // <AggregateFunctionList> ::= <AggregateFunction>
        RULE_ORDERBYVALUELIST_COMMA                    = 86 , // <OrderByValueList> ::= <OrderArgument> ',' <OrderByValueList>
        RULE_ORDERBYVALUELIST                          = 87 , // <OrderByValueList> ::= <OrderArgument>
        RULE_ORDERARGUMENT                             = 88 , // <OrderArgument> ::= <ObjectAttribute> <Order>
        RULE_ORDERARGUMENT2                            = 89 , // <OrderArgument> ::= <ObjectAttribute>
        RULE_ORDER_ASC                                 = 90 , // <Order> ::= ASC
        RULE_ORDER_DESC                                = 91 , // <Order> ::= DESC
        RULE_OBJECTATTRIBUTELIST_COMMA                 = 92 , // <ObjectAttributeList> ::= <ObjectAttribute> ',' <ObjectAttributeList>
        RULE_OBJECTATTRIBUTELIST                       = 93   // <ObjectAttributeList> ::= <ObjectAttribute>
    };
}